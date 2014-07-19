using WebAtScale.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System.Web.Configuration;
using Microsoft.WindowsAzure.Storage.Auth;

namespace WebAtScale.Controllers
{
    public class HomeController : Controller
    {
        private readonly DbConnectionContext db = new DbConnectionContext();
        CloudStorageAccount account = new CloudStorageAccount(new StorageCredentials(WebConfigurationManager.AppSettings["StorageAccountName"], WebConfigurationManager.AppSettings["StorageAccount"]), true);
        
        public ActionResult Index()
        {

            var imagesModel = new ImageGallery();

            if (WebConfigurationManager.AppSettings["BlobStore"] == "local")
            {
                var imageFiles = Directory.GetFiles(Server.MapPath("~/Upload_Files/"));
                foreach (var item in imageFiles)
                {
                    imagesModel.ImageList.Add("~/Upload_Files/" + Path.GetFileName(item));
                }
            
            }
            else 
            {
               
                CloudBlobClient client = account.CreateCloudBlobClient();
                CloudBlobContainer container = client.GetContainerReference("webatscale");
                container.CreateIfNotExists();
                container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

                var blobs = container.ListBlobs();
                foreach (var item in blobs)
                 {
                     imagesModel.ImageList.Add(item.Uri.OriginalString);
                 }
            }
            return View(imagesModel);
        }
        [HttpGet]
        public ActionResult UploadImage()
        {
            return View();
        }
        [HttpPost]
        public ActionResult UploadImageMethod()
        {
            if (Request.Files.Count != 0)
            {
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    HttpPostedFileBase file = Request.Files[i];
                    int fileSize = file.ContentLength;
                    string fileName = Request.Files.AllKeys[i];
                    if (WebConfigurationManager.AppSettings["BlobStore"] == "local")
                    {
                        file.SaveAs(Server.MapPath("~/Upload_Files/" + fileName));
                    
                    }
                    else
                    {
                        CloudBlobClient client = account.CreateCloudBlobClient();
                        CloudBlobContainer container = client.GetContainerReference("webatscale");
                        var contentType = "application/octet-stream";
                        switch (Path.GetExtension(fileName))
                        {
                            case "png": contentType = "image/png"; break;
                            case "jpg": contentType = "image/png"; break;
                        }

                        var blob = container.GetBlockBlobReference(fileName);
                        blob.Properties.ContentType = contentType;
                        blob.Properties.CacheControl = "public, max-age=3600";
                        blob.UploadFromStream(file.InputStream);
                        blob.SetProperties();


                    }
                    //ImageGallery imageGallery = new ImageGallery();
                    //imageGallery.ID = Guid.NewGuid();
                    //imageGallery.Name = fileName;
                    //imageGallery.Url = "~/Upload_Files/" + fileName;
                    //db.ImageGallery.Add(imageGallery);
                    //db.SaveChanges();
                }
                return Content("Success");
            }
            return Content("failed");
        }
    }
}
