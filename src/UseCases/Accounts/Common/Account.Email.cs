using System.Text;
using System.Text.Encodings.Web;

using Ardalis.GuardClauses;

using Core.Identity;
using Core.Identity.Users;

using ErrorOr;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

using UseCases.Common.Notification.Builders;
using UseCases.Common.Notification.Constants;
using UseCases.Common.Notification.Services;
using UseCases.Common.Systems.Options;

namespace UseCases.Accounts.Common;
public static partial class Account
{
    public static async Task<ErrorOr<Success>> GenerateAndSendConfirmationEmailAsync(
        this UserManager<User> userManager,
        INotificationService notificationService,
        IConfiguration configuration,
        User user,
        string? newEmail = null,
        string? clientUri = null,
        CancellationToken cancellationToken = default)
    {
        // Generate confirmation token
        string code = !string.IsNullOrWhiteSpace(newEmail)
            ? await userManager.GenerateChangeEmailTokenAsync(user, newEmail)
            : await userManager.GenerateEmailConfirmationTokenAsync(user);

        // Encode token for URL
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        string userId = await userManager.GetUserIdAsync(user);

        // Prepare route values
        var routeValues = new List<KeyValuePair<string, string?>>
        {
            new("userId", userId),
            new("code", code)
        };

        if (!string.IsNullOrWhiteSpace(newEmail))
        {
            routeValues.Add(new("changedEmail", newEmail));
        }

        // Check: front-end client URI fallback
        StorefrontOption? storefrontOption = configuration.GetSection(StorefrontOption.Section).Get<StorefrontOption>();
        if (storefrontOption == null)
            return Error.Validation("Auth.StorefrontOptionNotFound", "Storefront options not found in configuration.");
        string baseUrl = clientUri ?? storefrontOption.BaseUrl;

        // Generate: confirmation URL
        var confirmEmailUrl = $"{baseUrl}/confirm-email?{QueryString.Create(routeValues)}";

        // Determine: target email
        string? email = newEmail ?? user.Email;
        Guard.Against.NullOrWhiteSpace(email, nameof(email), "Email cannot be null or empty.");

        // Prepare notification
        var notificationDataResult = NotificationDataBuilder
            .WithUseCase(NotificationUseCases.NotificationUseCase.SystemActiveEmail)
            .AddParam(NotificationParameters.NotificationParameter.SystemName, storefrontOption.SystemName)
            .AddParam(NotificationParameters.NotificationParameter.SupportEmail, storefrontOption.SupportEmail)
            .AddParam(NotificationParameters.NotificationParameter.ActiveUrl, HtmlEncoder.Default.Encode(confirmEmailUrl))
            .AddParam(NotificationParameters.NotificationParameter.UserName, user.UserName)
            .WithReceivers([email])
            .Build();

        if (notificationDataResult.IsError)
            return notificationDataResult.Errors;

        // Send notification
        return await notificationService.AddNotificationAsync(notificationDataResult.Value, cancellationToken);
    }
}
