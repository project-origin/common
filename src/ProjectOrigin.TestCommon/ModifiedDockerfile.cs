using System;
using System.IO;

namespace ProjectOrigin.TestCommon;

public class ModifiedDockerfile : IDisposable
{
    private readonly string _tempFile;
    public string FullPath => _tempFile;
    public string FileName => Path.GetFileName(_tempFile);

    public ModifiedDockerfile(string sourcePath, Func<string, string> modification)
    {
        string directory = Path.GetDirectoryName(sourcePath)!;
        string fileName = Path.GetFileName(sourcePath);
        _tempFile = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(fileName)}.tmp{Path.GetExtension(fileName)}");

        string str = File.ReadAllText(sourcePath);
        File.WriteAllText(_tempFile, modification(str));
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
