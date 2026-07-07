namespace MediaFlow.Domain.Interfaces;

public interface ILoadProgressObserver
{
    void FileLoading(string fileName, int index, int total);
}
