using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Http;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Tokens.Services;

namespace UseCases.Accounts.Authentication.LogOut;
public static partial class Logout
{
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
            var ipAddress = accessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await tokenManagementService.LogoutAsync(request.Param.RefreshToken, ipAddress, cancellationToken);

            return result;
        }
    }
}
