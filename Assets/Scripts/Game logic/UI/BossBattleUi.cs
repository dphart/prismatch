using System.Collections.Generic;
using UnityEngine;

public class BossBattleUi : MonoBehaviour
{
    public void Hide() { if (powerUpPanel != null) powerUpPanel.SetActive(false); }
    [Header("References")]
    [SerializeField] private GameObject powerUpCardPrefab;   // Prefab with ShortPowerCardUI attached
    [SerializeField] private Transform contentParent;        // ScrollView Content object
    [SerializeField] private GameObject powerUpPanel;        // The full panel container
    [SerializeField] private GameController controller;      // Link GameController here in Inspector

    /// <summary>
    /// Called by GameController when a round starts with boss powers.
    /// Shows the intro screen and populates it with boss power cards.
    /// </summary>
    public void ShowBossIntro(List<BossPowers> bossPowers)
    {
        if (powerUpPanel == null) return;

        // ✅ Activate panel
        powerUpPanel.SetActive(true);
        Board.Instance?.SetVisible(false);

        // ✅ Clear any old cards
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // ✅ Add a card for each boss power
        foreach (BossPowers bp in bossPowers)
        {
            AddBossCard(bp);
        }
    }

    /// <summary>
    /// Instantiates a card prefab for a BossPower and hooks it up.
    /// </summary>
    private void AddBossCard(BossPowers bossPower)
    {
        GameObject cardGO = Instantiate(powerUpCardPrefab, contentParent);
        ShortPowerCardUI cardUI = cardGO.GetComponent<ShortPowerCardUI>();

        if (cardUI == null)
        {
            Debug.LogError("❌ The prefab is missing ShortPowerCardUI!");
            return;
        }

        // ✅ Setup card with Boss Power info
        cardUI.SetupBossPower(bossPower);
    }

    /// <summary>
    /// Handles the Continue button click → hides panel and tells GameController to move on.
    /// </summary>
    public void OnContinueButton()
    {
        powerUpPanel.SetActive(false);
        controller.ShowPowerPickerAfterBossIntro();
    }
}
