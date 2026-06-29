using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using MediaFlow.Application.UseCases;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Enums;
using MediaFlow.Domain.Interfaces;
using ReactiveUI;

namespace MediaFlow.Presentation.ViewModels;

public sealed class MediaBrowserViewModel : ViewModelBase
{
    private enum FilterMode { All, Images, Videos }
    private enum SortMode  { Desc, Asc, None }

    private readonly LoadMediaUseCase _loadMedia;
    private readonly RunPipelineUseCase _runPipeline;
    private readonly IThumbnailService _thumbnails;

    private DeviceProfile? _device;
    private bool _isLoading;
    private bool _hasMore = true;
    private string? _loadError;
    private int _offset;
    private FilterMode _filterMode = FilterMode.All;
    private SortMode   _sortModeDate   = SortMode.Desc;
    private SortMode   _sortModeName   = SortMode.Desc;
    private SortMode   _sortModeSize   = SortMode.Desc;
    private CancellationTokenSource _cts = new();

    private bool _isPipelineRunning;
    private string _currentFileName = "";
    private int _currentIndex;
    private int _totalFiles;
    private RunSummary? _lastRunSummary;
    private CancellationTokenSource _pipelineCts = new();
    private bool _wasRunCancelled;
    private bool _currentFileIsVideo;
    private bool _currentFileHasRotation;
    private bool _currentFileHasBackup;
    private bool _currentFileHasTelegram;
    private bool _currentFileHasDelete;

    // Raw list — all loaded files regardless of filter
    public ObservableCollection<FileRowViewModel> Files { get; } = [];

    // Display list — filtered subset bound by the view
    public ObservableCollection<FileRowViewModel> FilteredFiles { get; } = [];

    public string DeviceName => _device?.Name ?? "";
    public string SourcePath => _device?.SourceFolderPath ?? "";

    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public bool HasMore
    {
        get => _hasMore;
        private set => this.RaiseAndSetIfChanged(ref _hasMore, value);
    }

    public string? LoadError
    {
        get => _loadError;
        private set
        {
            this.RaiseAndSetIfChanged(ref _loadError, value);
            this.RaisePropertyChanged(nameof(HasLoadError));
        }
    }

    public bool HasLoadError => _loadError is not null;

    public bool IsPipelineRunning
    {
        get => _isPipelineRunning;
        private set => this.RaiseAndSetIfChanged(ref _isPipelineRunning, value);
    }

    public string CurrentFileName
    {
        get => _currentFileName;
        private set => this.RaiseAndSetIfChanged(ref _currentFileName, value);
    }

    public int CurrentIndex
    {
        get => _currentIndex;
        private set
        {
            this.RaiseAndSetIfChanged(ref _currentIndex, value);
            this.RaisePropertyChanged(nameof(ProgressPercent));
            this.RaisePropertyChanged(nameof(ProgressText));
            this.RaisePropertyChanged(nameof(ProgressPercentText));
        }
    }

    public int TotalFiles
    {
        get => _totalFiles;
        private set
        {
            this.RaiseAndSetIfChanged(ref _totalFiles, value);
            this.RaisePropertyChanged(nameof(PipelineSubtitle));
        }
    }

    public RunSummary? LastRunSummary
    {
        get => _lastRunSummary;
        private set
        {
            this.RaiseAndSetIfChanged(ref _lastRunSummary, value);
            this.RaisePropertyChanged(nameof(HasSummary));
            this.RaisePropertyChanged(nameof(SummaryHasFailures));
            this.RaisePropertyChanged(nameof(SummaryTitle));
        }
    }

    public bool HasSummary         => _lastRunSummary is not null;
    public bool SummaryHasFailures => _lastRunSummary?.Failed > 0;
    public string SummaryTitle     => _wasRunCancelled ? "Pipeline cancelled" : "Pipeline complete";

