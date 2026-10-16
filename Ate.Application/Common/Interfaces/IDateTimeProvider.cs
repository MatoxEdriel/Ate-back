namespace Ate.Application.Common.Interfaces;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
    DateOnly UtcToday { get; }
    DateTimeOffset BusinessNow { get; }
    DateTimeOffset ConvertToTimeZone(DateTimeOffset utcDateTime, string? timeZoneId = null);
}
