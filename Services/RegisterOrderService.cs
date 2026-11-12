using control_lavanderia.Components.Data;
using Microsoft.EntityFrameworkCore;
using DTOs;
using Models;

namespace Services
{
    public class RegisterOrderService(IDbContextFactory<LavanderiaDbContext> context, DataCacheService cache)
    {
        public async Task<Order> RegisterOrder(RegisterOrderRequest orderRequest)
        {
            await using var _context = await context.CreateDbContextAsync();
            var clientName = orderRequest.Client.Name.Trim();
            Client? existingClient = null;
            var clientCreated = false;

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
                clientCreated = true;
            }
            else
            {
                orderRequest.Order.Client = existingClient;
                orderRequest.Order.IdCliente = existingClient.Id;
            }
            orderRequest.Order.ReceivedDate = orderRequest.ReceivedDate.ToUniversalTime();
            orderRequest.Order.Adelanto = orderRequest.PaymentDetails.Adelanto;
            orderRequest.Order.OrderDetails = orderRequest.OrderDetails;
            // Folio temporal único para no chocar con el índice único antes de
            // conocer el Id autogenerado por la base de datos.
            orderRequest.Order.Folio = Guid.NewGuid().ToString();
            _context.Orders.Add(orderRequest.Order);
            await _context.SaveChangesAsync();

            // El folio final es el Id con ceros a la izquierda hasta 7 dígitos.
            orderRequest.Order.Folio = orderRequest.Order.Id.ToString("D7");
            await _context.SaveChangesAsync();
            

            // Mantener el cache al día: un cliente nuevo invalida la lista de clientes
            // y la orden recién recibida se refleja en las órdenes del día.
            if (clientCreated)
            {
                cache.MarkClientsAsNeedingRefresh();
            }

            await cache.RefreshOrderAsync(orderRequest.Order.Id);
            return orderRequest.Order;
        }
    }
}