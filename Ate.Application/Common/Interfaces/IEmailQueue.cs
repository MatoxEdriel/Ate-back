using Ate.Domain.Enums;

namespace Ate.Application.Common.Interfaces;

/// <summary>
/// Sends template emails in the background, so a request does not wait for SMTP and its
/// response time does not depend on whether an email was actually sent.
/// </summary>
public interface IEmailQueue
{
    /// <returns>False if the queue is full and the email was dropped.</returns>
    bool TryEnqueueTemplate<TModel>(string toEmail, string subject, EmailTemplateType templateType, TModel model);
}
