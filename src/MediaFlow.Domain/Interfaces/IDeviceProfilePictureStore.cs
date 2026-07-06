namespace MediaFlow.Domain.Interfaces;

public interface IDeviceProfilePictureStore
{
    Task<string> SaveAsync(byte[] imageBytes, string fileExtension, CancellationToken ct = default);
    void Delete(string path);
}
