using System;
using Avalonia.Threading;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Interfaces;

namespace MediaFlow.Presentation.ViewModels;

public sealed class PipelineProgressObserver : IProgressObserver
{
    private readonly Action<string, int, int> _onFileStarted;
    private readonly Action<RunSummary> _onFinished;
    private readonly Action _onCancelled;

    public PipelineProgressObserver(
        Action<string, int, int> onFileStarted,
        Action<RunSummary> onFinished,
        Action onCancelled)
    {
        _onFileStarted = onFileStarted;
        _onFinished = onFinished;
        _onCancelled = onCancelled;
    }

    public void FileStarted(string fileName, int index, int total) =>
        Dispatcher.UIThread.Post(() => _onFileStarted(fileName, index, total));

    public void FileCompleted(string fileName) { }

    public void FileFailed(string fileName, string reason) { }

    public void PipelineCancelled() =>
        Dispatcher.UIThread.Post(_onCancelled);

    public void PipelineFinished(RunSummary summary) =>
        Dispatcher.UIThread.Post(() => _onFinished(summary));
}
