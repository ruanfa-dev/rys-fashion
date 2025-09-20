using ErrorOr;
using MediatR;
using UseCases.Common.Security.Authentication.Models;

namespace UseCases.Accounts.Authentication.External.ExchangeToken;

/// <summary>
/// Command to exchange external OAuth token (Google/Facebook) for Keycloak token
/// </summary>
public sealed record ExchangeExternalTokenCommand : IRequest<ErrorOr<ExternalAuthResponse>>
{
    public string Provider { get; init; } = string.Empty; // "google" or "facebook"
    public string AccessToken { get; init; } = string.Empty;
    public string? IdToken { get; init; }
    public bool LinkToExistingUser { get; init; } = false;
    public string? ExistingUserId { get; init; }
}