namespace UseCases.Accounts.Authentication.Login.External.Exchange;

public static partial class ExchangeExternalToken
{
    public const string Name = "ExchangeExternalToken";
    public const string Summary = "Exchange external provider token for application tokens";
    public const string Description = "Accepts an external provider token (from frontend OAuth) and exchanges it for application JWT tokens using official provider SDKs for validation";
}