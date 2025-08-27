using System.Text.RegularExpressions;

using ErrorOr;

using Infrastructure.Notification.Options;

using Microsoft.Extensions.Options;

using Serilog;

using Sinch;
using Sinch.SMS.Batches.Send;

using UseCases.Common.Notification.Models;
using UseCases.Common.Notification.Services;

namespace Infrastructure.Notification.Services;

public sealed partial class SmsSenderService(IOptions<SmsOptions> smsOption, ISinchClient sinchClient) : ISmsSenderService
{
    private readonly SmsOptions _smsOption = smsOption.Value;

    public async Task<ErrorOr<Success>> AddSmsNotificationAsync(
        SmsNotificationData notificationData,
        CancellationToken cancellationToken = default)
    {
        var validationResult = notificationData.Validate();
        if (validationResult.IsError)
            return validationResult.Errors;

        foreach (var recipient in notificationData.Receivers)
        {
            if (!IsValidPhoneNumber(recipient))
                return Errors.InvalidPhoneNumber(recipient);
        }

        try
        {
            Log.Information("Sending SMS via Sinch to {Receivers} with UseCase {UseCase}",
                notificationData.Receivers, notificationData.UseCase);

            var smsApi = sinchClient.Sms;

            var response = await smsApi.Batches.Send(new SendTextBatchRequest
            {
                From = string.IsNullOrWhiteSpace(notificationData.SenderNumber)
                    ? _smsOption.SinchConfig.SenderPhoneNumber ?? _smsOption.DefaultSenderNumber
                    : notificationData.SenderNumber,
                To = notificationData.Receivers,
                Body = notificationData.Content
            }, cancellationToken);

            Log.Information("Sinch SMS batch sent. BatchId: {BatchId}", response.Id);

            return Result.Success;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send SMS via Sinch");
            return Error.Failure("SmsNotification.Failed", $"Failed to send SMS: {ex.Message}");
        }
    }

    private static bool IsValidPhoneNumber(string? phoneNumber)
    {
        return !string.IsNullOrWhiteSpace(phoneNumber)
               && PhoneFormat.IsMatch(phoneNumber);
    }

    public static class Errors
    {
        public static Error InvalidPhoneNumber(string phoneNumber) => Error.Validation(
            "SmsNotification.InvalidPhoneNumber", $"Invalid phone number: {phoneNumber}");
    }

    private static readonly Regex PhoneFormat = new(@"^\+?\d{10,15}$", RegexOptions.Compiled);
}
