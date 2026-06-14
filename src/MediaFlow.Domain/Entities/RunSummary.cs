namespace MediaFlow.Domain.Entities;

public sealed record RunSummary(
    int BackedUp,
    int SentToTelegram,
    int Deleted,
    int DuplicatesSkipped,
    int Failed,
    IReadOnlyList<FailedFile> FailedFiles
);

public sealed record FailedFile(string FileName, string Reason);
