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
        Task NotifyOneChange(Order newOrder);
        Task NotifyMultipleChanges(List<Order> newOrders);
    }

    public class OrderHub : Hub, IOrderHub
    {
        private readonly IHubContext<OrderHub> _hubContext;

        public OrderHub(IHubContext<OrderHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyOneChange(Order newOrder)
        {
            await _hubContext.Clients.All.SendAsync("OneOrder", newOrder);
        }

        public async Task NotifyMultipleChanges(List<Order> newOrders)
        {
            await _hubContext.Clients.All.SendAsync("MultipleOrders", newOrders);
        }
    }
}
