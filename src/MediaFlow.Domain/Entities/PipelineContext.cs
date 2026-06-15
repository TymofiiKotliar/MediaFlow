namespace MediaFlow.Domain.Entities;

public sealed record PipelineContext(
    DeviceProfile Device,
    FileContext File
);