    public bool CurrentFileIsVideo
    {
        get => _currentFileIsVideo;
        private set => this.RaiseAndSetIfChanged(ref _currentFileIsVideo, value);
    }

    public bool CurrentFileHasRotation
    {
        get => _currentFileHasRotation;
        private set => this.RaiseAndSetIfChanged(ref _currentFileHasRotation, value);
    }

    public bool CurrentFileHasBackup
    {
        get => _currentFileHasBackup;
        private set => this.RaiseAndSetIfChanged(ref _currentFileHasBackup, value);
    }

    public bool CurrentFileHasTelegram
    {
        get => _currentFileHasTelegram;
        private set => this.RaiseAndSetIfChanged(ref _currentFileHasTelegram, value);
    }

    public bool CurrentFileHasDelete
    {
        get => _currentFileHasDelete;
        private set => this.RaiseAndSetIfChanged(ref _currentFileHasDelete, value);
    }

    public double ProgressPercent => TotalFiles == 0 ? 0 : (double)CurrentIndex / TotalFiles * 100;
    public string ProgressText => $"{CurrentIndex} of {TotalFiles}";
    public string ProgressPercentText => $"{(int)ProgressPercent}%";
    public string PipelineSubtitle => $"{DeviceName} · {TotalFiles} file{(TotalFiles == 1 ? "" : "s")}";

    public string StatusText =>
        Files.Count == 0
            ? "No files loaded"
            : $"{Files.Count} file{(Files.Count == 1 ? "" : "s")} loaded";

    public bool IsFilterAll    => _filterMode == FilterMode.All;
    public bool IsFilterImages => _filterMode == FilterMode.Images;
    public bool IsFilterVideos => _filterMode == FilterMode.Videos;

    public string SortLabelDate => _sortModeDate switch { SortMode.Desc => "Date ▾", SortMode.Asc => "Date ▴", _ => "Date -" };
    public string SortLabelName => _sortModeName switch { SortMode.Desc => "Name ▾", SortMode.Asc => "Name ▴", _ => "Name -" };
    public string SortLabelSize => _sortModeSize switch { SortMode.Desc => "Size ▾", SortMode.Asc => "Size ▴", _ => "Size -" };

    public ReactiveCommand<Unit, Unit> LoadMoreCommand       { get; }
    public ReactiveCommand<Unit, Unit> BackCommand           { get; }
    public ReactiveCommand<Unit, Unit> ShowAllCommand        { get; }
    public ReactiveCommand<Unit, Unit> ShowImagesCommand     { get; }
    public ReactiveCommand<Unit, Unit> ShowVideosCommand     { get; }
    public ReactiveCommand<Unit, Unit> ApplyToAllCommand      { get; }
    public ReactiveCommand<Unit, Unit> RunPipelineCommand    { get; }
    public ReactiveCommand<Unit, Unit> CancelPipelineCommand { get; }
    public ReactiveCommand<Unit, Unit> DismissSummaryCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleSortDateCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleSortNameCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleSortSizeCommand { get; }

    public event Action? BackRequested;

