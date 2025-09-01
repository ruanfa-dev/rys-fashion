using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Core.Identity;

using ErrorOr;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using UseCases.Common.Security.Authentication.Options;
using UseCases.Common.Security.Authentication.Tokens.Models;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace Infrastructure.Security.Authentication.Tokens.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenService(IOptions<JwtOptions> jwtSettings)
    {
        _jwtOptions = jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        _tokenHandler = new JwtSecurityTokenHandler();

        // Pre-configure validation parameters for better performance
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret))
        };
    }

    public Task<ErrorOr<AccessTokenResult>> GenerateAccessTokenAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var expires = now.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes);

            // Essential claims only - avoid PII in JWT
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new(JwtRegisteredClaimNames.Aud, _jwtOptions.Audience),
                new(JwtRegisteredClaimNames.Iss, _jwtOptions.Issuer)
            };

            // Add: username only if needed (avoid email in JWT for privacy)
            if (!string.IsNullOrEmpty(user.UserName))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: expires.UtcDateTime,
                signingCredentials: credentials
            );

            var tokenString = _tokenHandler.WriteToken(token);

            return Task.FromResult<ErrorOr<AccessTokenResult>>(new AccessTokenResult
            {
                Token = tokenString,
                ExpiresAt = expires.ToUnixTimeSeconds()
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult<ErrorOr<AccessTokenResult>>(
                Error.Failure("JWT.GenerationFailed", $"Failed to generate JWT token: {ex.Message}")
            );
        }
    }

    public Task<ErrorOr<TokenValidationResult>> ValidateTokenAsync(
        string token,
        bool validateLifetime = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult<ErrorOr<TokenValidationResult>>(
                Error.Validation("JWT.EmptyToken", "Token cannot be empty")
            );
        }

        try
        {
            // Clone validation parameters to modify lifetime validation
            var validationParams = _validationParameters.Clone();
            validationParams.ValidateLifetime = validateLifetime;

            var principal = _tokenHandler.ValidateToken(token, validationParams, out var validatedToken);

            var result = new TokenValidationResult
            {
                IsValid = true,
                ClaimsIdentity = principal.Identities.FirstOrDefault(),
                SecurityToken = validatedToken,
                Issuer = validatedToken.Issuer
            };

            return Task.FromResult<ErrorOr<TokenValidationResult>>(result);
        }
        catch (SecurityTokenExpiredException)
        {
            var result = new TokenValidationResult
            {
                IsValid = false,
                Exception = new SecurityTokenExpiredException("Token has expired")
            };
            return Task.FromResult<ErrorOr<TokenValidationResult>>(result);
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return Task.FromResult<ErrorOr<TokenValidationResult>>(
                Error.Validation("JWT.InvalidSignature", "Token signature is invalid")
            );
        }
        catch (SecurityTokenException ex)
        {
            return Task.FromResult<ErrorOr<TokenValidationResult>>(
                Error.Validation("JWT.Invalid", $"Token validation failed: {ex.Message}")
            );
        }
        catch (Exception ex)
        {
            return Task.FromResult<ErrorOr<TokenValidationResult>>(
                Error.Failure("JWT.ValidationError", $"Unexpected error during token validation: {ex.Message}")
            );
        }
    }

    public ErrorOr<ClaimsPrincipal> GetPrincipalFromToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Error.Validation("JWT.EmptyToken", "Token cannot be empty");
        }

        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var claims = jwtToken.Claims;
            var identity = new ClaimsIdentity(claims, "JWT", JwtRegisteredClaimNames.UniqueName, ClaimTypes.Role);
            return new ClaimsPrincipal(identity);
        }
        catch (Exception ex)
        {
            return Error.Failure("JWT.PrincipalExtraction", $"Failed to extract principal: {ex.Message}");
        }
    }

    public ErrorOr<TimeSpan> GetTokenRemainingTime(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Error.Validation("JWT.EmptyToken", "Token cannot be empty");
        }

        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var exp = jwtToken.Payload.Expiration;

            if (!exp.HasValue)
            {
                return Error.Validation("JWT.NoExpiration", "Token does not have an expiration claim");
            }

            var expiration = DateTimeOffset.FromUnixTimeSeconds(exp.Value);
            var remaining = expiration - DateTimeOffset.UtcNow;

            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            return Error.Failure("JWT.RemainingTime", $"Failed to get remaining time: {ex.Message}");
        }
    }
}