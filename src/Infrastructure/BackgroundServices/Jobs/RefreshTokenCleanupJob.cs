using Infrastructure.Identity.Models;

using Microsoft.EntityFrameworkCore;

using UseCases.Common.Persistence.Context;

namespace Infrastructure.BackgroundServices.Jobs;

public sealed class RefreshTokenCleanupJob(IUnitOfWork unitOfWork)
{
    public const string RecurringJobId = "refresh-token-cleanup";
    public const string CronExpression = "0 2 * * *";
    public const string Description = "Cleans up expired and revoked refresh tokens from the database.";
    public const string Tag = "security";

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var expiredTokens = await unitOfWork.Context.Set<RefreshToken>()
                .Where(rt => rt.IsExpired || rt.IsRevoked)
                .ToListAsync(cancellationToken);

            if (expiredTokens.Count > 0)
            {
                unitOfWork.Context.Set<RefreshToken>().RemoveRange(expiredTokens);

                try
                {
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    await unitOfWork.CommitTransactionAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            }
            else
            {
                await unitOfWork.CommitTransactionAsync(cancellationToken);
            }
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

