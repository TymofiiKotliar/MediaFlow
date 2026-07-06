using MediaFlow.Domain.Interfaces;
using MediaFlow.Infrastructure.FileSystem;

namespace MediaFlow.Infrastructure.Persistence;

public sealed class DeviceProfilePictureStore : IDeviceProfilePictureStore
{
    public async Task<string> SaveAsync(byte[] imageBytes, string fileExtension, CancellationToken ct = default)
    {
        var extension = fileExtension.StartsWith('.') ? fileExtension : $".{fileExtension}";
        var path = Path.Combine(AppPaths.DeviceProfilePicturesFolder, $"{Guid.NewGuid()}{extension}");
        await File.WriteAllBytesAsync(path, imageBytes, ct);
        return path;
    }

    public void Delete(string path)
    {
        try { File.Delete(path); }
        catch { }
    }
}
