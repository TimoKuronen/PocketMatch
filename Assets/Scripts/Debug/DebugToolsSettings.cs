using UnityEngine;

public enum DebugLocaleOverride
{
    DeviceDefault = 0,
    English = 1,
    Spanish = 2,
}

[CreateAssetMenu(fileName = "DebugToolsSettings", menuName = "PocketMatch/Debug Tools Settings")]
public class DebugToolsSettings : ScriptableObject
{
    public int startingCoinsOverride = 500;
    public bool applyOnlyOnFreshSave = true;

    [Tooltip("Editor and development builds only. DeviceDefault uses Unity startup locale selectors. Game View also has a live locale dropdown during Play Mode.")]
    public DebugLocaleOverride localeOverride = DebugLocaleOverride.DeviceDefault;

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

    public string GetLocaleOverrideCode()
    {
        switch (localeOverride)
        {
            case DebugLocaleOverride.English:
                return "en";
            case DebugLocaleOverride.Spanish:
                return "es";
            default:
                return null;
        }
    }
}
