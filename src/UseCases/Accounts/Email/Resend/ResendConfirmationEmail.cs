namespace UseCases.Accounts.Email.Resend;
public static partial class ResendEmailConfirmation
{
    public const string Name = nameof(ResendEmailConfirmation);
    public const string Route = "email/resend-confirmation";
    public const string Description = "Resends a user's email confirmation link.";
    public const string Summary = "Resend confirmation email";
}
