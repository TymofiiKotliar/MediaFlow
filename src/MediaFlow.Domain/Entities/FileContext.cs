using MediaFlow.Domain.Enums;

namespace MediaFlow.Domain.Entities;

public sealed record FileContext(
    string OriginalName,
    string SourcePath,
    string TempPath,
    FileType Type,
    MediaAction AssignedActions,
    string? ExifCaptureDate
);
