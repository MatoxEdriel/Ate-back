using System.ComponentModel.DataAnnotations;
using Ate.Application.Common.Constants;

namespace Ate.Infrastructure.Services;

public class SmtpOptions
{
    public const string SectionName = ConfigurationConstants.SmtpSettings.SectionName;

    [Required(ErrorMessage = "SMTP Host is required.")]
    public string Host { get; set; } = "smtp.gmail.com";

    [Range(1, 65535, ErrorMessage = "SMTP Port must be between 1 and 65535.")]
    public int Port { get; set; } = 587;

    [Required(ErrorMessage = "SMTP UserName is required.")]
    [EmailAddress(ErrorMessage = "SMTP UserName must be a valid email.")]
    public string UserName { get; set; } = "ga99buggi@gmail.com";

    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "SMTP SenderEmail is required.")]
    [EmailAddress(ErrorMessage = "SMTP SenderEmail must be a valid email.")]
    public string SenderEmail { get; set; } = "ga99buggi@gmail.com";

    public string SenderName { get; set; } = "Ate Platform";

    public bool EnableSsl { get; set; } = true;

    [EmailAddress(ErrorMessage = "SMTP SupportEmail must be a valid email.")]
    public string SupportEmail { get; set; } = "ga99buggi@gmail.com";

    [EmailAddress(ErrorMessage = "SMTP DefaultCcEmail must be a valid email.")]
    public string? DefaultCcEmail { get; set; }
}
