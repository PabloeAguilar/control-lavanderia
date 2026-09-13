using control_lavanderia.Components.Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Services
{
    public class SuggestionsService(IDbContextFactory<LavanderiaDbContext> contextFactory)
    {
        public async Task<List<Suggestion>> GetAllSuggestions()
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Suggestions.ToListAsync();
        }

        public async Task<Suggestion?> GetSuggestion(int id)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Suggestions.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Suggestion> UpdateSuggestion(int id, int price)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var suggestion = await context.Suggestions.FirstOrDefaultAsync(s => s.Id == id);
            if (suggestion == null)
                throw new InvalidOperationException($"Sugerencia con ID {id} no encontrada");

            suggestion.Price = price;
            await context.SaveChangesAsync();
            return suggestion;
        }
    }
}
