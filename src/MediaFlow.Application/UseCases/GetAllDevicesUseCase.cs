using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Interfaces;

namespace MediaFlow.Application.UseCases;

public sealed class GetAllDevicesUseCase(IDeviceRepository repository)
{
    public Task<IReadOnlyList<DeviceProfile>> ExecuteAsync(CancellationToken ct = default)
        => repository.GetAllAsync(ct);
}
