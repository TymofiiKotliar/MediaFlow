namespace MediaFlow.Domain.ValueObjects;

public abstract record PipelineStageResult;

public sealed record Success : PipelineStageResult;
public sealed record Skipped(string Reason) : PipelineStageResult;
public sealed record Failed(string Reason) : PipelineStageResult;
