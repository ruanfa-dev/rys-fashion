using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;

namespace UseCases.Admin.Users.ResetPassword;

public static partial class ResetUserPasswordCommand
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : ICommandHandler<Command, ResetPasswordResult>
    {
        public async Task<ErrorOr<ResetPasswordResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                await keycloakService.SetUserPasswordAsync(request.UserId, request.Param.Password, request.Param.Temporary, cancellationToken);

                return new ResetPasswordResult { UserId = request.UserId, Success = true };
            }
            catch (Exception ex)
            {
                return Error.Failure("ResetPassword.Failed", $"Failed to reset password: {ex.Message}");
            }
        }
    }
}