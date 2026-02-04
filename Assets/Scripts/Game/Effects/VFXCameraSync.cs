using UnityEngine;

/// <summary>
/// Syncs the VFX camera with the main camera's position, rotation, and orthographic settings.
/// Attach this to your VFX camera GameObject.
/// </summary>
[RequireComponent(typeof(Camera))]
public class VFXCameraSync : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private bool syncPosition = true;
    [SerializeField] private bool syncRotation = true;
    [SerializeField] private bool syncOrthographic = true;
    [SerializeField] private bool syncOrthographicSize = true;
    
    private Camera vfxCamera;
    
    private void Awake()
    {
        vfxCamera = GetComponent<Camera>();
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("[VFXCameraSync] Main camera not found! Assign it in the inspector or ensure Camera.main is set.");
            enabled = false;
            return;
        }
        
        SyncCamera();
    }
    
    private void LateUpdate()
    {
        SyncCamera();
    }
    
    private void SyncCamera()
    {
        if (mainCamera == null || vfxCamera == null) return;
        
        if (syncPosition)
        {
            vfxCamera.transform.position = mainCamera.transform.position;
        }
        
        if (syncRotation)
        {
            vfxCamera.transform.rotation = mainCamera.transform.rotation;
        }
        
        if (syncOrthographic)
        {
            vfxCamera.orthographic = mainCamera.orthographic;
        }
        
        if (syncOrthographicSize && mainCamera.orthographic)
        {
            vfxCamera.orthographicSize = mainCamera.orthographicSize;
        }
    }
}
