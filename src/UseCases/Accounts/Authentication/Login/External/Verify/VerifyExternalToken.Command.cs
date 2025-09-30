using ErrorOr;

using Mapster;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Externals;

namespace UseCases.Accounts.Authentication.Login.External.Verify;
public static partial class VerifyExternalToken
{
    public sealed record Command(
        string? Provider = null,
        string? AccessToken = null,
        string? IdToken = null
    ) : ICommand<Result>;

    public sealed record Result : ExternalUserInfo;

    public sealed class Handler(
        IExternalTokenValidator tokenValidator
    ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Provider))
            {
                return Error.Validation("Provider.Required", "Provider is required");
            }

            if (string.IsNullOrWhiteSpace(request.AccessToken) && string.IsNullOrWhiteSpace(request.IdToken))
            {
                return Error.Validation("Token.Required", "Either access token or ID token is required");
            }

            ErrorOr<ExternalUserInfo> validationResult = await tokenValidator.ValidateTokenAsync(
                request.Provider,
                request.AccessToken,
                request.IdToken,
                null, // No auth code for verification
                null, // No redirect URI for verification
                cancellationToken
            );

            if (validationResult.IsError)
            {
                return validationResult.Errors;
            }

            return validationResult.Value.Adapt<Result>();
        }
    }
}