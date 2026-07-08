namespace PrettyWoman.Application.Common.Extensions;

public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo BusinessTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Managua");

    public static DateTime NormalizeToUtc(this DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => TimeZoneInfo.ConvertTimeToUtc(dateTime, BusinessTimeZone)
        };
    }

    public static DateTime? NormalizeToUtc(this DateTime? dateTime)
    {
        return dateTime?.NormalizeToUtc();
    }
}