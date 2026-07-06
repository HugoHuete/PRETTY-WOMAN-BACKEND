namespace PrettyWoman.Application.Common.Extensions;

public static class DateTimeExtensions
{
    public static DateTime NormalizeToUtc(this DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };
    }

    public static DateTime? NormalizeToUtc(this DateTime? dateTime)
    {
        return dateTime?.NormalizeToUtc();
    }
}