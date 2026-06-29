namespace MediaFlow.Presentation.Services;

public interface IDialogService
{
    Task<bool> ConfirmAsync(string message);
}
