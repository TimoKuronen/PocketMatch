using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
//using Firebase.Auth;
using UnityEngine;

public class CloudSaveService
{
    private FirebaseFirestore db;
  //  private FirebaseAuth auth;
    private string playerId;

    public async Task InitializeAsync()
    {
        db = FirebaseFirestore.DefaultInstance;
        playerId = SystemInfo.deviceUniqueIdentifier;

        //db = FirebaseFirestore.DefaultInstance;
        //auth = FirebaseAuth.DefaultInstance;

        //if (auth.CurrentUser == null)
        //{
        //    var result = await auth.SignInAnonymouslyAsync();
        //    Debug.Log($"Signed in anonymously: {result.User.UserId}");
        //}

        //playerId = auth.CurrentUser.UserId;
    }

    public async Task UploadAsync(PlayerData data)
    {
        string json = JsonUtility.ToJson(data);
        var dict = new Dictionary<string, object>
        {
            { "json", json },
            { "timestamp", FieldValue.ServerTimestamp }
        };

        await db.Collection("players").Document(playerId).SetAsync(dict);
        Debug.Log("Cloud save uploaded");
    }

    public async Task<PlayerData> DownloadAsync()
    {
        var snapshot = await db.Collection("players").Document(playerId).GetSnapshotAsync();
        if (snapshot.Exists)
        {
            string json = snapshot.GetValue<string>("json");
            return JsonUtility.FromJson<PlayerData>(json);
        }

        Debug.Log("No cloud save found for player.");
        return null;
    }
}
