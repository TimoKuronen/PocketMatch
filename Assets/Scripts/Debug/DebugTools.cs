/// <summary>
/// Compile-time and runtime gate for manual testing tools.
/// </summary>
public static class DebugTools
{
    public static bool IsEnabled
    {
        get
        {
#if UNITY_EDITOR
            return true;
#elif DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }
    }
}
