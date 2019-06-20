using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace FYP.Services
{
    public interface IS3Service
    {
        Task UploadImagesAsync(ICollection<IFormFile> images);
    }
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _client;
        private const string bucketName = "20190507test1";
        private byte[] imageBytes;

        public S3Service(IAmazonS3 client)
        {
            _client = client;
        }

        public async Task UploadImagesAsync(ICollection<IFormFile> images) // TODO: change to blob once front end is done
        {
            MemoryStream outputStream = null;
            List<MemoryStream> compressedImages = new List<MemoryStream>();
            foreach (var image in images)
            {
                if (image.Length > 0)
                {
                    // convert each image file to byte[]
                    using (var memoryStream = new MemoryStream())
                    {
                        await image.CopyToAsync(memoryStream);
                        imageBytes = memoryStream.ToArray();
                    }

                    // compress the image and convert to a memorystream for upload 
                    outputStream = new MemoryStream();

                    using (Image<Rgba32> compressedImage = Image.Load(imageBytes))
                    {
                        compressedImage.SaveAsJpeg(outputStream, new JpegEncoder { Quality = 75 });
                    }
                    outputStream.Seek(0, SeekOrigin.Begin);

                    // save to the list of images to upload
                    compressedImages.Add(outputStream);

                }
            }

            // upload to s3
            try
            {
                var fileTransferUtility = new TransferUtility(_client);

                foreach (MemoryStream img in compressedImages)
                {
                    int i = 6;
                    string FileName = "image" + i + ".jpg";
                    var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                    {
                        BucketName = bucketName,
                        InputStream = img,
                        StorageClass = S3StorageClass.Standard,
                        PartSize = 6291456, // 6mb
                        Key = FileName, // TODO: update to use unique name (probably order number + orderitem id or similar)
                        CannedACL = S3CannedACL.PublicRead
                    };
                    // upload to s3 asynchronously
                    await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
                    i++;
                }
                outputStream.Dispose();
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unknown error encountered on server. Message:'{0}' when writing an object", ex.Message);
            }

        }
    }
}
