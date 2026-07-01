namespace LogicFlowEnterpriseFramework.Application.DTOs;

public sealed record EmailMessage(
    string FromAddress,
    string FromDisplayName,
    IReadOnlyCollection<string> ToAddresses,
    string Subject,
    string HtmlBody,
    string TextBody,
    string? ReplyToAddress = null);
