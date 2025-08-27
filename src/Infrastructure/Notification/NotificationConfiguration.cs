using System.Net;
using System.Net.Mail;

using Ardalis.GuardClauses;

using Infrastructure.Notification.Options;
using Infrastructure.Notification.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Sinch;

using UseCases.Common.Notification.Services;

namespace Infrastructure.Notification;

public static class NotificationConfiguration
{
    public static IServiceCollection AddNotificationServices(this IServiceCollection services, IConfiguration configuration, Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment)
    {
        var emailSection = configuration.GetSection(SmtpOptions.Section);
        var smtpOptions = emailSection.Get<SmtpOptions>();
        Guard.Against.Null(smtpOptions, message: "SmtpOptions configuration section is missing or invalid.");

        var smsSection = configuration.GetSection(SmsOptions.Section);
        var smsOptions = smsSection.Get<SmsOptions>();
        Guard.Against.Null(smsOptions, message: "SmsOptions configuration section is missing or invalid.");

        AddEmailNotificationServices(services, smtpOptions);
        AddSmsNotificationServices(services, smsOptions);

        return services;
    }

    private static void AddEmailNotificationServices(IServiceCollection services, SmtpOptions smtpOptions)
    {
        if (smtpOptions.EnableEmailNotifications)
        {
            switch (smtpOptions.Provider.ToLowerInvariant())
            {
                case "papercut":
                case "smtp":
                    Guard.Against.Null(smtpOptions.SmtpConfig, message: "SmtpConfig is required for SMTP provider.");

                    var smtpClient = new SmtpClient(smtpOptions.SmtpConfig.Host, smtpOptions.SmtpConfig.Port)
                    {
                        EnableSsl = smtpOptions.SmtpConfig.EnableSsl,
                        UseDefaultCredentials = smtpOptions.SmtpConfig.UseDefaultCredentials,
                        Credentials = smtpOptions.SmtpConfig.UseDefaultCredentials
                            ? CredentialCache.DefaultNetworkCredentials
                            : new NetworkCredential(smtpOptions.SmtpConfig.Username, smtpOptions.SmtpConfig.Password)
                    };

                    services.AddFluentEmail(smtpOptions.FromEmail, smtpOptions.FromName)
                            .AddSmtpSender(smtpClient);
                    break;

                case "sendgrid":
                    Guard.Against.Null(smtpOptions.SendGridConfig, message: "SendGridConfig is required for SendGrid provider.");

                    services.AddFluentEmail(smtpOptions.FromEmail, smtpOptions.FromName)
                            .AddSendGridSender(smtpOptions.SendGridConfig.ApiKey);
                    break;

                default:
                    services.AddSingleton<IEmailSenderService, EmptyEmailSenderService>();
                    break;
            }

            services.AddSingleton<IEmailSenderService, EmailSenderService>();
        }
        else
        {
            services.AddSingleton<IEmailSenderService, EmptyEmailSenderService>();
        }
    }

    private static void AddSmsNotificationServices(IServiceCollection services, SmsOptions smsOptions)
    {
        if (smsOptions.EnableSmsNotifications)
        {
            services.AddSingleton<ISmsSenderService, SmsSenderService>();
            services.AddSingleton<ISinchClient>(sp =>
            {
                var sinchOption = smsOptions.SinchConfig;

                return new SinchClient(sinchOption.ProjectId, sinchOption.KeyId, keySecret: sinchOption.KeySecret, options =>
                {
                    if (!string.IsNullOrWhiteSpace(sinchOption.SmsRegion))
                    {
                        options.SmsRegion = sinchOption.SmsRegion.Trim().ToLower() switch
                        {
                            "us" => Sinch.SMS.SmsRegion.Us,
                            "eu" => Sinch.SMS.SmsRegion.Eu,
                            _ => options.SmsRegion
                        };
                    }
                });
            });
        }
        else
        {
            services.AddSingleton<ISmsSenderService, EmptySmsSenderService>();
        }
    }
}