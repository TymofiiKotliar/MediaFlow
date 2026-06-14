namespace MediaFlow.Domain.Enums;

[Flags]
public enum MediaAction
{
    None           = 0,
    RotateLeft     = 1 << 0,
    RotateRight    = 1 << 1,
    Flip180        = 1 << 2,
    SaveToBackup   = 1 << 3,
    SendToTelegram = 1 << 4,
    DeleteAfter    = 1 << 5
}
