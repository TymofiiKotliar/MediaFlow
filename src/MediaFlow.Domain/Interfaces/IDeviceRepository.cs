using MediaFlow.Domain.Entities;

namespace MediaFlow.Domain.Interfaces;

public interface IDeviceRepository
{
    Task<IReadOnlyList<DeviceProfile>> GetAllAsync();
    Task<DeviceProfile?> GetByIdAsync(string id);
    Task SaveAsync(DeviceProfile profile);
    Task DeleteAsync(string id);
}
