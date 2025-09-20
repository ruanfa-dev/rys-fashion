using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Users.ResetPassword;

public static partial class ResetUserPasswordCommand
{
    public const string Name = nameof(ResetUserPasswordCommand);
    public const string Summary = "Reset user password in Keycloak";
    public const string Description = "Resets a user's password in Keycloak realm";

    public record Command(string UserId, ResetPasswordParam Param) : ICommand<ResetPasswordResult>;

    public record ResetPasswordParam
    {
        public string Password { get; init; } = string.Empty;
        public bool Temporary { get; init; } = false;
    }

    public record ResetPasswordResult
    {
        public string UserId { get; init; } = string.Empty;
        public bool Success { get; init; }
    }
}