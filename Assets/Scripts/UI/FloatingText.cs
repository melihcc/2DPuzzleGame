using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    private TextMeshPro tmp;

    public static void Spawn(Vector3 worldPos, string message, Color color, float fontSize = 3.5f)
    {
        GameObject go = new GameObject("FloatingText");
        go.transform.position = worldPos + Vector3.up * 0.3f;

        FloatingText ft = go.AddComponent<FloatingText>();
        ft.Initialize(message, color, fontSize);
    }

    private void Initialize(string message, Color color, float fontSize)
    {
        tmp = gameObject.AddComponent<TextMeshPro>();
        tmp.text = message;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.sortingOrder = 20;

        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float duration = 1.1f;
        float timer = 0f;
        Vector3 startPos = transform.position;
        Color baseColor = tmp.color;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            transform.position = startPos + Vector3.up * (t * 2f);

            float alpha = t < 0.15f
                ? t / 0.15f
                : 1f - ((t - 0.15f) / 0.85f);

            float scale = t < 0.12f
                ? Mathf.LerpUnclamped(0f, 1.25f, t / 0.12f)
                : t < 0.25f
                    ? Mathf.LerpUnclamped(1.25f, 1f, (t - 0.12f) / 0.13f)
                    : 1f;

            Color c = baseColor;
            c.a = Mathf.Clamp01(alpha);
            tmp.color = c;
            transform.localScale = Vector3.one * scale;

            yield return null;
        }

        Destroy(gameObject);
    }
}
