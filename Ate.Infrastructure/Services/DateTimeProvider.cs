using Ate.Application.Common.Constants;
using Ate.Application.Common.Interfaces;

namespace Ate.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider, ISingletonDependency
{
    private static readonly TimeZoneInfo DefaultBusinessTimeZone = ResolveTimeZone(DateTimeConstants.DefaultBusinessTimeZoneId);

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly UtcToday => DateOnly.FromDateTime(DateTime.UtcNow);

    public DateTimeOffset BusinessNow => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, DefaultBusinessTimeZone);

    public DateTimeOffset ConvertToTimeZone(DateTimeOffset utcDateTime, string? timeZoneId = null)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.ConvertTime(utcDateTime, DefaultBusinessTimeZone);
        }

        var targetTimeZone = ResolveTimeZone(timeZoneId);
        return TimeZoneInfo.ConvertTime(utcDateTime, targetTimeZone);
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Utc;
            }
        }
    }
}
