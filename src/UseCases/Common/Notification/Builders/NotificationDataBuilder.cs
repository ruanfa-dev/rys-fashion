using ErrorOr;
using static UseCases.Common.Notification.Constants.NotificationFormats;
using static UseCases.Common.Notification.Constants.NotificationParameters;
using static UseCases.Common.Notification.Constants.NotificationSendMethods;
using static UseCases.Common.Notification.Constants.NotificationUseCases;
using static UseCases.Common.Notification.Constants.NotificationPriorities;
using UseCases.Common.Notification.Models;

namespace UseCases.Common.Notification.Builders;

public static class NotificationDataBuilder
{
    private static readonly Dictionary<NotificationUseCase, NotificationTemplate> Templates = new(); // Placeholder for Templates

    public static ErrorOr<NotificationData> Create(NotificationUseCase useCase = NotificationUseCase.None)
    {
        var template = Templates.GetValueOrDefault(useCase);
        var notificationData = new NotificationData
        {
            UseCase = useCase,
            SendMethodType = GetDefaultSendMethod(useCase),
            TemplateFormatType = template?.TemplateFormatType ?? NotificationFormat.Default,
            Content = template?.TemplateContent,
            HtmlContent = template?.HtmlTemplateContent,
            Title = template?.Name,
            Values = new Dictionary<NotificationParameter, string?>(),
            Receivers = new List<string>(),
            Attachments = new List<string>()
        };

        return notificationData.Validate();
    }

    public static ErrorOr<NotificationData> WithUseCase(this NotificationData? notificationData, NotificationUseCase useCase = NotificationUseCase.None)
    {
        if (notificationData == null)
            return NotificationData.Errors.NullData;

        var template = Templates.GetValueOrDefault(useCase);
        notificationData.UseCase = useCase;
        notificationData.SendMethodType = GetDefaultSendMethod(useCase);
        notificationData.TemplateFormatType = template?.TemplateFormatType ?? NotificationFormat.Default;
        notificationData.Content = template?.TemplateContent ?? notificationData.Content;
        notificationData.HtmlContent = template?.HtmlTemplateContent ?? notificationData.HtmlContent;
        notificationData.Title = template?.Name ?? notificationData.Title;

        return notificationData;
    }

    public static ErrorOr<NotificationData> WithSendMethodType(this NotificationData? notificationData, NotificationSendMethod sendMethodType)
    {
        if (notificationData == null)
            return NotificationData.Errors.NullData;

        notificationData.SendMethodType = sendMethodType;
        return notificationData;
    }

    public static ErrorOr<NotificationData> AddParam(this NotificationData? notificationData, NotificationParameter parameter, string? value)
    {
        if (notificationData == null)
            return NotificationData.Errors.NullData;

        notificationData.Values ??= new Dictionary<NotificationParameter, string?>();
        notificationData.Values[parameter] = value;
        return notificationData;
    }

