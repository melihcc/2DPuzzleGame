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

    public GridCell CurrentCell { get; private set; }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (levelText == null)
            levelText = GetComponentInChildren<TMP_Text>();
    }

    public void Setup(int level, GridCell cell)
    {
        Level = level;
        CurrentCell = cell;

        transform.position = cell.transform.position;
        gameObject.name = $"Tile_Level_{level}";

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

        float duration = 0.18f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            float easedT = Mathf.Sin(t * Mathf.PI * 0.5f);

            transform.localScale = Vector3.Lerp(
                Vector3.zero,
                originalScale,
                easedT
            );

            yield return null;
        }

        transform.localScale = originalScale;
    }

    public void IncreaseLevel()
    {
        Level++;
        gameObject.name = $"Tile_Level_{Level}";
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
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        if (levelText != null)
        {
            Color textColor = levelText.color;
            textColor.a = alpha;
            levelText.color = textColor;
        }
    }

    private void UpdateVisual()
    {
        if (levelText != null)
            levelText.text = Level.ToString();

        if (spriteRenderer == null)
            return;

        if (Level == 1)
            spriteRenderer.color = Color.blue;
        else if (Level == 2)
            spriteRenderer.color = Color.green;
        else if (Level == 3)
            spriteRenderer.color = Color.yellow;
        else if (Level == 4)
            spriteRenderer.color = Color.red;
        else
            spriteRenderer.color = Color.magenta;
    }
    public IEnumerator PlayMergePunch()
{
    Vector3 originalScale = transform.localScale;
    Vector3 targetScale = originalScale * mergePunchScale;

    float timer = 0f;

    // Büyüme
    while (timer < mergePunchDuration)
    {
        timer += Time.deltaTime;

        float t = timer / mergePunchDuration;

        transform.localScale = Vector3.Lerp(
            originalScale,
            targetScale,
            t
        );

        yield return null;
    }

    transform.localScale = targetScale;

    timer = 0f;

    // Küçülme
    while (timer < mergePunchDuration)
    {
        timer += Time.deltaTime;

        float t = timer / mergePunchDuration;

        transform.localScale = Vector3.Lerp(
            targetScale,
            originalScale,
            t
        );

        yield return null;
    }

    transform.localScale = originalScale;
}
}