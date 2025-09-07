namespace UseCases.Accounts.Authentication.Login.External.Verify;
public static partial class VerifyExternalToken
{
    public const string Name = "VerifyExternalToken";
    public const string Summary = "Verify external provider token";
    public const string Description = "Validates an external provider token and returns user information without creating a session";
}