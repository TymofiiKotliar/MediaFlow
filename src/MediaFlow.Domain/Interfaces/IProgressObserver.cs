using MediaFlow.Domain.Entities;
using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Domain.Interfaces;

public interface IProgressObserver
{
    void FileStarted(string fileName, int index, int total);
    void FileCompleted(string fileName, PipelineStageResult result);
    void FileFailed(string fileName, string reason);
    void PipelineCancelled();
    void PipelineFinished(RunSummary summary);
}
