using Core.Identity.Tokens;
using Core.Identity.Users;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Http;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Contexts;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace UseCases.Accounts.Authentication.LogAllOut;
public static class LogoutFromAll
{
    public const string Name = nameof(LogoutFromAll);
    public const string Route = "logout/all";
    public const string Description = "Logs out the currently authenticated user from all sessions.";
    public const string Summary = "Logout All";

    public sealed record Param(string RefreshToken);
    public sealed class ParamValidator : AbstractValidator<Param>
    {
        public ParamValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty()
                .WithErrorCode(RefreshToken.Errors.RefreshTokenRequired.Code)
                .WithMessage(RefreshToken.Errors.RefreshTokenRequired.Description);
        }
    }
    public sealed record Command(Param Param) : ICommand<Deleted>;
    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Param).SetValidator(new ParamValidator());
        }
    }

    public sealed class Handler(IRefreshTokenService refreshTokenService, IHttpContextAccessor accessor) : ICommandHandler<Command, Deleted>
    {
        private readonly IRefreshTokenService _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        private readonly IHttpContextAccessor _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));

        public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guid? userId = _accessor.HttpContext?.User.GetUserId();
            bool isAuthenticated = _accessor.HttpContext?.User.IsAuthenticated() ?? false;
            string ipAddress = _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Check: user is authenticated
            if (userId is null || !isAuthenticated)
                return User.Errors.UserUnauthorized;

            // Revoke: all tokens for user except the provided current token (keeps current session)
            ErrorOr<int> revokeResult = await _refreshTokenService.RevokeAllUserTokensAsync(
                userId: userId.Value,
                ipAddress: ipAddress,
                reason: "User requested logout from all devices",
                exceptToken: request.Param.RefreshToken,
                cancellationToken: cancellationToken);

            if (revokeResult.IsError)
                return revokeResult.Errors;

            return Result.Deleted;
        }
    }
}
