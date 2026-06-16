using FluentAssertions;
using LiteDB;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.ValueObjects;
using MediaFlow.Infrastructure.Persistence;

namespace MediaFlow.Infrastructure.Tests.Persistence;

public sealed class DeviceRepositoryAdapterTests : IDisposable
{
    private readonly ILiteDatabase _db = new LiteDatabase(":memory:");
    private readonly DeviceRepositoryAdapter _sut;

    public DeviceRepositoryAdapterTests() => _sut = new DeviceRepositoryAdapter(_db);

    public void Dispose() => _db.Dispose();

    // ── helpers ───────────────────────────────────────────────────────────────

    private static DeviceProfile Profile(
        string id = "id-1",
        string name = "Canon R5",
        IReadOnlyList<NamingToken>? template = null) => new(
            Id: id,
            Name: name,
            SourceFolderPath: @"C:\Source",
            BackupFolderPath: @"C:\Backup",
            NamingTemplate: template ?? [],
            TelegramBotToken: "token",
            TelegramChatId: "123456",
            FilesPerLoad: 100);

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_AfterSavingProfiles_ReturnsAllOfThem()
    {
        await _sut.SaveAsync(Profile("a", "Alpha"));
        await _sut.SaveAsync(Profile("b", "Beta"));

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        result.Select(p => p.Id).Should().BeEquivalentTo(["a", "b"]);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsProfile()
    {
        var original = Profile("id-1");
        await _sut.SaveAsync(original);

        var result = await _sut.GetByIdAsync("id-1");

        result.Should().NotBeNull();
        result!.Id.Should().Be("id-1");
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync("does-not-exist");

        result.Should().BeNull();
    }

    // ── SaveAsync (insert + update) ───────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_NewProfile_PersistsAllFields()
    {
        var profile = Profile("id-1", "Sony A7");

        await _sut.SaveAsync(profile);
        var retrieved = await _sut.GetByIdAsync("id-1");

        retrieved.Should().BeEquivalentTo(profile);
    }

    [Fact]
    public async Task SaveAsync_ExistingProfile_UpdatesInPlace()
    {
        await _sut.SaveAsync(Profile("id-1", "Old Name"));
        await _sut.SaveAsync(Profile("id-1", "New Name"));

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("New Name");
    }

    // ── NamingTemplate round-trip ─────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_NamingTemplateWithAllTokenTypes_RoundTripsCorrectly()
    {
        IReadOnlyList<NamingToken> template =
        [
            new PrefixToken("Holiday-"),
            new SequenceNumberToken(),
            new CurrentDateToken(),
            new PhotoDateToken()
        ];
        await _sut.SaveAsync(Profile("id-1", template: template));

        var retrieved = await _sut.GetByIdAsync("id-1");

        retrieved!.NamingTemplate.Should().HaveCount(4);
        retrieved.NamingTemplate[0].Should().BeOfType<PrefixToken>()
            .Which.Text.Should().Be("Holiday-");
        retrieved.NamingTemplate[1].Should().BeOfType<SequenceNumberToken>();
        retrieved.NamingTemplate[2].Should().BeOfType<CurrentDateToken>();
        retrieved.NamingTemplate[3].Should().BeOfType<PhotoDateToken>();
    }

    [Fact]
    public async Task SaveAsync_EmptyNamingTemplate_RoundTripsCorrectly()
    {
        await _sut.SaveAsync(Profile("id-1", template: []));

        var retrieved = await _sut.GetByIdAsync("id-1");

        retrieved!.NamingTemplate.Should().BeEmpty();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingProfile_RemovesIt()
    {
        await _sut.SaveAsync(Profile("id-1"));

        await _sut.DeleteAsync("id-1");

        var result = await _sut.GetByIdAsync("id-1");
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ExistingProfile_DoesNotAffectOtherProfiles()
    {
        await _sut.SaveAsync(Profile("a"));
        await _sut.SaveAsync(Profile("b"));

        await _sut.DeleteAsync("a");

        var result = await _sut.GetAllAsync();
        result.Should().ContainSingle(p => p.Id == "b");
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_DoesNotThrow()
    {
        var act = () => _sut.DeleteAsync("ghost");

        await act.Should().NotThrowAsync();
    }
}
