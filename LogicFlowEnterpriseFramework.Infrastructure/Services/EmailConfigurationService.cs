using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Domain.Entities;
using LogicFlowEnterpriseFramework.Infrastructure.Notifications;
using LogicFlowEnterpriseFramework.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class EmailConfigurationService(
    ApplicationDbContext dbContext,
    ITenantProvider tenantProvider,
    IEmailSender emailSender,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<EmailTransportOptions> transportOptions) : IEmailConfigurationService
{
    private const string ProtectorPurpose = "LogicFlowEnterpriseFramework.EmailTransport.Password.v1";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
    private readonly EmailTransportOptions _transportOptions = transportOptions.Value;

    public async Task<EmailTransportConfigurationResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        var settings = await dbContext.EmailSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        return MapResponse(settings);
    }

    public async Task<EmailTransportConfigurationResponse> UpsertAsync(UpsertEmailTransportConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);

        var settings = await dbContext.EmailSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (settings is null)
        {
            settings = new EmailSettings
            {
                TenantId = tenantId
            };
            await dbContext.EmailSettings.AddAsync(settings, cancellationToken);
        }

        settings.Provider = string.IsNullOrWhiteSpace(request.Provider) ? "Smtp" : request.Provider.Trim();
        settings.Host = Normalize(request.Host);
        settings.Port = request.Port;
        settings.EnableSsl = request.EnableSsl;
        settings.UserName = Normalize(request.UserName);
        settings.DefaultFromAddress = Normalize(request.DefaultFromAddress);
        settings.DefaultReplyToAddress = Normalize(request.DefaultReplyToAddress);

        if (request.ClearStoredPassword)
        {
            settings.EncryptedPassword = null;
        }
        else if (!string.IsNullOrWhiteSpace(request.Password))
        {
            settings.EncryptedPassword = _protector.Protect(request.Password.Trim());
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapResponse(settings);
    }

    public async Task<SendTestEmailResponse> SendTestAsync(SendTestEmailRequest request, CancellationToken cancellationToken = default)
    {
        var transport = await ResolveTransportAsync(cancellationToken);

        if (!transport.IsConfigured || string.IsNullOrWhiteSpace(transport.DefaultFromAddress))
        {
            throw new InvalidOperationException("Complete the SMTP transport setup first before sending a test email.");
        }

        var recipientEmail = request.RecipientEmail.Trim();
        var subject = "Platform SMTP transport test";
        var sentAt = DateTimeOffset.UtcNow;

        var htmlBody = $"""
            <html>
            <body style="font-family:Segoe UI,Arial,sans-serif;color:#111827;">
                <h2>Platform SMTP transport test</h2>
                <p>This message verifies the shared SMTP transport configured for the platform.</p>
                <table style="border-collapse:collapse;">
                    <tr><td style="padding:4px 12px 4px 0;"><strong>Provider</strong></td><td>{System.Net.WebUtility.HtmlEncode(transport.Provider)}</td></tr>
                    <tr><td style="padding:4px 12px 4px 0;"><strong>Host</strong></td><td>{System.Net.WebUtility.HtmlEncode(transport.Host ?? "Not set")}</td></tr>
                    <tr><td style="padding:4px 12px 4px 0;"><strong>Port</strong></td><td>{transport.Port?.ToString() ?? "Not set"}</td></tr>
                    <tr><td style="padding:4px 12px 4px 0;"><strong>Default From</strong></td><td>{System.Net.WebUtility.HtmlEncode(transport.DefaultFromAddress)}</td></tr>
                    <tr><td style="padding:4px 12px 4px 0;"><strong>Reply-To</strong></td><td>{System.Net.WebUtility.HtmlEncode(transport.DefaultReplyToAddress ?? "Not set")}</td></tr>
                    <tr><td style="padding:4px 12px 4px 0;"><strong>Timestamp</strong></td><td>{sentAt:yyyy-MM-dd HH:mm:ss 'UTC'}</td></tr>
                </table>
            </body>
            </html>
            """;

        var textBody = $"""
            Platform SMTP transport test

            This message verifies the shared SMTP transport configured for the platform.

            Provider: {transport.Provider}
            Host: {transport.Host ?? "Not set"}
            Port: {transport.Port?.ToString() ?? "Not set"}
            Default From: {transport.DefaultFromAddress}
            Reply-To: {transport.DefaultReplyToAddress ?? "Not set"}
            Timestamp: {sentAt:yyyy-MM-dd HH:mm:ss UTC}
            """;

        await emailSender.SendAsync(
            new EmailMessage(
                transport.DefaultFromAddress,
                "LogicFlow Platform",
                [recipientEmail],
                subject,
                htmlBody,
                textBody,
                transport.DefaultReplyToAddress),
            cancellationToken);

        return new SendTestEmailResponse(recipientEmail, subject, transport.DefaultFromAddress, sentAt);
    }

    private async Task<Guid> ResolveTenantIdAsync(CancellationToken cancellationToken)
    {
        if (tenantProvider.TenantId.HasValue)
        {
            return tenantProvider.TenantId.Value;
        }

        return await dbContext.Tenants
            .AsNoTracking()
            .Where(x => x.Identifier == "default")
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);
    }

    private async Task<ResolvedEmailTransportSettings> ResolveTransportAsync(CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(cancellationToken);
        var settings = await dbContext.EmailSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

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

    private EmailTransportConfigurationResponse MapResponse(EmailSettings? settings)
    {
        var provider = Normalize(settings?.Provider) ?? Normalize(_transportOptions.Provider) ?? "Smtp";
        var host = Normalize(settings?.Host) ?? Normalize(_transportOptions.Host);
        var port = settings?.Port ?? _transportOptions.Port;
        var userName = Normalize(settings?.UserName) ?? Normalize(_transportOptions.UserName);
        var hasPassword = !string.IsNullOrWhiteSpace(settings?.EncryptedPassword) || !string.IsNullOrWhiteSpace(_transportOptions.Password);
        var defaultFromAddress = Normalize(settings?.DefaultFromAddress) ?? Normalize(_transportOptions.DefaultFromAddress);
        var defaultReplyToAddress = Normalize(settings?.DefaultReplyToAddress) ?? Normalize(_transportOptions.DefaultReplyToAddress);
        var isConfigured = !string.IsNullOrWhiteSpace(host)
            && port.HasValue
            && !string.IsNullOrWhiteSpace(defaultFromAddress)
            && (!string.IsNullOrWhiteSpace(userName) ? hasPassword : true);

        return new EmailTransportConfigurationResponse(
            provider,
            host,
            port,
            settings?.EnableSsl ?? _transportOptions.EnableSsl,
            userName,
            hasPassword,
            defaultFromAddress,
            defaultReplyToAddress,
            isConfigured,
            settings?.UpdatedAt ?? settings?.CreatedAt,
            settings?.UpdatedBy ?? settings?.CreatedBy);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
