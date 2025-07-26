using TMPro;
using UnityEngine;
using DG.Tweening;

public class FloatingText : MonoBehaviour
{
    public TextMeshProUGUI text;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

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
        Vector2 targetOffset = burstDirection.normalized * Random.Range(80f, 50f);
        Vector2 endPos = startPos + targetOffset;

        Sequence seq = DOTween.Sequence();
        seq.Append(rectTransform.DOAnchorPos(endPos, 1f).SetEase(Ease.OutCubic));
        seq.Join(rectTransform.DOScale(1.2f, 0.25f).SetLoops(2, LoopType.Yoyo));
        seq.Join(canvasGroup.DOFade(0f, 01f));
        seq.OnComplete(() => Destroy(gameObject));
        seq.Play();
    }


}
