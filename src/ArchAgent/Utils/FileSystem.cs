using System.IO;

namespace ArchAgent.Utils;

public static class FileSystem
{
    public static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public static void WriteAllText(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            EnsureDirectory(dir);
        }
        File.WriteAllText(path, content);
    }
}
