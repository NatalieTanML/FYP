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
        Task<IEnumerable<Hotel>> GetHotelsEcommerce();
        Task<Hotel> GetById(int id);
        Task AddHotel(Hotel inHotel);
        Task UpdateHotel(Hotel inHotel);
        Task DeleteHotel(int id);
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

        public async Task<IEnumerable<Hotel>> GetHotelsEcommerce()
        {
            return await _context.Hotels.Include(hotel => hotel.Addresses).Where(hotel => hotel.IsActive == true).ToListAsync();
        }

        public async Task<Hotel> GetById(int id)
        {
            return await _context.Hotels.Include(hotel => hotel.Addresses).FirstOrDefaultAsync(h => h.HotelId == id);
        }

        public async Task AddHotel(Hotel inHotel)
        {
            try
            {
                // check if existing hotel exists
                if (await _context.Hotels.AnyAsync(h => h.HotelName == inHotel.HotelName))
                    throw new AppException("Hotel name '" + inHotel.HotelName + "' already exists in the database.");
                else
                {
                    await _context.Hotels.AddAsync(inHotel);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new AppException("Unable to add hotel.", ex.Message);
            }
        }

        public async Task UpdateHotel(Hotel inHotel)
        {

            var hotel = await _context.Hotels.FindAsync(inHotel.HotelId);
            if (hotel == null)
                throw new AppException("Hotel not found.");

            try
            {
                // checks if another product with the same name exists already
                if (await _context.Hotels
                    .Where(h => h.HotelId != inHotel.HotelId)
                    .AnyAsync(h => h.HotelName == inHotel.HotelName))
                {
                    throw new AppException("Hotel '" + inHotel.HotelName + "' already exists in the database.");
                }
                hotel.HotelName = inHotel.HotelName;
                hotel.HotelAddress = inHotel.HotelAddress;
                hotel.HotelPostalCode = inHotel.HotelPostalCode;

                _context.Hotels.Update(hotel);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new AppException("Unable to update hotel record.", new { message = ex.Message });
            }
        }

        public async Task DeleteHotel(int id)
        {
            try
            {
                var hotel = await _context.Hotels.FindAsync(id);
                if (hotel != null)
                {
                    _context.Hotels.Remove(hotel);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException.Message.ToUpper().Contains("REFERENCE CONSTRAINT"))
                    throw new AppException("Unable to delete hotel record. The hotel information might have been linked to other records.");
                else
                    throw new AppException("Unable to delete hotel record.");
            }
        }
    }
}
