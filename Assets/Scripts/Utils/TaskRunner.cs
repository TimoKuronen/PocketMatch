using System.Collections;
using UnityEngine;

public static class TaskRunner
{
    private static TaskRunnerMonoBehaviour instance;

    public static MonoBehaviour Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("TaskRunner (Hidden)");
                go.hideFlags = HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(go);
                instance = go.AddComponent<TaskRunnerMonoBehaviour>();
            }
            return instance;
        }
    }

    private class TaskRunnerMonoBehaviour : MonoBehaviour
    {
        void Awake()
        {
            // Ensure singleton behavior
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
