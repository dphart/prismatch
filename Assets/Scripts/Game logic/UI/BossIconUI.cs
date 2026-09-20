using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the Boss Icon FAB (Floating Action Button) that shows the count of active boss powers
/// and opens the boss power display panel when clicked.
/// </summary>
public class BossIconUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button iconButton;
    [SerializeField] private TextMeshProUGUI countText;
    
    [Header("Dependencies")]
    [SerializeField] private GameController gameController;
    
    [Header("Visual Settings")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Red-ish for boss powers
    [SerializeField] private Color highlightColor = new Color(1f, 0.3f, 0.3f, 1f);
    
    private void Start()
    {
        if (iconButton == null)
            iconButton = GetComponent<Button>();
            
        iconButton.onClick.AddListener(OnIconClicked);
        
        // Set initial background color
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
            
        UpdateDisplay();
    }
    
    /// <summary>
    /// Called when the boss icon is clicked - toggles the boss power display panel through GameController
    /// This follows the same pattern as the Powerups FAB and properly pauses the game
    /// </summary>
    private void OnIconClicked()
    {
        if (gameController != null)
        {
            gameController.ToggleBossPowerScreen();
        }
        else
        {
            Debug.LogWarning("GameController not assigned to BossIconUI!");
        }
    }
    
    /// <summary>
    /// Updates the count display and visibility based on level boss powers
    /// Now goes through GameController for consistency
    /// </summary>
    public void UpdateDisplay()
    {
        if (gameController == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // Check if current level has any boss powers via GameController
        bool levelHasBossPowers = gameController.DoesCurrentLevelHaveBossPowers();
        
        if (levelHasBossPowers)
        {
            // Count total boss powers in the level via GameController
            int totalBossPowers = gameController.GetTotalBossPowersInCurrentLevel();
            
            if (countText != null)
                countText.text = totalBossPowers.ToString();
            
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Public method that can be called by GameController when boss powers change
    /// </summary>
    public void OnBossPowersChanged()
    {
        UpdateDisplay();
    }
}