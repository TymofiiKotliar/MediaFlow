using FluentAssertions;
using MediaFlow.Application.Tests.Fixtures;
using MediaFlow.Application.UseCases;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Enums;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Domain.ValueObjects;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MediaFlow.Application.Tests.UseCases;

public class RunPipelineUseCaseTests
{
    private readonly IVideoConversionStage _conversionStage = Substitute.For<IVideoConversionStage>();
    private readonly IRotationStage _rotationStage = Substitute.For<IRotationStage>();
    private readonly IBackupStage _backupStage = Substitute.For<IBackupStage>();
    private readonly ITelegramStage _telegramStage = Substitute.For<ITelegramStage>();
    private readonly IFileService _fileService = Substitute.For<IFileService>();
    private readonly IProgressObserver _observer = Substitute.For<IProgressObserver>();
    private readonly RunPipelineUseCase _sut;
    private readonly DeviceProfile _device = DeviceFixtures.ExistingProfile();

    public RunPipelineUseCaseTests()
    {
        _sut = new RunPipelineUseCase(
            _conversionStage, _rotationStage, _backupStage, _telegramStage, _fileService);

        PassThrough(_conversionStage);
        PassThrough(_rotationStage);
        PassThrough(_backupStage);
        PassThrough(_telegramStage);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static void PassThrough(IPipelineStage stage) =>
        stage.ExecuteAsync(Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult((call.Arg<PipelineContext>(), (PipelineStageResult)new Success())));

    private static void SetupResult(IPipelineStage stage, PipelineStageResult result) =>
        stage.ExecuteAsync(Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult((call.Arg<PipelineContext>(), result)));

    private Task<RunSummary> Run(params FileContext[] files) =>
        _sut.ExecuteAsync(files, _device, _observer);

    // ── stage selection ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ImageFile_DoesNotCallConversionStage()
    {
        await Run(DeviceFixtures.ImageFile());

        await _conversionStage.DidNotReceive().ExecuteAsync(Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_VideoFile_CallsConversionStage()
    {
        await Run(DeviceFixtures.VideoFile());

        await _conversionStage.Received(1).ExecuteAsync(Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MediaAction.RotateLeft)]
    [InlineData(MediaAction.RotateRight)]
    [InlineData(MediaAction.Flip180)]
    public async Task ExecuteAsync_RotationFlagSet_CallsRotationStage(MediaAction action)
    {
        await Run(DeviceFixtures.ImageFile(actions: action));

        await _rotationStage.Received(1).ExecuteAsync(Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_NoRotationFlags_DoesNotCallRotationStage()
    {
        await Run(DeviceFixtures.ImageFile(actions: MediaAction.SaveToBackup));

        await _rotationStage.DidNotReceive().ExecuteAsync(Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SaveToBackupSet_CallsBackupStage()
    {
        await Run(DeviceFixtures.ImageFile(actions: MediaAction.SaveToBackup));

        await _backupStage.Received(1).ExecuteAsync(Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SaveToBackupNotSet_DoesNotCallBackupStage()
    {
        await Run(DeviceFixtures.ImageFile());

        await _backupStage.DidNotReceive().ExecuteAsync(Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SendToTelegramSet_CallsTelegramStage()
    {
        await Run(DeviceFixtures.ImageFile(actions: MediaAction.SendToTelegram));

        await _telegramStage.Received(1).ExecuteAsync(Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SendToTelegramNotSet_DoesNotCallTelegramStage()
    {
        await Run(DeviceFixtures.ImageFile());

        await _telegramStage.DidNotReceive().ExecuteAsync(Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>());
    }

    // ── summary counters ──────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_NoFiles_ReturnsAllZeroSummary()
    {
        var summary = await Run();

        summary.BackedUp.Should().Be(0);
        summary.SentToTelegram.Should().Be(0);
        summary.Deleted.Should().Be(0);
        summary.DuplicatesSkipped.Should().Be(0);
        summary.Failed.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_BackupSucceeds_IncrementsBackedUp()
    {
        var summary = await Run(DeviceFixtures.ImageFile(actions: MediaAction.SaveToBackup));

        summary.BackedUp.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_BackupSkipped_IncrementsDuplicatesSkipped()
    {
        SetupResult(_backupStage, new Skipped("duplicate"));

        var summary = await Run(DeviceFixtures.ImageFile(actions: MediaAction.SaveToBackup));

        summary.DuplicatesSkipped.Should().Be(1);
        summary.BackedUp.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_BackupFailed_IncrementsFailedCount()
    {
        SetupResult(_backupStage, new Failed("disk full"));

        var summary = await Run(DeviceFixtures.ImageFile(actions: MediaAction.SaveToBackup));

        summary.Failed.Should().Be(1);
        summary.FailedFiles.Should().ContainSingle(f => f.FileName == "photo.jpg");
    }

    [Fact]
    public async Task ExecuteAsync_TelegramSucceeds_IncrementsSentToTelegram()
    {
        var summary = await Run(DeviceFixtures.ImageFile(actions: MediaAction.SendToTelegram));

        summary.SentToTelegram.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_TelegramFailed_IncrementsFailedCount()
    {
        SetupResult(_telegramStage, new Failed("network error"));

        var summary = await Run(DeviceFixtures.ImageFile(actions: MediaAction.SendToTelegram));

        summary.Failed.Should().Be(1);
        summary.FailedFiles.Should().ContainSingle(f => f.Reason.Contains("Telegram"));
    }

    // ── deletion ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_DeleteAfterWithNoFailures_CallsFileServiceDelete()
    {
        var file = DeviceFixtures.ImageFile(actions: MediaAction.DeleteAfter);

        await Run(file);

        await _fileService.Received(1).DeleteAsync(file.SourcePath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_DeleteAfterWithNoFailures_IncrementsDeleted()
    {
        var summary = await Run(DeviceFixtures.ImageFile(actions: MediaAction.DeleteAfter));

        summary.Deleted.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_DeleteAfterWhenStageFailed_DoesNotDelete()
    {
        SetupResult(_backupStage, new Failed("error"));
        var file = DeviceFixtures.ImageFile(actions: MediaAction.SaveToBackup | MediaAction.DeleteAfter);

        await Run(file);

        await _fileService.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_FileServiceThrowsOnDelete_RecordsFailure()
    {
        _fileService.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new IOException("locked"));
        var file = DeviceFixtures.ImageFile(actions: MediaAction.DeleteAfter);

        var summary = await Run(file);

        summary.Failed.Should().Be(1);
        summary.FailedFiles.Should().ContainSingle(f => f.Reason.Contains("Delete"));
    }

    // ── multi-failure collapse ────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_MultipleFailuresOnSameFile_CollapseToSingleFailedFileEntry()
    {
        SetupResult(_backupStage, new Failed("disk full"));
        SetupResult(_telegramStage, new Failed("network error"));
        var file = DeviceFixtures.ImageFile(
            actions: MediaAction.SaveToBackup | MediaAction.SendToTelegram);

        var summary = await Run(file);

        summary.Failed.Should().Be(1);
        summary.FailedFiles.Should().ContainSingle()
            .Which.Reason.Should().Contain("Backup").And.Contain("Telegram");
    }

    // ── observer events ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_FiresFileStartedAndFileCompletedForEachFile()
    {
        await Run(DeviceFixtures.ImageFile("a.jpg"), DeviceFixtures.ImageFile("b.jpg"));

        _observer.Received(1).FileStarted("a.jpg", 1, 2);
        _observer.Received(1).FileStarted("b.jpg", 2, 2);
        _observer.Received(1).FileCompleted("a.jpg");
        _observer.Received(1).FileCompleted("b.jpg");
    }

    [Fact]
    public async Task ExecuteAsync_FiresPipelineFinishedWithCorrectSummary()
    {
        var summary = await Run(DeviceFixtures.ImageFile(actions: MediaAction.SaveToBackup));

        _observer.Received(1).PipelineFinished(Arg.Is<RunSummary>(s => s.BackedUp == 1));
    }

    // ── cancellation ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_CancelledToken_CallsPipelineCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await _sut.ExecuteAsync(
            [DeviceFixtures.ImageFile(), DeviceFixtures.ImageFile()],
            _device, _observer, cts.Token);

        _observer.Received(1).PipelineCancelled();
    }

    [Fact]
    public async Task ExecuteAsync_CancelledToken_ProcessesNoFiles()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await _sut.ExecuteAsync([DeviceFixtures.ImageFile()], _device, _observer, cts.Token);

        _observer.DidNotReceive().FileStarted(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>());
    }
}
