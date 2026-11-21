using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Edit Mode coverage for <see cref="SaveService"/> orchestration with a fake cloud provider: local persistence,
/// backup recovery, the first-sync comparison, offline and failure paths, and level progress.
/// </summary>
public class SaveServiceTest
{
    private const string Key = "0123456789abcdef0123456789abcdef";

    private string directory;
    private string savePath;
    private bool online;
    private readonly List<SaveService> created = new List<SaveService>();

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "PocketMatchSaveTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        savePath = Path.Combine(directory, "save.dat");
        online = true;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var service in created)
            service.Dispose();
        created.Clear();

        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
    }

    [Test]
    public void NoSaveFile_StartsWithDefaults()
    {
        var service = Create(new FakeCloud());

        Assert.AreEqual(0, service.PlayerData.coins);
        Assert.AreEqual(0, service.PlayerData.nextLevelIndex);
    }

    [Test]
    public void SaveThenReload_RestoresProgressAndCoins()
    {
        var service = Create(new FakeCloud { IsEnabled = false });
        service.PlayerData.coins = 250;
        service.PlayerData.nextLevelIndex = 3;
        Run(service.SaveAsync());

        var reloaded = Create(new FakeCloud { IsEnabled = false });

        Assert.AreEqual(250, reloaded.PlayerData.coins);
        Assert.AreEqual(3, reloaded.PlayerData.nextLevelIndex);
    }

    [Test]
    public void UnreadableSave_FallsBackToBackup()
    {
        var service = Create(new FakeCloud { IsEnabled = false });
        service.PlayerData.coins = 10;
        Run(service.SaveAsync());
        service.PlayerData.coins = 20;
        Run(service.SaveAsync());

        File.WriteAllText(savePath, "not a save");
        LogAssert.Expect(LogType.Error, new Regex("Failed to load file"));

        var reloaded = Create(new FakeCloud { IsEnabled = false });

        Assert.AreEqual(10, reloaded.PlayerData.coins);
    }

    [Test]
    public void UnreadableSaveWithoutBackup_StartsFresh()
    {
        File.WriteAllText(savePath, "not a save");
        LogAssert.Expect(LogType.Error, new Regex("Failed to load file"));

        var service = Create(new FakeCloud { IsEnabled = false });

        Assert.AreEqual(0, service.PlayerData.coins);
        Assert.AreEqual(0, service.PlayerData.nextLevelIndex);
    }

    [Test]
    public void CloudDisabled_ReportsDisabledAndNeverUploads()
    {
        var cloud = new FakeCloud { IsEnabled = false };
        var service = Create(cloud);

        Run(service.InitializeCloudAsync());
        Run(service.SaveAsync());

        Assert.AreEqual(CloudSyncStatus.Disabled, service.CloudSyncStatus);
        Assert.AreEqual(0, cloud.InitCount);
        Assert.AreEqual(0, cloud.UploadCount);
    }

    [Test]
    public void Init_WithNoCloudCopy_UploadsLocalAndReportsReady()
    {
        var cloud = new FakeCloud();
        var service = Create(cloud);
        service.PlayerData.coins = 5;

        Run(service.InitializeCloudAsync());

        Assert.AreEqual(1, cloud.UploadCount);
        Assert.AreEqual(5, cloud.Stored.coins);
        Assert.AreEqual(CloudSyncStatus.Ready, service.CloudSyncStatus);
    }

    [Test]
    public void Init_WithCloudFurtherAlong_AppliesCloudAndPersistsIt()
    {
        var cloud = new FakeCloud { Stored = new PlayerData { nextLevelIndex = 4, coins = 99 } };
        var service = Create(cloud);
        service.PlayerData.nextLevelIndex = 1;
        int replaced = 0;
        service.PlayerDataReplaced += () => replaced++;

        Run(service.InitializeCloudAsync());

        Assert.AreEqual(4, service.PlayerData.nextLevelIndex);
        Assert.AreEqual(99, service.PlayerData.coins);
        Assert.AreEqual(CloudSyncStatus.AppliedFromCloud, service.CloudSyncStatus);
        Assert.AreEqual(1, replaced);
        Assert.AreEqual(0, cloud.UploadCount);

        var reloaded = Create(new FakeCloud { IsEnabled = false });
        Assert.AreEqual(99, reloaded.PlayerData.coins);
    }

    [Test]
    public void Init_WithLocalFurtherAlong_KeepsLocalAndUploadsIt()
    {
        var cloud = new FakeCloud { Stored = new PlayerData { nextLevelIndex = 1, coins = 5 } };
        var service = Create(cloud);
        service.PlayerData.nextLevelIndex = 6;
        service.PlayerData.coins = 40;

        Run(service.InitializeCloudAsync());

        Assert.AreEqual(6, service.PlayerData.nextLevelIndex);
        Assert.AreEqual(40, service.PlayerData.coins);
        Assert.AreEqual(6, cloud.Stored.nextLevelIndex);
        Assert.AreEqual(CloudSyncStatus.Ready, service.CloudSyncStatus);
    }

    [Test]
    public void Init_WhileOffline_SkipsDownloadAndReportsOffline()
    {
        online = false;
        var cloud = new FakeCloud { Stored = new PlayerData { nextLevelIndex = 4 } };
        var service = Create(cloud);

        Run(service.InitializeCloudAsync());

        Assert.AreEqual(0, cloud.DownloadCount);
        Assert.AreEqual(CloudSyncStatus.Offline, service.CloudSyncStatus);
        Assert.AreEqual(0, service.PlayerData.nextLevelIndex);
    }

    [Test]
    public void Init_WhenDownloadFails_KeepsLocalAndReportsInitFailed()
    {
        var cloud = new FakeCloud { FailDownload = true };
        var service = Create(cloud);
        service.PlayerData.coins = 77;

        Run(service.InitializeCloudAsync());

        Assert.AreEqual(77, service.PlayerData.coins);
        Assert.AreEqual(CloudSyncStatus.InitFailed, service.CloudSyncStatus);
    }

    [Test]
    public void Init_WhenProviderInitFails_ReportsInitFailedAndNeverSyncs()
    {
        var cloud = new FakeCloud { FailInit = true };
        var service = Create(cloud);

        Run(service.InitializeCloudAsync());
        Run(service.SaveAsync());

        Assert.AreEqual(CloudSyncStatus.InitFailed, service.CloudSyncStatus);
        Assert.AreEqual(0, cloud.DownloadCount);
        Assert.AreEqual(0, cloud.UploadCount);
    }

    [Test]
    public void Save_AfterFirstSync_UploadsCurrentData()
    {
        var cloud = new FakeCloud();
        var service = Create(cloud);
        Run(service.InitializeCloudAsync());

        service.PlayerData.coins = 7;
        Run(service.SaveAsync());

        Assert.AreEqual(2, cloud.UploadCount);
        Assert.AreEqual(7, cloud.Stored.coins);
    }

    [Test]
    public void Save_WhenUploadFails_LocalSaveIsStillWritten()
    {
        var cloud = new FakeCloud();
        var service = Create(cloud);
        Run(service.InitializeCloudAsync());

        cloud.FailUpload = true;
        service.PlayerData.coins = 8;
        Run(service.SaveAsync());

        Assert.AreEqual(CloudSyncStatus.UploadFailed, service.CloudSyncStatus);
        var reloaded = Create(new FakeCloud { IsEnabled = false });
        Assert.AreEqual(8, reloaded.PlayerData.coins);
    }

    [Test]
    public void Save_BeforeFirstComparison_ComparesInsteadOfOverwritingBetterCloudCopy()
    {
        online = false;
        var cloud = new FakeCloud { Stored = new PlayerData { nextLevelIndex = 5, coins = 500 } };
        var service = Create(cloud);
        Run(service.InitializeCloudAsync());

        online = true;
        service.PlayerData.coins = 1;
        Run(service.SaveAsync());

        Assert.AreEqual(0, cloud.UploadCount);
        Assert.AreEqual(500, service.PlayerData.coins);
        Assert.AreEqual(CloudSyncStatus.AppliedFromCloud, service.CloudSyncStatus);
    }

    [Test]
    public void ResetToDefaults_OverwritesCloudWithoutComparing()
    {
        online = false;
        var cloud = new FakeCloud { Stored = new PlayerData { nextLevelIndex = 5, coins = 500 } };
        var service = Create(cloud);
        Run(service.InitializeCloudAsync());

        online = true;
        Run(service.ResetToDefaultsAsync());

        Assert.AreEqual(0, cloud.DownloadCount);
        Assert.AreEqual(1, cloud.UploadCount);
        Assert.AreEqual(0, cloud.Stored.nextLevelIndex);
        Assert.AreEqual(0, service.PlayerData.coins);
    }

    [Test]
    public void LevelCompleted_AdvancesProgressOnlyOnTheFrontierLevel()
    {
        var service = Create(new FakeCloud { IsEnabled = false });
        service.Start();

        RaiseCompleted(levelIndex: 0, capReached: false);
        Assert.AreEqual(1, service.PlayerData.nextLevelIndex);

        RaiseCompleted(levelIndex: 0, capReached: false);
        Assert.AreEqual(1, service.PlayerData.nextLevelIndex, "Replaying an earlier level keeps progress");

        RaiseCompleted(levelIndex: 1, capReached: false);
        Assert.AreEqual(2, service.PlayerData.nextLevelIndex);
    }

    [Test]
    public void LevelCompleted_AtLevelCap_DoesNotAdvanceProgress()
    {
        var service = Create(new FakeCloud { IsEnabled = false });
        service.Start();
        service.PlayerData.nextLevelIndex = 3;

        RaiseCompleted(levelIndex: 3, capReached: true);

        Assert.AreEqual(3, service.PlayerData.nextLevelIndex);
    }

    private SaveService Create(FakeCloud cloud)
    {
        var service = new SaveService(cloud, savePath, Key, () => online);
        created.Add(service);
        return service;
    }

    private static void Run(Task task) => task.GetAwaiter().GetResult();

    private static void RaiseCompleted(int levelIndex, bool capReached)
    {
        LevelEvents.RaiseLevelCompleted(new LevelCompletedEventArgs("Level", 5, 5, 100, 30, capReached, levelIndex));
    }

    private sealed class FakeCloud : ICloudSaveProvider
    {
        public bool IsEnabled { get; set; } = true;
        public PlayerData Stored { get; set; }
        public bool FailInit { get; set; }
        public bool FailDownload { get; set; }
        public bool FailUpload { get; set; }
        public int InitCount { get; private set; }
        public int UploadCount { get; private set; }
        public int DownloadCount { get; private set; }

        public Task InitializeAsync()
        {
            InitCount++;
            return FailInit ? Task.FromException(new InvalidOperationException("init failed")) : Task.CompletedTask;
        }

        public Task UploadAsync(PlayerData data)
        {
            if (FailUpload)
                return Task.FromException(new InvalidOperationException("upload failed"));

            UploadCount++;
            Stored = Copy(data);
            return Task.CompletedTask;
        }

        public Task<PlayerData> DownloadAsync()
        {
            if (FailDownload)
                return Task.FromException<PlayerData>(new InvalidOperationException("download failed"));

            DownloadCount++;
            return Task.FromResult(Stored == null ? null : Copy(Stored));
        }

        private static PlayerData Copy(PlayerData data) => JsonUtility.FromJson<PlayerData>(JsonUtility.ToJson(data));
    }
}
