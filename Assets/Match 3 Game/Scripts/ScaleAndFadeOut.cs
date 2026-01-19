using UnityEngine;

public class ScaleAndFadeOut : MonoBehaviour
{
    [Header("Size Settings")]
    public Vector2 startSize = Vector2.one;
    public Vector2 endSize = Vector2.zero;

    [Header("Curves")]
    public AnimationCurve sizeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Timing")]
    public float duration = 1f;

    private SpriteRenderer spriteRenderer;
    private float timer = 0f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        transform.localScale = startSize;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        // Size interpolation with curve
        float sizeFactor = sizeCurve.Evaluate(t);
        Vector2 newSize = Vector2.LerpUnclamped(startSize, endSize, sizeFactor);
        transform.localScale = new Vector3(newSize.x, newSize.y, 1f);

        // Alpha fade with curve
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = alphaCurve.Evaluate(t);
            spriteRenderer.color = c;
        }

        // Destroy after completion
        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