    public static ErrorOr<NotificationData> AddParams(this NotificationData? notificationData, Dictionary<NotificationParameter, string?>? values)
    {
        if (notificationData == null)
            return NotificationData.Errors.NullData;
        if (values == null)
            return NotificationData.Errors.NullParameters;

        notificationData.Values ??= new Dictionary<NotificationParameter, string?>();
        foreach (var item in values)
        {
            notificationData.Values[item.Key] = item.Value;
        }
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithReceivers(this NotificationData? notificationData, List<string>? receivers)
    {
        if (notificationData == null)
            return NotificationData.Errors.NullData;
        if (receivers == null || !receivers.Any(r => !string.IsNullOrWhiteSpace(r)))
            return notificationData;

        notificationData.Receivers ??= new List<string>();
        var uniqueReceivers = receivers.Where(r => !string.IsNullOrWhiteSpace(r) && !notificationData.Receivers.Contains(r)).ToList();
        notificationData.Receivers.AddRange(uniqueReceivers);
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithReceiver(this NotificationData? notificationData, string? receiver)
    {
        if (notificationData == null)
            return NotificationData.Errors.NullData;
        if (string.IsNullOrWhiteSpace(receiver))
            return notificationData;

        notificationData.Receivers ??= new List<string>();
        if (!notificationData.Receivers.Contains(receiver))
            notificationData.Receivers.Add(receiver);
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithTitle(this NotificationData? notificationData, string? title)
    {
        if (notificationData == null)
            return NotificationData.Errors.NullData;
        if (string.IsNullOrWhiteSpace(title))
            return NotificationData.Errors.InvalidTitle;

        notificationData.Title = title;
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithContent(this NotificationData? notificationData, string? content)
    {
        if (notificationData == null)
            return NotificationData.Errors.NullData;
        if (string.IsNullOrWhiteSpace(content))
            return NotificationData.Errors.InvalidContent;

        notificationData.Content = content;
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithHtmlContent(this NotificationData? notificationData, string? htmlContent)
    {
        if (notificationData == null)
            return NotificationData.Errors.NullData;
        if (string.IsNullOrWhiteSpace(htmlContent))
            return NotificationData.Errors.InvalidHtmlContent;

        notificationData.HtmlContent = htmlContent;
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithCreatedBy(this NotificationData? notificationData, string? createdBy)
    {
        if (notificationData == null)
            return NotificationData.Errors.NullData;
        if (string.IsNullOrWhiteSpace(createdBy))
            return NotificationData.Errors.InvalidCreatedBy;

        notificationData.CreatedBy = createdBy;
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithAttachments(this NotificationData? notificationData, List<string>? attachments)
    {
        if (notificationData == null)
            return NotificationData.Errors.NullData;
        if (attachments == null || !attachments.Any(a => !string.IsNullOrWhiteSpace(a)))
            return notificationData;

        notificationData.Attachments ??= new List<string>();
        var uniqueAttachments = attachments.Where(a => !string.IsNullOrWhiteSpace(a) && !notificationData.Attachments.Contains(a)).ToList();
        notificationData.Attachments.AddRange(uniqueAttachments);
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithPriority(this NotificationData? notificationData, NotificationPriority priority)
    {
        if (notificationData == null)
            return NotificationData.Errors.NullData;

        notificationData.Priority = priority;
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithLanguage(this NotificationData? notificationData, string? language)
    {
        if (notificationData == null)
            return NotificationData.Errors.NullData;
        if (string.IsNullOrWhiteSpace(language))
            return NotificationData.Errors.InvalidLanguage;

        notificationData.Language = language;
        return notificationData;
    }

    public static ErrorOr<NotificationData> SetCreatedBy(this NotificationData? notificationData, string? createdBy, DateTimeOffset? createAt = null)
    {
        if (notificationData == null)
            return NotificationData.Errors.NullData;
        if (string.IsNullOrWhiteSpace(createdBy))
            return NotificationData.Errors.InvalidCreatedBy;

        notificationData.CreatedBy = createdBy;
        notificationData.CreatedAt = createAt;
        return notificationData;
    }

    public static ErrorOr<NotificationData> Build(this NotificationData? notificationData)
    {
        if (notificationData == null)
            return NotificationData.Errors.NullData;

        return notificationData.Validate();
    }

    public static ErrorOr<SmsNotificationData> CreateSmsNotificationData(
        NotificationUseCase useCase,
        List<string>? receivers,
        Dictionary<NotificationParameter, string?>? parameters,
        string senderNumber = "")
    {
        if (receivers == null || !receivers.Any(r => !string.IsNullOrWhiteSpace(r)))
            return SmsNotificationData.Errors.MissingReceivers;
        if (parameters == null)
            return NotificationData.Errors.NullParameters;

        var template = Templates.GetValueOrDefault(useCase);
        var content = template?.TemplateContent ?? string.Empty;

        var smsData = new SmsNotificationData
        {
            UseCase = useCase,
            Receivers = receivers.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList(),
            Content = content,
            SenderNumber = senderNumber,
            Priority = useCase == NotificationUseCase.System2faOtp ? NotificationPriority.High : NotificationPriority.Normal
        };

        if (!string.IsNullOrWhiteSpace(content))
        {
            foreach (var param in parameters)
            {
                var placeholder = $"{{{param.Key}}}";
                smsData.Content = smsData.Content.Replace(placeholder, param.Value ?? string.Empty);
            }
        }

        return smsData.Validate();
    }

    public static ErrorOr<EmailNotificationData> CreateEmailNotificationData(
        NotificationUseCase useCase,
        List<string>? receivers,
        Dictionary<NotificationParameter, string?>? parameters)
    {
        if (receivers == null || !receivers.Any(r => !string.IsNullOrWhiteSpace(r)))
            return EmailNotificationData.Errors.MissingReceivers;
        if (parameters == null)
            return NotificationData.Errors.NullParameters;

        var template = Templates.GetValueOrDefault(useCase);
        var title = template?.Name ?? useCase.ToString();
        var content = template?.TemplateContent ?? string.Empty;
        var htmlContent = template?.HtmlTemplateContent ?? string.Empty;

        var emailData = new EmailNotificationData
        {
            UseCase = useCase,
            Receivers = receivers.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList(),
            Title = title,
            Content = content,
            HtmlContent = htmlContent,
            Attachments = new List<string>(),
            Priority = useCase == NotificationUseCase.SystemResetPassword ? NotificationPriority.High : NotificationPriority.Normal
        };

        if (!string.IsNullOrWhiteSpace(content))
        {
            foreach (var param in parameters)
            {
                var placeholder = $"{{{param.Key}}}";
                emailData.Content = emailData.Content.Replace(placeholder, param.Value ?? string.Empty);
                emailData.HtmlContent = emailData.HtmlContent?.Replace(placeholder, param.Value ?? string.Empty);
                emailData.Title = emailData.Title.Replace(placeholder, param.Value ?? string.Empty);
            }
        }

        return emailData.Validate();
    }

    public static ErrorOr<NotificationData> CreateNotificationData(
        NotificationUseCase useCase,
        List<string>? receivers,
        Dictionary<NotificationParameter, string?>? parameters)
    {
        if (receivers == null || !receivers.Any(r => !string.IsNullOrWhiteSpace(r)))
            return NotificationData.Errors.InvalidReceivers;
        if (parameters == null)
            return NotificationData.Errors.NullParameters;

        var template = Templates.GetValueOrDefault(useCase);
        var notificationData = new NotificationData
        {
            UseCase = useCase,
            SendMethodType = template?.SendMethodType ?? GetDefaultSendMethod(useCase),
            TemplateFormatType = template?.TemplateFormatType ?? NotificationFormat.Default,
            Content = template?.TemplateContent,
            HtmlContent = template?.HtmlTemplateContent,
            Title = template?.Name ?? useCase.ToString(),
            Receivers = receivers.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList(),
            Values = new Dictionary<NotificationParameter, string?>(parameters),
            Attachments = new List<string>(),
            Priority = GetDefaultPriority(useCase)
        };

        if (template?.ParamValues != null)
        {
            foreach (var requiredParam in template.ParamValues)
            {
                if (!notificationData.Values.ContainsKey(requiredParam))
                    notificationData.Values[requiredParam] = null;
            }
        }

        return notificationData.Validate();
    }

    private static NotificationSendMethod GetDefaultSendMethod(NotificationUseCase useCase)
    {
        return useCase switch
        {
            NotificationUseCase.System2faOtp => NotificationSendMethod.SMS,
            NotificationUseCase.FlashSaleNotification => NotificationSendMethod.PushNotification,
            NotificationUseCase.BackInStockNotification => NotificationSendMethod.PushNotification,
            _ => NotificationSendMethod.Email
        };
    }

    private static NotificationPriority GetDefaultPriority(NotificationUseCase useCase)
    {
        return useCase switch
        {
            NotificationUseCase.System2faOtp => NotificationPriority.High,
            NotificationUseCase.SystemResetPassword => NotificationPriority.High,
            NotificationUseCase.FlashSaleNotification => NotificationPriority.High,
            _ => NotificationPriority.Normal
        };
    }

    // Placeholder for NotificationTemplate
    private class NotificationTemplate
    {
        public NotificationSendMethod? SendMethodType { get; set; }
        public NotificationFormat? TemplateFormatType { get; set; }
        public string? TemplateContent { get; set; }
        public string? HtmlTemplateContent { get; set; }
        public string? Name { get; set; }
        public List<NotificationParameter>? ParamValues { get; set; }
    }
}