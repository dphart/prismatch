using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpSelector : MonoBehaviour
{
    private const float HorizontalCardMargin = 16f;
    private const float CardHeight = 180f;
    private const float CardSpacing = 12f;
    private const float VerticalListPadding = 8f;
    private const float ChoiceViewportHeight = CardHeight * 3f + CardSpacing * 2f + VerticalListPadding * 2f;
    private const float PickerHeight = 760f;
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject shortPowerCardPrefab;
    [SerializeField] private Transform cardParent;
    [SerializeField] public GameController gameController;
    private readonly HashSet<string> selected = new();
    private bool acceptingChoice;

    public void ShowChoices()
    {
        if (gameController.Phase != GamePhase.PowerSelection) return;
        panel.SetActive(true);
        ResizePickerFrame();
        Canvas.ForceUpdateCanvases();
        ConfigureCardContainer();
        Canvas.ForceUpdateCanvases();
        var choiceScroll = panel.GetComponentInChildren<ScrollRect>(true);
        float parentWidth = choiceScroll != null && choiceScroll.viewport != null
            ? choiceScroll.viewport.rect.width
            : (cardParent as RectTransform)?.rect.width ?? 0f;
        float cardWidth = Mathf.Max(0f, parentWidth - HorizontalCardMargin * 2f);
        foreach (Transform child in cardParent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        var level = GameState.Instance.CurrentLevel;
        selected.Clear();
        selected.UnionWith(gameController.PowerManager.ActivePowers().Select(p => p.Name));
        var source = gameController.PowerManager.ActivePowers().Count == 0
            ? PowerUpLibrary.Starters() : level.GetPowerUpsForRound(gameController.CurrentRound);
        source = source.Where(power => power.CanUseWithMatchRules(gameController.GetEffectiveMatchRules())).ToList();
        var choices = BuildChoices(source, selected, ItemUtils.GetItemTypes(gameController.IconList), 3, new System.Random(Random.Range(0, int.MaxValue)));
        foreach (var power in choices)
        {
            var card = Instantiate(shortPowerCardPrefab, cardParent).GetComponent<ShortPowerCardUI>();
            ResizeCard(card.transform as RectTransform, cardWidth);
            card.Setup(power, OnPowerUpSelected);
            card.ApplyResponsiveLayout(cardWidth, CardHeight);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardParent as RectTransform);
        AnimateChoices();
        if (choiceScroll != null) choiceScroll.verticalNormalizedPosition = 1f;
        acceptingChoice = true;
        Board.Instance?.SetVisible(false);
    }

    private void ResizePickerFrame()
    {
        if (panel.transform is RectTransform panelRect && panelRect.parent is RectTransform parentRect)
        {
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Mathf.Max(0f, parentRect.rect.width - HorizontalCardMargin * 2f));
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Min(PickerHeight, Mathf.Max(0f, parentRect.rect.height - HorizontalCardMargin * 2f)));
        }

        var scroll = panel.GetComponentInChildren<ScrollRect>(true);
        if (scroll == null) return;
        scroll.vertical = true;
        if (scroll.transform is RectTransform scrollRect)
        {
            scrollRect.sizeDelta = new Vector2(0f, ChoiceViewportHeight);
            scrollRect.anchoredPosition = new Vector2(scrollRect.anchoredPosition.x, -55f);
        }
    }

    private void ConfigureCardContainer()
    {
        if (panel.TryGetComponent(out Image panelImage))
        {
            PrismatchTheme.Round(panelImage, new Color(PrismatchTheme.Navy.r, PrismatchTheme.Navy.g, PrismatchTheme.Navy.b, .97f));
            PrismatchTheme.Shadow(panelImage, new Color(0f, .04f, .12f, .55f), new Vector2(0f, -7f));
        }
        StyleHeading();
        if (cardParent.TryGetComponent(out VerticalLayoutGroup layout))
        {
            layout.padding.left = Mathf.RoundToInt(HorizontalCardMargin);
            layout.padding.right = Mathf.RoundToInt(HorizontalCardMargin);
            layout.padding.top = Mathf.RoundToInt(VerticalListPadding);
            layout.padding.bottom = Mathf.RoundToInt(VerticalListPadding);
            layout.spacing = CardSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
        }
        if (cardParent is RectTransform contentRect)
        {
            contentRect.anchorMin = new Vector2(0f, contentRect.anchorMin.y);
            contentRect.anchorMax = new Vector2(1f, contentRect.anchorMax.y);
            contentRect.pivot = new Vector2(.5f, contentRect.pivot.y);
            contentRect.anchoredPosition = new Vector2(0f, contentRect.anchoredPosition.y);
            contentRect.sizeDelta = new Vector2(0f, contentRect.sizeDelta.y);
        }
        var contentFitter = cardParent.GetComponent<ContentSizeFitter>();
        if (contentFitter != null) contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    private void StyleHeading()
    {
        foreach (var label in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (!label.text.Contains("Select one")) continue;
            label.text = "Select a <color=#FF9F1C>Powerup</color>";
            label.fontSize = 38f;
            label.fontStyle = FontStyles.Bold;
            label.color = PrismatchTheme.Cream;
            if (panel.transform.Find("PowerChoiceSubtitle") != null) return;
            var subtitleObject = new GameObject("PowerChoiceSubtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subtitleObject.transform.SetParent(panel.transform, false);
            var rect = (RectTransform)subtitleObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0f, 245f);
            rect.sizeDelta = new Vector2(500f, 38f);
            var subtitle = subtitleObject.GetComponent<TextMeshProUGUI>();
            subtitle.text = "P I C K   O N E   T O   B O O S T   Y O U R   R U N";
            subtitle.fontSize = 15f;
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.color = PrismatchTheme.Mist;
            subtitle.raycastTarget = false;
            return;
        }
    }

    private void AnimateChoices()
    {
        int index = 0;
        foreach (Transform child in cardParent)
        {
            if (!child.gameObject.activeSelf || child.GetComponent<ShortPowerCardUI>() == null) continue;
            var rect = child as RectTransform;
            var group = child.GetComponent<CanvasGroup>();
            if (group == null) group = child.gameObject.AddComponent<CanvasGroup>();
            Vector2 destination = rect.anchoredPosition;
            rect.anchoredPosition = destination + new Vector2(35f, 0f);
            group.alpha = 0f;
            float delay = index++ * .07f;
            rect.DOAnchorPos(destination, .28f).SetDelay(delay).SetEase(Ease.OutCubic).SetUpdate(true).Play();
            group.DOFade(1f, .22f).SetDelay(delay).SetUpdate(true)
                .OnKill(() => { if (group != null) group.alpha = 1f; })
                .Play();
        }
    }

    private static void ResizeCard(RectTransform cardRect, float width)
    {
        if (cardRect == null || width <= 0f) return;
        cardRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        var layout = cardRect.GetComponent<LayoutElement>();
        if (layout == null) layout = cardRect.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.flexibleWidth = 0f;
        layout.minHeight = CardHeight;
        layout.preferredHeight = CardHeight;
        layout.flexibleHeight = 0f;
    }

    public static List<PowerUp> BuildChoices(IEnumerable<PowerUp> source, ISet<string> selected,
        IList<ItemType> items, int count, System.Random random)
    {
        var valid = source.Where(p => p != null && (!(p is IconBonusPower icon) || items.Contains(icon.ItemType)))
            .GroupBy(p => p.Name).Select(g => g.First()).ToList();
        var fresh = valid.Where(p => !selected.Contains(p.Name)).OrderBy(_ => random.Next()).ToList();
        // Exhaustion offers repeatable powers instead of trapping the player in an empty picker.
        fresh.AddRange(valid.Where(p => selected.Contains(p.Name)).OrderBy(_ => random.Next()));
        if (fresh.Count < count)
            fresh.AddRange(PowerUpLibrary.Starters().Where(p => fresh.All(f => f.Name != p.Name)));
        return fresh.Take(count).Select(p => p.Clone()).ToList();
    }

    private async void OnPowerUpSelected(PowerUp power)
    {
        if (!acceptingChoice || gameController.Phase != GamePhase.PowerSelection) return;
        acceptingChoice = false;
        var selectedCard = cardParent.GetComponentsInChildren<ShortPowerCardUI>(true)
            .FirstOrDefault(card => card.PowerName == power.Name);
        if (selectedCard != null)
        {
            var rect = selectedCard.transform as RectTransform;
            rect?.DOPunchScale(Vector3.one * .06f, .16f, 5, .4f).SetUpdate(true);
            var image = selectedCard.GetComponent<Image>();
            image?.DOColor(Color.Lerp(PrismatchTheme.Card, PrismatchTheme.PowerColor(power.Name), .24f), .12f)
                .SetUpdate(true);
            await Task.Delay(170);
        }
        selected.Add(power.Name);
        gameController.RegisterSelectedPower(power);
        Hide();
        gameController.OnPowerUpSelected();
    }

    public void Hide()
    {
        acceptingChoice = false;
        if (panel != null) panel.SetActive(false);
        if (cardParent == null) return;
        foreach (Transform child in cardParent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        if (gameController != null && gameController.Phase != GamePhase.PowerSelection)
            Board.Instance?.SetVisible(true);
    }
    public void ResetOfferedList() => selected.Clear();
    public void ResetPowerSelection() => ResetOfferedList();
}
