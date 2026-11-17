using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpSelector : MonoBehaviour
{
    private const float HorizontalCardMargin = PowerPanelLayout.Margin;
    private const float CardHeight = PowerPanelLayout.CardHeight;
    private const float CardSpacing = PowerPanelLayout.CardSpacing;
    private const float VerticalListPadding = PowerPanelLayout.VerticalListPadding;
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject shortPowerCardPrefab;
    [SerializeField] private Transform cardParent;
    [SerializeField] public GameController gameController;
    private readonly HashSet<string> selected = new();
    private bool acceptingChoice;
    private int selectionVersion;
    private bool inventoryOverlay;

    public void SetInventoryOverlay(bool open)
    {
        inventoryOverlay = open;
        if (panel == null) return;
        var group = panel.GetComponent<CanvasGroup>();
        if(group==null)group=panel.AddComponent<CanvasGroup>();
        group.alpha = open ? 0f : 1f;
        group.interactable = group.blocksRaycasts = !open;
    }

    public void ShowChoices()
    {
        if (gameController.Phase != GamePhase.PowerSelection) return;
        SetInventoryOverlay(false);
        panel.SetActive(true);
        PowerPanelLayout.Configure(panel, cardParent, "Choose a <color=#FF9F1C>Power</color>",
            "Pick one bonus for this run", reserveCloseButton: false);
        var choiceScroll = panel.GetComponentInChildren<ScrollRect>(true);
        float parentWidth = choiceScroll != null && choiceScroll.viewport != null
            ? choiceScroll.viewport.rect.width
            : (cardParent as RectTransform)?.rect.width ?? 0f;
        float cardWidth = Mathf.Max(0f, parentWidth - HorizontalCardMargin * 2f);
        float viewportHeight = choiceScroll != null && choiceScroll.viewport != null
            ? choiceScroll.viewport.rect.height : CardHeight * 3f + CardSpacing * 2f + VerticalListPadding * 2f;
        float choiceHeight = Mathf.Max(104f, (viewportHeight - CardSpacing * 2f - VerticalListPadding * 2f) / 3f);
        foreach (Transform child in cardParent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        var level = GameState.Instance.CurrentLevel;
        selected.Clear();
        selected.UnionWith(gameController.PowerManager.ActivePowers().Select(p => p.Name));
        var source = level.GetPowerUpsForRound(gameController.CurrentRound);
        if (gameController.CurrentGoal == GoalType.BlocksCleared && !SharedGameContent.HasPool(level.GetLevelNumber,gameController.CurrentRound))
            source = gameController.PowerManager.ActivePowers().Count == 0 ? PowerUpLibrary.BlockStarters() : PowerUpLibrary.BlockPowers();
        var mode = gameController.IsTimed ? FreePlayMode.Timed : FreePlayMode.LimitedMoves;
        var schedule = (level as IConfiguredPowerOffers)?.PowerOffers;
        bool allowStarterRewards = (level as CampaignLevel)?.AllowStarterRewards == true;
        if (schedule != null) source = source.Where(p => schedule.AllowsOrdinary(p, mode)).ToList();
        var requiredTags = (level as IRoundPowerOffers)?.RequiredPowerTags(gameController.CurrentRound) ?? PowerTags.None;
        if (requiredTags != PowerTags.None)
            source = PowerUpLibrary.GetAll().Concat(PowerUpLibrary.BlockPowers()).ToList();
        var offerSubtitle = panel.transform.Find("PowerChoiceSubtitle")?.GetComponent<TextMeshProUGUI>();
        if (offerSubtitle != null) offerSubtitle.text = requiredTags == PowerTags.None
            ? "Pick one bonus for this run" : "ROUND " + gameController.CurrentRound + " / " + requiredTags.ToString().Replace(", ", " + ").ToUpperInvariant();
        source = SharedGameContent.ForLevel(source,level.GetLevelNumber).Where(power => power.CanUseWithMatchRules(gameController.GetEffectiveMatchRules())).ToList();
        var choices = BuildChoices(source, selected, ItemUtils.GetItemTypes(gameController.IconList), 3, new System.Random(Random.Range(0, int.MaxValue)),
            gameController.CurrentGoal, gameController.IsTimed ? FreePlayMode.Timed : FreePlayMode.LimitedMoves,
            gameController.GetEffectiveMatchRules(), gameController.PowerManager.ActivePowers(), requiredTags,
            filter: p => requiredTags != PowerTags.None || schedule == null || schedule.AllowsOrdinary(p, mode),
            round: gameController.CurrentRound, allowStarterRewards: allowStarterRewards);
        var definition = schedule?.ForRound(gameController.CurrentRound);
        if (definition != null)
        {
            var objective = (level as CampaignLevel)?.BlockObjectives?.ForRound(gameController.CurrentRound);
            var result = PowerOfferBuilder.Build(definition, SharedGameContent.ForLevel(objective == null ? PowerOfferBuilder.Catalog() : BlockObjective.CatalogFor(objective, gameController.PowerManager.ActivePowers()),level.GetLevelNumber), selected,
                ItemUtils.GetItemTypes(gameController.IconList), new System.Random(Random.Range(0, int.MaxValue)), gameController.CurrentGoal, mode,
                gameController.GetEffectiveMatchRules(), gameController.PowerManager.ActivePowers(), gameController.CurrentRound, allowStarterRewards);
            choices = result.Choices.ToList();
            if (!result.IsComplete)
                Debug.LogWarning("Power offer round " + gameController.CurrentRound + ": " + string.Join(" | ", result.Diagnostics));
            if (offerSubtitle != null) offerSubtitle.text = definition.Label;
        }
        if (choices.Count == 0)
        {
            Hide();
            gameController.ShowMessage("No compatible powers remain for this round's offer. Continuing with your current powers.");
            gameController.OnPowerUpSelected();
            return;
        }
        foreach (var power in choices)
        {
            var card = Instantiate(shortPowerCardPrefab, cardParent).GetComponent<ShortPowerCardUI>();
            ResizeCard(card.transform as RectTransform, cardWidth);
            card.Setup(power, OnPowerUpSelected);
            card.ApplyResponsiveLayout(cardWidth, choiceHeight);
        }
        PowerPanelLayout.FitToContent(panel, cardParent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardParent as RectTransform);
        AnimateChoices();
        if (choiceScroll != null) choiceScroll.verticalNormalizedPosition = 1f;
        acceptingChoice = true;
        Board.Instance?.SetVisible(false);
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
        IList<ItemType> items, int count, System.Random random,
        GoalType goal = GoalType.Score, FreePlayMode mode = FreePlayMode.Timed, IReadOnlyList<MatchRule> rules = null, IReadOnlyList<PowerUp> active = null, PowerTags requiredTags = PowerTags.None, bool allowFallback = true,
        System.Func<PowerUp, bool> filter = null, int round = 1, bool allowStarterRewards = false)
    {
        bool Eligible(PowerUp p) => PowerOfferBuilder.IsEligible(p, items, goal, mode, rules, active, round, allowStarterRewards) &&
            (p.OfferTags(mode) & requiredTags) == requiredTags && (filter == null || filter(p));
        var valid = source.Where(p=>p!=null).Select(p=>p.Clone().ForGoal(goal)).Where(Eligible)
            .GroupBy(p => p.Name).Select(g => g.First()).ToList();
        var fresh = valid.Where(p => !selected.Contains(p.Name)).OrderBy(_ => random.Next()).ToList();
        // Exhaustion offers repeatable powers instead of trapping the player in an empty picker.
        fresh.AddRange(valid.Where(p => selected.Contains(p.Name)).OrderBy(_ => random.Next()));
        if (fresh.Count < count && requiredTags == PowerTags.None && allowFallback)
            fresh.AddRange((round == 1 ? goal == GoalType.BlocksCleared ? PowerUpLibrary.BlockStarters() : PowerUpLibrary.Starters()
                : goal == GoalType.BlocksCleared ? PowerUpLibrary.BlockPowers() : PowerUpLibrary.GetAll())
                .Select(p=>p.ForGoal(goal))
                .Where(p => Eligible(p) && fresh.All(f => f.Name != p.Name)));
        return fresh.Take(count).Select(p => p.Clone().ForGoal(goal)).ToList();
    }

    public static List<PowerUp> BuildThemedChoices(CampaignOfferTheme theme, ISet<string> selected,
        IList<ItemType> items, System.Random random, GoalType goal, FreePlayMode mode,
        IReadOnlyList<MatchRule> rules, IReadOnlyList<PowerUp> active)
    {
        var definition = CampaignExpansion.Offer(theme, goal, mode);
        return definition == null ? new List<PowerUp>() : PowerOfferBuilder.Build(definition,
            PowerOfferBuilder.Catalog(), selected, items, random, goal, mode, rules, active).Choices.ToList();
    }

    private async void OnPowerUpSelected(PowerUp power)
    {
        if (!acceptingChoice || inventoryOverlay || gameController.Phase != GamePhase.PowerSelection) return;
        acceptingChoice = false;
        int version = selectionVersion;
        var selectedCard = cardParent.GetComponentsInChildren<ShortPowerCardUI>(true)
            .FirstOrDefault(card => card.PowerName == power.Name);
        if (selectedCard != null)
        {
            var rect = selectedCard.transform as RectTransform;
            rect?.DOPunchScale(Vector3.one * .06f, .16f, 5, .4f).SetUpdate(true);
            var image = selectedCard.GetComponent<Image>();
            image?.DOColor(Color.Lerp(PrismatchTheme.Card, PrismatchTheme.PowerColor(power.Name), .24f), .12f)
                .SetUpdate(true);
            // Browser builds cannot depend on the .NET timer/thread pool.
            float finishAt = Time.realtimeSinceStartup + .17f;
            while (Time.realtimeSinceStartup < finishAt)
            {
                await Awaitable.NextFrameAsync();
                if (this == null || version != selectionVersion) return;
            }
        }
        if (gameController == null || gameController.Phase != GamePhase.PowerSelection) return;
        selected.Add(power.Name);
        gameController.RegisterSelectedPower(power);
        Hide();
        gameController.OnPowerUpSelected();
    }

    public void Hide()
    {
        SetInventoryOverlay(false);
        selectionVersion++;
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