    public MediaBrowserViewModel(
        LoadMediaUseCase loadMedia,
        RunPipelineUseCase runPipeline,
        IThumbnailService thumbnails)
    {
        _loadMedia = loadMedia;
        _runPipeline = runPipeline;
        _thumbnails = thumbnails;

        Files.CollectionChanged += (_, _) => this.RaisePropertyChanged(nameof(StatusText));

        var canLoadMore = this.WhenAnyValue(
            x => x.IsLoading,
            x => x.HasMore,
            (loading, more) => !loading && more);

        var canRunPipeline = this.WhenAnyValue(
            x => x.IsLoading,
            x => x.IsPipelineRunning,
            (loading, running) => !loading && !running);

        var canCancel = this.WhenAnyValue(x => x.IsPipelineRunning);

        LoadMoreCommand       = ReactiveCommand.CreateFromTask(LoadBatchAsync, canLoadMore);
        BackCommand           = ReactiveCommand.Create(Back);
        ShowAllCommand        = ReactiveCommand.Create(() => SetFilter(FilterMode.All));
        ShowImagesCommand     = ReactiveCommand.Create(() => SetFilter(FilterMode.Images));
        ShowVideosCommand     = ReactiveCommand.Create(() => SetFilter(FilterMode.Videos));
        ApplyToAllCommand     = ReactiveCommand.Create(() => { });
        RunPipelineCommand    = ReactiveCommand.CreateFromTask(RunPipelineAsync, canRunPipeline);
        CancelPipelineCommand = ReactiveCommand.Create(CancelPipeline, canCancel);
        DismissSummaryCommand = ReactiveCommand.Create(DismissSummary);
        ToggleSortDateCommand = ReactiveCommand.Create(ToggleSortDate);
        ToggleSortNameCommand = ReactiveCommand.Create(ToggleSortName);
        ToggleSortSizeCommand = ReactiveCommand.Create(ToggleSortSize);
    }

    public void Initialize(DeviceProfile device)
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();

        _device = device;
        _offset = 0;
        _filterMode = FilterMode.All;
        HasMore = true;
        LoadError = null;
        Files.Clear();
        FilteredFiles.Clear();

        this.RaisePropertyChanged(nameof(DeviceName));
        this.RaisePropertyChanged(nameof(SourcePath));
        this.RaisePropertyChanged(nameof(IsFilterAll));
        this.RaisePropertyChanged(nameof(IsFilterImages));
        this.RaisePropertyChanged(nameof(IsFilterVideos));

