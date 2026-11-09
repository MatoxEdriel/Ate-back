using System.ComponentModel.DataAnnotations;
using Ate.Application.Common.Constants;

namespace Ate.Infrastructure.Services;

public class LoginThrottlingOptions
{
    public const string SectionName = ConfigurationConstants.LoginThrottlingSettings.SectionName;

    /// <summary>Wrong passwords allowed for one document from one IP inside the window.</summary>
    [Range(1, 100)]
    public int MaxFailuresPerDocumentAndIp { get; set; } = 5;

    /// <summary>Wrong passwords allowed for one document from every IP together inside the window.</summary>
    [Range(1, 1000)]
    public int MaxFailuresPerDocument { get; set; } = 20;

    /// <summary>Length of the counting window; a block lasts at most this long.</summary>
    [Range(1, 1440)]
    public int WindowMinutes { get; set; } = 15;

    /// <summary>Login requests accepted per IP per minute, whatever the document.</summary>
    [Range(1, 10000)]
    public int MaxRequestsPerIpPerMinute { get; set; } = 30;
}
