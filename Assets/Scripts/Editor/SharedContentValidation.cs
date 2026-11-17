#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>New native powers must be registered in the shared catalog before shipping.</summary>
public sealed class SharedContentValidation : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report) => Validate();
    [MenuItem("Prismatch/Validate shared content")]
    public static void Validate()
    {
        SharedGameContent.Load(File.ReadAllText("Assets/Resources/Content/prismatch.json"));
        SharedGameContent.ValidateCoverage();
        LevelLibrary.ReloadSharedContent();
        UnityEngine.Debug.Log("Shared powers and levels validated.");
    }
}
#endif
