using ErrorOr;

using Microsoft.Extensions.Logging;

using UseCases.Common.Persistence.Context;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace Infrastructure.BackgroundServices.Jobs;

public sealed class RefreshTokenCleanupJob(
    IRefreshTokenService refreshTokenService,
    IUnitOfWork unitOfWork,
    ILogger<RefreshTokenCleanupJob> logger)
{
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly ILogger<RefreshTokenCleanupJob> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public const string RecurringJobId = "refresh-token-cleanup";
    public const string CronExpression = "0 2 * * *"; // Every day at 2 AM
    public const string Description = "Cleans up expired and revoked refresh tokens.";
    public const string Tag = "security";

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting refresh token cleanup job at {Time}", DateTimeOffset.UtcNow);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            ErrorOr<int> result = await _refreshTokenService.CleanupExpiredTokensAsync(cancellationToken);

            if (result.IsError)
            {
                _logger.LogError("Failed to clean up refresh tokens: {Errors}",
                    string.Join(", ", result.Errors.Select(e => $"{e.Code}:{e.Description}")));

                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return; // don’t throw, just log — otherwise Hangfire/Quartz might retry endlessly
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully cleaned up {Count} expired/invalid refresh tokens",
                result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token cleanup job failed unexpectedly");
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
        }
    }
}
