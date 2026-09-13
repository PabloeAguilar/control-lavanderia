using control_lavanderia.Components.Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Services
{
    public class ConfigurationsService(IDbContextFactory<LavanderiaDbContext> contextFactory)
    {
        public async Task<Configuration?> GetConfiguration(string key)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Configurations
                .FirstOrDefaultAsync(configuration => configuration.Key == key);
        }

        public async Task<List<Configuration>> GetAllVisibleConfigurations()
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Configurations
                .Where(c => c.ShowInSettings)
                .ToListAsync();
        }

        public async Task<Configuration> UpdateConfiguration(int id, string value)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var config = await context.Configurations.FirstOrDefaultAsync(c => c.Id == id);
            if (config == null)
                throw new InvalidOperationException($"Configuración con ID {id} no encontrada");

            config.Value = value;
            await context.SaveChangesAsync();
            return config;
        }
    }
}