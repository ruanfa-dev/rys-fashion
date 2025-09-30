using Microsoft.Extensions.Hosting;

using Serilog;

namespace Infrastructure.Persistence.Seeders;
public class SeedOrchestrator(IEnumerable<IDataSeeder> seeders) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (IDataSeeder seeder in seeders)
        {
            string name = seeder.GetType().Name;
            Log.Information("[SeedOrchestrator] Running {Seeder}", name);
            await seeder.SeedAsync(cancellationToken);
            Log.Information("[SeedOrchestrator] {Seeder} completed", name);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
