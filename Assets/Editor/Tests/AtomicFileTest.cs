using System.IO;
using NUnit.Framework;

/// <summary>
/// Edit Mode coverage for the save file write: the previous file is kept as a backup and no temporary file is left behind.
/// </summary>
public class AtomicFileTest
{
    private string directory;
    private string path;

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "atomicfile-" + Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, "save.dat");
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(directory, true);
    }

    [Test]
    public void FirstWrite_CreatesFileWithoutBackup()
    {
        AtomicFile.Write(path, "one");

        Assert.AreEqual("one", File.ReadAllText(path));
        Assert.IsFalse(File.Exists(AtomicFile.BackupPath(path)));
        Assert.IsFalse(File.Exists(path + ".tmp"));
    }

    [Test]
    public void SecondWrite_ReplacesFileAndKeepsPreviousAsBackup()
    {
        AtomicFile.Write(path, "one");
        AtomicFile.Write(path, "two");

        Assert.AreEqual("two", File.ReadAllText(path));
        Assert.AreEqual("one", File.ReadAllText(AtomicFile.BackupPath(path)));
        Assert.IsFalse(File.Exists(path + ".tmp"));
    }
}
