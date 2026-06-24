using System;
using System.Collections.Generic;
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

    private string? _nameError;
    private string? _sourceFolderError;
    private string? _backupFolderError;
    private string? _filesPerLoadError;
    private string? _telegramBotTokenError;
    private string? _telegramChatIdError;

    public string? NameError
    {
        get => _nameError;
        private set { this.RaiseAndSetIfChanged(ref _nameError, value); this.RaisePropertyChanged(nameof(HasNameError)); }
    }
    public bool HasNameError => _nameError is not null;

    public string? SourceFolderError
    {
        get => _sourceFolderError;
        private set { this.RaiseAndSetIfChanged(ref _sourceFolderError, value); this.RaisePropertyChanged(nameof(HasSourceFolderError)); }
    }
    public bool HasSourceFolderError => _sourceFolderError is not null;

    public string? BackupFolderError
    {
        get => _backupFolderError;
        private set { this.RaiseAndSetIfChanged(ref _backupFolderError, value); this.RaisePropertyChanged(nameof(HasBackupFolderError)); }
    }
    public bool HasBackupFolderError => _backupFolderError is not null;

    public string? FilesPerLoadError
    {
        get => _filesPerLoadError;
        private set { this.RaiseAndSetIfChanged(ref _filesPerLoadError, value); this.RaisePropertyChanged(nameof(HasFilesPerLoadError)); }
    }
    public bool HasFilesPerLoadError => _filesPerLoadError is not null;

    public string? TelegramBotTokenError
    {
        get => _telegramBotTokenError;
        private set { this.RaiseAndSetIfChanged(ref _telegramBotTokenError, value); this.RaisePropertyChanged(nameof(HasTelegramBotTokenError)); }
    }
    public bool HasTelegramBotTokenError => _telegramBotTokenError is not null;

    public string? TelegramChatIdError
    {
        get => _telegramChatIdError;
        private set { this.RaiseAndSetIfChanged(ref _telegramChatIdError, value); this.RaisePropertyChanged(nameof(HasTelegramChatIdError)); }
    }
    public bool HasTelegramChatIdError => _telegramChatIdError is not null;

    public string Title => _editingId is null ? "New Device" : "Edit Device";

    public ObservableCollection<NamingChipViewModel> Chips { get; } = [];

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

        ClearErrors();
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

    private void ClearErrors()
    {
        NameError = null;
        SourceFolderError = null;
        BackupFolderError = null;
        FilesPerLoadError = null;
        TelegramBotTokenError = null;
        TelegramChatIdError = null;
    }

    private void ApplyErrors(IReadOnlyList<string> errors)
    {
        foreach (var e in errors)
        {
            if (e.StartsWith("Device name"))            NameError = e;
            else if (e.StartsWith("Source folder"))     SourceFolderError = e;
            else if (e.StartsWith("Backup folder"))     BackupFolderError = e;
            else if (e.StartsWith("Files per load"))    FilesPerLoadError = e;
            else if (e.StartsWith("Telegram bot token")) TelegramBotTokenError = e;
            else if (e.StartsWith("Telegram chat ID"))  TelegramChatIdError = e;
        }
    }

    private async Task SaveAsync()
    {
        ClearErrors();

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
                ApplyErrors(vf.Errors);
                return;
            }
        }
        else
        {
            var result = await _edit.ExecuteAsync(_editingId, input);
            if (result is EditDeviceResult.ValidationFailed vf)
            {
                ApplyErrors(vf.Errors);
                return;
            }
        }

        SaveCompleted?.Invoke();
    }
}
