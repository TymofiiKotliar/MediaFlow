using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MediaFlow.Application.UseCases;
using MediaFlow.Application.Validation;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.ValueObjects;
using ReactiveUI;

namespace MediaFlow.Presentation.ViewModels;

public sealed class ProfileEditorViewModel : ViewModelBase
{
    private readonly RegisterDeviceUseCase _register;
    private readonly EditDeviceUseCase _edit;
    private readonly BuildNamingTemplateUseCase _buildNaming;

    private string? _editingId;
    private string _name = "";
    private string _sourceFolderPath = "";
    private string _backupFolderPath = "";
    private string _telegramBotToken = "";
    private string _telegramChatId = "";
    private string _filesPerLoad = "50";
    private string _prefixInput = "";
    private string _namingPreview = "";
    private bool _hasErrors;

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public string SourceFolderPath
    {
        get => _sourceFolderPath;
        set => this.RaiseAndSetIfChanged(ref _sourceFolderPath, value);
    }

    public string BackupFolderPath
    {
        get => _backupFolderPath;
        set => this.RaiseAndSetIfChanged(ref _backupFolderPath, value);
    }

    public string TelegramBotToken
    {
        get => _telegramBotToken;
        set => this.RaiseAndSetIfChanged(ref _telegramBotToken, value);
    }

    public string TelegramChatId
    {
        get => _telegramChatId;
        set => this.RaiseAndSetIfChanged(ref _telegramChatId, value);
    }

    public string FilesPerLoad
    {
        get => _filesPerLoad;
        set => this.RaiseAndSetIfChanged(ref _filesPerLoad, value);
    }

    public string PrefixInput
    {
        get => _prefixInput;
        set => this.RaiseAndSetIfChanged(ref _prefixInput, value);
    }

    public string NamingPreview
    {
        get => _namingPreview;
        private set => this.RaiseAndSetIfChanged(ref _namingPreview, value);
    }

    public bool HasErrors
    {
        get => _hasErrors;
        private set => this.RaiseAndSetIfChanged(ref _hasErrors, value);
    }

    public string Title => _editingId is null ? "New Device" : "Edit Device";

    public ObservableCollection<NamingChipViewModel> Chips { get; } = [];
    public ObservableCollection<string> Errors { get; } = [];

    // Set by the View after DataContext is assigned, so Browse commands can open a folder picker.
    public Func<Task<string?>>? BrowseFolderFunc { get; set; }

    public ReactiveCommand<Unit, Unit> BrowseSourceCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseBackupCommand { get; }
    public ReactiveCommand<Unit, Unit> AddPrefixCommand { get; }
    public ReactiveCommand<Unit, Unit> AddSequenceNumberCommand { get; }
    public ReactiveCommand<Unit, Unit> AddCurrentDateCommand { get; }
    public ReactiveCommand<Unit, Unit> AddPhotoDateCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public event Action? SaveCompleted;
    public event Action? CancelRequested;

    public ProfileEditorViewModel(
        RegisterDeviceUseCase register,
        EditDeviceUseCase edit,
        BuildNamingTemplateUseCase buildNaming)
    {
        _register = register;
        _edit = edit;
        _buildNaming = buildNaming;

        Chips.CollectionChanged += (_, _) => UpdatePreview();
        Errors.CollectionChanged += (_, _) => HasErrors = Errors.Count > 0;

        var hasPrefixInput = this.WhenAnyValue(x => x.PrefixInput)
            .Select(s => !string.IsNullOrWhiteSpace(s));

        BrowseSourceCommand = ReactiveCommand.CreateFromTask(BrowseSourceAsync);
        BrowseBackupCommand = ReactiveCommand.CreateFromTask(BrowseBackupAsync);
        AddPrefixCommand = ReactiveCommand.Create(AddPrefix, hasPrefixInput);
        AddSequenceNumberCommand = ReactiveCommand.Create(() => AddChip(new SequenceNumberToken()));
        AddCurrentDateCommand = ReactiveCommand.Create(() => AddChip(new CurrentDateToken()));
        AddPhotoDateCommand = ReactiveCommand.Create(() => AddChip(new PhotoDateToken()));
        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
        CancelCommand = ReactiveCommand.Create(() => CancelRequested?.Invoke());

        UpdatePreview();
    }

    public void Initialize(DeviceProfile? profile)
    {
        _editingId = profile?.Id;
        Name = profile?.Name ?? "";
        SourceFolderPath = profile?.SourceFolderPath ?? "";
        BackupFolderPath = profile?.BackupFolderPath ?? "";
        TelegramBotToken = profile?.TelegramBotToken ?? "";
        TelegramChatId = profile?.TelegramChatId ?? "";
        FilesPerLoad = (profile?.FilesPerLoad ?? 50).ToString();
        PrefixInput = "";

        Chips.Clear();
        if (profile?.NamingTemplate is not null)
            foreach (var token in profile.NamingTemplate)
                AddChip(token);

        Errors.Clear();
        this.RaisePropertyChanged(nameof(Title));
    }

    private void AddPrefix()
    {
        if (string.IsNullOrWhiteSpace(PrefixInput)) return;
        AddChip(new PrefixToken(PrefixInput.Trim()));
        PrefixInput = "";
    }

    private void AddChip(NamingToken token)
    {
        NamingChipViewModel? chip = null;
        chip = new NamingChipViewModel(token, () => Chips.Remove(chip!));
        Chips.Add(chip);
    }

    private void UpdatePreview()
    {
        var template = Chips.Select(c => c.Token).ToList();
        NamingPreview = _buildNaming.Execute(template, "example.jpg", null, 1);
    }

    private async Task BrowseSourceAsync()
    {
        if (BrowseFolderFunc is null) return;
        var path = await BrowseFolderFunc();
        if (path is not null) SourceFolderPath = path;
    }

    private async Task BrowseBackupAsync()
    {
        if (BrowseFolderFunc is null) return;
        var path = await BrowseFolderFunc();
        if (path is not null) BackupFolderPath = path;
    }

    private async Task SaveAsync()
    {
        Errors.Clear();

        if (!int.TryParse(FilesPerLoad, out var filesPerLoad))
            filesPerLoad = 0;

        var input = new DeviceProfileInput(
            Name: Name,
            SourceFolderPath: SourceFolderPath,
            BackupFolderPath: BackupFolderPath,
            NamingTemplate: Chips.Select(c => c.Token).ToList(),
            TelegramBotToken: TelegramBotToken,
            TelegramChatId: TelegramChatId,
            FilesPerLoad: filesPerLoad);

        if (_editingId is null)
        {
            var result = await _register.ExecuteAsync(input);
            if (result is RegisterDeviceResult.ValidationFailed vf)
            {
                foreach (var e in vf.Errors) Errors.Add(e);
                return;
            }
        }
        else
        {
            var result = await _edit.ExecuteAsync(_editingId, input);
            if (result is EditDeviceResult.ValidationFailed vf)
            {
                foreach (var e in vf.Errors) Errors.Add(e);
                return;
            }
        }

        SaveCompleted?.Invoke();
    }
}
