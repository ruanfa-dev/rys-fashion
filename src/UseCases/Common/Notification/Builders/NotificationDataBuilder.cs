using ErrorOr;

using UseCases.Common.Notification.Constants;
using UseCases.Common.Notification.Models;

using static UseCases.Common.Notification.Constants.NotificationFormats;
using static UseCases.Common.Notification.Constants.NotificationParameters;
using static UseCases.Common.Notification.Constants.NotificationPriorities;
using static UseCases.Common.Notification.Constants.NotificationSendMethods;
using static UseCases.Common.Notification.Constants.NotificationUseCases;

namespace UseCases.Common.Notification.Builders;

public static class NotificationDataBuilder
{
    private static readonly Dictionary<NotificationUseCase, TemplateDescription> Templates = NotificationUseCases.Templates;

    public static ErrorOr<NotificationData> WithUseCase(NotificationUseCase useCase = NotificationUseCase.None)
    {
        TemplateDescription? template = Templates.GetValueOrDefault(useCase);
        NotificationData notificationData = new NotificationData
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

        return notificationData;
    }

    public static ErrorOr<NotificationData> WithUseCase(this ErrorOr<NotificationData> result, NotificationUseCase useCase = NotificationUseCase.None)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        TemplateDescription? template = Templates.GetValueOrDefault(useCase);
        notificationData.UseCase = useCase;
        notificationData.SendMethodType = GetDefaultSendMethod(useCase);
        notificationData.TemplateFormatType = template?.TemplateFormatType ?? NotificationFormat.Default;
        notificationData.Content = template?.TemplateContent ?? notificationData.Content;
        notificationData.HtmlContent = template?.HtmlTemplateContent ?? notificationData.HtmlContent;
        notificationData.Title = template?.Name ?? notificationData.Title;

        return notificationData;
    }

    public static ErrorOr<NotificationData> WithSendMethodType(this ErrorOr<NotificationData> result, NotificationSendMethod sendMethodType)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        notificationData.SendMethodType = sendMethodType;

        return notificationData;
    }

    public static ErrorOr<NotificationData> AddParam(this ErrorOr<NotificationData> result, NotificationParameter parameter, string? value)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        notificationData.Values ??= new Dictionary<NotificationParameter, string?>();
        notificationData.Values[parameter] = value;

        return notificationData;
    }

    public static ErrorOr<NotificationData> AddParams(this ErrorOr<NotificationData> result, Dictionary<NotificationParameter, string?>? values)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (values == null)
            return NotificationData.Errors.NullParameters;

        notificationData.Values ??= new Dictionary<NotificationParameter, string?>();
        foreach (KeyValuePair<NotificationParameter, string?> item in values)
        {
            notificationData.Values[item.Key] = item.Value;
        }
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithReceivers(this ErrorOr<NotificationData> result, List<string>? receivers)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (receivers == null || !receivers.Any(r => !string.IsNullOrWhiteSpace(r)))
            return notificationData;

        notificationData.Receivers ??= new List<string>();
        List<string> uniqueReceivers = receivers.Where(r => !string.IsNullOrWhiteSpace(r) && !notificationData.Receivers.Contains(r)).ToList();
        notificationData.Receivers.AddRange(uniqueReceivers);
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithReceiver(this ErrorOr<NotificationData> result, string? receiver)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (string.IsNullOrWhiteSpace(receiver))
            return notificationData;

        notificationData.Receivers ??= new List<string>();
        if (!notificationData.Receivers.Contains(receiver))
            notificationData.Receivers.Add(receiver);
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithTitle(this ErrorOr<NotificationData> result, string? title)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (string.IsNullOrWhiteSpace(title))
            return NotificationData.Errors.InvalidTitle;

        notificationData.Title = title;
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithContent(this ErrorOr<NotificationData> result, string? content)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (string.IsNullOrWhiteSpace(content))
            return NotificationData.Errors.InvalidContent;

        notificationData.Content = content;
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithHtmlContent(this ErrorOr<NotificationData> result, string? htmlContent)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (string.IsNullOrWhiteSpace(htmlContent))
            return NotificationData.Errors.InvalidHtmlContent;

        notificationData.HtmlContent = htmlContent;
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithCreatedBy(this ErrorOr<NotificationData> result, string? createdBy)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (string.IsNullOrWhiteSpace(createdBy))
            return NotificationData.Errors.InvalidCreatedBy;

        notificationData.CreatedBy = createdBy;
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithAttachments(this ErrorOr<NotificationData> result, List<string>? attachments)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (attachments == null || !attachments.Any(a => !string.IsNullOrWhiteSpace(a)))
            return notificationData;

        notificationData.Attachments ??= new List<string>();
        List<string> uniqueAttachments = attachments.Where(a => !string.IsNullOrWhiteSpace(a) && !notificationData.Attachments.Contains(a)).ToList();
        notificationData.Attachments.AddRange(uniqueAttachments);
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithPriority(this ErrorOr<NotificationData> result, NotificationPriority priority)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        notificationData.Priority = priority;
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithLanguage(this ErrorOr<NotificationData> result, string? language)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (string.IsNullOrWhiteSpace(language))
            return NotificationData.Errors.InvalidLanguage;

        notificationData.Language = language;
        return notificationData;
    }

    public static ErrorOr<NotificationData> SetCreatedBy(this ErrorOr<NotificationData> result, string? createdBy, DateTimeOffset? createAt = null)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (string.IsNullOrWhiteSpace(createdBy))
            return NotificationData.Errors.InvalidCreatedBy;

        notificationData.CreatedBy = createdBy;
        notificationData.CreatedAt = createAt;
        return notificationData;
    }

    public static ErrorOr<NotificationData> Build(this ErrorOr<NotificationData> result)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

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

        TemplateDescription? template = Templates.GetValueOrDefault(useCase);
        string content = template?.TemplateContent ?? string.Empty;

        SmsNotificationData smsData = new SmsNotificationData
        {
            UseCase = useCase,
            Receivers = receivers.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList(),
            Content = content,
            SenderNumber = senderNumber,
            Priority = useCase == NotificationUseCase.System2faOtp ? NotificationPriority.High : NotificationPriority.Normal
        };

        if (!string.IsNullOrWhiteSpace(content))
        {
            foreach (KeyValuePair<NotificationParameter, string?> param in parameters)
            {
                string placeholder = $"{{{param.Key}}}";
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

        TemplateDescription? template = Templates.GetValueOrDefault(useCase);
        string title = template?.Name ?? useCase.ToString();
        string content = template?.TemplateContent ?? string.Empty;
        string htmlContent = template?.HtmlTemplateContent ?? string.Empty;

        EmailNotificationData emailData = new EmailNotificationData
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
            foreach (KeyValuePair<NotificationParameter, string?> param in parameters)
            {
                string placeholder = $"{{{param.Key}}}";
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

        TemplateDescription? template = Templates.GetValueOrDefault(useCase);
        NotificationData notificationData = new NotificationData
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
            foreach (NotificationParameter requiredParam in template.ParamValues)
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
}