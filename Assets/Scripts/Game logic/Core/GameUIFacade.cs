using TMPro;
using UnityEngine;

/// <summary>
/// Facade for UI updates tied to GameController. Keeps UI calls centralized.
/// </summary>
public class GameUIFacade
{
    private readonly GameUIManager _uiManager;
    private readonly GameObject _gameOverPanel;
    private readonly TextMeshProUGUI _restartsRemainingText;

    public GameUIFacade(GameUIManager uiManager, GameObject gameOverPanel, TextMeshProUGUI restartsRemainingText)
    {
        _uiManager = uiManager;
        _gameOverPanel = gameOverPanel;
        _restartsRemainingText = restartsRemainingText;
    }

    public void UpdateScore(int score) => _uiManager?.UpdateScore(score);
    public void UpdateTarget(int target) => _uiManager?.UpdateTarget(target);
    public void UpdateTimer(float time) => _uiManager?.UpdateTimer(time);
    public void UpdateRound(int round) => _uiManager?.UpdateRound(round);

    public void ShowGameOver(int restartsLeft)
    {
        if (_restartsRemainingText != null)
        {
            _restartsRemainingText.text = $"Restarts Round (Remaining: {restartsLeft})";
        }
        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(true);
        }
    }

    public void HideGameOver()
    {
        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(false);
        }
    }
}
