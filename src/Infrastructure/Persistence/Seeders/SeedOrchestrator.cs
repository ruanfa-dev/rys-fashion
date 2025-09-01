using Microsoft.Extensions.Hosting;

using Serilog;

namespace Infrastructure.Persistence.Seeders;
public class SeedOrchestrator : IHostedService
{
    private readonly IEnumerable<IDataSeeder> _seeders;

    public SeedOrchestrator(IEnumerable<IDataSeeder> seeders)
        => _seeders = seeders;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var seeder in _seeders)
        {
            var name = seeder.GetType().Name;
            Log.Information("[SeedOrchestrator] Running {Seeder}", name);
            await seeder.SeedAsync(cancellationToken);
            Log.Information("[SeedOrchestrator] {Seeder} completed", name);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
