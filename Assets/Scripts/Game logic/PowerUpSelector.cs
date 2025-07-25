using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class PowerUpSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private PowerCardUI[] cardSlots;
    [SerializeField] public GameController gameController;

    public void ShowChoices()
    {
        List<PowerUp> choices = GetRandomPowerUps(3);
        Debug.Log($"We have {choices.Count} powers returned");

        Debug.Log($"Setting up {cardSlots.Length} card slots...");

        for (int i = 0; i < cardSlots.Length; i++)
        {
            cardSlots[i].gameObject.SetActive(true);
            Debug.Log($"Setting up card {i}");
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
        List<PowerUp> source = PowerUpLibrary.All;

        var valid = new List<PowerUp>();


        //foreach (var iconType in source)
        {
            var listOfItemTypes = ItemUtils.GetItemTypes(gameController.IconList);
            foreach (PowerUp p in source)
            {
                if(p is IconBonusPower)
                {
                    if (listOfItemTypes.Contains(((IconBonusPower)(p)).ItemType))
                    {
                        valid.Add(p);
                    }
                } else 
                {
                    valid.Add(p);
                }
            }
        }

        return source
            .OrderBy(_ => Random.value)
            .Take(count)
            .Select(ClonePowerUp)
            .ToList();
    }

    private PowerUp ClonePowerUp(PowerUp original)
    {
        return original?.Clone();
    }

   
}
