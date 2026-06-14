namespace PrettyWoman.Application.Common.Extensions;

public static class StringExtensions
{
    public static string NormalizeRequired(this string? value, string fieldName)
    {
        var normalizedValue = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            throw new ArgumentException($"{fieldName} es obligatorio.");
        }

        return normalizedValue;
    }

    public static string? NormalizeOptional(this string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
    }
}
