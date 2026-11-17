#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PrismatchUiPreview
{
    private static bool refreshQueued, rebuildSelectorsQueued;

    static PrismatchUiPreview()
    {
        EditorSceneManager.sceneOpened += (_, _) => QueueRefresh(true);
        EditorApplication.hierarchyChanged += () => QueueRefresh(false);
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) QueueRefresh(true);
        };
        QueueRefresh(true);
    }

    private static void QueueRefresh(bool rebuildSelectors)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        rebuildSelectorsQueued |= rebuildSelectors;
        if (refreshQueued) return;
        refreshQueued = true;
        EditorApplication.delayCall += () =>
        {
            refreshQueued = false;
            bool rebuild = rebuildSelectorsQueued;
            rebuildSelectorsQueued = false;
            Refresh(rebuild);
        };
    }

    [MenuItem("Prismatch/Refresh UI Preview")]
    public static void Refresh() => Refresh(true);

    private static void Refresh(bool rebuildSelectors)
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
        foreach (var selector in Resources.FindObjectsOfTypeAll<LevelSelectUI>())
        {
            if (selector == null || !selector.gameObject.scene.IsValid() || !selector.gameObject.scene.isLoaded) continue;
            selector.ApplyEditorPreview(rebuildSelectors);
        }
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        SceneView.RepaintAll();
    }
}

[CustomEditor(typeof(LevelSelectUI))]
public sealed class LevelSelectUIPreviewInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool changed = EditorGUI.EndChangeCheck();
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            bool refresh = GUILayout.Button("Refresh Free Play Preview");
            if (changed || refresh)
            {
                var selector = (LevelSelectUI)target;
                // Build outside inspector drawing so layout and hierarchy edits are safe.
                EditorApplication.delayCall += () =>
                {
                    if (selector == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
                    selector.ApplyEditorPreview(true);
                    SceneView.RepaintAll();
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                };
            }
        }
        EditorGUILayout.HelpBox("Edit the defaults to preview either mode. Preview Scroll shows the lower controls. Use Play mode to test interaction.", MessageType.Info);
    }
}
#endif
