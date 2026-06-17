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

        



    }
}