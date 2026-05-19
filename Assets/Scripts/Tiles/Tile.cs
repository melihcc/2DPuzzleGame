using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public TMP_Text levelText;

    [Header("Movement")]
    public float moveSpeed = 8f;

    [Header("Juice")]
    public float mergePunchScale = 1.25f;
    public float mergePunchDuration = 0.12f;

    [Header("Effects")]
    public GameObject mergeParticlePrefab;

    public int Level { get; private set; }
    public TileType TileType { get; private set; }
    public GridCell CurrentCell { get; private set; }

    private Coroutine hintCoroutine;

    // Her level için ayrı renk (1-8, sonrası döngüsel)
    private static readonly Color[] LevelColors =
    {
        new Color(0.31f, 0.76f, 0.97f), // 1 - Açık mavi
        new Color(0.50f, 0.78f, 0.52f), // 2 - Yeşil
        new Color(1.00f, 0.84f, 0.32f), // 3 - Amber
        new Color(1.00f, 0.54f, 0.40f), // 4 - Mercan
        new Color(0.90f, 0.35f, 0.35f), // 5 - Kırmızı
        new Color(0.74f, 0.42f, 0.78f), // 6 - Mor
        new Color(0.30f, 0.71f, 0.67f), // 7 - Turkuaz
        new Color(0.96f, 0.38f, 0.57f), // 8 - Pembe
    };

    private static readonly Color WildColor  = new Color(1.00f, 0.85f, 0.00f); // Altın
    private static readonly Color BombColor  = new Color(0.22f, 0.22f, 0.28f); // Koyu antrasit

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (levelText == null)
            levelText = GetComponentInChildren<TMP_Text>();
    }

    public void Setup(int level, GridCell cell, TileType tileType = TileType.Normal)
    {
        Level = level;
        TileType = tileType;
        CurrentCell = cell;

        transform.position = cell.transform.position;
        gameObject.name = $"Tile_{tileType}_{level}";

        UpdateVisual();
    }

    public void SetCell(GridCell newCell)
    {
        CurrentCell = newCell;
        transform.position = newCell.transform.position;
    }

    public void SetCellWithoutMoving(GridCell newCell)
    {
        CurrentCell = newCell;
    }

    public IEnumerator MoveAlongPath(List<GridCell> path)
    {
        foreach (GridCell cell in path)
        {
            Vector3 targetPosition = cell.transform.position;

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    moveSpeed * Time.deltaTime
                );

                yield return null;
            }

            transform.position = targetPosition;
        }
    }

    public IEnumerator PlaySpawnAnimation()
    {
        Vector3 originalScale = transform.localScale;
        transform.localScale = Vector3.zero;

        float duration = 0.28f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            // Overshoot: 0→1.2 sonra 1.2→1.0
            float scale = t < 0.72f
                ? Mathf.LerpUnclamped(0f, 1.2f, t / 0.72f)
                : Mathf.LerpUnclamped(1.2f, 1f, (t - 0.72f) / 0.28f);

            transform.localScale = originalScale * scale;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    public void IncreaseLevel()
    {
        Level++;
        gameObject.name = $"Tile_{TileType}_{Level}";
        UpdateVisual();
    }

    public void PlayMergeParticle()
    {
        if (mergeParticlePrefab == null)
            return;

        GameObject particle = Instantiate(
            mergeParticlePrefab,
            transform.position,
            Quaternion.identity
        );

        Destroy(particle, 1f);
    }

    public void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }

        if (levelText != null)
        {
            Color c = levelText.color;
            c.a = alpha;
            levelText.color = c;
        }
    }

    // --- Hint pulse ---

    public void StartHintPulse()
    {
        StopHintPulse();
        hintCoroutine = StartCoroutine(HintPulseRoutine());
    }

    public void StopHintPulse()
    {
        if (hintCoroutine == null)
            return;

        StopCoroutine(hintCoroutine);
        hintCoroutine = null;
        transform.localScale = Vector3.one;
    }

    private IEnumerator HintPulseRoutine()
    {
        float time = 0f;

        while (true)
        {
            time += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(time * 7f) * 0.13f;
            transform.localScale = Vector3.one * pulse;
            yield return null;
        }
    }

    // --- Merge punch ---

    public IEnumerator PlayMergePunch()
    {
        Vector3 original = transform.localScale;
        Vector3 target   = original * mergePunchScale;
        float timer = 0f;

        while (timer < mergePunchDuration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(original, target, timer / mergePunchDuration);
            yield return null;
        }

        transform.localScale = target;
        timer = 0f;

        while (timer < mergePunchDuration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(target, original, timer / mergePunchDuration);
            yield return null;
        }

        transform.localScale = original;
    }

    // --- Visuals ---

    private void UpdateVisual()
    {
        switch (TileType)
        {
            case TileType.Wild:
                if (spriteRenderer != null) spriteRenderer.color = WildColor;
                if (levelText != null)
                {
                    levelText.text  = "W";
                    levelText.color = new Color(0.15f, 0.08f, 0f);
                }
                break;

            case TileType.Bomb:
                if (spriteRenderer != null) spriteRenderer.color = BombColor;
                if (levelText != null)
                {
                    levelText.text  = "B";
                    levelText.color = Color.white;
                }
                break;

            default:
                if (levelText != null)
                {
                    levelText.text  = Level.ToString();
                    levelText.color = Color.white;
                }
                if (spriteRenderer != null)
                {
                    int idx = Mathf.Clamp(Level - 1, 0, LevelColors.Length - 1);
                    spriteRenderer.color = LevelColors[idx];
                }
                break;
        }
    }
}
