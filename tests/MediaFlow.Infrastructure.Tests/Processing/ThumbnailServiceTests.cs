using FluentAssertions;
using MediaFlow.Infrastructure.Processing;

namespace MediaFlow.Infrastructure.Tests.Processing;

public sealed class ThumbnailServiceTests
{
    private readonly ThumbnailService _sut = new();

    [Theory]
    [InlineData(".txt")]
    [InlineData(".raw")]
    [InlineData(".pdf")]
    [InlineData("")]
    public void GenerateAsync_UnsupportedExtension_ThrowsNotSupported(string ext)
    {
        Action act = () => { _ = _sut.GenerateAsync($"file{ext}"); };

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Unsupported extension*");
    }

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    [InlineData(".mp4")]
    [InlineData(".avi")]
    [InlineData(".mov")]
    public void GenerateAsync_SupportedExtension_DoesNotThrowSynchronously(string ext)
    {
        // The dispatch doesn't throw — FFMpeg work happens inside the returned Task.
        // Full processing requires FFMpeg installed (integration test).
        Action act = () => { _ = _sut.GenerateAsync($"C:\\file{ext}"); };

        act.Should().NotThrow<NotSupportedException>();
    }
}
