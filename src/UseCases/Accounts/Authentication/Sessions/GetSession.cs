namespace UseCases.Accounts.Authentication.Sessions;
public static partial class GetSession
{
    public const string Name = nameof(GetSession);
    public const string Route = "session/get";
    public const string Description = "Retrieves the session information for the current user.";
    public const string Summary = "Get user session";
}
