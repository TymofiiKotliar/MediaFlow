using System;
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

    public ReactiveCommand<Unit, Unit> LoadMoreCommand    { get; }
    public ReactiveCommand<Unit, Unit> BackCommand        { get; }
    public ReactiveCommand<Unit, Unit> ShowAllCommand     { get; }
    public ReactiveCommand<Unit, Unit> ShowImagesCommand  { get; }
    public ReactiveCommand<Unit, Unit> ShowVideosCommand  { get; }
    public ReactiveCommand<Unit, Unit> ApplyToAllCommand  { get; }
    public ReactiveCommand<Unit, Unit> RunPipelineCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleSortDateCommand  { get; }
    public ReactiveCommand<Unit, Unit> ToggleSortNameCommand  { get; }
    public ReactiveCommand<Unit, Unit> ToggleSortSizeCommand  { get; }

    public event Action? BackRequested;

    public MediaBrowserViewModel(LoadMediaUseCase loadMedia, IThumbnailService thumbnails)
    {
        _loadMedia = loadMedia;
        _thumbnails = thumbnails;

        Files.CollectionChanged += (_, _) => this.RaisePropertyChanged(nameof(StatusText));

        var canLoadMore = this.WhenAnyValue(
            x => x.IsLoading,
            x => x.HasMore,
            (loading, more) => !loading && more);

        LoadMoreCommand    = ReactiveCommand.CreateFromTask(LoadBatchAsync, canLoadMore);
        BackCommand        = ReactiveCommand.Create(Back);
        ShowAllCommand     = ReactiveCommand.Create(() => SetFilter(FilterMode.All));
        ShowImagesCommand  = ReactiveCommand.Create(() => SetFilter(FilterMode.Images));
        ShowVideosCommand  = ReactiveCommand.Create(() => SetFilter(FilterMode.Videos));
        ApplyToAllCommand  = ReactiveCommand.Create(() => { }); // Phase 5
        RunPipelineCommand = ReactiveCommand.Create(() => { }); // Phase 5
        ToggleSortDateCommand  = ReactiveCommand.Create(ToggleSortDate);
        ToggleSortNameCommand  = ReactiveCommand.Create(ToggleSortName);
        ToggleSortSizeCommand  = ReactiveCommand.Create(ToggleSortSize);
    }

    public void Initialize(DeviceProfile device)
    {
        _cts.Cancel();
        CleanupTempFiles();
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
        CleanupTempFiles();
        BackRequested?.Invoke();
    }

    private void CleanupTempFiles()
    {
        foreach (var row in Files)
        {
            try { File.Delete(row.Context.TempPath); }
            catch { /* best-effort; file may still be open by thumbnail loader */ }
        }
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
