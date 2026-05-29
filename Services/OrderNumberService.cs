using control_lavanderia.Components.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace Services
{
    public class OrderNumberService
    {
        private readonly LavanderiaDbContext _context;
        private static readonly ConcurrentDictionary<string, int> _cache = new();

        public OrderNumberService(LavanderiaDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<string> GenerarFolioAsync()
        {
            var hoy = DateTime.Now.ToString("yyyyMMdd");

            // Intentar obtener del caché primero (performance)
            if (_cache.TryGetValue(hoy, out int numero))
            {
                var nuevoNumero = Interlocked.Increment(ref numero);
                _cache[hoy] = nuevoNumero;
                return $"{hoy}{nuevoNumero:D4}";
            }

            // Si no está en caché, consultar BD
            using var transaction = await _context.Database.BeginTransactionAsync();

            var ultimoFolio = await _context.Orders
                .Where(o => o.Folio.StartsWith(hoy))
                .OrderByDescending(o => o.Folio)
                .Select(o => o.Folio)
                .FirstOrDefaultAsync();

            int ultimoNumero = 0;
            if (ultimoFolio != null && ultimoFolio.Length >= 12)
            {
                var numeroStr = ultimoFolio.Substring(8, 4);
                bool correctConversion = int.TryParse(numeroStr, out ultimoNumero);
                ultimoNumero = correctConversion ? ultimoNumero : 0;
            }

            var nuevoNumeroBD = ultimoNumero + 1;
            _cache[hoy] = nuevoNumeroBD;

            await transaction.CommitAsync();

            return $"{hoy}{nuevoNumeroBD:D4}";
        }

        // Limpiar caché diariamente (opcional)
        public static void LimpiarCache()
        {
            var hoy = DateTime.Now.ToString("yyyyMMdd");
            var keysToRemove = _cache.Keys.Where(k => k != hoy).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
        }

    }
}
