using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Domain.Interfaces;

public interface IMetadataReader
{
    FileMetadata Read(string filePath);
}
