using PrettyWoman.Infrastructure.Media;

namespace PrettyWoman.Infrastructure.Tests.Media;

public class R2MediaObjectStorageTests
{
    [Fact]
    public void CreateUploadRequest_DisablesStreamingPayloadSigningAndDefaultChecksumValidation()
    {
        using var content = new MemoryStream([1, 2, 3]);

        var request = R2MediaObjectStorage.CreateUploadRequest(
            "public-bucket",
            "products/3/image.webp",
            content,
            "image/webp");

        Assert.Equal("public-bucket", request.BucketName);
        Assert.Equal("products/3/image.webp", request.Key);
        Assert.Same(content, request.InputStream);
        Assert.Equal("image/webp", request.ContentType);
        Assert.False(request.AutoCloseStream);
        Assert.True(request.DisablePayloadSigning);
        Assert.True(request.DisableDefaultChecksumValidation);
    }
}
