using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ActivePowerUpsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject powerUpCardPrefab;   // Prefab for each power card
    [SerializeField] private Transform contentParent;        // ScrollView Content

    // ✅ Now we track cards by the GUID Id on each PowerUp
    private readonly Dictionary<Guid, GameObject> activeCards = new Dictionary<Guid, GameObject>();

    /// <summary>
    /// Creates a card for the given power-up and tracks it by its Id.
    /// </summary>
    public void AddCard(PowerUp power)
    {
        if (activeCards.ContainsKey(power.Id))
        {
            Debug.LogWarning($"⚠️ Tried to add duplicate card for power-up with ID {power.Id}");
            return;
        }

        GameObject card = Instantiate(powerUpCardPrefab, contentParent);
        activeCards[power.Id] = card;

        UpdateCardUI(card, power);
        Debug.Log($"🃏 Added Power Card for {power.Name} (ID: {power.Id}) for {activeCards.Count}");
    }

    /// <summary>
    /// Removes a card for the given power-up.
    /// </summary>
    public void RemoveCard(PowerUp power)
    {
        if (activeCards.TryGetValue(power.Id, out GameObject card))
        {
            Destroy(card);
            activeCards.Remove(power.Id);
            Debug.Log($"❌ Removed Power Card for {power.Name} (ID: {power.Id})");
        }
        else
        {
            Debug.LogWarning($"⚠️ Tried to remove a card for {power.Name} but no card was found.");
        }
    }

    /// <summary>
    /// Clears all cards from the UI.
    /// </summary>
    public void ClearAllCards()
    {
        foreach (GameObject card in activeCards.Values)
        {
            Destroy(card);
        }

        activeCards.Clear();
        Debug.Log("🧹 Cleared all active power-up cards.");
    }

    /// <summary>
    /// Updates the UI card fields for a given power-up.
    /// </summary>
    private void UpdateCardUI(GameObject card, PowerUp power)
    {
        TextMeshProUGUI nameText = card.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = card.transform.Find("Description").GetComponent<TextMeshProUGUI>();

        nameText.text = power.Name;
        descriptionText.text = power.GetCurrentPowerDescription();
    }
}
