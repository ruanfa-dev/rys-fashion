using Core.Identity;

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

    public sealed class Handler(ITokenManagementService tokenManagementService, IHttpContextAccessor accessor) : ICommandHandler<Command, Deleted>
    {
        public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
        {
            var userId = accessor.HttpContext?.User.GetUserId();
            var isAuthenticated = accessor.HttpContext?.User.IsAuthenticated() ?? false;
            var ipAddress = accessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Check: user is authenticated
            if (userId is null || !isAuthenticated)
                return User.Errors.UserUnauthorized;

            var result = await tokenManagementService.LogoutFromAllDevicesAsync(
                userId: userId.Value,
                ipAddress: ipAddress,
                currentToken: request.Param.RefreshToken,
                cancellationToken);

            if (result.IsError)
                return result.Errors;

            return Result.Deleted;
        }
    }
}