        _ = LoadBatchAsync();
    }

    private void Back()
    {
        _cts.Cancel();
        BackRequested?.Invoke();
    }

    private static SortMode NextSortMode(SortMode current) => current switch
    {
        SortMode.Desc => SortMode.Asc,
        SortMode.Asc  => SortMode.None,
        _             => SortMode.Desc
    };

    private void ToggleSortDate()
    {
        _sortModeDate = NextSortMode(_sortModeDate);
        this.RaisePropertyChanged(nameof(SortLabelDate));
        ApplySortTo(FilteredFiles.ToList());
    }

    private void ToggleSortName()
    {
        _sortModeName = NextSortMode(_sortModeName);
        this.RaisePropertyChanged(nameof(SortLabelName));
        ApplySortTo(FilteredFiles.ToList());
    }

    private void ToggleSortSize()
    {
        _sortModeSize = NextSortMode(_sortModeSize);
        this.RaisePropertyChanged(nameof(SortLabelSize));
        ApplySortTo(FilteredFiles.ToList());
    }

    private void SetFilter(FilterMode mode)
    {
        if (_filterMode == mode) return;
        _filterMode = mode;
        this.RaisePropertyChanged(nameof(IsFilterAll));
        this.RaisePropertyChanged(nameof(IsFilterImages));
        this.RaisePropertyChanged(nameof(IsFilterVideos));
        ApplySortTo(Files.Where(MatchesFilter));
    }

    private bool MatchesFilter(FileRowViewModel row) => _filterMode switch
    {
        FilterMode.Images => row.Context.Type == FileType.Image,
        FilterMode.Videos => row.Context.Type == FileType.Video,
        _                 => true
    };

    private static DateTime ParseExifDate(string? exif)
    {
        if (exif is not null &&
            DateTime.TryParseExact(exif, "yyyy:MM:dd HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
            return dt;
        return DateTime.MaxValue;
    }

    private void ApplySortTo(IEnumerable<FileRowViewModel> source)
    {
        IOrderedEnumerable<FileRowViewModel>? sorted = null;

        if (_sortModeDate != SortMode.None)
            sorted = _sortModeDate == SortMode.Desc
                ? source.OrderByDescending(f => ParseExifDate(f.Context.ExifCaptureDate))
                : source.OrderBy(f => ParseExifDate(f.Context.ExifCaptureDate));

        if (_sortModeName != SortMode.None)
            sorted = sorted is null
                ? (_sortModeName == SortMode.Desc
                    ? source.OrderByDescending(f => f.FileName)
                    : source.OrderBy(f => f.FileName))
                : (_sortModeName == SortMode.Desc
                    ? sorted.ThenByDescending(f => f.FileName)
                    : sorted.ThenBy(f => f.FileName));

        if (_sortModeSize != SortMode.None)
            sorted = sorted is null
                ? (_sortModeSize == SortMode.Desc
                    ? source.OrderByDescending(f => f.FileSize)
                    : source.OrderBy(f => f.FileSize))
                : (_sortModeSize == SortMode.Desc
                    ? sorted.ThenByDescending(f => f.FileSize)
                    : sorted.ThenBy(f => f.FileSize));

        FilteredFiles.Clear();
        foreach (var f in sorted ?? source)
            FilteredFiles.Add(f);
    }

    private void UpdateCurrentFileStages(string fileName)
    {
        var row = Files.FirstOrDefault(r => r.FileName == fileName);
        if (row is null) return;
        var actions = row.AssignedActions;
        CurrentFileIsVideo     = row.Context.Type == FileType.Video;
        CurrentFileHasRotation = (actions & (MediaAction.RotateLeft | MediaAction.RotateRight | MediaAction.Flip180)) != MediaAction.None;
        CurrentFileHasBackup   = actions.HasFlag(MediaAction.SaveToBackup);
        CurrentFileHasTelegram = actions.HasFlag(MediaAction.SendToTelegram);
        CurrentFileHasDelete   = actions.HasFlag(MediaAction.DeleteAfter);
    }

    private async Task RunPipelineAsync()
    {
        if (_device is null) return;

        var filesToProcess = Files
            .Where(r => r.HasAnyAction)
            .Select(r => r.Context with { AssignedActions = r.AssignedActions })
            .ToList();

        if (filesToProcess.Count == 0) return;

        _pipelineCts = new CancellationTokenSource();
        var ct = _pipelineCts.Token;

        _wasRunCancelled = false;
        TotalFiles = filesToProcess.Count;
        IsPipelineRunning = true;

        var observer = new PipelineProgressObserver(
            onFileStarted: (name, index, _) => { CurrentFileName = name; CurrentIndex = index; UpdateCurrentFileStages(name); },
            onFinished: summary => LastRunSummary = summary,
            onCancelled: () => { _wasRunCancelled = true; });

        try
        {
            await _runPipeline.ExecuteAsync(filesToProcess, _device, observer, ct);
        }
        catch (OperationCanceledException) { }

        finally
        {
            IsPipelineRunning = false;
        }
    }

    private void CancelPipeline() => _pipelineCts.Cancel();

    private void DismissSummary()
    {
        _wasRunCancelled = false;
        LastRunSummary = null;
    }

    private async Task LoadBatchAsync()
    {
        if (_device is null || IsLoading) return;

        var ct = _cts.Token;
        IsLoading = true;
        LoadError = null;

        try
        {
            var result = await _loadMedia.ExecuteAsync(_device, _offset, ct);

            switch (result)
            {
                case LoadMediaResult.Success success:
                    foreach (var file in success.Files)
                    {
                        var row = new FileRowViewModel(file, _thumbnails, ct);
                        Files.Add(row);
                        if (MatchesFilter(row)) FilteredFiles.Add(row);
                    }
                    _offset += success.Files.Count;
                    HasMore = success.Files.Count == _device.FilesPerLoad;
                    break;

                case LoadMediaResult.SourceNotAccessible err:
                    LoadError = $"Cannot access source folder: {err.Reason}";
                    HasMore = false;
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // Navigation away or back pressed while loading — silently stop
        }
        finally
        {
            if (!ct.IsCancellationRequested)
                IsLoading = false;
        }
    }
}
