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
        currentCardWidth = PowerPanelLayout.Configure(powerUpPanel, contentParent, "Powers", "Level mechanics and your bonuses");
        if (powerUpPanel.transform.Find("ClosePowers") == null)
        {
            var close = new GameObject("ClosePowers", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            close.transform.SetParent(powerUpPanel.transform, false);
            var rect = (RectTransform)close.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-16, -16);
            rect.sizeDelta = new Vector2(68, 68);
            PrismatchTheme.Round(close.GetComponent<UnityEngine.UI.Image>(), PrismatchTheme.RaisedNavy);
            close.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => controller.ToggleActivePowerUpScreen());
            var label = new GameObject("CloseLabel", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            label.transform.SetParent(close.transform, false);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var text = label.GetComponent<TMPro.TextMeshProUGUI>();
            text.text = "X"; text.fontSize = 30; text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = PrismatchTheme.Cream; text.raycastTarget = false;
        }
        foreach (Transform child in contentParent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        if (controller.PowerManager == null)
        {
            Debug.LogWarning("No PowerManager linked to ActivePowerUpsUI!");
            return;
        }

        if (controller.PowerManager.LevelPowers.Count > 0)
        {
            AddSectionHeading("Level power");
            foreach (var power in controller.PowerManager.LevelPowers)
                AddCard(power);
        }

        var collectedPowers = controller.PowerManager.ActivePowers();
        if (collectedPowers.Count > 0)
            AddSectionHeading("Your powers");
        foreach (PowerUp power in collectedPowers)
        {
            AddCard(power);
        }

        Canvas.ForceUpdateCanvases();
        var scroll = powerUpPanel.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
    }

    private void AddSectionHeading(string title)
    {
        var heading = new GameObject(title, typeof(RectTransform),
            typeof(TMPro.TextMeshProUGUI), typeof(UnityEngine.UI.LayoutElement));
        heading.transform.SetParent(contentParent, false);
        var text = heading.GetComponent<TMPro.TextMeshProUGUI>();
        text.text = title;
        text.fontSize = 32f;
        text.fontStyle = TMPro.FontStyles.Bold;
        text.color = PrismatchTheme.Cream;
        text.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        var layout = heading.GetComponent<UnityEngine.UI.LayoutElement>();
        layout.minWidth = layout.preferredWidth = currentCardWidth;
        layout.minHeight = layout.preferredHeight = 48f;
        layout.flexibleHeight = 0f;
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
