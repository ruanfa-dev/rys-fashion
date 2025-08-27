namespace Infrastructure.Persistence.Options;

public static class DbConnectionOptions
{
    public const string Default = "Postgres";

    public const string Postgres = "Postgres";
    public const string SqlServer = "SqlServer";
    public const string Sqlite = "Sqlite";
}