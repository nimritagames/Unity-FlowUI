using UnityEngine;
/// <summary>
/// Animation controller for UI elements.
/// </summary>
public class UIAnimationController : MonoBehaviour
{
    public UIAnimationType animationType = UIAnimationType.None;
    public float duration = 0.3f;
    public bool playOnStart = true;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool destroyOnComplete = false;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private float originalAlpha = 1f;
    private bool isPlaying = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
            originalScale = rectTransform.localScale;
            originalRotation = rectTransform.localRotation;
        }

        if (canvasGroup != null)
        {
            originalAlpha = canvasGroup.alpha;
        }

        // Set initial state based on animation type
        SetupInitialState();
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayAnimation();
        }
    }

    private void SetupInitialState()
    {
        if (rectTransform == null) return;

        switch (animationType)
        {
            case UIAnimationType.FadeIn:
                EnsureCanvasGroup();
                canvasGroup.alpha = 0f;
                break;

            case UIAnimationType.SlideFromLeft:
                rectTransform.anchoredPosition = new Vector2(-Screen.width, originalPosition.y);
                break;

            case UIAnimationType.SlideFromRight:
                rectTransform.anchoredPosition = new Vector2(Screen.width, originalPosition.y);
                break;

            case UIAnimationType.SlideFromTop:
                rectTransform.anchoredPosition = new Vector2(originalPosition.x, Screen.height);
                break;

            case UIAnimationType.SlideFromBottom:
                rectTransform.anchoredPosition = new Vector2(originalPosition.x, -Screen.height);
                break;

            case UIAnimationType.ScaleUp:
                rectTransform.localScale = Vector3.zero;
                break;

            case UIAnimationType.ScaleDown:
                rectTransform.localScale = originalScale * 1.5f;
                break;

            case UIAnimationType.FadeOut:
                EnsureCanvasGroup();
                canvasGroup.alpha = originalAlpha;
                break;
        }
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            originalAlpha = 1f;
        }
    }

    public void PlayAnimation()
    {
        if (isPlaying) return;

        switch (animationType)
        {
            case UIAnimationType.None:
                break;

            case UIAnimationType.FadeIn:
                StartCoroutine(FadeCoroutine(0f, originalAlpha, duration));
                break;

            case UIAnimationType.FadeOut:
                StartCoroutine(FadeCoroutine(originalAlpha, 0f, duration));
                break;

            case UIAnimationType.SlideFromLeft:
            case UIAnimationType.SlideFromRight:
            case UIAnimationType.SlideFromTop:
            case UIAnimationType.SlideFromBottom:
                StartCoroutine(SlideCoroutine(rectTransform.anchoredPosition, originalPosition, duration));
                break;

            case UIAnimationType.ScaleUp:
                StartCoroutine(ScaleCoroutine(rectTransform.localScale, originalScale, duration));
                break;

            case UIAnimationType.ScaleDown:
                StartCoroutine(ScaleCoroutine(rectTransform.localScale, originalScale, duration));
                break;

            case UIAnimationType.Pulse:
                StartCoroutine(PulseCoroutine(duration));
                break;

            case UIAnimationType.Bounce:
                StartCoroutine(BounceCoroutine(duration));
                break;

            case UIAnimationType.Rotate:
                StartCoroutine(RotateCoroutine(duration));
                break;
        }
    }

    private System.Collections.IEnumerator FadeCoroutine(float startValue, float endValue, float duration)
    {
        EnsureCanvasGroup();
        isPlaying = true;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float curveValue = curve.Evaluate(t);
            canvasGroup.alpha = Mathf.Lerp(startValue, endValue, curveValue);
            yield return null;
        }

        canvasGroup.alpha = endValue;
        isPlaying = false;

        if (destroyOnComplete && animationType == UIAnimationType.FadeOut)
        {
            Destroy(gameObject);
        }
    }

    private System.Collections.IEnumerator SlideCoroutine(Vector2 startValue, Vector2 endValue, float duration)
    {
        isPlaying = true;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float curveValue = curve.Evaluate(t);
            rectTransform.anchoredPosition = Vector2.Lerp(startValue, endValue, curveValue);
            yield return null;
        }

        rectTransform.anchoredPosition = endValue;
        isPlaying = false;

        if (destroyOnComplete)
        {
            Destroy(gameObject);
        }
    }

    private System.Collections.IEnumerator ScaleCoroutine(Vector3 startValue, Vector3 endValue, float duration)
    {
        isPlaying = true;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float curveValue = curve.Evaluate(t);
            rectTransform.localScale = Vector3.Lerp(startValue, endValue, curveValue);
            yield return null;
        }

        rectTransform.localScale = endValue;
        isPlaying = false;

        if (destroyOnComplete)
        {
            Destroy(gameObject);
        }
    }

    private System.Collections.IEnumerator PulseCoroutine(float duration)
    {
        isPlaying = true;
        float time = 0;
        float halfDuration = duration / 2f;

        // Pulse up
        while (time < halfDuration)
        {
            time += Time.deltaTime;
            float t = time / halfDuration;
            float curveValue = curve.Evaluate(t);
            rectTransform.localScale = Vector3.Lerp(originalScale, originalScale * 1.2f, curveValue);
            yield return null;
        }

        time = 0;

        // Pulse down
        while (time < halfDuration)
        {
            time += Time.deltaTime;
            float t = time / halfDuration;
            float curveValue = curve.Evaluate(t);
            rectTransform.localScale = Vector3.Lerp(originalScale * 1.2f, originalScale, curveValue);
            yield return null;
        }

        rectTransform.localScale = originalScale;
        isPlaying = false;
    }

    private System.Collections.IEnumerator BounceCoroutine(float duration)
    {
        isPlaying = true;
        float time = 0;
        Vector2 startPos = originalPosition;
        Vector2 bouncePos = new Vector2(originalPosition.x, originalPosition.y + 20);

        // Up-down-up cycle
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // Create bounce effect with four keypoints
            float bounceValue;
            if (t < 0.25f)
                bounceValue = Mathf.SmoothStep(0, 1, t * 4);
            else if (t < 0.75f)
                bounceValue = Mathf.SmoothStep(1, 0, (t - 0.25f) * 2);
            else
                bounceValue = Mathf.SmoothStep(0, 1, (t - 0.75f) * 4);

            rectTransform.anchoredPosition = Vector2.Lerp(startPos, bouncePos, bounceValue);
            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
        isPlaying = false;
    }

    private System.Collections.IEnumerator RotateCoroutine(float duration)
    {
        isPlaying = true;
        float time = 0;
        Quaternion startRotation = rectTransform.localRotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0, 0, 360);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float curveValue = curve.Evaluate(t);
            rectTransform.localRotation = Quaternion.Slerp(startRotation, endRotation, curveValue);
            yield return null;
        }

        rectTransform.localRotation = startRotation;
        isPlaying = false;
    }
}