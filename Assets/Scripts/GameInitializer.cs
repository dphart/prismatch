using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitializer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeGame()
    {
        // ✅ Check if GameState already exists in the scene
        if (GameState.Instance == null)
        {
            GameObject gameStateObj = new GameObject("GameState");
            gameStateObj.AddComponent<GameState>();

            Debug.Log("✅ GameState created by GameInitializer before any scene loads.");
        }
    }
}