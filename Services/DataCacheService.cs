using control_lavanderia.Components.Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Services
{
    /// <summary>
    /// Cache en memoria compartido con la información que se consulta constantemente:
    /// <list type="bullet">
    /// <item>Lista de clientes (máximo <see cref="MaxCachedClients"/>).</item>
    /// <item>Órdenes recibidas y entregadas durante el día actual.</item>
    /// </list>
    /// El "día actual" se calcula siempre con la zona horaria del negocio (UTC-6,
    /// ver <see cref="BusinessTimeZoneOffset"/>), independientemente de la zona
    /// horaria del servidor donde corra la aplicación.
    /// Cada sección expone una variable de control ("dirty flag") que indica si la
    /// información cacheada quedó desactualizada y debe recargarse desde la base de datos.
    /// Las lecturas reciben una copia inmutable de la lista, por lo que no es necesario
    /// que quien la consume tome bloqueos.
    /// </summary>
    public class DataCacheService(IDbContextFactory<LavanderiaDbContext> contextFactory)
    {
        /// <summary>Cantidad máxima de clientes que se mantienen en el cache.</summary>
        public const int MaxCachedClients = 1000;

        /// <summary>Diferencia con UTC de la zona horaria del negocio (UTC-6).</summary>
        public static readonly TimeSpan BusinessTimeZoneOffset = TimeSpan.FromHours(-6);

        private readonly SemaphoreSlim _clientsSemaphore = new(1, 1);
        private readonly SemaphoreSlim _ordersSemaphore = new(1, 1);

        private List<Client> _clients = [];
        private List<Order> _receivedToday = [];
        private List<Order> _deliveredToday = [];

        private bool _clientsLoaded;
        private bool _ordersLoaded;
        private DateOnly _ordersDay;

        // Variables de control: indican que el cache quedó desactualizado.
        private volatile bool _clientsNeedRefresh = true;
        private volatile bool _ordersNeedRefresh = true;

        /// <summary>
        /// Indica si la lista de clientes debe volver a cargarse desde la base de datos.
        /// Se activa cuando se registra un cliente nuevo.
        /// </summary>
        public bool ClientsNeedRefresh => _clientsNeedRefresh;

        /// <summary>
        /// Indica si las listas de órdenes del día deben volver a cargarse.
        /// Se activa cuando se recibe/entrega una orden o cuando cambia el día.
        /// </summary>
        public bool OrdersNeedRefresh => _ordersNeedRefresh || (_ordersLoaded && _ordersDay != TodayKey());

        /// <summary>Copia actual de la lista de clientes (puede estar desactualizada; usa <see cref="GetClientsAsync"/>).</summary>
        public IReadOnlyList<Client> CachedClients => _clients;

        /// <summary>Órdenes recibidas hoy en el cache (puede estar desactualizada; usa <see cref="GetOrdersReceivedTodayAsync"/>).</summary>
        public IReadOnlyList<Order> CachedReceivedToday => _receivedToday;

        /// <summary>Órdenes entregadas hoy en el cache (puede estar desactualizada; usa <see cref="GetOrdersDeliveredTodayAsync"/>).</summary>
        public IReadOnlyList<Order> CachedDeliveredToday => _deliveredToday;

        // ---------------------------------------------------------------------
        // Clientes
        // ---------------------------------------------------------------------

        /// <summary>
        /// Obtiene la lista de clientes (hasta <see cref="MaxCachedClients"/>).
        /// Si el cache no se ha cargado o la variable de control <see cref="ClientsNeedRefresh"/>
        /// está activa, se recarga desde la base de datos.
        /// </summary>
        public async Task<IReadOnlyList<Client>> GetClientsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _clientsLoaded && !_clientsNeedRefresh)
            {
                return _clients;
            }

            await _clientsSemaphore.WaitAsync();
            try
            {
                // Otra llamada pudo refrescar mientras esperábamos el semáforo.
                if (!forceRefresh && _clientsLoaded && !_clientsNeedRefresh)
                {
                    return _clients;
                }

                await using var dbContext = await contextFactory.CreateDbContextAsync();
                _clients = await dbContext.Clients
                    .AsNoTracking()
                    .OrderBy(client => client.Name)
                    .Take(MaxCachedClients)
                    .ToListAsync();

                _clientsLoaded = true;
                _clientsNeedRefresh = false;

                return _clients;
            }
            finally
            {
                _clientsSemaphore.Release();
            }
        }

        /// <summary>
        /// Marca la lista de clientes como desactualizada. Debe llamarse cuando se
        /// registra un cliente nuevo; la próxima lectura recargará la lista.
        /// </summary>
        public void MarkClientsAsNeedingRefresh()
        {
            _clientsNeedRefresh = true;
        }

        /// <summary>Limpia el cache de clientes para que se recargue completo en la próxima lectura.</summary>
        public void ClearClients()
        {
            _clientsNeedRefresh = true;
            _clientsLoaded = false;
        }

        // ---------------------------------------------------------------------
        // Órdenes del día (recibidas / entregadas)
        // ---------------------------------------------------------------------

        /// <summary>Obtiene las órdenes recibidas durante el día actual.</summary>
        public async Task<IReadOnlyList<Order>> GetOrdersReceivedTodayAsync(bool forceRefresh = false)
        {
            await EnsureOrdersLoadedAsync(forceRefresh);
            return _receivedToday;
        }

        /// <summary>Obtiene las órdenes entregadas durante el día actual.</summary>
        public async Task<IReadOnlyList<Order>> GetOrdersDeliveredTodayAsync(bool forceRefresh = false)
        {
            await EnsureOrdersLoadedAsync(forceRefresh);
            return _deliveredToday;
        }

        /// <summary>
        /// Recarga (si es necesario) ambas listas de órdenes del día. Se recarga cuando
        /// el cache no está inicializado, cuando <see cref="OrdersNeedRefresh"/> está activo
        /// o cuando cambió la fecha.
        /// </summary>
        public async Task EnsureOrdersLoadedAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && IsOrdersCacheValid())
            {
                return;
            }

            await _ordersSemaphore.WaitAsync();
            try
            {
                if (!forceRefresh && IsOrdersCacheValid())
                {
                    return;
                }

                var (dayStart, dayEnd) = GetTodayRange();

                await using var dbContext = await contextFactory.CreateDbContextAsync();
                var todayOrders = await dbContext.Orders
                    .AsNoTracking()
                    .Include(order => order.Client)
                    .Include(order => order.OrderDetails)
                    .Where(order =>
                        (order.ReceivedDate >= dayStart && order.ReceivedDate < dayEnd) ||
                        (order.DeliveryDate >= dayStart && order.DeliveryDate < dayEnd))
                    .ToListAsync();

                _receivedToday = todayOrders
                    .Where(order => InRange(order.ReceivedDate, dayStart, dayEnd) && !order.CancelDate.HasValue)
                    .OrderByDescending(order => order.ReceivedDate)
                    .ThenByDescending(order => order.Id)
                    .ToList();

                _deliveredToday = todayOrders
                    .Where(order => InRange(order.DeliveryDate, dayStart, dayEnd))
                    .OrderByDescending(order => order.DeliveryDate)
                    .ThenByDescending(order => order.Id)
                    .ToList();

                _ordersDay = TodayKey();
                _ordersLoaded = true;
                _ordersNeedRefresh = false;
            }
            finally
            {
                _ordersSemaphore.Release();
            }
        }

        /// <summary>
        /// Marca las listas de órdenes del día como desactualizadas. La próxima lectura
        /// las recargará desde la base de datos.
        /// </summary>
        public void MarkOrdersAsNeedingRefresh()
        {
            _ordersNeedRefresh = true;
        }

        /// <summary>
        /// Actualiza las listas del día con el estado actual de una orden concreta.
        /// Debe llamarse después de recibir o entregar una orden: agrega el registro a
        /// la lista correspondiente (recibidas/entregadas) o lo quita si ya no aplica.
        /// Si el cache aún no se ha cargado no hace nada; la carga inicial lo incluirá.
        /// </summary>
        public async Task RefreshOrderAsync(int orderId)
        {
            if (!_ordersLoaded)
            {
                return;
            }

            Order? order;
            await using (var dbContext = await contextFactory.CreateDbContextAsync())
            {
                order = await dbContext.Orders
                    .AsNoTracking()
                    .Include(o => o.Client)
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.Id == orderId);
            }

            if (order is null)
            {
                _ordersNeedRefresh = true;
                return;
            }

            await _ordersSemaphore.WaitAsync();
            try
            {
                if (!_ordersLoaded)
                {
                    return;
                }

                var (dayStart, dayEnd) = GetTodayRange();

                _receivedToday = UpdateList(
                    _receivedToday,
                    order,
                    InRange(order.ReceivedDate, dayStart, dayEnd) && !order.CancelDate.HasValue,
                    o => o.ReceivedDate);

                _deliveredToday = UpdateList(
                    _deliveredToday,
                    order,
                    InRange(order.DeliveryDate, dayStart, dayEnd),
                    o => o.DeliveryDate);
            }
            finally
            {
                _ordersSemaphore.Release();
            }
        }

        /// <summary>Limpia el cache de órdenes del día para que se recargue en la próxima lectura.</summary>
        public void ClearOrders()
        {
            _ordersNeedRefresh = true;
            _ordersLoaded = false;
        }

        /// <summary>Invalida todo el cache.</summary>
        public void ClearAll()
        {
            ClearClients();
            ClearOrders();
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------

        private bool IsOrdersCacheValid()
        {
            return _ordersLoaded
                && !_ordersNeedRefresh
                && _ordersDay == TodayKey();
        }

        private static DateOnly TodayKey() => DateOnly.FromDateTime(NowInBusinessZone().DateTime);

        /// <summary>
        /// Hora actual en la zona horaria del negocio (UTC-6), sin importar la zona
        /// horaria configurada en el servidor.
        /// </summary>
        public static DateTimeOffset NowInBusinessZone() => DateTimeOffset.UtcNow.ToOffset(BusinessTimeZoneOffset);

        private static (DateTimeOffset DayStart, DateTimeOffset DayEnd) GetTodayRange()
        {
            var now = NowInBusinessZone();
            var dayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, BusinessTimeZoneOffset);
            return (dayStart.ToUniversalTime(), dayStart.AddDays(1).ToUniversalTime());
        }

        private static bool InRange(DateTimeOffset? value, DateTimeOffset dayStart, DateTimeOffset dayEnd)
        {
            return value.HasValue && value.Value >= dayStart && value.Value < dayEnd;
        }

        /// <summary>
        /// Devuelve una nueva lista (inmutable para el lector) sin la orden indicada y
        /// la agrega solo si pertenece al conjunto, manteniendo el orden por fecha.
        /// </summary>
        private static List<Order> UpdateList(
            List<Order> current,
            Order order,
            bool belongs,
            Func<Order, DateTimeOffset?> sortKey)
        {
            var updated = current.Where(existing => existing.Id != order.Id).ToList();

            if (belongs)
            {
                updated.Add(order);
            }

            return updated
                .OrderByDescending(sortKey)
                .ThenByDescending(existing => existing.Id)
                .ToList();
        }
    }
}
