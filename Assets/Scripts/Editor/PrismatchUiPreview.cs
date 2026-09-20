#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PrismatchUiPreview
{
    private static bool refreshQueued;

    static PrismatchUiPreview()
    {
        EditorSceneManager.sceneOpened += (_, _) => QueueRefresh();
        EditorApplication.hierarchyChanged += QueueRefresh;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) QueueRefresh();
        };
        QueueRefresh();
    }

    private static void QueueRefresh()
    {
        if (refreshQueued || EditorApplication.isPlayingOrWillChangePlaymode) return;
        refreshQueued = true;
        EditorApplication.delayCall += () =>
        {
            refreshQueued = false;
            Refresh();
        };
    }

    [MenuItem("Prismatch/Refresh UI Preview")]
    public static void Refresh()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var controllers = Resources.FindObjectsOfTypeAll<GameController>();
        foreach (var ui in Resources.FindObjectsOfTypeAll<GameUIManager>())
        {
            if (ui == null || !ui.gameObject.scene.IsValid() || !ui.gameObject.scene.isLoaded) continue;
            var controller = System.Array.Find(controllers, candidate =>
                candidate != null && candidate.gameObject.scene == ui.gameObject.scene);
            if (controller == null) continue;
            ui.ApplyEditorPreview(controller);
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        }
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        SceneView.RepaintAll();
    }
}
#endif
