using UnityEngine;

[CreateAssetMenu(fileName = "DebugToolsSettings", menuName = "PocketMatch/Debug Tools Settings")]
public class DebugToolsSettings : ScriptableObject
{
    public int startingCoinsOverride = 500;
    public bool applyOnlyOnFreshSave = true;

    private static DebugToolsSettings cached;

    public static DebugToolsSettings Load()
    {
        if (cached != null)
            return cached;

        cached = Resources.Load<DebugToolsSettings>("DebugToolsSettings");
        if (cached != null)
            return cached;

        cached = CreateInstance<DebugToolsSettings>();
        return cached;
    }
}
