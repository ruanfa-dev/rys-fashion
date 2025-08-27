using Microsoft.Extensions.Logging;

using UseCases.Common.Persistence.Context;

namespace Infrastructure.BackgroundServices.Jobs;

public sealed class AbandonedCartEmailJob
{
    public const string RecurringJobId = "abandoned-cart-emails";
    public const string CronExpression = "0 * * * *";// Every hour at minute 0
    public const string Description = "Sends reminder emails for carts abandoned for 24+ hours.";
    public const string Tag = "abandoned-cart";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AbandonedCartEmailJob> _logger;

    public AbandonedCartEmailJob(IUnitOfWork unitOfWork, ILogger<AbandonedCartEmailJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting abandoned cart email job");

        try
        {
            var emailsSent = await SendAbandonedCartEmails(cancellationToken);

            _logger.LogInformation("Successfully sent {Count} abandoned cart emails", emailsSent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during abandoned cart email processing");
            throw;
        }
    }

    private static async Task<int> SendAbandonedCartEmails(CancellationToken cancellationToken)
    {
        // Find carts abandoned for 24+ hours and send reminder emails
        await Task.Delay(100, cancellationToken); // Placeholder
        return 0;
    }
}
