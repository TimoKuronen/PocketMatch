using System.Collections;
using UnityEngine;

public class CoroutineMonoBehavior : MonoBehaviour
{
    public static CoroutineMonoBehavior Instance;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }
    }

    public static Coroutine RunStatic(IEnumerator routine)
    {
        if (Instance == null)
        {
            var go = new GameObject("[CoroutineMonoBehavior]");
            Instance = go.AddComponent<CoroutineMonoBehavior>();
            DontDestroyOnLoad(go);
        }

        return Instance.StartCoroutine(routine);
    }
}