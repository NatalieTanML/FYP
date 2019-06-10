using FYP.Data;
using FYP.Helpers;
using FYP.Hubs;
using FYP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<Order>> GetAll();
        Task<Order> GetById(int id);
        //Task<Order> Create(Order order);
        Task UpdateStatus(int userId, Order inOrder);
    }

    public class OrderService : IOrderService
    {
        private ApplicationDbContext _context;
        private readonly IOrderHub _orderHub;
        private readonly AppSettings _appSettings;

        public OrderService(ApplicationDbContext context, 
            IOrderHub orderHub, IOptions<AppSettings> appSettings)
        {
            _context = context;
            _orderHub = orderHub;
            _appSettings = appSettings.Value;
        }

        public async Task<IEnumerable<Order>> GetAll()
        {
            return await _context.Orders
                .Include(order => order.OrderItems)
                .ToListAsync();
        }

        public async Task<Order> GetById(int id)
        {
            return await _context.Orders
                .Include(order => order.DeliveryType)
                .Include(order => order.Status)
                .Include(order => order.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);
        }

        //public async Task<Order> Create(Order order)
        //{
        //    throw new NotImplementedException();
        //}

        public async Task UpdateStatus(int id, Order inOrder)
        {
            var order = await _context.Orders.FindAsync(id);

            // if order does not exist
            if (order == null)
                throw new AppException("Order not found.");

            // update product status
            order.Status = inOrder.Status;
            order.StatusId = inOrder.StatusId;
            order.UpdatedAt = DateTime.Now;
            order.UpdatedById = inOrder.UpdatedById;

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            await _orderHub.NotifyAllClients();
        }
    }
}
