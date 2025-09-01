namespace Infrastructure.Persistence.Seeders;
public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

