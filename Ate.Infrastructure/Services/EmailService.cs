using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using Ate.Application.Common.Interfaces;
using Ate.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scriban;

namespace Ate.Infrastructure.Services;

public class EmailService : IEmailService, IScopedDependency
{
    private static readonly ConcurrentDictionary<string, string> TemplateCache = new();
    private readonly SmtpOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpOptions> options, ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = true,
        IEnumerable<string>? ccEmails = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Password))
        {
            _logger.LogWarning("SMTP Password is not configured. Email to {ToEmail} with subject '{Subject}' was skipped.", toEmail, subject);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.SenderEmail, _options.SenderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };
        message.To.Add(toEmail);

        if (ccEmails != null)
        {
            foreach (var cc in ccEmails)
            {
                if (!string.IsNullOrWhiteSpace(cc))
                {
                    message.CC.Add(cc.Trim());
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(_options.DefaultCcEmail) &&
            !message.CC.Any(c => string.Equals(c.Address, _options.DefaultCcEmail, StringComparison.OrdinalIgnoreCase)))
        {
            message.CC.Add(_options.DefaultCcEmail.Trim());
        }

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.UserName, _options.Password)
        };

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }

    public async Task SendTemplateEmailAsync<TModel>(
        string toEmail,
        string subject,
        EmailTemplateType templateType,
        TModel model,
        IEnumerable<string>? ccEmails = null,
        CancellationToken cancellationToken = default)
    {
        var renderedHtml = await RenderTemplateAsync(templateType, subject, model);
        await SendEmailAsync(toEmail, subject, renderedHtml, true, ccEmails, cancellationToken);
    }

    private async Task<string> RenderTemplateAsync<TModel>(EmailTemplateType templateType, string subject, TModel model)
    {
        var templateName = $"{templateType}.html";
        var innerTemplateSource = await GetTemplateSourceAsync(templateName);
        var layoutSource = await GetTemplateSourceAsync("_Layout.html");

        var innerTemplate = Template.Parse(innerTemplateSource);
        var innerContent = await innerTemplate.RenderAsync(model);

        var layoutTemplate = Template.Parse(layoutSource);
        var layoutModel = new
        {
            subject,
            content = innerContent,
            year = DateTime.UtcNow.Year,
            support_email = _options.SupportEmail ?? _options.SenderEmail
        };

        return await layoutTemplate.RenderAsync(layoutModel);
    }

    private static async Task<string> GetTemplateSourceAsync(string templateFileName)
    {
        if (TemplateCache.TryGetValue(templateFileName, out var cachedSource))
        {
            return cachedSource;
        }

        var assembly = typeof(EmailService).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(templateFileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName != null)
        {
            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                TemplateCache.TryAdd(templateFileName, content);
                return content;
            }
        }

        var filePath = Path.Combine(AppContext.BaseDirectory, "Templates", "Emails", templateFileName);
        if (File.Exists(filePath))
        {
            var content = await File.ReadAllTextAsync(filePath);
            TemplateCache.TryAdd(templateFileName, content);
            return content;
        }

        throw new FileNotFoundException($"Email template '{templateFileName}' was not found as embedded resource or file.");
    }
}
