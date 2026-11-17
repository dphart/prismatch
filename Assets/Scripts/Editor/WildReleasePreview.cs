#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Playtest the new mechanic without changing campaign unlocks.</summary>
[InitializeOnLoad]
public static class WildReleasePreview
{
    private const string Pending="Prismatch.PreviewWildRelease";
    static WildReleasePreview()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Pending,false))return;
            SessionState.SetBool(Pending,false);EditorApplication.delayCall+=Launch;
        };
    }
    [MenuItem("Prismatch/Preview/Wild Release")]
    public static void Preview()
    {
        if(EditorApplication.isPlaying){Launch();return;}
        SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    private static void Launch()
    {
        if(GameState.Instance==null)new GameObject("Preview GameState").AddComponent<GameState>();
        GameState.Instance.CurrentLevel=LevelLibrary.GetLevel(31).CloneNew();
        SceneManager.LoadScene("prismatch");
    }
    [MenuItem("Prismatch/Preview/Charge Wild Release Cage")]
    public static void ChargeCage()
    {
        var board=Board.Instance;
        if(board?.Strategy?.Bonus!=LevelBonus.WildRelease||board.gameController?.CanInteract!=true)return;
        var cage=board.Strategy.ReleaseCage;
        var neighbor=new BoardModel.Cell(cage.X==0?1:cage.X-1,cage.Y);
        for(int i=0;i<StrategyLevelState.CageMatchesRequired;i++)
            board.Strategy.PrepareWave(board.Model,new List<List<BoardModel.Cell>>{new(){neighbor}},null);
        typeof(Board).GetMethod("SyncVisuals",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(board,null);
        board.gameController.GameUIManager?.UpdateAnimalBonus(board.gameController,board);
    }
    [MenuItem("Prismatch/Preview/Charge Wild Release Cage",true)]
    private static bool CanCharge()=>EditorApplication.isPlaying&&Board.Instance?.Strategy?.Bonus==LevelBonus.WildRelease&&Board.Instance.gameController.CanInteract;
}
#endif
