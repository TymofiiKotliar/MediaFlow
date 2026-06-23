using MediaFlow.Domain.Enums;

namespace MediaFlow.Domain.ValueObjects;

public sealed record FileMetadata(
    string? CaptureDate,      // EXIF DateTimeOriginal: "YYYY:MM:DD HH:MM:SS", or null
    MediaAction? AutoRotation // derived from EXIF orientation tag, or null if absent/unsupported
);
