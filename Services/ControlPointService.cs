using control_lavanderia.Components.Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Services
{
    public class ControlPointService(IDbContextFactory<LavanderiaDbContext> contextFactory)
    {
        public async Task<List<Location>> GetLocations()
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Locations
                .OrderBy(location => location.Position)
                .ThenBy(location => location.Name)
                .ToListAsync();
        }

        public async Task<List<Order>> GetAvailableOrders()
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Orders
                .Include(order => order.Client)
                .Where(order => !order.DeliveryDate.HasValue && !order.CancelDate.HasValue)
                .OrderByDescending(order => order.RegisterDate)
                .ToListAsync();
        }

        public async Task<List<OrderMovement>> GetMovementsByLocation(int locationId, DateTimeOffset? startDate = null)
        {
            var reference = startDate ?? DateTimeOffset.Now;
            var dayStart = new DateTimeOffset(reference.Year, reference.Month, reference.Day, 0, 0, 0, reference.Offset).ToUniversalTime();
            var dayEnd = dayStart.AddDays(1);
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.OrderMovements
                .Include(movement => movement.Order)
                .ThenInclude(order => order!.Client)
                .Where(movement => movement.LocationId == locationId)
                .Where(movement => !movement.DeletedDate.HasValue)
                .Where(movement => movement.Date >= dayStart && movement.Date < dayEnd)
                .OrderByDescending(movement => movement.Date)
                .ToListAsync();
        }

        public async Task<bool> UpdateMovement(OrderMovement movement, int selectedLocationId)
        {
            if (!movement.Id.HasValue)
            {
                return false;
            }

            await using var context = await contextFactory.CreateDbContextAsync();
            var movementDb = await context.OrderMovements.FirstOrDefaultAsync(x => x.Id == movement.Id.Value);

            if (movementDb is null)
            {
                return false;
            }

            movementDb.OrderId = movement.OrderId;
            movementDb.Quantity = movement.Quantity;
            movementDb.Date = movement.Date.ToUniversalTime();
            movementDb.Comments = string.IsNullOrWhiteSpace(movement.Comments) ? null : movement.Comments.Trim();
            movementDb.LocationId = selectedLocationId;

            await UpdateOrderCurrentLocation(context, movementDb.OrderId, selectedLocationId);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<OrderMovement> CreateMovement(OrderMovement movement, int selectedLocationId)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            movement.LocationId = selectedLocationId;
            movement.Date = movement.Date.ToUniversalTime();
            movement.Comments = string.IsNullOrWhiteSpace(movement.Comments) ? null : movement.Comments.Trim();

            context.OrderMovements.Add(movement);
            await UpdateOrderCurrentLocation(context, movement.OrderId, selectedLocationId);

            await context.SaveChangesAsync();
            return movement;
        }

        public async Task<bool> DeleteMovement(int movementId)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var movementDb = await context.OrderMovements.FirstOrDefaultAsync(x => x.Id == movementId);

            if (movementDb is null)
            {
                return false;
            }
            movementDb.DeletedDate = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
            return true;
        }

        private static async Task UpdateOrderCurrentLocation(LavanderiaDbContext context, int? orderId, int selectedLocationId)
        {
            if (!orderId.HasValue)
            {
                return;
            }

            var orderDb = await context.Orders.FirstOrDefaultAsync(order => order.Id == orderId.Value);
            if (orderDb is null)
            {
                return;
            }

            orderDb.IdCurrentLocation = selectedLocationId;
        }
    }
}
