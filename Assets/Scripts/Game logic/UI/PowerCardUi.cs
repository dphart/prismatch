using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerCardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image icon;
    //[SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button selectButton;

    private PowerUp powerUp;
    private PowerUpSelector selector;

    public void Setup(PowerUp power, PowerUpSelector selectorContext)
    {
        powerUp = power;
        selector = selectorContext;

        //nameText.text = powerUp.Name;
        descriptionText.text = powerUp.PowerUpDescription;
        icon.sprite = powerUp.Icon;
        icon.gameObject.SetActive(powerUp.Icon != null);

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelect);
    }

    private void OnSelect()
    {
        Debug.Log($"🃏 Selected power-up: {powerUp?.Name}");

        // Register power-up with controller (which owns the PowerManager)
        selector.gameController.RegisterSelectedPower(powerUp);

        selector.Hide();
        selector.gameController.OnPowerUpSelected();
    }
}
