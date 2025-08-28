using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Core.Identity;

using ErrorOr;

using Infrastructure.Security.Authentication.Options;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using UseCases.Common.Security.Authentication.Tokens.Models;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace Infrastructure.Security.Authentication.Tokens.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public JwtTokenService(IOptions<JwtOptions> jwtSettings)
    {
        _jwtOptions = jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
    }

    public async Task<ErrorOr<AccessTokenResult>> GenerateAccessTokenAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_jwtOptions.ExpiryInMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds
        );
        await Task.CompletedTask;
        var accessToken = _tokenHandler.WriteToken(token);

        return new AccessTokenResult
        {
            Token = accessToken,
            ExpiresAt = expires.ToUnixTimeSeconds(),
        };
    }

    public async Task<ErrorOr<TokenValidationResult>> ValidateTokenAsync(
        string token,
        bool validateLifetime = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Error.Validation("Token is empty.");
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = validateLifetime,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret))
        };

        var result = new TokenValidationResult();

        try
        {
            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            result.IsValid = true;
            result.ClaimsIdentity = principal.Identities.FirstOrDefault();
            result.SecurityToken = validatedToken;
            result.Issuer = validatedToken.Issuer;

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Exception = ex;
            return await Task.FromResult(result);
        }
    }

    public ErrorOr<ClaimsPrincipal> GetPrincipalFromToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Error.Validation("Token is empty.");
        }

        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            if (jwtToken == null)
            {
                return Error.Validation("Invalid token format.");
            }

            var claims = jwtToken.Claims;
            var identity = new ClaimsIdentity(claims, "JWT", JwtRegisteredClaimNames.UniqueName, "roles");
            var principal = new ClaimsPrincipal(identity);

            return principal;
        }
        catch (Exception ex)
        {
            return Error.Failure($"Failed to extract principal: {ex.Message}");
        }
    }

    public ErrorOr<TimeSpan> GetTokenRemainingTime(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Error.Validation("Token is empty.");
        }

        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            if (jwtToken == null)
            {
                return Error.Validation("Invalid token format.");
            }

            var exp = jwtToken.Payload.Expiration;
            if (!exp.HasValue)
            {
                return Error.Validation("Token does not have an expiration claim.");
            }

            var expiration = DateTimeOffset.FromUnixTimeSeconds(exp.Value);
            var now = DateTimeOffset.UtcNow;
            var remaining = expiration - now;

            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            return Error.Failure($"Failed to get remaining time: {ex.Message}");
        }
    }
}