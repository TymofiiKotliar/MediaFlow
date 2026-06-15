using MediaFlow.Domain.Entities;

namespace MediaFlow.Domain.Interfaces;

public interface IDeviceRepository
{
    Task<IReadOnlyList<DeviceProfile>> GetAllAsync(CancellationToken ct = default);
    Task<DeviceProfile?> GetByIdAsync(string id, CancellationToken ct = default);
    Task SaveAsync(DeviceProfile profile, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
