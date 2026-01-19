using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class PointerAnimation : MonoBehaviour
{
    [Header("Settings")]
    public float scaleMultiplier = 1.5f;   // How big it grows
    public float duration = 1f;            // Time for fade out + scale up
    public Ease easeType = Ease.OutQuad;   // Easing type

    private RectTransform rectTransform;
    private SpriteRenderer spriteRenderer;
    private Image image;
    private Vector3 originalScale;
    private Color spriteOriginalColor;
    private Color imageOriginalColor;
    private Sequence pulseSequence;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        image = GetComponent<Image>();

        originalScale = rectTransform.localScale;

        if (spriteRenderer != null)
            spriteOriginalColor = spriteRenderer.color;

        if (image != null)
            imageOriginalColor = image.color;
    }

    void OnEnable()
    {
        StartPulse();
    }

    void OnDisable()
    {
        pulseSequence?.Kill();
    }

    void StartPulse()
    {
        pulseSequence?.Kill();
        pulseSequence = DOTween.Sequence();

        // Scale up + fade out
        pulseSequence.Append(rectTransform.DOScale(originalScale * scaleMultiplier, duration).SetEase(easeType));
        if (spriteRenderer != null)
            pulseSequence.Join(spriteRenderer.DOFade(0f, duration).SetEase(easeType));
        if (image != null)
            pulseSequence.Join(image.DOFade(0f, duration).SetEase(easeType));

        // Instantly reset to original state (scale & alpha)
        pulseSequence.AppendCallback(() =>
        {
            rectTransform.localScale = originalScale;

            if (spriteRenderer != null)
                spriteRenderer.color = spriteOriginalColor;

            if (image != null)
                image.color = imageOriginalColor;
        });

        // Loop forever
        pulseSequence.SetLoops(-1);
    }
}
