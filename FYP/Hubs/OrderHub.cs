using FYP.Data;
using FYP.Helpers;
using FYP.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Hubs
{
    public interface IOrderHub
    {
        Task NotifyOneChange(int orderId);
        Task NotifyMultipleChanges(List<int> orderIds);
        Task NotifyLowStock(Option option);
    }

    public class OrderHub : Hub, IOrderHub
    {
        private readonly IHubContext<OrderHub> _hubContext;

        public OrderHub(IHubContext<OrderHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyOneChange(int orderId)
        {
            await _hubContext.Clients.All.SendAsync("OneOrder", orderId);
        }

        public async Task NotifyMultipleChanges(List<int> orderIds)
        {
            await _hubContext.Clients.All.SendAsync("MultipleOrders", orderIds);
        }

        public async Task NotifyLowStock(Option option)
        {
            await _hubContext.Clients.All.SendAsync("LowStock", option);
        }
    }
}
