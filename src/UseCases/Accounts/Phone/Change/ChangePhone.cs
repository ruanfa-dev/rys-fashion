namespace UseCases.Accounts.Phone.Change;

public static partial class ChangePhone
{
    public const string Name = nameof(ChangePhone);
    public const string Route = "change";
    public const string Description = "Sends verification SMS to new phone number for change.";
    public const string Summary = "Initiate phone number change";
}