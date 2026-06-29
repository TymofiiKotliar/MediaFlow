namespace MediaFlow.Infrastructure.FileSystem;

public static class AppPaths
{
    private static readonly string Base = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MediaFlow");

    public static string AppData   => Base;
    public static string TempFolder => Path.Combine(Base, "temp");
    public static string DatabaseFile => Path.Combine(Base, "devices.db");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(AppData);
        Directory.CreateDirectory(TempFolder);
        SweepTempFolder();
    }

    private static void SweepTempFolder()
    {
        foreach (var file in Directory.EnumerateFiles(TempFolder))
        {
            try { File.Delete(file); }
            catch { }
        }
    }
}
