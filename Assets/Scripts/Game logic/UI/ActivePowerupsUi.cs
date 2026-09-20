using System;
using System.Collections.Generic;
using UnityEngine;

public class ActivePowerUpsUI : MonoBehaviour
{
    public bool IsOpen => powerUpPanel != null && powerUpPanel.activeSelf;
    public void HidePanel() { if (powerUpPanel != null) powerUpPanel.SetActive(false); }
    [Header("References")]
    [SerializeField] private GameObject powerUpCardPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject powerUpPanel;
    [SerializeField] private GameController controller;
    private float currentCardWidth;

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
        currentCardWidth = PowerPanelLayout.Configure(powerUpPanel, contentParent, "Your Powers");
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        if (controller.PowerManager == null)
        {
            Debug.LogWarning("No PowerManager linked to ActivePowerUpsUI!");
            return;
        }

        foreach (PowerUp power in controller.PowerManager.ActivePowers())
        {
            AddCard(power);
        }
    }

    private void AddCard(PowerUp power)
    {
        GameObject cardGO = Instantiate(powerUpCardPrefab, contentParent);
        ShortPowerCardUI cardUI = cardGO.GetComponent<ShortPowerCardUI>();

        if (cardUI == null)
        {
            Debug.LogError("The prefab is missing ShortPowerCardUI!");
            return;
        }

        // Active view: show dynamic/current description
        cardUI.Setup(power, OnCardClicked, useDynamicDescription: true);
        PowerPanelLayout.SizeCard(cardGO.transform as RectTransform, currentCardWidth);
        cardUI.ApplyResponsiveLayout(currentCardWidth, PowerPanelLayout.CardHeight);
    }

    private void OnCardClicked(PowerUp power)
    {
        controller?.ShowMessage($"{power.Name}: {power.GetCurrentPowerDescription()}");
    }
}
