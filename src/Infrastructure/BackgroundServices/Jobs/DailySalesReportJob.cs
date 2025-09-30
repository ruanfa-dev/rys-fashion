using Microsoft.Extensions.Logging;

using UseCases.Common.Persistence.Context;

namespace Infrastructure.BackgroundServices.Jobs;

public sealed class SalesReportData
{
    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
}

public sealed class DailySalesReportJob(IUnitOfWork unitOfWork, ILogger<DailySalesReportJob> logger)
{
    public const string RecurringJobId = "daily-sales-report";
    public const string CronExpression = "0 0 * * *"; // Every day at midnight
    public const string Description = "Generates daily sales report for the previous day.";
    public const string Tag = "sales-report";

    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting daily sales report generation");

        try
        {
            SalesReportData reportData = await GenerateDailySalesReport(cancellationToken);

            logger.LogInformation("Successfully generated daily sales report with {Orders} orders and total revenue {Revenue}", reportData.OrderCount, reportData.TotalRevenue);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during daily sales report generation");
            throw;
        }
    }

    private async Task<SalesReportData> GenerateDailySalesReport(CancellationToken cancellationToken)
    {
        // TODO: Replace with actual query logic to get order count and total revenue for previous day
        await Task.Delay(100, cancellationToken); // Placeholder
        return new SalesReportData { OrderCount = 0, TotalRevenue = 0 };
    }
}
