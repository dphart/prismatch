using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossPowerCardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI levelText; // New: shows level number
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button button;
    
    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeColor = Color.yellow; // Highlight color for active powers
    
    private BossPowers currentBossPower;
    private int currentLevel;
    private bool isActive;

    /// <summary>
    /// Setup the boss power card with all its information
    /// </summary>
    public void Setup(BossPowers bossPower, int activeRound, bool isCurrentlyActive)
    {
        currentBossPower = bossPower;
        currentLevel = activeRound;
        isActive = isCurrentlyActive;

        // Set the text content
        nameText.text = bossPower.Name;
        descriptionText.text = bossPower.PowerUpDescription;
        levelText.text = activeRound.ToString();
        nameText.fontSize = 26f;
        descriptionText.fontSize = 19f;
        levelText.fontSize = 24f;

        // Set the visual state based on whether it's active in current round
        SetActiveState(isCurrentlyActive);

        // Optional: You could add click functionality here if needed
        // button.onClick.RemoveAllListeners();
        // button.onClick.AddListener(() => OnCardClicked());
    }

    /// <summary>
    /// Updates the visual state to show if the power is active or not
    /// </summary>
    private void SetActiveState(bool active)
    {
        isActive = active;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = active ? activeColor : normalColor;
            PrismatchTheme.Round(backgroundImage, active
                ? Color.Lerp(PrismatchTheme.Card, PrismatchTheme.Coral, .16f)
                : PrismatchTheme.Card);
        }
        
        // Additional visual effects for active state
        if (active)
        {
            // More prominent highlighting for active powers
            transform.localScale = Vector3.one;
            
            // Make the level text more prominent when active
            if (levelText != null)
            {
                levelText.color = Color.white;
                levelText.fontStyle = FontStyles.Bold;
            }
        }
        else
        {
            transform.localScale = Vector3.one;
            
            // Dimmed text for inactive powers
            if (levelText != null)
            {
                levelText.color = Color.gray;
                levelText.fontStyle = FontStyles.Normal;
            }
        }
    }

    /// <summary>
    /// Optional: Handle card click events
    /// </summary>
    private void OnCardClicked()
    {
        Debug.Log($"Clicked on boss power: {currentBossPower.Name} (Level {currentLevel})");
        // You could implement additional functionality here like showing detailed info
    }
}
