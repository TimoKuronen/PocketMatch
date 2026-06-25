using UnityEngine;
using VContainer;

public class CloudSaveBootstrap : MonoBehaviour
{
    [Inject] private ISaveService saveService;

    private async void Start()
    {
        // Give Firebase a frame to initialize internally
        await System.Threading.Tasks.Task.Yield();

        await saveService.InitializeCloudAsync();
    }
}