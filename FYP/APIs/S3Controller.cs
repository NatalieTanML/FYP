using FYP.Helpers;
using FYP.Models;
using FYP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.APIs
{
    [Authorize]
    [Route("api/[controller]")]
    public class S3Controller : Controller
    {
        private IS3Service _s3Service;
        private readonly AppSettings _appSettings;

        public S3Controller(IS3Service s3Service, IOptions<AppSettings> appSettings)
        {
            _s3Service = s3Service;
            _appSettings = appSettings.Value;
        }

        // this method is called when a user adds a customized product to the cart
        [HttpPost("temp")]
        [AllowAnonymous]
        public async Task<IActionResult> UploadImage(IFormFile file, string guidString)
        {
            try
            {
                string imgUrl = await _s3Service.UploadImageAsync(file, guidString);
                return Ok(new {
                    message = "Upload successful",
                    imageUrl = imgUrl
                });
            }
            catch (Exception ex)
            {
                // return error message 
                return BadRequest(new { message = ex.Message });
            }
        }

        // this method is called when a user successfully completes payment for an order
        [HttpPost("perm")]
        [AllowAnonymous]
        public async Task<IActionResult> CopyToPerm(List<string> imgKeys)
        {
            try
            {
                List<string> imgUrls = await _s3Service.CopyImagesAsync(imgKeys);
                return Ok(new
                {
                    message = "Images saved successfully",
                    imageUrls = imgUrls
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // this method is called when the admin adds a new product
        // it takes in a collection of blob files (image files) and
        // returns a list of ProductImage objects, that will be added
        // to the next json call to CreateProduct in ProductsController.
        // the list returned contains the image key + url for each image
        [HttpPost("product")]
        [AllowAnonymous]
        public async Task<IActionResult> UploadProductImages(ICollection<IFormFile> imageFiles)
        {
            try
            {
                List<ProductImage> outImages = await _s3Service.UploadProductImagesAsync(imageFiles);
                return Ok(new
                {
                    message = "Uploaded product images",
                    productImages = outImages
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // returns a presigned URL for access to private bucket objects
        [HttpPost("url")]
        [AllowAnonymous]
        public IActionResult GetPresignedImageURLs([FromBody] List<string> guids)
        {
            try
            {
                List<string> urls = _s3Service.GetPresignedImageURLs(guids);
                return Ok(new
                {
                    message = "Image GET successful",
                    imgUrls = urls
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // remove images from s3 using the keys
        [HttpPost("delete")]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteImagesFromS3([FromBody] List<string> guids)
        {
            try
            {
                await _s3Service.DeleteCustomerImagesAsync(guids);
                return Ok(new
                {
                    message = "Deleted images successfully!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
