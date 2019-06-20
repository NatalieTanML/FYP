using FYP.Data;
using FYP.Helpers;
using FYP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Services
{
    public interface IHotelService
    {
        Task<IEnumerable<Hotel>> GetHotels();
        Task<Hotel> GetById(int id);
      

    }
    public class HotelService : IHotelService
    {
        private ApplicationDbContext _context;
        private readonly AppSettings _appSettings;


        public HotelService(ApplicationDbContext context, IOptions<AppSettings> appSettings)
        {
            _context = context;
            _appSettings = appSettings.Value;
        }
        public async Task<IEnumerable<Hotel>> GetHotels()
        {
            return await _context.Hotels.Include(hotel => hotel.Addresses).ToListAsync();
        }
        public async Task<Hotel> GetById(int id)
        {
            // searches product, including join with category
            return await _context.Hotels.Include(hotel => hotel.Addresses).FirstOrDefaultAsync(h => h.HotelId == id);
        }

    }
}
