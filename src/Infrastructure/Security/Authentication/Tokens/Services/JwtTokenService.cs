using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Core.Identity.Tokens;
using Core.Identity.Users;

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
            RequireSignedTokens = true,
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
            if (user is null || user.Id == Guid.Empty)
                return Task.FromResult<ErrorOr<AccessTokenResult>>(Jwt.Errors.InvalidUser);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset expires = now.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes);

            // Essential claims only - avoid PII in JWT
            List<Claim> claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Add: username only if needed (avoid email in JWT for privacy)
            if (!string.IsNullOrEmpty(user.UserName))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName));
            }

            // Add essential security claims
            if (user.EmailConfirmed)
            {
                claims.Add(new Claim("email_verified", "true"));
            }

            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: expires.UtcDateTime,
                signingCredentials: credentials
            );

            string? tokenString = _tokenHandler.WriteToken(token);

            return Task.FromResult<ErrorOr<AccessTokenResult>>(new AccessTokenResult
            {
                Token = tokenString,
                ExpiresAt = expires.ToUnixTimeSeconds()
            });
        }
        catch (SecurityTokenException)
        {
            return Task.FromResult<ErrorOr<AccessTokenResult>>(Jwt.Errors.SecurityTokenError);
        }
        catch (Exception)
        {
            return Task.FromResult<ErrorOr<AccessTokenResult>>(Jwt.Errors.GenerationFailed);
        }
    }

    public ErrorOr<ClaimsPrincipal> GetPrincipalFromToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Jwt.Errors.EmptyToken;
        }

        try
        {
            // Read token without validation for expired tokens
            JwtSecurityToken? jwtToken = _tokenHandler.ReadJwtToken(token);
            IEnumerable<Claim>? claims = jwtToken.Claims;
            ClaimsIdentity identity = new ClaimsIdentity(claims, "JWT", JwtRegisteredClaimNames.UniqueName, ClaimTypes.Role);
            return new ClaimsPrincipal(identity);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(Jwt.Errors.InvalidFormat.Code, $"Invalid token format: {ex.Message}");
        }
        catch (Exception)
        {
            return Jwt.Errors.PrincipalExtraction;
        }
    }

    public ErrorOr<TimeSpan> GetTokenRemainingTime(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Jwt.Errors.EmptyToken;
        }

        try
        {
            JwtSecurityToken? jwtToken = _tokenHandler.ReadJwtToken(token);
            long? exp = jwtToken.Payload.Expiration;

            if (!exp.HasValue)
            {
                return Jwt.Errors.NoExpiration;
            }

            DateTimeOffset expiration = DateTimeOffset.FromUnixTimeSeconds(exp.Value);
            TimeSpan remaining = expiration - DateTimeOffset.UtcNow;

            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(Jwt.Errors.InvalidFormat.Code, $"Invalid token format: {ex.Message}");
        }
        catch (Exception)
        {
            return Jwt.Errors.RemainingTime;
        }
    }

    public ErrorOr<bool> ValidateTokenFormat(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Jwt.Errors.EmptyToken;
        }

        try
        {
            // Basic format validation - should have 3 parts separated by dots
            string[] parts = token.Split('.');
            if (parts.Length != Jwt.Constraints.TokenParts)
            {
                return Error.Validation(Jwt.Errors.InvalidFormat.Code, "JWT must have exactly 3 parts separated by dots");
            }

            // Try to read the token structure
            JwtSecurityToken? jwtToken = _tokenHandler.ReadJwtToken(token);

            // Basic header validation
            if (string.IsNullOrEmpty(jwtToken.Header.Alg))
            {
                return Jwt.Errors.MissingAlgorithm;
            }

            // Basic payload validation
            if (jwtToken.Payload.Count == 0)
            {
                return Jwt.Errors.EmptyPayload;
            }

            return true;
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(Jwt.Errors.InvalidFormat.Code, $"Invalid token format: {ex.Message}");
        }
        catch (Exception)
        {
            return Jwt.Errors.FormatValidation;
        }
    }

    public ErrorOr<JwtSecurityToken> ParseToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Jwt.Errors.EmptyToken;
        }

        try
        {
            JwtSecurityToken? jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken;
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(Jwt.Errors.InvalidFormat.Code, $"Invalid token format: {ex.Message}");
        }
        catch (Exception)
        {
            return Jwt.Errors.ParseFailed;
        }
    }

    public ErrorOr<JwtTokenValidationResult> ValidateToken(
        string token,
        bool validateLifetime = true)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Jwt.Errors.EmptyToken;
        }

        try
        {
            // Clone validation parameters to modify lifetime validation
            TokenValidationParameters? validationParams = _validationParameters.Clone();
            validationParams.ValidateLifetime = validateLifetime;

            ClaimsPrincipal? principal = _tokenHandler.ValidateToken(token, validationParams, out SecurityToken? validatedToken);

            JwtTokenValidationResult result = new JwtTokenValidationResult
            {
                IsValid = true,
                ClaimsIdentity = principal.Identities.FirstOrDefault(),
                SecurityToken = validatedToken,
                Issuer = validatedToken.Issuer
            };

            return result;
        }
        catch (SecurityTokenExpiredException ex)
        {
            JwtTokenValidationResult result = new JwtTokenValidationResult
            {
                IsValid = false,
                Exception = ex
            };
            return result;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return Jwt.Errors.InvalidSignature;
        }
        catch (SecurityTokenValidationException)
        {
            return Jwt.Errors.ValidationFailed;
        }
        catch (Exception)
        {
            return Jwt.Errors.ValidationError;
        }
    }

    public ErrorOr<Dictionary<string, object>> GetTokenClaims(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Jwt.Errors.EmptyToken;
        }

        try
        {
            JwtSecurityToken? jwtToken = _tokenHandler.ReadJwtToken(token);
            Dictionary<string, object> claims = jwtToken.Claims.ToDictionary(c => c.Type, c => (object)c.Value);
            return claims;
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(Jwt.Errors.InvalidFormat.Code, $"Invalid token format: {ex.Message}");
        }
        catch (Exception)
        {
            return Jwt.Errors.ClaimsExtraction;
        }
    }
}