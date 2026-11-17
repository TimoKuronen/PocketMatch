using NUnit.Framework;

/// <summary>
/// Edit Mode coverage for the local versus cloud conflict rule: more progress wins, equal progress goes to the newer save.
/// </summary>
public class SaveConflictResolverTest
{
    private const string Earlier = "2026-11-10T08:00:00.0000000Z";
    private const string Later = "2026-11-11T08:00:00.0000000Z";

    [Test]
    public void NoCloudCopy_KeepsLocal()
    {
        Assert.IsFalse(SaveConflictResolver.CloudWins(Data(3, Later), null));
    }

    [Test]
    public void CloudFurtherAlong_WinsEvenWhenOlder()
    {
        Assert.IsTrue(SaveConflictResolver.CloudWins(Data(2, Later), Data(5, Earlier)));
    }

    [Test]
    public void LocalFurtherAlong_WinsEvenWhenOlder()
    {
        Assert.IsFalse(SaveConflictResolver.CloudWins(Data(5, Earlier), Data(2, Later)));
    }

    [Test]
    public void EqualProgress_NewerCloudWins()
    {
        Assert.IsTrue(SaveConflictResolver.CloudWins(Data(4, Earlier), Data(4, Later)));
    }

    [Test]
    public void EqualProgress_NewerLocalWins()
    {
        Assert.IsFalse(SaveConflictResolver.CloudWins(Data(4, Later), Data(4, Earlier)));
    }

    [Test]
    public void EqualProgressAndTime_KeepsLocal()
    {
        Assert.IsFalse(SaveConflictResolver.CloudWins(Data(4, Later), Data(4, Later)));
    }

    [Test]
    public void EqualProgress_MissingLocalTimestamp_CloudWins()
    {
        Assert.IsTrue(SaveConflictResolver.CloudWins(Data(4, null), Data(4, Earlier)));
    }

    [Test]
    public void EqualProgress_MissingCloudTimestamp_KeepsLocal()
    {
        Assert.IsFalse(SaveConflictResolver.CloudWins(Data(4, Earlier), Data(4, null)));
    }

    private static PlayerData Data(int nextLevelIndex, string lastSaveTime)
    {
        var data = new PlayerData { nextLevelIndex = nextLevelIndex };
        data.meta.lastSaveTime = lastSaveTime;
        return data;
    }
}
