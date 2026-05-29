using control_lavanderia.Components.Data;
using Models;

namespace Services
{
    public class OrdersService(LavanderiaDbContext context)
    {
        private readonly LavanderiaDbContext _context = context;

        public async Task<List<Order>> GetLastOrders()
        {
            // return await _context.Orders.OrderByDescending(o => o.Id).Take(10).ToListAsync();
            return new List<Order>
            {
                new(1, "Sin comentarios", 1, "FOLIO123"),
                new(2, "Urgente", 2, "FOLIO124"),
                new(3, "Entregar el lunes", 3, "FOLIO125")
            };
        }



    }
}