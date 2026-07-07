using System;
using Avalonia.Threading;
using MediaFlow.Domain.Interfaces;

namespace MediaFlow.Presentation.ViewModels;

public sealed class LoadProgressObserver(Action<string, int, int> onFileLoading) : ILoadProgressObserver
{
    public void FileLoading(string fileName, int index, int total) =>
        Dispatcher.UIThread.Post(() => onFileLoading(fileName, index, total));
}
