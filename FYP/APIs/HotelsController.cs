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
                    hotelAddress = hotel.HotelAddress,
                    hotelPostalCode = hotel.HotelPostalCode
                });
            }
            return new JsonResult(hotelList);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOneHotel(int id)
        {
            try
            {
                var hotel = await _hotelService.GetById(id);
                return Ok(new
                {
                    hotelId = hotel.HotelId,
                    hotelName = hotel.HotelName,
                    hotelAddress = hotel.HotelAddress,
                    hotelPostalCode = hotel.HotelPostalCode
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> AddHotel([FromBody] Hotel newHotel)
        {
            try
            {
                await _hotelService.AddHotel(newHotel);
                return Ok(new
                {
                    message = "Created hotel successfully!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        
        [HttpPut]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateHotel([FromBody] Hotel hotel)
        {
            try
            {
                await _hotelService.UpdateHotel(hotel);
                return Ok(new
                {
                    message = "Updated hotel successfully!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteHotel(int id)
        {
            try
            {
                await _hotelService.DeleteHotel(id);
                return Ok(new
                {
                    message = "Deleted hotel successfully!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
