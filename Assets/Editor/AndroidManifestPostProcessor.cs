using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;

public class AndroidManifestPostProcessor : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
        
        if (!File.Exists(manifestPath))
        {
            UnityEngine.Debug.LogWarning($"[AndroidManifestPostProcessor] Manifest not found at {manifestPath}");
            return;
        }

        XmlDocument manifest = new XmlDocument();
        manifest.Load(manifestPath);

        XmlNode root = manifest.DocumentElement;
        if (root == null)
        {
            UnityEngine.Debug.LogError("[AndroidManifestPostProcessor] Could not find root element in manifest");
            return;
        }

        // Check if ACCESS_WIFI_STATE permission already exists
        bool hasWifiPermission = false;
        foreach (XmlNode node in root.ChildNodes)
        {
            if (node.Name == "uses-permission")
            {
                XmlAttribute nameAttr = node.Attributes?["android:name"];
                if (nameAttr != null && nameAttr.Value == "android.permission.ACCESS_WIFI_STATE")
                {
                    hasWifiPermission = true;
                    break;
                }
            }
        }

        // Add the permission if it doesn't exist
        if (!hasWifiPermission)
        {
            XmlElement permission = manifest.CreateElement("uses-permission");
            permission.SetAttribute("android:name", "http://schemas.android.com/apk/res/android", "android.permission.ACCESS_WIFI_STATE");
            root.InsertBefore(permission, root.FirstChild);
            manifest.Save(manifestPath);
            UnityEngine.Debug.Log("[AndroidManifestPostProcessor] Added ACCESS_WIFI_STATE permission to AndroidManifest.xml");
        }
        else
        {
            UnityEngine.Debug.Log("[AndroidManifestPostProcessor] ACCESS_WIFI_STATE permission already exists in AndroidManifest.xml");
        }
    }
}
