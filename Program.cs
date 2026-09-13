using control_lavanderia.Components;
using control_lavanderia.Components.Data;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Services;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();

builder.Services.AddDbContextFactory<LavanderiaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LavanderiaConnection")));

builder.Services.AddScoped<OrderNumberService>();
builder.Services.AddHostedService<OrderNumberCacheCleanupService>();
builder.Services.AddScoped<OrdersService>();
builder.Services.AddScoped<ControlPointService>();
builder.Services.AddScoped<OrderDetailsService>();
builder.Services.AddScoped<ClientsService>();
builder.Services.AddScoped<RegisterOrderService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ConfigurationsService>();
builder.Services.AddScoped<SuggestionsService>();

var app = builder.Build();

// Apply pending EF Core migrations automatically at startup.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LavanderiaDbContext>>();

    try
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations.");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
