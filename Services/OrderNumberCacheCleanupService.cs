namespace Services;

public sealed class OrderNumberCacheCleanupService : BackgroundService
{
    private readonly ILogger<OrderNumberCacheCleanupService> _logger;

    public OrderNumberCacheCleanupService(ILogger<OrderNumberCacheCleanupService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextMidnight = now.Date.AddDays(1);
            var delay = nextMidnight - now;

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            OrderNumberService.LimpiarCache();
            _logger.LogInformation("LimpiarCache ejecutado a las {HoraLocal}", DateTime.Now);
        }
    }
}
