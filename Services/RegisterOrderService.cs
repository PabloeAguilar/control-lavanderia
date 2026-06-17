using control_lavanderia.Components.Data;
using Microsoft.EntityFrameworkCore;
using DTOs;

namespace Services
{
    public class RegisterOrderService(IDbContextFactory<LavanderiaDbContext> context)
    {
        public async Task RegisterOrder(RegisterOrderRequest orderRequest)
        {
            await using var _context = await context.CreateDbContextAsync();
            // Add the client if it doesn't exist
            var clientName = orderRequest.Client.Name.Trim();
            var existingClient = await _context.Clients
                .FirstOrDefaultAsync(c => EF.Functions.Like(c.Name, clientName));
            if (existingClient == null)
            {
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