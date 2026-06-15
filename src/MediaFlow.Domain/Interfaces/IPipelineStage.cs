using MediaFlow.Domain.Entities;
using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Domain.Interfaces;

public interface IPipelineStage
{
    Task<(PipelineContext Context, PipelineStageResult Result)> ExecuteAsync(
        PipelineContext context, CancellationToken ct);
}
