using System;
using System.IO;

public class ModifiedDockerfile : IDisposable
{
    private readonly string _tempFile;
    public string FullPath => _tempFile;
    public string FileName => Path.GetFileName(_tempFile);

    public ModifiedDockerfile(string sourcePath, Func<string, string> modification)
    {
        _tempFile = $"{sourcePath}.tmp";
        var oldContent = File.ReadAllText(sourcePath);
        var newContent = modification(oldContent);
        File.WriteAllText(_tempFile, newContent);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    ~ModifiedDockerfile()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }
}
