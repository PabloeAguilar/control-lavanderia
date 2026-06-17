using control_lavanderia.Components.Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Services
{
    public class OrderDetailsService(IDbContextFactory<LavanderiaDbContext> context)
    {
        public async Task<OrderDetail?> GetById(int orderDetailId)
        {
            await using var _context = await context.CreateDbContextAsync();
            return await _context.OrderDetails.FirstOrDefaultAsync(od => od.Id == orderDetailId);
        }

        public async Task<OrderDetail> UpdateDetail(int orderDetailId, decimal quantity, string? description)
        {
            await using var _context = await context.CreateDbContextAsync();
            var orderDetail = await _context.OrderDetails.FirstOrDefaultAsync(od => od.Id == orderDetailId);

            if (orderDetail is null)
                throw new Exception("Order detail not found");

            if (quantity < 0)
                throw new Exception("Quantity cannot be negative");

            orderDetail.Quantity = quantity;
            orderDetail.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            orderDetail.Total = (int)Math.Floor(orderDetail.Price * orderDetail.Quantity);
            orderDetail.UpdateDate = DateTimeOffset.UtcNow;

            _context.OrderDetails.Update(orderDetail);
            await _context.SaveChangesAsync();

            return orderDetail;
        }
    }
}
