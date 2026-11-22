using control_lavanderia.Components.Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Services
{
    public class ClientsService(IDbContextFactory<LavanderiaDbContext> context, DataCacheService cache)
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

        public async Task<Client> UpdateClient(int id, string name, string? phone)
        {
            var normalizedName = name?.Trim() ?? string.Empty;
            var normalizedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                throw new InvalidOperationException("El nombre del cliente es obligatorio");
            }

            if (normalizedName.Length > 50)
            {
                throw new InvalidOperationException("El nombre del cliente no puede exceder 50 caracteres");
            }

            if (normalizedPhone?.Length > 15)
            {
                throw new InvalidOperationException("El teléfono no puede exceder 15 caracteres");
            }

            await using var dbContext = await context.CreateDbContextAsync();
            var client = await dbContext.Clients.FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Cliente no encontrado");

            var duplicateNameExists = await dbContext.Clients
                .AnyAsync(candidate => candidate.Id != id && EF.Functions.ILike(candidate.Name, normalizedName));

            if (duplicateNameExists)
            {
                throw new InvalidOperationException("Ya existe un cliente con ese nombre");
            }

            var nameChanged = !string.Equals(client.Name, normalizedName, StringComparison.Ordinal);
            client.Name = normalizedName;
            client.Phone = normalizedPhone;
            await dbContext.SaveChangesAsync();

            await cache.UpdateCachedClientAsync(client);

            if (nameChanged)
            {
                // Las órdenes cacheadas incluyen el nombre del cliente; si cambió,
                // deben recargarse para reflejarlo.
                cache.MarkOrdersAsNeedingRefresh();
            }

            return client;
        }
    }
}