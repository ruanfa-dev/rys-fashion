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
    public static async Task<ErrorOr<Success>> GenerateAndSendPasswordResetCodeAsync(
      this UserManager<User> userManager,
      INotificationService notificationService,
      IConfiguration configuration,
      User user,
      string? clientUri = null,
      CancellationToken cancellationToken = default)
    {
        var resetCode = await userManager.GeneratePasswordResetTokenAsync(user);

        // Encode reset code for URL
        var encodedResetCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetCode));
        string userId = await userManager.GetUserIdAsync(user);

        // Prepare route values
        var routeValues = new List<KeyValuePair<string, string?>>
        {
            new("userId", userId),
            new("code", encodedResetCode)
        };

        // Check: front-end client URI fallback
        StorefrontOption? storefrontOption = configuration.GetSection(StorefrontOption.Section).Get<StorefrontOption>();
        if (storefrontOption == null)
            return Error.Validation("Auth.StorefrontOptionNotFound", "Storefront options not found in configuration.");
        string baseUrl = clientUri ?? storefrontOption.BaseUrl;

        // Generate reset password URL
        var resetPasswordUrl = $"{baseUrl}/reset-password?{QueryString.Create(routeValues)}";

        // Determine: target email
        string? email = user.Email;
        Guard.Against.NullOrWhiteSpace(email, nameof(email), "Email cannot be null or empty.");

        // Prepare: notification
        var notificationDataResult = NotificationDataBuilder
            .WithUseCase(NotificationUseCases.NotificationUseCase.SystemResetPassword)
            .AddParam(NotificationParameters.NotificationParameter.SystemName, storefrontOption.SystemName)
            .AddParam(NotificationParameters.NotificationParameter.SupportEmail, storefrontOption.SupportEmail)
            .AddParam(NotificationParameters.NotificationParameter.ActiveUrl, HtmlEncoder.Default.Encode(resetPasswordUrl))
            .AddParam(NotificationParameters.NotificationParameter.UserName, user.UserName)
            .WithReceivers([email])
            .Build();

        if (notificationDataResult.IsError)
            return notificationDataResult.Errors;

        // Send: notification
        return await notificationService.AddNotificationAsync(notificationDataResult.Value, cancellationToken);
    }
}
