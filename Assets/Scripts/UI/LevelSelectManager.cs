using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectManager : MonoBehaviour
{
    [Header("References")]
    public LevelManager levelManager;
    public Transform    nodesContainer;  // ScrollView > Viewport > Content
    public GameObject   levelNodePrefab;

    [Header("Layout")]
    public float nodeSpacingY  = 200f;
    public float zigzagOffsetX = 160f;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private IEnumerator Start()
    {
        BuildMap();

        // Canvas layout'un hesaplanması için 2 frame bekle
        yield return null;
        yield return null;

        Canvas.ForceUpdateCanvases();
        ScrollToCurrentLevel();
    }

    // ─── Map builder ─────────────────────────────────────────────────────────

    private void BuildMap()
    {
        if (levelManager == null || levelNodePrefab == null || nodesContainer == null)
        {
            Debug.LogError("LevelSelectManager: Eksik referans!");
            return;
        }

        // Manuel yerleştirme ile çakışan layout component'larını kaldır
        var anyLayout  = nodesContainer.GetComponent<LayoutGroup>();
        var sizeFitter = nodesContainer.GetComponent<ContentSizeFitter>();
        if (anyLayout  != null) DestroyImmediate(anyLayout);
        if (sizeFitter != null) DestroyImmediate(sizeFitter);

        int totalLevels     = levelManager.levels.Length;
        int highestUnlocked = PlayerPrefs.GetInt("HighestUnlockedLevel", 0);

        // Content RectTransform'u top-center, sabit boyutlu yap
        RectTransform contentRect = nodesContainer.GetComponent<RectTransform>();
        if (contentRect != null)
        {
            contentRect.anchorMin        = new Vector2(0.5f, 1f);
            contentRect.anchorMax        = new Vector2(0.5f, 1f);
            contentRect.pivot            = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            // Genişlik: zigzag + node boyutu için 700px yeterli
            // Yükseklik: tüm node'lar + üst/alt padding
            float totalHeight = totalLevels * nodeSpacingY + nodeSpacingY;
            contentRect.sizeDelta = new Vector2(700f, totalHeight);
        }

        for (int i = 0; i < totalLevels; i++)
        {
            GameObject nodeObj = Instantiate(levelNodePrefab, nodesContainer);

            // Çift index → sol, tek → sağ
            float xOffset = (i % 2 == 0) ? -zigzagOffsetX : zigzagOffsetX;
            // Pivot en üstte: Y negatife gider
            float yOffset = -(i * nodeSpacingY + nodeSpacingY * 0.5f);

            RectTransform rt = nodeObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin        = new Vector2(0.5f, 0.5f);
                rt.anchorMax        = new Vector2(0.5f, 0.5f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(xOffset, yOffset);
            }

            int  stars    = PlayerPrefs.GetInt($"Stars_{i}", 0);
            bool unlocked = i <= highestUnlocked;

            LevelNode node = nodeObj.GetComponent<LevelNode>();
            if (node != null)
                node.Setup(i, stars, unlocked);
        }
    }

    // ─── Scroll ───────────────────────────────────────────────────────────────

    private void ScrollToCurrentLevel()
    {
        ScrollRect scrollRect = GetComponentInChildren<ScrollRect>();
        if (scrollRect == null) return;

        int totalLevels     = levelManager.levels.Length;
        int highestUnlocked = PlayerPrefs.GetInt("HighestUnlockedLevel", 0);

        // 1 = en üst (Level 1 görünür), 0 = en alt (son level görünür)
        float t = totalLevels <= 1
            ? 1f
            : 1f - (float)highestUnlocked / (totalLevels - 1);

        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(t);
    }
}
