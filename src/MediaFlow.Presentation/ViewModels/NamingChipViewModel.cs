using System;
using System.Reactive;
using MediaFlow.Domain.ValueObjects;
using ReactiveUI;

namespace MediaFlow.Presentation.ViewModels;

public sealed class NamingChipViewModel : ViewModelBase
{
    public NamingToken Token { get; }
    public string Label { get; }
    public ReactiveCommand<Unit, Unit> RemoveCommand { get; }

    public NamingChipViewModel(NamingToken token, Action onRemove)
    {
        Token = token;
        Label = token switch
        {
            PrefixToken p      => p.Text,
            SequenceNumberToken => "# Sequence",
            CurrentDateToken    => "Current Date",
            PhotoDateToken      => "Photo Date",
            _                   => token.ToString()!
        };
        RemoveCommand = ReactiveCommand.Create(onRemove);
    }
}
