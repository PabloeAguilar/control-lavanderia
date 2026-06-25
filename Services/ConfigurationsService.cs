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

    }
}