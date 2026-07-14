namespace PrettyWoman.Infrastructure.Media;

public class R2MediaOptions
{
    public const string SectionName = "R2Media";

    public string ServiceUrl { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string PublicBucketName { get; set; } = string.Empty;
    public string PrivateBucketName { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
}
