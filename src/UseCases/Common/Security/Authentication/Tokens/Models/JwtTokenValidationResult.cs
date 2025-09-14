using System.Security.Claims;

using Microsoft.IdentityModel.Tokens;

namespace UseCases.Common.Security.Authentication.Tokens.Models;

/// <summary>
/// Result of JWT token validation operation.
/// </summary>
public sealed record JwtTokenValidationResult
{
    public bool IsValid { get; init; }
    public ClaimsIdentity? ClaimsIdentity { get; init; }
    public SecurityToken? SecurityToken { get; init; }
    public string? Issuer { get; init; }
    public Exception? Exception { get; init; }
}