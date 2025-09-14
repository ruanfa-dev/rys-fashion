namespace UseCases.Accounts.Password.Reset;
public static partial class ResetPassword
{
    public const string Name = nameof(ResetPassword);
    public const string Route = "reset";
    public const string Description = "Resets the password for a user's account using a valid reset code.";
    public const string Summary = "Reset password";
}
