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

    // Call this from PowerUpSelector to initialize this card
    public void Setup(PowerUp power, PowerUpSelector selectorContext)
    {

        powerUp = power;
        selector = selectorContext;

        //nameText.text = powerUp.Name;
        descriptionText.text = powerUp.Description;
        icon.sprite = powerUp.Icon;

        // Enable or disable icon display
        icon.gameObject.SetActive(powerUp.Icon != null);


        // Hook up button (optional — or wire through inspector)
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelect);
    }

    private void OnSelect()
    {
        Debug.Log($"Clicked on: {powerUp?.Name}");
        powerUp.Apply(GameState.Instance);
        GameState.Instance.RegisterPowerUp(powerUp);

        Debug.Log($"Selected power-up: {powerUp.Name}");
        Debug.Log($"You now have {GameState.Instance.ActivePowers.Count} active powers");


        // Notify GameController to start next level
        selector.Hide();
        selector.gameController.OnPowerUpSelected();
    }
}
