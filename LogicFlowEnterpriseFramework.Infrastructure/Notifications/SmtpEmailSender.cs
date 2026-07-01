using System.Net;
using System.Net.Mail;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LogicFlowEnterpriseFramework.Infrastructure.Notifications;

public sealed class SmtpEmailSender(
    ApplicationDbContext dbContext,
    ITenantProvider tenantProvider,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<EmailTransportOptions> transportOptions) : IEmailSender
{
    private const string ProtectorPurpose = "LogicFlowEnterpriseFramework.EmailTransport.Password.v1";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
    private readonly EmailTransportOptions _transportOptions = transportOptions.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var transport = await ResolveTransportAsync(cancellationToken);

        if (!string.Equals(transport.Provider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Email provider '{transport.Provider}' is not supported for test delivery.");
        }

        if (string.IsNullOrWhiteSpace(transport.Host))
        {
            throw new InvalidOperationException("Email transport host is not configured.");
        }

        if (message.ToAddresses.Count == 0)
        {
            throw new InvalidOperationException("At least one recipient email address is required.");
        }

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(message.FromAddress, message.FromDisplayName),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true
        };

        foreach (var address in message.ToAddresses.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            mailMessage.To.Add(address.Trim());
        }

        if (mailMessage.To.Count == 0)
        {
            throw new InvalidOperationException("At least one valid recipient email address is required.");
        }

        if (!string.IsNullOrWhiteSpace(message.ReplyToAddress))
        {
            mailMessage.ReplyToList.Add(message.ReplyToAddress.Trim());
        }

        mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.TextBody, null, "text/plain"));

        using var smtpClient = new SmtpClient(transport.Host, transport.Port ?? 587)
        {
            EnableSsl = transport.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(transport.UserName))
        {
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential(transport.UserName, transport.Password ?? string.Empty);
        }

        await smtpClient.SendMailAsync(mailMessage).WaitAsync(cancellationToken);
    }

    private async Task<ResolvedEmailTransportSettings> ResolveTransportAsync(CancellationToken cancellationToken)
    {
        Guid? tenantId = tenantProvider.TenantId;

        if (!tenantId.HasValue)
        {
            tenantId = await dbContext.Tenants
                .AsNoTracking()
                .Where(x => x.Identifier == "default")
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var settings = tenantId.HasValue
            ? await dbContext.EmailSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value, cancellationToken)
            : null;

        var provider = Normalize(settings?.Provider) ?? Normalize(_transportOptions.Provider) ?? "Smtp";
        var host = Normalize(settings?.Host) ?? Normalize(_transportOptions.Host);
        var port = settings?.Port ?? _transportOptions.Port;
        var userName = Normalize(settings?.UserName) ?? Normalize(_transportOptions.UserName);
        var password = settings?.EncryptedPassword is { Length: > 0 }
            ? _protector.Unprotect(settings.EncryptedPassword)
            : Normalize(_transportOptions.Password);
        var defaultFromAddress = Normalize(settings?.DefaultFromAddress) ?? Normalize(_transportOptions.DefaultFromAddress);
        var defaultReplyToAddress = Normalize(settings?.DefaultReplyToAddress) ?? Normalize(_transportOptions.DefaultReplyToAddress);
        var hasPassword = !string.IsNullOrWhiteSpace(password);
        var isConfigured = !string.IsNullOrWhiteSpace(host)
            && port.HasValue
            && !string.IsNullOrWhiteSpace(defaultFromAddress)
            && (!string.IsNullOrWhiteSpace(userName) ? hasPassword : true);

        return new ResolvedEmailTransportSettings(
            provider,
            host,
            port,
            settings?.EnableSsl ?? _transportOptions.EnableSsl,
            userName,
            password,
            defaultFromAddress,
            defaultReplyToAddress,
            hasPassword,
            isConfigured);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
