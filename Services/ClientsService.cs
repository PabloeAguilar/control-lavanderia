using control_lavanderia.Components.Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Services
{
    public class ClientsService(IDbContextFactory<LavanderiaDbContext> context)
    {
        public async Task<List<Client>> SearchClientsByName(string? searchTerm, int take = 10)
        {
            await using var dbContext = await context.CreateDbContextAsync();
            var normalizedSearchTerm = searchTerm?.Trim();
            var clientsQuery = dbContext.Clients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
            {
                clientsQuery = clientsQuery.Where(client => EF.Functions.ILike(client.Name, $"%{normalizedSearchTerm}%"));
            }

            return await clientsQuery
                .OrderBy(client => client.Name)
                .Take(take)
                .ToListAsync();
        }

        public async Task<Client?> GetClientByExactName(string? clientName)
        {
            if (string.IsNullOrWhiteSpace(clientName))
            {
                return null;
            }

            await using var dbContext = await context.CreateDbContextAsync();
            var normalizedClientName = clientName.Trim();

            return await dbContext.Clients
                .OrderBy(client => client.Id)
                .FirstOrDefaultAsync(client => EF.Functions.ILike(client.Name, normalizedClientName));
        }
    }
}