using ErrorOr;

using FluentEmail.Core;

using Infrastructure.Notification.Options;

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

using Serilog;

using UseCases.Common.Notification.Models;
using UseCases.Common.Notification.Services;

namespace Infrastructure.Notification.Services;

public sealed class EmailSenderService(
    IOptions<SmtpOptions> emailSettings,
    IFluentEmail fluentEmail)
    : IEmailSenderService
{
    private readonly SmtpOptions _emailOption = emailSettings.Value;

    public async Task<ErrorOr<Success>> AddEmailNotificationAsync(EmailNotificationData notificationData,
        CancellationToken cancellationToken = default)
    {
        var validationResult = notificationData.Validate();
        if (validationResult.IsError)
            return validationResult.Errors;

        foreach (var recipient in notificationData.Receivers)
        {
            if (!IsValidEmail(recipient))
                return Errors.InvalidEmail(recipient);
        }

        if (notificationData.Attachments.Count != 0)
        {
            var maxSizeInBytes = _emailOption.MaxAttachmentSize ?? 25 * 1024 * 1024; // Default to 25MB
            var missingAttachments = notificationData.Attachments.Where(a => !File.Exists(a)).ToList();
            if (missingAttachments.Any())
                return Errors.InvalidAttachments(missingAttachments);

            foreach (var attachment in notificationData.Attachments)
            {
                var fileInfo = new FileInfo(attachment);
                if (fileInfo.Length > maxSizeInBytes)
                    return Errors.AttachmentSize(attachment, maxSizeInBytes);
            }
        }

        var email = fluentEmail
            .SetFrom(_emailOption.FromEmail, _emailOption.FromName)
            .To(notificationData.Receivers.Select(m => new FluentEmail.Core.Models.Address(m)))
            .Subject(notificationData.Title)
            .PlaintextAlternativeBody(notificationData.Content)
            .Body(notificationData.HtmlContent, isHtml: true);

        if (notificationData.Attachments.Count != 0)
        {
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            foreach (var attachmentPath in notificationData.Attachments)
            {
                var attachmentBytes = await File.ReadAllBytesAsync(attachmentPath, cancellationToken);
                email.Attach(new FluentEmail.Core.Models.Attachment
                {
                    Filename = Path.GetFileName(attachmentPath),
                    Data = new MemoryStream(attachmentBytes),
                    ContentType = contentTypeProvider.TryGetContentType(attachmentPath, out var contentType)
                        ? contentType
                        : "application/octet-stream"
                });
            }
        }

        Log.Information("Sending email notification with UseCase: {UseCase}, Priority: {Priority}, Language: {Language} to {Receivers}",
            notificationData.UseCase, notificationData.Priority, notificationData.Language, notificationData.Receivers);

        var sendResult = await email.SendAsync(cancellationToken);
        if (!sendResult.Successful)
        {
            Log.Error("Failed to send email notification. Errors: {Errors}", sendResult.ErrorMessages);
            return Errors.SendFailed(sendResult.ErrorMessages);
        }

        Log.Information("Email notification sent successfully to {Receivers}", notificationData.Receivers);
        return Result.Success;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public static class Errors
    {
        public static Error InvalidEmail(string email) => Error.Validation(
            "EmailNotification.InvalidEmail", $"Invalid email address: {email}");

        public static Error InvalidAttachments(List<string> missingAttachments) => Error.Validation(
            "EmailNotification.InvalidAttachments", $"The following attachments were not found: {string.Join(", ", missingAttachments)}");

        public static Error AttachmentSize(string attachment, long maxSizeInBytes) => Error.Validation(
            "EmailNotification.AttachmentSize", $"Attachment {attachment} exceeds the maximum size of {maxSizeInBytes / 1024 / 1024}MB.");

        public static Error SendFailed(IList<string> errorMessages) => Error.Unexpected(
            "EmailNotification.SendFailed", $"Failed to send email: {string.Join(", ", errorMessages)}");
    }
}