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
        Task NotifyAllClients(Order newOrder);
    }

    public class OrderHub : Hub, IOrderHub
    {
        private readonly IHubContext<OrderHub> _hubContext;

        public OrderHub(IHubContext<OrderHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyAllClients(Order newOrder)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveChanges", newOrder);
        }
    }
}
