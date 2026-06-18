using control_lavanderia.Components.Data;
using Microsoft.EntityFrameworkCore;
using DTOs;
using Models;

namespace Services
{
    public class RegisterOrderService(IDbContextFactory<LavanderiaDbContext> context)
    {
        public async Task RegisterOrder(RegisterOrderRequest orderRequest)
        {
            await using var _context = await context.CreateDbContextAsync();
            var clientName = orderRequest.Client.Name.Trim();
            Client? existingClient = null;

            if (orderRequest.Client.Id > 0)
            {
                existingClient = await _context.Clients
                    .FirstOrDefaultAsync(c => c.Id == orderRequest.Client.Id);
            }
            else
            {
                existingClient ??= await _context.Clients
                    .OrderBy(c => c.Id)
                    .FirstOrDefaultAsync(c => EF.Functions.ILike(c.Name, clientName));
            }

            if (existingClient == null)
            {
                orderRequest.Client.Name = clientName;
                _context.Clients.Add(orderRequest.Client);
                orderRequest.Order.Client = orderRequest.Client;
            }
            else
            {
                orderRequest.Order.Client = existingClient;
                orderRequest.Order.IdCliente = existingClient.Id;
            }
            orderRequest.Order.ReceivedDate = orderRequest.ReceivedDate.ToUniversalTime();
            orderRequest.Order.Adelanto = orderRequest.PaymentDetails.Adelanto;
            orderRequest.Order.OrderDetails = orderRequest.OrderDetails;
            _context.Orders.Add(orderRequest.Order);
            await _context.SaveChangesAsync();
        }
    }
}