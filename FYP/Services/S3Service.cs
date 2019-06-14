using Amazon.S3;
using Amazon.S3.Transfer;
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
        Task UploadImageAsync(string url);
    }
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _client;
        private const string bucketName = "20190507test1"; 
        private const string FileName = "image5.jpg";
        private byte[] imageBytes;

        public S3Service(IAmazonS3 client)
        {
            _client = client;
        }

        public async Task UploadImageAsync(string url) // TODO: change to blob once front end is done
        {
            // TODO: update this to receive the bytes from the blob
            using (var webClient = new WebClient())
            {
                imageBytes = await webClient.DownloadDataTaskAsync(url);
            }

            // compress the image and convert to a memorystream for upload 
            var outputStream = new MemoryStream();
            using (Image<Rgba32> image = Image.Load(imageBytes))
            {
                image.SaveAsJpeg(outputStream, new JpegEncoder { Quality = 75 });
            }
            outputStream.Seek(0, SeekOrigin.Begin);

            // upload to s3
            try
            {
                var fileTransferUtility = new TransferUtility(_client);

                var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                {
                    BucketName = bucketName,
                    InputStream = outputStream,
                    StorageClass = S3StorageClass.Standard,
                    PartSize = 6291456, // 6mb
                    Key = FileName, // TODO: update to use unique name (probably order number + orderitem id or similar)
                    CannedACL = S3CannedACL.PublicRead
                };
                // upload to s3 asynchronously
                await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
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
