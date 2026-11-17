using System.IO;

/// <summary>
/// Writes a file through a temporary copy so a failed write never leaves a half-written save.
/// The file being replaced is kept beside it as a backup.
/// </summary>
public static class AtomicFile
{
    public static string BackupPath(string path) => path + ".bak";

    public static void Write(string path, string content)
    {
        string temp = path + ".tmp";
        File.WriteAllText(temp, content);

        if (File.Exists(path))
            File.Replace(temp, path, BackupPath(path));
        else
            File.Move(temp, path);
    }
}
