using System;
using System.IO;

public class TestContainerDockerFile : IDisposable
{
    private readonly string _tempFile;
    public string FullPath => _tempFile;
    public string FileName => Path.GetFileName(_tempFile);

    public TestContainerDockerFile(string directory, string dockerfile)
    {
        var source = Path.Combine(directory, dockerfile);

        _tempFile = $"{source}.tmp";

        var content = File.ReadAllText(source)
            .Replace(" --platform=$BUILDPLATFORM", "") // not supported by Testcontainers
            .Replace("-jammy-chiseled-extra", ""); // not supported by Testcontainers because of user permissions

        File.WriteAllText(_tempFile, content);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    ~TestContainerDockerFile()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }
}
