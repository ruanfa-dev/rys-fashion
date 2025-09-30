using UseCases.Common.Notification.Constants;

namespace UseCases.Common.Notification.Models;

public static class NotificationDataMapper
{
    /// <summary>
    /// Maps a NotificationData instance to an SmsNotificationData instance.
    /// </summary>
    /// <param name="notificationData">The NotificationData instance to map.</param>
    /// <returns>An SmsNotificationData instance with mapped properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown if notificationData is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if required fields are missing or invalid.</exception>
    public static SmsNotificationData ToSmsNotificationData(this NotificationData notificationData)
    {
        if (notificationData == null)
            throw new ArgumentNullException(nameof(notificationData));

        // Fetch template for default content if needed
        NotificationUseCases.TemplateDescription? template = NotificationUseCases.Templates[notificationData.UseCase];
        string content = notificationData.Content ?? template?.TemplateContent ?? string.Empty;

        // Replace placeholders with parameter values
        if (!string.IsNullOrWhiteSpace(content) && notificationData.Values != null)
        {
            foreach (KeyValuePair<NotificationParameters.NotificationParameter, string?> param in notificationData.Values)
            {
                string placeholder = $"{{{param.Key}}}";
                content = content.Replace(placeholder, param.Value ?? string.Empty);
            }
        }

        SmsNotificationData smsData = new SmsNotificationData
        {
            UseCase = notificationData.UseCase,
            Receivers = notificationData.Receivers?.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList() ?? new List<string>(),
            Content = content,
            CreatedBy = notificationData.CreatedBy,
            CreatedAt = notificationData.CreatedAt,
            Priority = notificationData.Priority,
            Language = notificationData.Language
        };

        // Validate the resulting SMS data
        smsData.Validate();
        return smsData;
    }

    /// <summary>
    /// Maps a NotificationData instance to an EmailNotificationData instance.
    /// </summary>
    /// <param name="notificationData">The NotificationData instance to map.</param>
    /// <returns>An EmailNotificationData instance with mapped properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown if notificationData is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if required fields are missing or invalid.</exception>
    public static EmailNotificationData ToEmailNotificationData(this NotificationData notificationData)
    {
        if (notificationData == null)
            throw new ArgumentNullException(nameof(notificationData));

        // Fetch template for default content if needed
        NotificationUseCases.TemplateDescription? template = NotificationUseCases.Templates[notificationData.UseCase];
        string title = notificationData.Title ?? template?.Name ?? notificationData.UseCase.ToString();
        string content = notificationData.Content ?? template?.TemplateContent ?? string.Empty;
        string? htmlContent = notificationData.HtmlContent ?? template?.HtmlTemplateContent ?? string.Empty;

        // Replace placeholders with parameter values
        if (notificationData.Values != null)
        {
            foreach (KeyValuePair<NotificationParameters.NotificationParameter, string?> param in notificationData.Values)
            {
                string placeholder = $"{{{param.Key}}}";
                title = title.Replace(placeholder, param.Value ?? string.Empty);
                content = content.Replace(placeholder, param.Value ?? string.Empty);
                htmlContent = htmlContent?.Replace(placeholder, param.Value ?? string.Empty);
            }
        }

        EmailNotificationData emailData = new EmailNotificationData
        {
            UseCase = notificationData.UseCase,
            Receivers = notificationData.Receivers?.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList() ?? new List<string>(),
            Title = title,
            Content = content,
            HtmlContent = string.IsNullOrWhiteSpace(htmlContent) ? null : htmlContent,
            CreatedBy = notificationData.CreatedBy,
            CreatedAt = notificationData.CreatedAt,
            Attachments = notificationData.Attachments?.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList() ?? new List<string>(),
            Priority = notificationData.Priority,
            Language = notificationData.Language
        };

        // Validate the resulting Email data
        emailData.Validate();
        return emailData;
    }
}
