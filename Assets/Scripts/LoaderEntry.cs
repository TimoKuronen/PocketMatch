using UnityEngine;

public class LoaderEntry : MonoBehaviour
{
    private void Awake()
    {
        // Start the next scene load immediately
        Loader.ContinueFromLoader();
    }
}
