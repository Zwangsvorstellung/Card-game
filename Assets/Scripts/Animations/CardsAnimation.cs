using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CardsAnimation : MonoBehaviour
{
    private RectTransform rectTransform;
    public Image targetImage;

    void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
            Debug.LogError("CardsAnimation: Aucun RectTransform trouvé sur ce GameObject !");
    }

    // 1. Pop Scale (zoom rapide)
    public IEnumerator PopScale(float duration = 0.2f, float scaleAmount = 1.2f)
    {
        Vector3 originalScale = rectTransform.localScale;
        Vector3 targetScale = originalScale * scaleAmount;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            rectTransform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rectTransform.localScale = targetScale;

        elapsed = 0f;
        while (elapsed < duration)
        {
            rectTransform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rectTransform.localScale = originalScale;
    }

    // 3. Bounce (rebond vertical)
    public IEnumerator Bounce(float duration = 0.5f, float bounceHeight = 30f)
    {
        Vector2 originalPos = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float y = Mathf.Sin(elapsed / duration * Mathf.PI) * bounceHeight;
            rectTransform.anchoredPosition = originalPos + new Vector2(0, y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rectTransform.anchoredPosition = originalPos;
    }

    // 4. Pulse (pulsation continue, attention à bien StopCoroutine quand tu veux arrêter)
    public IEnumerator Pulse(float minScale = 0.9f, float maxScale = 1.1f, float speed = 2f)
    {
        while (true)
        {
            float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * speed) + 1f) / 2f);
            rectTransform.localScale = new Vector3(scale, scale, 1);
            yield return null;
        }
    }

    public IEnumerator Rotate(float maxAngle = 10f, float speed = 2f)
    {
        while (true)
        {
            float angle = Mathf.Sin(Time.time * speed) * maxAngle;
            rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            yield return null;
        }
    }
    
    public IEnumerator ColorFlash(CardAI cardAI, Color flashColor = default, float duration = 0.5f)
    {
        if (flashColor == default) flashColor = Color.red;

        Image img = cardAI.GetComponentInChildren<Image>();
        if (img == null) yield break;

        Color originalColor = img.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            img.color = Color.Lerp(originalColor, flashColor, Mathf.PingPong(elapsed * 4, 1));
            elapsed += Time.deltaTime;
            yield return null;
        }
        img.color = originalColor;
    }

    public IEnumerator Flip(float duration = 0.5f)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float angleY = Mathf.Lerp(0, 180, elapsed / duration);
            rectTransform.localRotation = Quaternion.Euler(0, angleY, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.localRotation = Quaternion.Euler(0, 0, 0);
    }

    public IEnumerator Glow(GameObject cardGO, float duration = 1f, float glowIntensity = 0.5f, float pulseSpeed = 3f)
    {
        Image img = cardGO.GetComponentInChildren<Image>();
        if (img == null)
        {
            Debug.LogError("Image component not found!");
            yield break;
        }

        Color originalColor = img.color;
        Color glowColor = originalColor * (1f + glowIntensity);

        Vector3 originalScale = img.rectTransform.localScale;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.PingPong(elapsed * pulseSpeed, 1f);
            img.color = Color.Lerp(originalColor, glowColor, t);
            float scale = Mathf.Lerp(1f, 1.05f, t);
            img.rectTransform.localScale = originalScale * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        img.color = originalColor;
        img.rectTransform.localScale = originalScale;
    }

    public IEnumerator Wobble(float duration = 0.5f, float angle = 10f, float speed = 10f)
    {
        float elapsed = 0f;
        Quaternion originalRot = rectTransform.rotation;

        while (elapsed < duration)
        {
            float zAngle = Mathf.Sin(elapsed * speed) * angle;
            rectTransform.rotation = Quaternion.Euler(0, 0, zAngle);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.rotation = originalRot;
    }

    public IEnumerator Fade(GameObject cardGO, float startAlpha, float endAlpha, float duration = 0.5f)
    {
        if (cardGO == null) yield break;
        
        CanvasGroup canvasGroup = cardGO.GetComponent<CanvasGroup>();
        if (canvasGroup == null) 
                canvasGroup = cardGO.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        //canvasGroup.interactable = visible;
        //canvasGroup.blocksRaycasts = visible;
    }

    public IEnumerator Rotate360(float duration = 1f)
    {
        float elapsed = 0f;
        Quaternion startRot = rectTransform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, 0, 360);

        while (elapsed < duration)
        {
            rectTransform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rectTransform.rotation = startRot;
    }

    public IEnumerator Shake(float duration = 0.3f, float magnitude = 5f)
    {
        Vector3 originalPos = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;

            rectTransform.anchoredPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = originalPos;
    }

    public IEnumerator ChangeColorSmoothly(Image image, Color targetColor, float duration)
    {
        Color startColor = image.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            image.color = Color.Lerp(startColor, targetColor, elapsed / duration);
            yield return null;
        }
        image.color = targetColor;
    }

    public static IEnumerator SwingSablier(RectTransform passedRect, float angleMax = 10f, float speed = 2f)
    {
        float elapsed = 0f;
        while (true)
        {
            // oscillation entre -angleMax et +angleMax (en degrés)
            float angle = Mathf.Sin(elapsed * speed) * angleMax;
            passedRect.localRotation = Quaternion.Euler(0, 0, angle);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public IEnumerator Pulse(RectTransform rectTransform, float minScale = 0.9f, float maxScale = 1.1f, float speed = 2f)
    {
        float elapsed = 0f;
        while (true)
        {
            float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(elapsed * speed) + 1f) / 2f);
            rectTransform.localScale = new Vector3(scale, scale, 1f);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
