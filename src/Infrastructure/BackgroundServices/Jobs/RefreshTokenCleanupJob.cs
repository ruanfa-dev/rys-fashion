using ErrorOr;

using Microsoft.Extensions.Logging;

using UseCases.Common.Persistence.Context;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace Infrastructure.BackgroundServices.Jobs;

public sealed class RefreshTokenCleanupJob
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshTokenCleanupJob> _logger;

    public const string RecurringJobId = "refresh-token-cleanup";
    public const string CronExpression = "0 2 * * *"; // Runs daily at 2 AM
    public const string Description = "Cleans up expired and revoked refresh tokens from the database.";
    public const string Tag = "security";

    public RefreshTokenCleanupJob(
        IRefreshTokenService refreshTokenService,
        IUnitOfWork unitOfWork,
        ILogger<RefreshTokenCleanupJob> logger)
    {
        _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting refresh token cleanup job at {Time}", DateTimeOffset.UtcNow);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            ErrorOr<int> result = await _refreshTokenService.CleanupExpiredTokensAsync(cancellationToken);

            if (result.IsError)
            {
                _logger.LogError("Failed to clean up refresh tokens: {Errors}", string.Join(", ", result.Errors));
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw new InvalidOperationException($"Cleanup failed: {string.Join(", ", result.Errors)}");
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            _logger.LogInformation("Successfully cleaned up {Count} refresh tokens", result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token cleanup job failed");
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}