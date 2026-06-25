using UnityEditor;

[InitializeOnLoad]
static class BoardDebugHooksEditor
{
    static BoardDebugHooksEditor()
    {
        BoardDebugHooks.BoardInitialized += BoardDebugService.OnBoardInitialized;
        BoardDebugHooks.BoardUpdated += BoardDebugService.OnBoardUpdated;
        BoardDebugHooks.BoardShuffled += BoardDebugService.OnBoardShuffled;
    }
}
