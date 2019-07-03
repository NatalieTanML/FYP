using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using FYP.Models;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
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
        List<string> GetPresignedImageURLs(List<string> guids);
        Task<string> UploadImageAsync(IFormFile image, string guid);
        Task<List<ProductImage>> UploadProductImagesAsync(ICollection<IFormFile> imageFiles);
        Task<List<string>> CopyImagesAsync(List<string> imgKeys);
        Task DeleteImagesAsync(List<string> keys);
    }
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _client;
        private const string tempBucket = "mayf-test-temp1";
        private const string permBucket = "mayf-test-perm1";
        private const string productBucket = "mayf-test-prod1";
        private const string thumbBucket = "mayf-test-thumb1";

        public S3Service(IAmazonS3 client)
        {
            _client = client;
        }

        public List<string> GetPresignedImageURLs(List<string> guids)
        {
            List<string> urlStrings = new List<string>();
            try
            {
                foreach (string guid in guids)
                {
                    GetPreSignedUrlRequest request = new GetPreSignedUrlRequest
                    {
                        BucketName = permBucket,
                        Key = guid,
                        Expires = DateTime.Now.AddMinutes(5),
                        // if you want to download directly on link click/open
                        //ResponseHeaderOverrides = new ResponseHeaderOverrides
                        //{
                        //    ContentDisposition = "attachment; filename=" + guid
                        //}
                    };
                    urlStrings.Add(_client.GetPreSignedURL(request));
                }
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            return urlStrings;
        }

        public async Task<string> UploadImageAsync(IFormFile image, string guid)
        {
            if (image.Length > 0)
            {
                MemoryStream outputStream = new MemoryStream();

                using (var memoryStream = new MemoryStream())
                {
                    // convert image file to memoryStream
                    await image.CopyToAsync(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    // compress image
                    using (Image<Rgba32> compressedImage = Image.Load(memoryStream))
                    {
                        compressedImage.Mutate(x => x.BackgroundColor(Rgba32.White));
                        compressedImage.SaveAsJpeg(outputStream, new JpegEncoder { Quality = 75 });
                    }
                    outputStream.Seek(0, SeekOrigin.Begin);
                }

                // prepare the file for upload to s3
                try
                {
                    var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                    {
                        BucketName = tempBucket,
                        InputStream = outputStream,
                        StorageClass = S3StorageClass.Standard,
                        //PartSize = 10485760, // 10mb
                        Key = guid,
                        CannedACL = S3CannedACL.Private
                    };

                    // upload to s3 asynchronously, then dispose the memorystream
                    using (var fileTransferUtility = new TransferUtility(_client))
                    {
                        fileTransferUtility.Upload(fileTransferUtilityRequest);
                    }

                    outputStream.Dispose();
                    return "https://" + thumbBucket + ".s3-ap-southeast-1.amazonaws.com/" + guid;
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
            return null;
        }

        public async Task<List<ProductImage>> UploadProductImagesAsync(ICollection<IFormFile> imageFiles)
        {
            List<ProductImage> images = new List<ProductImage>();

            foreach (var file in imageFiles)
            {
                if (file.Length > 0)
                {
                    MemoryStream outputStream = new MemoryStream();

                    using (var memoryStream = new MemoryStream())
                    {
                        // convert image file to memoryStream
                        await file.CopyToAsync(memoryStream);
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        // compress image
                        using (Image<Rgba32> compressedImage = Image.Load(memoryStream))
                        {
                            compressedImage.Mutate(x => x.BackgroundColor(Rgba32.White));
                            compressedImage.SaveAsJpeg(outputStream, new JpegEncoder { Quality = 75 });
                        }
                        outputStream.Seek(0, SeekOrigin.Begin);
                    }

                    // prepare the file for upload to s3
                    try
                    {
                        var fileTransferUtility = new TransferUtility(_client);

                        string FileName = Guid.NewGuid().ToString("N").ToUpper() + ".jpg";

                        images.Add(new ProductImage
                        {
                            ImageKey = FileName,
                            ImageUrl = "https://" + productBucket + ".s3-ap-southeast-1.amazonaws.com/" + FileName
                        });

                        var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                        {
                            BucketName = productBucket,
                            InputStream = outputStream,
                            StorageClass = S3StorageClass.Standard,
                            //PartSize = 10485760, // 10mb
                            Key = FileName, 
                            CannedACL = S3CannedACL.PublicRead
                        };

                        // upload to s3 asynchronously, then dispose the memorystream
                        await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
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

            return images;
        }

        public async Task<List<string>> CopyImagesAsync(List<string> imgKeys)
        {
            List<string> imgUrls = new List<string>();
            
            try
            {
                foreach (string key in imgKeys)
                {
                    CopyObjectRequest request = new CopyObjectRequest
                    {
                        SourceBucket = tempBucket,
                        SourceKey = key,
                        DestinationBucket = permBucket,
                        DestinationKey = key
                    };
                    await _client.CopyObjectAsync(request);
                    imgUrls.Add("https://" + thumbBucket + ".s3-ap-southeast-1.amazonaws.com/" + key);
                }
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unknown error encountered on server. Message:'{0}' when writing an object", ex.Message);
            }
            return imgUrls;
        }

        public async Task DeleteImagesAsync(List<string> keys)
        {
            List<KeyVersion> delKeys = new List<KeyVersion>();
            foreach (string key in keys)
            {
                delKeys.Add(new KeyVersion { Key = key });
            }

            DeleteObjectsRequest multiObjectDeleteRequest = new DeleteObjectsRequest
            {
                BucketName = productBucket,
                Objects = delKeys // This includes the object keys and null version IDs.
            };
            try
            {
                DeleteObjectsResponse response = await _client.DeleteObjectsAsync(multiObjectDeleteRequest);
                Console.WriteLine("Successfully deleted all the {0} items", response.DeletedObjects.Count);
            }
            catch (DeleteObjectsException e)
            {
                Console.WriteLine("Unable to delete {0} objects out of {1} objects.", e.Response.DeleteErrors.Count, e.Response.DeletedObjects.Count);
            }
        }
    }
}
