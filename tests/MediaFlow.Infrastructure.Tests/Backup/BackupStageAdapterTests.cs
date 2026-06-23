using FluentAssertions;
using MediaFlow.Application.UseCases;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Enums;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Domain.ValueObjects;
using MediaFlow.Infrastructure.Backup;
using MediaFlow.Infrastructure.FileSystem;
using MediaFlow.Infrastructure.Naming;

namespace MediaFlow.Infrastructure.Tests.Backup;

public sealed class BackupStageAdapterTests : IDisposable
{
    private readonly string _backupDir =
        Path.Combine(Path.GetTempPath(), $"MediaFlow.Backup.{Guid.NewGuid():N}");
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), $"MediaFlow.Temp.{Guid.NewGuid():N}");

    private readonly FileSystemAdapter _fs = new();
    private readonly BackupStageAdapter _sut;

    public BackupStageAdapterTests()
    {
        Directory.CreateDirectory(_tempDir);

        var resolvers = new INamingTokenResolver[]
        {
            new PrefixTokenResolver(),
            new SequenceNumberTokenResolver(),
            new CurrentDateTokenResolver(),
            new PhotoDateTokenResolver()
        };
        _sut = new BackupStageAdapter(_fs, new BuildNamingTemplateUseCase(resolvers));
    }

    public void Dispose()
    {
        if (Directory.Exists(_backupDir)) Directory.Delete(_backupDir, recursive: true);
        if (Directory.Exists(_tempDir))  Directory.Delete(_tempDir,  recursive: true);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string TempFile(string name)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, name);
        return path;
    }

    private PipelineContext Ctx(
        string originalName = "photo.jpg",
        MediaAction actions = MediaAction.SaveToBackup,
        IReadOnlyList<NamingToken>? template = null,
        string? exifDate = null)
    {
        var tempPath = TempFile(originalName);
        var device = new DeviceProfile(
            "id", "Cam", @"C:\src", _backupDir,
            template ?? [], "tok", "chat", 50);
        var file = new FileContext(
            originalName, @"C:\src\" + originalName, tempPath,
            FileType.Image, actions, exifDate);
        return new PipelineContext(device, file);
    }

    // ── Skip logic ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SaveToBackupNotAssigned_ReturnsSkipped()
    {
        var ctx = Ctx(actions: MediaAction.None);

        var (_, result) = await _sut.ExecuteAsync(ctx, default);

        result.Should().BeOfType<Skipped>();
    }

    [Theory]
    [InlineData(MediaAction.DeleteAfter)]
    [InlineData(MediaAction.SendToTelegram)]
    [InlineData(MediaAction.RotateLeft | MediaAction.DeleteAfter)]
    public async Task ExecuteAsync_OtherActionsWithoutSaveToBackup_ReturnsSkipped(MediaAction actions)
    {
        var (_, result) = await _sut.ExecuteAsync(Ctx(actions: actions), default);

        result.Should().BeOfType<Skipped>();
    }

    [Fact]
    public async Task ExecuteAsync_Skipped_ReturnsSameContext()
    {
        var ctx = Ctx(actions: MediaAction.None);

        var (returnedCtx, _) = await _sut.ExecuteAsync(ctx, default);

        returnedCtx.Should().BeSameAs(ctx);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ValidContext_ReturnsSuccess()
    {
        var (_, result) = await _sut.ExecuteAsync(Ctx(), default);

        result.Should().BeOfType<Success>();
    }

    [Fact]
    public async Task ExecuteAsync_ValidContext_CopiesFileToBackupFolder()
    {
        var ctx = Ctx("photo.jpg");

        await _sut.ExecuteAsync(ctx, default);

        Directory.GetFiles(_backupDir).Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyTemplate_UsesOriginalFileName()
    {
        var ctx = Ctx("photo.jpg", template: []);

        await _sut.ExecuteAsync(ctx, default);

        Directory.GetFiles(_backupDir)
            .Select(Path.GetFileName)
            .Should().Contain("photo.jpg");
    }

    [Fact]
    public async Task ExecuteAsync_CreatesBackupFolderIfMissing()
    {
        Directory.Exists(_backupDir).Should().BeFalse();

        await _sut.ExecuteAsync(Ctx(), default);

        Directory.Exists(_backupDir).Should().BeTrue();
    }

    // ── Sequence number seeding ───────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SequenceSeededByExistingFiles()
    {
        Directory.CreateDirectory(_backupDir);
        File.WriteAllText(Path.Combine(_backupDir, "existing1.jpg"), "x");
        File.WriteAllText(Path.Combine(_backupDir, "existing2.jpg"), "x");

        var template = (IReadOnlyList<NamingToken>)new NamingToken[] { new SequenceNumberToken() };
        var ctx = Ctx("photo.jpg", template: template);

        await _sut.ExecuteAsync(ctx, default);

        // 2 existing files → sequence 3 → "0003.jpg"
        Directory.GetFiles(_backupDir)
            .Select(Path.GetFileName)
            .Should().Contain("0003.jpg");
    }

    // ── Duplicate guard ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_DestinationAlreadyExists_ReturnsFailed()
    {
        Directory.CreateDirectory(_backupDir);
        File.WriteAllText(Path.Combine(_backupDir, "photo.jpg"), "existing");

        var ctx = Ctx("photo.jpg", template: []);

        var (_, result) = await _sut.ExecuteAsync(ctx, default);

        result.Should().BeOfType<Failed>()
            .Which.Reason.Should().Contain("photo.jpg");
    }

    // ── Cancellation ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await _sut.Invoking(s => s.ExecuteAsync(Ctx(), cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }
}
