using TMPro;
using UnityEngine;
using DG.Tweening;

public class FloatingText : MonoBehaviour
{
    public TextMeshProUGUI text;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Sequence sequence;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Animate(string value, Vector2 burstDirection, Color color, float fontSize)
    {
        text.text = value;
        text.color = color;
        text.fontSize = fontSize;

        canvasGroup.alpha = 1f;
        rectTransform.localScale = Vector3.one;

        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 targetOffset = burstDirection.normalized * Random.Range(50f, 80f);
        Vector2 endPos = startPos + targetOffset;

        sequence?.Kill();
        sequence = DOTween.Sequence().SetId(this);
        sequence.Append(rectTransform.DOAnchorPos(endPos, 1f).SetEase(Ease.OutCubic));
        sequence.Join(rectTransform.DOScale(1.2f, 0.25f).SetLoops(2, LoopType.Yoyo));
        sequence.Join(canvasGroup.DOFade(0f, 1f));
        sequence.OnComplete(() => Destroy(gameObject));
        sequence.Play();
    }

    private void OnDestroy() => DOTween.Kill(this);


}
