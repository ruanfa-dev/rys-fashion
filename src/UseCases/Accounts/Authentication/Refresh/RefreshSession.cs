namespace UseCases.Accounts.Authentication.Sessions.Refresh;
public static partial class RefreshSession
{
    public const string Name = nameof(RefreshSession);
    public const string Route = "refresh";
    public const string Description = "Refreshes the authentication token for the currently authenticated user.";
    public const string Summary = "Refresh Token";

}
