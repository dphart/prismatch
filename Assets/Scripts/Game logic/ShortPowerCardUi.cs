using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShortPowerCardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image icon;
    [SerializeField] private Button button;

    private PowerUp currentPower;
    private Action<PowerUp> clickAction;

    /// <summary>
    /// Setup the card UI and attach the click callback.
    /// </summary>
    public void Setup(PowerUp power, Action<PowerUp> onClick)
    {
        currentPower = power;
        clickAction = onClick;

        // ✅ Populate UI
        nameText.text = power.Name;
        descriptionText.text = power.GetCurrentPowerDescription();
        // If you have an icon:
        // icon.sprite = power.Icon;

        // ✅ Hook up click event
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => clickAction?.Invoke(currentPower));
    }
    public void SetupBossPower(BossPowers bossPower)
    {
        nameText.text = bossPower.Name;
        descriptionText.text = bossPower.PowerUpDescription;

        if (bossPower.Icon != null)
            icon.sprite = bossPower.Icon;
    }
}
