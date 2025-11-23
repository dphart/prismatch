using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the boss power display panel that shows all active boss powers with their details.
/// Similar to ActivePowerUpsUI but specialized for boss powers.
/// </summary>
public class BossPowerDisplayUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject bossPowerCardPrefab;   // Prefab with BossPowerCardUI attached
    [SerializeField] private Transform contentParent;         // ScrollView Content object
    [SerializeField] private GameObject bossPowerPanel;       // The full panel container
    [SerializeField] private GameController controller;       // Link GameController here in Inspector

    /// <summary>
    /// Toggle the panel on/off and refresh when opened.
    /// </summary>
    public void TogglePanel()
    {
        if (bossPowerPanel == null) 
        {
            Debug.LogWarning("Boss Power Panel not assigned in BossPowerDisplayUI!");
            return;
        }

        bool isActive = !bossPowerPanel.activeSelf;
        bossPowerPanel.SetActive(isActive);

        if (isActive)
            RefreshUI();
    }

    /// <summary>
    /// Force show the panel and refresh content
    /// </summary>
    public void ShowPanel()
    {
        if (bossPowerPanel != null)
        {
            bossPowerPanel.SetActive(true);
            RefreshUI();
        }
    }

    /// <summary>
    /// Force hide the panel
    /// </summary>
    public void HidePanel()
    {
        if (bossPowerPanel != null)
        {
            bossPowerPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Clears old cards and repopulates with ALL boss powers from the current level.
    /// </summary>
    private void RefreshUI()
    {
        // Clear old cards
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        if (controller == null)
        {
            Debug.LogWarning("⚠️ No GameController linked to BossPowerDisplayUI!");
            return;
        }

        // Get all boss powers from the current level
        var allBossPowers = GetAllBossPowersFromLevel();
        
        if (allBossPowers.Count == 0)
        {
            Debug.Log("No boss powers found in current level");
            return;
        }

        foreach (var bossPowerInfo in allBossPowers)
        {
            AddCard(bossPowerInfo.power, bossPowerInfo.activeRound);
        }
        
        Debug.Log($"Added {allBossPowers.Count} boss power cards to display");
    }

    /// <summary>
    /// Get all boss powers from the current level with their active rounds
    /// </summary>
    private List<BossPowerInfo> GetAllBossPowersFromLevel()
    {
        var result = new List<BossPowerInfo>();
        var currentLevel = GameState.Instance?.CurrentLevel;
        
        if (currentLevel == null) return result;

        var bossPowerMap = currentLevel.GetBossPowersMap();
        if (bossPowerMap == null) return result;

        // Use a dictionary to track unique powers and their first active round
        var powers = new List<BossPowerInfo>();

        for (int round = 1; round <= currentLevel.GetTotalRounds; round++)
        {
            var powersForRound = bossPowerMap.GetBossPowersForRound(round);
            if (powersForRound != null)
            {
                foreach (var power in powersForRound)
                {
                    // Only add if we haven't seen this power before (by name)


                    powers.Add(new BossPowerInfo
                    {
                        power = power,
                        activeRound = round
                    });
                    
                }
            }
        }

        result.AddRange(powers);
        return result;
    }

    /// <summary>
    /// Data structure to hold boss power and its active round
    /// </summary>
    private struct BossPowerInfo
    {
        public BossPowers power;
        public int activeRound;
    }

    /// <summary>
    /// Instantiates a card prefab and hooks up the display.
    /// </summary>
    private void AddCard(BossPowers bossPower, int activeRound)
    {
        if (bossPowerCardPrefab == null)
        {
            Debug.LogError("❌ Boss Power Card Prefab not assigned!");
            return;
        }

        GameObject cardGO = Instantiate(bossPowerCardPrefab, contentParent);
        BossPowerCardUI cardUI = cardGO.GetComponent<BossPowerCardUI>();

        if (cardUI == null)
        {
            Debug.LogError("❌ The prefab is missing BossPowerCardUI!");
            return;
        }

        // Check if this power is active in the current round
        bool isActive = (controller.CurrentRound == activeRound);

        // Setup card with boss power info
        cardUI.Setup(bossPower, activeRound, isActive);
    }
}