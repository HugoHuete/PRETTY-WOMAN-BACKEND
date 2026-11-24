using Microsoft.Extensions.DependencyInjection;
using PrettyWoman.Infrastructure;
using PrettyWoman.Workers;

var builder = Host.CreateApplicationBuilder(args);
if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
    throw new InvalidOperationException("Configura ConnectionStrings__DefaultConnection para ejecutar la limpieza.");

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddScoped<Worker>();

using var host = builder.Build();
await host.StartAsync();
try
{
    await using var scope = host.Services.CreateAsyncScope();
    var worker = scope.ServiceProvider.GetRequiredService<Worker>();
    var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
    await worker.RunAsync(lifetime.ApplicationStopping);
}
finally
{
    await host.StopAsync();
}
