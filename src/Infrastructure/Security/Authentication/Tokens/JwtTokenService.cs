using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using ErrorOr;

using Infrastructure.Security.Authentication.Options;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using UseCases.Common.Security.Authentication.Tokens;
using UseCases.Common.Security.Authorization.Claims;

namespace Infrastructure.Security.Authentication.Tokens;

public sealed class JwtTokenService(IOptions<JwtOptions> jwtSettings) : IJwtTokenService
{
    private readonly JwtOptions _jwtOption = jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));

    public Task<(string Token, DateTimeOffset ExpiresAt)> GenerateAccessTokenAsync(
        string userId,
        string email,
        string userName,
        IEnumerable<string> roles,
        IEnumerable<string> permissions)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_jwtOption.ExpiryInMinutes);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.UniqueName, userName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        claims.AddRange(permissions.Select(p => new Claim(CustomClaim.Permission, p)));
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOption.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOption.Issuer,
            audience: _jwtOption.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        var accessToken = tokenHandler.WriteToken(token);

        return Task.FromResult((accessToken, expires));
    }

    public Task<(string Token, DateTimeOffset ExpiresAt)> GenerateRefreshTokenAsync(string userId, bool rememberMe = false)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddDays(
            rememberMe ? _jwtOption.RefreshTokenExpiryRememberMeInDays : _jwtOption.RefreshTokenExpiryInDays);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim("typ", "refresh")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOption.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOption.Issuer,
            audience: _jwtOption.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        var refreshToken = tokenHandler.WriteToken(token);

        return Task.FromResult((refreshToken, expires));
    }

    public Task<ErrorOr<ClaimsPrincipal?>> ValidateTokenAsync(string token, bool validateLifetime = true)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult<ErrorOr<ClaimsPrincipal?>>(Error.Validation("token", "Token is empty."));
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtOption.Secret);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = validateLifetime,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtOption.Issuer,
            ValidAudience = _jwtOption.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken jwt &&
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<ErrorOr<ClaimsPrincipal?>>(Error.Validation("token", "Invalid token algorithm."));
            }

            return Task.FromResult<ErrorOr<ClaimsPrincipal?>>(principal);
        }
        catch (SecurityTokenExpiredException)
        {
            return Task.FromResult<ErrorOr<ClaimsPrincipal?>>(Error.Validation("token", "Token has expired."));
        }
        catch (SecurityTokenException ex)
        {
            return Task.FromResult<ErrorOr<ClaimsPrincipal?>>(Error.Validation("token", $"Token validation failed: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult<ErrorOr<ClaimsPrincipal?>>(Error.Unexpected($"Token validation failed: {ex.Message}"));
        }
    }
}
