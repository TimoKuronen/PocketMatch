using UnityEngine;

public class LoaderEntry : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("[LoaderEntry] Awake - Starting scene load");
        // Start the next scene load immediately
        Loader.ContinueFromLoader();
    }

    private void OnDestroy()
    {
        //Debug.LogWarning("[LoaderEntry] DESTROYED! (Scene unload or object destroyed)");
    }
}
