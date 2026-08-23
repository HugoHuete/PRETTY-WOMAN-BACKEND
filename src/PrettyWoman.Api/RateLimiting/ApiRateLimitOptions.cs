namespace PrettyWoman.Api.RateLimiting;

public sealed class ApiRateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public int LoginPermitLimit { get; init; } = 5;
    public int ReadPermitLimit { get; init; } = 120;
    public int WritePermitLimit { get; init; } = 30;
    public int ImagePermitLimit { get; init; } = 10;
    public int WindowSeconds { get; init; } = 60;
    public int SegmentsPerWindow { get; init; } = 6;

    public void Validate()
    {
        if (LoginPermitLimit <= 0 || ReadPermitLimit <= 0 || WritePermitLimit <= 0 || ImagePermitLimit <= 0)
        {
            throw new InvalidOperationException("Los límites de RateLimiting deben ser mayores que cero.");
        }

        if (WindowSeconds <= 0 || SegmentsPerWindow <= 0)
        {
            throw new InvalidOperationException("La ventana y los segmentos de RateLimiting deben ser mayores que cero.");
        }
    }
}
