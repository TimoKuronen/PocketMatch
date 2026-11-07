using UnityEngine;
using VContainer;

public class CloudSaveBootstrap : MonoBehaviour
{
    [Inject] private ISaveService saveService;

    private async void Start()
    {
        await saveService.InitializeCloudAsync();
    }
}
