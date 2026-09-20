using System.Collections;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[TestFixture]
public class GameplaySmokeTests
{
    [UnityTest]
    public IEnumerator GameplayScene_StartsWithPlayableBoardAndPowerChoice()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = UnityEngine.Object.FindAnyObjectByType<GameController>();
        Assert.That(controller, Is.Not.Null);
        Assert.That(Board.Instance, Is.Not.Null);
        Assert.That(Board.Instance.Model, Is.Not.Null);
        Assert.That(Board.Instance.Model.Matches(), Is.Empty);
        Assert.That(Board.Instance.Model.HasLegalMove(), Is.True);
        Assert.That(controller.Phase, Is.EqualTo(GamePhase.PowerSelection).Or.EqualTo(GamePhase.BossIntro));
        if (controller.Phase == GamePhase.PowerSelection)
        {
            var card = Object.FindAnyObjectByType<ShortPowerCardUI>();
            Assert.That(card, Is.Not.Null);
            var viewport = Object.FindAnyObjectByType<PowerUpSelector>()
                .GetComponentInChildren<UnityEngine.UI.ScrollRect>(true).viewport;
            Assert.That(((RectTransform)card.transform).rect.width,
                Is.EqualTo(viewport.rect.width - 32f).Within(1f), "Power cards should fill their parent with 16-unit side margins.");
            Assert.That(((RectTransform)card.transform).rect.height, Is.EqualTo(180f).Within(1f));
        }
    }

    [UnityTest]
    public IEnumerator ClickingTwoTiles_ResolvesAMoveAndReturnsInputControl()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = Object.FindAnyObjectByType<GameController>();
        var board = Board.Instance;
        var powerCard = Object.FindAnyObjectByType<ShortPowerCardUI>();
        Assert.That(powerCard, Is.Not.Null, "Power picker should create selectable cards.");
        var powerButton = powerCard.GetComponentInChildren<UnityEngine.UI.Button>();
        Assert.That(powerButton, Is.Not.Null);
        powerButton.onClick.Invoke();
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(controller.CanInteract, Is.True, "Power selection should enter Playing.");

        Tile first = null, second = null;
        for (int a = 0; a < board.Width * board.Height && first == null; a++)
        for (int b = a + 1; b < board.Width * board.Height; b++)
        {
            var ca = new BoardModel.Cell(a % board.Width, a / board.Width);
            var cb = new BoardModel.Cell(b % board.Width, b / board.Width);
            if (board.Model[ca.X, ca.Y] == board.Model[cb.X, cb.Y]) continue;
            board.Model.Swap(ca, cb);
            bool legal = board.Model.Connected(ca.X, ca.Y).Count >= 3 || board.Model.Connected(cb.X, cb.Y).Count >= 3;
            board.Model.Swap(ca, cb);
            if (!legal) continue;
            first = board.Tiles[ca.X, ca.Y]; second = board.Tiles[cb.X, cb.Y]; break;
        }
        Assert.That(first, Is.Not.Null, "Generated board promised a legal move.");
        Assert.That(first.button, Is.Not.Null);
        Assert.That(second.button, Is.Not.Null);
        var firstStart = first.icon.transform.position;
        var secondStart = second.icon.transform.position;
        first.button.onClick.Invoke();
        Assert.That(first.background.color, Is.EqualTo(Color.darkGray), "First click should select the tile.");
        second.button.onClick.Invoke();
        Assert.That(board.isSwapping, Is.True, "Second click should start resolution.");
        yield return new WaitForSeconds(.08f);
        Assert.That(first.icon.transform.position, Is.Not.EqualTo(firstStart), "First icon should visibly move.");
        Assert.That(second.icon.transform.position, Is.Not.EqualTo(secondStart), "Second icon should visibly move.");
        float timeout = Time.realtimeSinceStartup + 4f;
        while (board.isSwapping && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(board.isSwapping, Is.False, "Resolution should release the board lock.");
        Assert.That(controller.Phase, Is.EqualTo(GamePhase.Playing));
        Assert.That(controller.CurrentScore, Is.GreaterThan(0));
    }

    [UnityTest]
    public IEnumerator ClickingTwoTiles_WhenTimeScaleIsZero_DoesNotLockTheBoard()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = Object.FindAnyObjectByType<GameController>();
        var board = Board.Instance;
        Object.FindAnyObjectByType<ShortPowerCardUI>().GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(controller.CanInteract, Is.True);

        var first = board.Tiles[0, 0];
        var second = board.Tiles[1, 0];
        Time.timeScale = 0f;
        first.button.onClick.Invoke();
        second.button.onClick.Invoke();
        Assert.That(board.isSwapping, Is.True);

        float timeout = Time.realtimeSinceStartup + 2f;
        while (board.isSwapping && Time.realtimeSinceStartup < timeout) yield return null;
        Time.timeScale = 1f;
        Assert.That(board.isSwapping, Is.False, "A frozen scaled clock must not strand board input.");
        Assert.That(controller.Phase, Is.EqualTo(GamePhase.Playing));
    }

    [UnityTest]
    public IEnumerator CompletingRound_SlidesBoardLeftBeforeNextPowerChoice()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = Object.FindAnyObjectByType<GameController>();
        var boardRect = (RectTransform)Board.Instance.transform;
        float startX = boardRect.anchoredPosition.x;

        typeof(GameController).GetField("score", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, controller.CurrentTarget);
        typeof(GameController).GetField("<Phase>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, GamePhase.Resolving);
        controller.EndResolution();

        Assert.That(controller.Phase, Is.EqualTo(GamePhase.LevelComplete));
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(boardRect.anchoredPosition.x, Is.LessThan(startX), "Board should move off the left side.");

        float timeout = Time.realtimeSinceStartup + 2f;
        while (controller.Phase == GamePhase.LevelComplete && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(controller.CurrentRound, Is.EqualTo(2));
        Assert.That(controller.Phase, Is.EqualTo(GamePhase.PowerSelection).Or.EqualTo(GamePhase.BossIntro));
        Assert.That(boardRect.anchoredPosition.x, Is.EqualTo(startX).Within(.1f));
    }

    [UnityTest]
    public IEnumerator DraggingFromOneTileToAnother_SubmitsTheSameMoveAsTwoClicks()
    {
        SceneManager.LoadScene("prismatch");
        yield return null;
        yield return null;
        var controller = Object.FindAnyObjectByType<GameController>();
        Object.FindAnyObjectByType<ShortPowerCardUI>().GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(controller.CanInteract, Is.True);

        var board = Board.Instance;
        var source = board.Tiles[0, 0];
        var destination = board.Tiles[1, 0];
        var pointer = new PointerEventData(EventSystem.current)
        {
            pointerCurrentRaycast = new RaycastResult { gameObject = destination.gameObject }
        };
        ExecuteEvents.Execute(source.gameObject, pointer, ExecuteEvents.beginDragHandler);
        Assert.That(source.background.color, Is.EqualTo(Color.darkGray));
        ExecuteEvents.Execute(source.gameObject, pointer, ExecuteEvents.endDragHandler);
        Assert.That(board.isSwapping, Is.True, "Releasing over another tile should begin a swap.");

        float timeout = Time.realtimeSinceStartup + 2f;
        while (board.isSwapping && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(board.isSwapping, Is.False);
    }
}
