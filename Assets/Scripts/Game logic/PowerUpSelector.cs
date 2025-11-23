using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PowerUpSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;                // Panel that displays power-up choices
    [SerializeField] private GameObject shortPowerCardPrefab; // Prefab for ShortPowerCardUI
    [SerializeField] private Transform cardParent;            // ScrollView Content object
    [SerializeField] public GameController gameController;

    private List<PowerUp> powerUps; 
    private readonly HashSet<string> _offeredThisLevel = new HashSet<string>();

    /// <summary>
    /// Displays random power-ups for selection.
    /// </summary>
    public void ShowChoices()
    {
        // 🔹 Clear old cards before spawning new ones
        foreach (Transform child in cardParent)
        {
            Destroy(child.gameObject);
        }

        // 🔹 Get 3 random powerups FROM THE LEVEL
        List<PowerUp> choices = GetRandomPowerUps(3);
        Debug.Log($"🎴 Showing {choices.Count} power-ups to the player");

        // 🔹 Log these choices so they aren't offered again
        foreach (var choice in choices)
        {
            _offeredThisLevel.Add(choice.Name);
        }

        // 🔹 Spawn new ShortPowerCards into the scroll content
        foreach (PowerUp power in choices)
        {
            GameObject cardObj = Instantiate(shortPowerCardPrefab, cardParent);
            ShortPowerCardUI cardUI = cardObj.GetComponent<ShortPowerCardUI>();

            if (cardUI != null)
            {
                // ✅ Setup card with the power and a click callback
                cardUI.Setup(power, OnPowerUpSelected);
            }
            else
            {
                Debug.LogWarning("⚠️ ShortPowerCard prefab is missing ShortPowerCardUI!");
            }
        }

        // 🔹 Show the panel
        panel.SetActive(true);
        Debug.Log($"✅ PowerUpSelector panel shown.");
    }

    /// <summary>
    /// Hides the panel and clears the power-up cards.
    /// </summary>
    public void Hide()
    {
        panel.SetActive(false);

        // Optional: clear the cards when hiding
        foreach (Transform child in cardParent)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Handles when a power-up card is clicked.
    /// </summary>
    private void OnPowerUpSelected(PowerUp selectedPower)
    {
        Debug.Log($"🖱 Selected power-up: {selectedPower.Name}");

        // ✅ Add the chosen power to the player
        gameController.RegisterSelectedPower(selectedPower);
        gameController.OnPowerUpSelected();

        // ✅ Hide the selector after a choice is made
        Hide();
    }

    /// <summary>
    /// Picks random power-ups from the CURRENT LEVEL (clones to avoid shared state).
    /// </summary>
    private List<PowerUp> GetRandomPowerUps(int count)
    {
        // 🔹 Pull available powers from the current level, excluding ones already offered
        List<PowerUp> source = GameState.Instance.CurrentLevel.GetPowerUpsForRound(gameController.CurrentRound)
            .Where(p => !_offeredThisLevel.Contains(p.Name))
            .ToList();

        var valid = new List<PowerUp>();

        // ✅ Filter for only powers valid for current icon list
        var listOfItemTypes = ItemUtils.GetItemTypes(gameController.IconList);
        foreach (PowerUp p in source)
        {
            if (p is IconBonusPower iconBonus)
            {
                if (listOfItemTypes.Contains(iconBonus.ItemType))
                {
                    valid.Add(p);
                }
            }
            else
            {
                valid.Add(p);
            }
        }

        // ✅ Shuffle + clone so each card gets its own copy
        return valid
            .OrderBy(_ => Random.value)
            .Take(count)
            .Select(ClonePowerUp)
            .ToList();
    }

    private PowerUp ClonePowerUp(PowerUp original)
    {
        return original?.Clone();
    }
    
    /// <summary>
    /// Clears the set of power-ups that have been offered in this level.
    /// </summary>
    public void ResetOfferedList()
    {
        _offeredThisLevel.Clear();
        Debug.Log("🔄 Power-up offer list has been reset.");
    }

    public void ResetPowerSelection()
    {
        powerUps =  PowerUpLibrary.GetAll();
    }
}
