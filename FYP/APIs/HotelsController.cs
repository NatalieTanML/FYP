using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FYP.Helpers;
using FYP.Models;
using FYP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FYP.APIs
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class HotelsController : ControllerBase
    {

        private IHotelService _hotelService;
        //private IUserService _userService;
        private readonly AppSettings _appSettings;

        public HotelsController(IHotelService hotelService, IOptions<AppSettings> appSettings)
        {
            _hotelService = hotelService;
            _appSettings = appSettings.Value;
        }


        // GET: api/Details
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetHotels()
        {
            //calls hotelService
            var hotels = await _hotelService.GetHotels();
            //creates a list of objects(of any type) to store hotel info 
            List<object> hotelList = new List<object>();
            foreach (Hotel hotel in hotels){
                hotelList.Add(new
                {
                    value = hotel.HotelId,
                    text = hotel.HotelName,
                    //hotelAddress = hotel.HotelAddress,
                    //hotelPostalCode = hotel.HotelPostalCode

                });

                
            }
            
                return new JsonResult(hotelList);
        }

        //[AllowAnonymous]
        //[HttpGet("{id}")]
        //public async Task<IActionResult> GetHotel(int id)
        //{
        //    try
        //    {
        //        var hotel = await _hotelService.GetById(id);
        //        return Ok(new
        //        {
        //            hotelId = hotel.HotelId,
        //            hotelName = hotel.HotelName,
        //            hotelAddress = hotel.HotelAddress,
        //            hotelPostalCode = hotel.HotelPostalCode
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }

        //}





    }
}
