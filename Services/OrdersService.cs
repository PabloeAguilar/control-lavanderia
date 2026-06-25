using control_lavanderia.Components.Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Services
{
    public class OrdersService(IDbContextFactory<LavanderiaDbContext> context)
    {
        public async Task<List<Order>> GetLastOrders()
        {
            await using var _context = await context.CreateDbContextAsync();
            return await _context.Orders
                .OrderByDescending(order => order.RegisterDate)
                .ThenByDescending(order => order.Id)
                .Take(10)
                .Include(order => order.Client)
                .Include(order => order.OrderDetails)
                .ToListAsync();
        }

        public async Task<List<Order>> GetLastOrdersByClient(int clientId)
        {
            await using var _context = await context.CreateDbContextAsync();
            return await _context.Orders
            .Where(order => order.IdCliente == clientId)
            .OrderByDescending(order => order.RegisterDate)
            .ThenByDescending(order => order.Id)
            .Take(10)
            .Include(order => order.Client)
            .Include(order => order.OrderDetails)
            .ToListAsync();
        }

        public async Task<Order?> GetOrderById(int orderId)
        {
            await using var _context = await context.CreateDbContextAsync();
            return await _context.Orders
                .Include(order => order.Client)
                .Include(order => order.OrderDetails)
                .FirstOrDefaultAsync(order => order.Id == orderId);
        }

        public async Task<Order> DeliverOrder(int orderId)
        {
            await using var _context = await context.CreateDbContextAsync();
            var order = await _context.Orders
                .FirstOrDefaultAsync(order => order.Id == orderId);

            if (order is null)
                throw new Exception("Order not found");

            ValidateOrderCanBeManaged(order);

            order.DeliveryDate = DateTimeOffset.UtcNow;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<Order> CancelOrder(int orderId)
        {
            await using var _context = await context.CreateDbContextAsync();
            var order = await _context.Orders
                .FirstOrDefaultAsync(order => order.Id == orderId);

            if (order is null)
                throw new Exception("Order not found");

            ValidateOrderCanBeManaged(order);

            order.CancelDate = DateTimeOffset.UtcNow;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return order;
        }

        private static void ValidateOrderCanBeManaged(Order order)
        {
            if (order.CancelDate.HasValue)
                throw new Exception("Canceled orders cannot be updated");

            if (order.DeliveryDate.HasValue)
                throw new Exception("Delivered orders cannot be updated");
        }
    }
}