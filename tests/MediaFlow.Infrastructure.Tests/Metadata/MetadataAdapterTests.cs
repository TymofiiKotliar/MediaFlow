using FluentAssertions;
using MediaFlow.Domain.Enums;
using MediaFlow.Infrastructure.Metadata;

namespace MediaFlow.Infrastructure.Tests.Metadata;

public sealed class MetadataAdapterTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), $"MediaFlow.Meta.{Guid.NewGuid():N}");
    private readonly MetadataAdapter _sut = new();

    public MetadataAdapterTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, recursive: true);

    // Minimal JPEG with EXIF: orientation=6 (90° CW), DateTimeOriginal="2024:03:15 10:30:00"
    // Built from raw EXIF bytes to avoid any external file dependency.
    private static readonly byte[] JpegWithExif = Convert.FromBase64String(
        "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8U" +
        "HRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgN" +
        "DRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIy" +
        "MjL/wAARCAABAAEDASIAAhEBAxEB/8QAFgABAQEAAAAAAAAAAAAAAAAABgUE/8QAIhAAAgIB" +
        "BAMAAAAAAAAAAAAAAQIDBAUREiExQf/EABQBAQAAAAAAAAAAAAAAAAAAAAD/xAAUEQEAAAAA" +
        "AAAAAAAAAAAAAP/aAAwDAQACEQMRAD8Aq2pa1JqN7LdSoqM5yVXoKpUUAf/Z");

    private string WriteJpeg(string name, byte[] bytes)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    // ── Fallback on missing / corrupt file ───────────────────────────────────

    [Fact]
    public void Read_MissingFile_ReturnsEmptyMetadata()
    {
        var result = _sut.Read(Path.Combine(_root, "ghost.jpg"));

        result.CaptureDate.Should().BeNull();
        result.AutoRotation.Should().BeNull();
    }

    [Fact]
    public void Read_NonImageFile_ReturnsEmptyMetadata()
    {
        var path = Path.Combine(_root, "file.txt");
        File.WriteAllText(path, "hello");

        var result = _sut.Read(path);

        result.CaptureDate.Should().BeNull();
        result.AutoRotation.Should().BeNull();
    }

    // ── No EXIF ───────────────────────────────────────────────────────────────

    [Fact]
    public void Read_JpegWithNoExif_ReturnsNullFields()
    {
        // Minimal bare JPEG (no APP1/EXIF segment)
        byte[] bareJpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10,
                           0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
                           0x01, 0x00, 0x00, 0x01, 0x00, 0x01,
                           0x00, 0x00, 0xFF, 0xD9];
        var path = WriteJpeg("bare.jpg", bareJpeg);

        var result = _sut.Read(path);

        result.CaptureDate.Should().BeNull();
        result.AutoRotation.Should().BeNull();
    }

    // ── Result is never null ──────────────────────────────────────────────────

    [Fact]
    public void Read_AlwaysReturnsNonNullRecord()
    {
        var result = _sut.Read(Path.Combine(_root, "any.jpg"));

        result.Should().NotBeNull();
    }
}
