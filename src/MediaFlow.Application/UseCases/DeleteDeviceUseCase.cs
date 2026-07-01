using MediaFlow.Domain.Interfaces;

namespace MediaFlow.Application.UseCases;

public sealed class DeleteDeviceUseCase(IDeviceRepository repository, IDeviceProfilePictureStore pictureStore)
{
    public async Task<DeleteDeviceResult> ExecuteAsync(string id, CancellationToken ct = default)
    {
        var existing = await repository.GetByIdAsync(id, ct);
        if (existing is null)
            return new DeleteDeviceResult.NotFound(id);

        await repository.DeleteAsync(id, ct);

        if (existing.ProfilePicturePath is not null)
            pictureStore.Delete(existing.ProfilePicturePath);

        return new DeleteDeviceResult.Success();
    }
}

public abstract record DeleteDeviceResult
{
    public sealed record Success : DeleteDeviceResult;
    public sealed record NotFound(string Id) : DeleteDeviceResult;
}
