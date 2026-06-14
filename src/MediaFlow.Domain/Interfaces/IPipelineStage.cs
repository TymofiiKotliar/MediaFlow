using MediaFlow.Domain.Entities;
using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Domain.Interfaces;

public interface IPipelineStage
{
    Task<(FileContext Context, PipelineStageResult Result)> ExecuteAsync(
        FileContext context, CancellationToken ct);
}
