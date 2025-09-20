using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;

namespace UseCases.Accounts.Authentication.LogOut;

public static partial class LogoutCommand
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : ICommandHandler<Command, LogoutResult>
    {
        public async Task<ErrorOr<LogoutResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                if (!string.IsNullOrEmpty(request.RefreshToken))
                {
                    await keycloakService.LogoutAsync(request.RefreshToken, cancellationToken);
                }

                return new LogoutResult { Success = true };
            }
            catch
            {
                // Log error but still return success for security
                return new LogoutResult { Success = true };
            }
        }
    }
}