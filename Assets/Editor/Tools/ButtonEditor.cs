using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UnityEngine.Object), true)]
public class ButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var targetObject = target;
        var methods = targetObject.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var method in methods)
        {
            var attributes = method.GetCustomAttributes(typeof(ButtonAttribute), true);
            if (attributes.Length > 0)
            {
                if (GUILayout.Button(method.Name))
                {
                    method.Invoke(targetObject, null);
                }
            }
        }
    }
}