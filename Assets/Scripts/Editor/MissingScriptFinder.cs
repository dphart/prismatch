#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class MissingScriptFinder
{
    [MenuItem("Tools/Find Missing Scripts in Scene")]
    public static void FindMissingScripts()
    {
        Debug.Log("🔍 Running script...");
        int missingCount = 0;

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject go in allObjects)
        {
            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.LogWarning($"⚠️ Missing script on: {GetGameObjectPath(go)}", go);
                    missingCount++;
                }
            }
        }

        Debug.Log(missingCount == 0
            ? "✅ No missing scripts found."
            : $"❗ Total missing scripts found: {missingCount}");
    }

    private static string GetGameObjectPath(GameObject go)
    {
        if (go.transform.parent == null) return go.name;
        return GetGameObjectPath(go.transform.parent.gameObject) + "/" + go.name;
    }
}
#endif
