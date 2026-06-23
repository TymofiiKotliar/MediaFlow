using System.Globalization;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Infrastructure.Naming;

public sealed class PrefixTokenResolver : INamingTokenResolver
{
    public bool CanResolve(NamingToken token) => token is PrefixToken;

    public string Resolve(NamingToken token, string? exifCaptureDate, int sequenceNumber)
        => ((PrefixToken)token).Text;
}

public sealed class SequenceNumberTokenResolver : INamingTokenResolver
{
    public bool CanResolve(NamingToken token) => token is SequenceNumberToken;

    public string Resolve(NamingToken token, string? exifCaptureDate, int sequenceNumber)
        => sequenceNumber.ToString("D4");
}

public sealed class CurrentDateTokenResolver : INamingTokenResolver
{
    public bool CanResolve(NamingToken token) => token is CurrentDateToken;

    public string Resolve(NamingToken token, string? exifCaptureDate, int sequenceNumber)
        => DateTime.Today.ToString("yyyy-MM-dd");
}

public sealed class PhotoDateTokenResolver : INamingTokenResolver
{
    private const string ExifDateFormat = "yyyy:MM:dd HH:mm:ss";

    public bool CanResolve(NamingToken token) => token is PhotoDateToken;

    public string Resolve(NamingToken token, string? exifCaptureDate, int sequenceNumber)
    {
        if (exifCaptureDate is null) return string.Empty;

        return DateTime.TryParseExact(
            exifCaptureDate, ExifDateFormat,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date.ToString("yyyy-MM-dd")
                : string.Empty;
    }
}
