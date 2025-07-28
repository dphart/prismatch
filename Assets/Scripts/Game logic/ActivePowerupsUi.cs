using System;
using System.Collections.Generic;
using UnityEngine;

public class ActivePowerUpsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject powerUpCardPrefab;   // ✅ Prefab with ShortPowerCardUI attached
    [SerializeField] private Transform contentParent;        // ✅ ScrollView Content object
    [SerializeField] private GameObject powerUpPanel;        // ✅ The full panel container
    [SerializeField] private GameController controller;      // ✅ Source of truth for active powers

    /// <summary>
    /// Toggle the panel on/off and refresh when opened.
    /// </summary>
    public void TogglePanel()
    {
        if (powerUpPanel == null) return;

        bool isActive = !powerUpPanel.activeSelf;
        powerUpPanel.SetActive(isActive);

        if (isActive)
            RefreshUI();
    }

    /// <summary>
    /// Clears old cards and repopulates from PowerManager.
    /// </summary>
    private void RefreshUI()
    {
        // ✅ Remove old cards
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // ✅ Repopulate cards
        if (controller.PowerManager == null)
        {
            Debug.LogWarning("⚠️ No PowerManager linked to ActivePowerUpsUI!");
            return;
        }

        foreach (PowerUp power in controller.PowerManager.ActivePowers())
        {
            AddCard(power);
        }
    }

    /// <summary>
    /// Instantiates a card prefab and hooks up the click behavior.
    /// </summary>
    private void AddCard(PowerUp power)
    {
        GameObject cardGO = Instantiate(powerUpCardPrefab, contentParent);
        ShortPowerCardUI cardUI = cardGO.GetComponent<ShortPowerCardUI>();

        if (cardUI == null)
        {
            Debug.LogError("❌ The prefab is missing ShortPowerCardUI!");
            return;
        }

        // ✅ Setup card with click callback
        cardUI.Setup(power, OnCardClicked);
    }

    /// <summary>
    /// Handles clicks on cards.
    /// </summary>
    private void OnCardClicked(PowerUp power)
    {
        Debug.Log($"🖱 Clicked on power-up: {power.Name}");
        // TODO: Implement what should happen (show details, activate, etc.)
    }
}
