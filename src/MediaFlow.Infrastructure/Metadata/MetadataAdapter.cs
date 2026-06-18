using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MediaFlow.Domain.Enums;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Infrastructure.Metadata;

public sealed class MetadataAdapter : IMetadataReader
{
    public FileMetadata Read(string filePath)
    {
        try
        {
            var directories = ImageMetadataReader.ReadMetadata(filePath);

            var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var captureDate = subIfd?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);

            var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            var autoRotation = ifd0 is not null && ifd0.TryGetInt32(ExifIfd0Directory.TagOrientation, out var raw)
                ? MapOrientation(raw)
                : null;

            return new FileMetadata(captureDate, autoRotation);
        }
        catch
        {
            // File has no EXIF, is corrupt, or format is unsupported — treat as no metadata.
            return new FileMetadata(null, null);
        }
    }

    private static MediaAction? MapOrientation(int orientation) => orientation switch
    {
        3 => MediaAction.Flip180,
        6 => MediaAction.RotateRight,  // 90° CW
        8 => MediaAction.RotateLeft,   // 90° CCW
        _ => null                      // normal (1) or flip variants (2,4,5,7) — unsupported
    };
}
