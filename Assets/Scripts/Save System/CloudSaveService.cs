using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CloudSaveService
{
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string playerId;

    public async Task InitializeAsync()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status != DependencyStatus.Available)
        {
            Debug.LogError($"[CloudSave] Firebase dependency error: {status}");
            throw new System.Exception("Firebase dependencies not available");
        }

        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        if (auth.CurrentUser == null)
        {
            var result = await auth.SignInAnonymouslyAsync();
            Debug.Log($"[CloudSave] Signed in anonymously: {result.User.UserId}");
        }

        playerId = auth.CurrentUser.UserId;
        Debug.Log($"[CloudSave] Using UID: {playerId}");
    }

    public async Task UploadAsync(PlayerData data)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning("[CloudSave] Upload skipped - not authenticated");
            return;
        }

        string json = JsonUtility.ToJson(data);

        var dict = new Dictionary<string, object>
        {
            { "json", json },
            { "timestamp", FieldValue.ServerTimestamp }
        };

        await db.Collection("users")
            .Document(playerId)
            .SetAsync(dict);

        Debug.Log("[CloudSave] Cloud save uploaded");
    }

    public async Task<PlayerData> DownloadAsync()
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning("[CloudSave] Download skipped - not authenticated");
            return null;
        }

        var snapshot = await db.Collection("users")
            .Document(playerId)
            .GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            Debug.Log("[CloudSave] No cloud save found");
            return null;
        }

        string json = snapshot.GetValue<string>("json");
        Debug.Log("[CloudSave] Cloud save downloaded");

        return JsonUtility.FromJson<PlayerData>(json);
    }
}