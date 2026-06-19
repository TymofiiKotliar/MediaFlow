using FluentAssertions;
using MediaFlow.Domain.ValueObjects;
using MediaFlow.Infrastructure.Naming;

namespace MediaFlow.Infrastructure.Tests.Naming;

public sealed class NamingTokenResolverTests
{
    // ── PrefixTokenResolver ───────────────────────────────────────────────────

    private readonly PrefixTokenResolver _prefix = new();

    [Fact]
    public void Prefix_CanResolve_PrefixToken() =>
        _prefix.CanResolve(new PrefixToken("IMG_")).Should().BeTrue();

    [Theory]
    [MemberData(nameof(NonPrefixTokens))]
    public void Prefix_CannotResolve_OtherTokens(NamingToken token) =>
        _prefix.CanResolve(token).Should().BeFalse();

    [Fact]
    public void Prefix_Resolve_ReturnsText() =>
        _prefix.Resolve(new PrefixToken("Holiday-"), null, 1).Should().Be("Holiday-");

    [Fact]
    public void Prefix_Resolve_EmptyText_ReturnsEmpty() =>
        _prefix.Resolve(new PrefixToken(""), null, 1).Should().Be("");

    // ── SequenceNumberTokenResolver ───────────────────────────────────────────

    private readonly SequenceNumberTokenResolver _seq = new();

    [Fact]
    public void Sequence_CanResolve_SequenceNumberToken() =>
        _seq.CanResolve(new SequenceNumberToken()).Should().BeTrue();

    [Theory]
    [MemberData(nameof(NonSequenceTokens))]
    public void Sequence_CannotResolve_OtherTokens(NamingToken token) =>
        _seq.CanResolve(token).Should().BeFalse();

    [Theory]
    [InlineData(1,    "0001")]
    [InlineData(42,   "0042")]
    [InlineData(999,  "0999")]
    [InlineData(9999, "9999")]
    public void Sequence_Resolve_ReturnsFourDigitPaddedNumber(int n, string expected) =>
        _seq.Resolve(new SequenceNumberToken(), null, n).Should().Be(expected);

    // ── CurrentDateTokenResolver ──────────────────────────────────────────────

    private readonly CurrentDateTokenResolver _today = new();

    [Fact]
    public void CurrentDate_CanResolve_CurrentDateToken() =>
        _today.CanResolve(new CurrentDateToken()).Should().BeTrue();

    [Theory]
    [MemberData(nameof(NonCurrentDateTokens))]
    public void CurrentDate_CannotResolve_OtherTokens(NamingToken token) =>
        _today.CanResolve(token).Should().BeFalse();

    [Fact]
    public void CurrentDate_Resolve_ReturnsYyyyMmDd()
    {
        var result = _today.Resolve(new CurrentDateToken(), null, 0);

        result.Should().Be(DateTime.Today.ToString("yyyy-MM-dd"));
        result.Should().HaveLength(10).And.MatchRegex(@"^\d{4}-\d{2}-\d{2}$");
    }

    // ── PhotoDateTokenResolver ────────────────────────────────────────────────

    private readonly PhotoDateTokenResolver _photo = new();

    [Fact]
    public void PhotoDate_CanResolve_PhotoDateToken() =>
        _photo.CanResolve(new PhotoDateToken()).Should().BeTrue();

    [Theory]
    [MemberData(nameof(NonPhotoDateTokens))]
    public void PhotoDate_CannotResolve_OtherTokens(NamingToken token) =>
        _photo.CanResolve(token).Should().BeFalse();

    [Fact]
    public void PhotoDate_Resolve_ValidExifDate_ReturnsYyyyMmDd() =>
        _photo.Resolve(new PhotoDateToken(), "2024:03:15 10:30:00", 0).Should().Be("2024-03-15");

    [Fact]
    public void PhotoDate_Resolve_NullExifDate_ReturnsEmpty() =>
        _photo.Resolve(new PhotoDateToken(), null, 0).Should().Be("");

    [Theory]
    [InlineData("2024-03-15")]
    [InlineData("not-a-date")]
    [InlineData("")]
    public void PhotoDate_Resolve_MalformedDate_ReturnsEmpty(string badDate) =>
        _photo.Resolve(new PhotoDateToken(), badDate, 0).Should().Be("");

    // ── MemberData helpers ────────────────────────────────────────────────────

    public static IEnumerable<object[]> NonPrefixTokens() =>
    [
        [new SequenceNumberToken()],
        [new CurrentDateToken()],
        [new PhotoDateToken()]
    ];

    public static IEnumerable<object[]> NonSequenceTokens() =>
    [
        [new PrefixToken("x")],
        [new CurrentDateToken()],
        [new PhotoDateToken()]
    ];

    public static IEnumerable<object[]> NonCurrentDateTokens() =>
    [
        [new PrefixToken("x")],
        [new SequenceNumberToken()],
        [new PhotoDateToken()]
    ];

    public static IEnumerable<object[]> NonPhotoDateTokens() =>
    [
        [new PrefixToken("x")],
        [new SequenceNumberToken()],
        [new CurrentDateToken()]
    ];
}
