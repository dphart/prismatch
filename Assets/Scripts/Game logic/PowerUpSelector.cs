using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PowerUpSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel; // The UI container (like the black box)
    [SerializeField] private PowerCardUI[] cardSlots; // PowerCard_1, 2, 3
    [SerializeField] public GameController gameController; // Drag your GameController here

    [Header("Power-Up Pool")]
    public List<PowerUp> allAvailablePowers = new(); // Optional if not using static library

    public void ShowChoices()
    {
        // Choose 3 random power-ups
        List<PowerUp> choices = GetRandomPowerUps(3);

        Debug.Log($"Setup for X cards {cardSlots.Length}" );


        for (int i = 0; i < cardSlots.Length; i++)
        {
            cardSlots[i].gameObject.SetActive(true);
            Debug.Log("Setup for card" +  i);
            cardSlots[i].Setup(choices[i], this);
        }

        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }


    private List<PowerUp> GetRandomPowerUps(int count)
    {
        // Use a static library if desired
        List<PowerUp> source = allAvailablePowers.Count > 0 ? allAvailablePowers : PowerUpLibrary.All;

        return source
            .OrderBy(_ => Random.value)
            .Take(count)
            .Select(p => ClonePowerUp(p))
            .ToList();
    }

    private PowerUp ClonePowerUp(PowerUp original)
    {
        // Factory-style recreation to avoid instance reuse side effects
        if (original is AddDiagonalScoring) return new AddDiagonalScoring();
        if (original is FlexibleMatchPower f) return new FlexibleMatchPower(f.increase);
        if (original is BasePowerUp) return new BasePowerUp();
        // Add new types here

        Debug.LogWarning($"Unknown power-up type: {original.GetType()}");
        return null;
    }
}
