using System.IO;

namespace ProjectOrigin.TestCommon;

public static class TempFile
{
    public static string WriteAllText(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }

    public static string WriteAllText(string content, string extension)
    {
        var path = Path.GetTempFileName();
        path = Path.ChangeExtension(path, extension);
        File.WriteAllText(path, content);
        return path;
    }
}
