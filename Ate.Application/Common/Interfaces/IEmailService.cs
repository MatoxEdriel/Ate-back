using Ate.Domain.Enums;

namespace Ate.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = true,
        IEnumerable<string>? ccEmails = null,
        CancellationToken cancellationToken = default);

    Task SendTemplateEmailAsync<TModel>(
        string toEmail,
        string subject,
        EmailTemplateType templateType,
        TModel model,
        IEnumerable<string>? ccEmails = null,
        CancellationToken cancellationToken = default);
}
