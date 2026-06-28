using System;
using System.Reactive;
using MediaFlow.Domain.ValueObjects;
using ReactiveUI;

namespace MediaFlow.Presentation.ViewModels;

public sealed class NamingChipViewModel : ViewModelBase
{
    public NamingToken? Token { get; }
    public string Label { get; }
    public bool IsCursor { get; }
    public bool IsNotCursor => !IsCursor;

    public ReactiveCommand<Unit, Unit> RemoveCommand { get; }
    public ReactiveCommand<Unit, Unit> InsertBeforeCommand { get; }

    public NamingChipViewModel(NamingToken token, Action onRemove, Action onInsertBefore)
    {
        Token = token;
        IsCursor = false;
        Label = token switch
        {
            PrefixToken p       => p.Text,
            SequenceNumberToken => "# Sequence",
            CurrentDateToken    => "Current Date",
            PhotoDateToken      => "Photo Date",
            _                   => token.ToString()!
        };
        RemoveCommand = ReactiveCommand.Create(onRemove);
        InsertBeforeCommand = ReactiveCommand.Create(onInsertBefore);
    }

    // Cursor chip — no token, no meaningful commands
    public NamingChipViewModel()
    {
        IsCursor = true;
        Label = "";
        RemoveCommand = ReactiveCommand.Create(() => { });
        InsertBeforeCommand = ReactiveCommand.Create(() => { });
    }
}
