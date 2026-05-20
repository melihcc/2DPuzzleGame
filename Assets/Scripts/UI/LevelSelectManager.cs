using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Level Select sahnesini yönetir.
/// Candy Crush tarzı zigzag düzeninde level node'larını oluşturur.
/// Level 1 en üstte, en alta doğru devam eder.
/// </summary>
public class LevelSelectManager : MonoBehaviour
{
    [Header("References")]
    public LevelManager levelManager;
    public Transform    nodesContainer;  // ScrollView > Viewport > Content
    public GameObject   levelNodePrefab;

    [Header("Layout")]
    [Tooltip("Node'lar arasındaki dikey boşluk (px)")]
    public float nodeSpacingY  = 200f;
    [Tooltip("Zigzag için sağa/sola offset (px)")]
    public float zigzagOffsetX = 160f;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private IEnumerator Start()
    {
        BuildMap();
        yield return null; // Layout hesaplanması için bir frame bekle
        yield return null; // İki frame daha güvenli
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

        int totalLevels     = levelManager.levels.Length;
        int highestUnlocked = PlayerPrefs.GetInt("HighestUnlockedLevel", 0);

        // Content'i top-center, sabit boyutlu yap (koddan zorla)
        RectTransform contentRect = nodesContainer.GetComponent<RectTransform>();
        if (contentRect != null)
        {
            contentRect.anchorMin        = new Vector2(0.5f, 1f);
            contentRect.anchorMax        = new Vector2(0.5f, 1f);
            contentRect.pivot            = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta        = new Vector2(700f, totalLevels * nodeSpacingY + nodeSpacingY);
        }

        for (int i = 0; i < totalLevels; i++)
        {
            GameObject nodeObj = Instantiate(levelNodePrefab, nodesContainer);

            // Zigzag: çift → sol, tek → sağ
            float xOffset = (i % 2 == 0) ? -zigzagOffsetX : zigzagOffsetX;
            // Content'in pivot'u en üstte → Y değerleri negatife gider
            float yOffset = -(i * nodeSpacingY + nodeSpacingY * 0.5f);

            RectTransform rt = nodeObj.GetComponent<RectTransform>();
            if (rt != null)
                rt.anchoredPosition = new Vector2(xOffset, yOffset);

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

        Canvas.ForceUpdateCanvases();

        int totalLevels     = levelManager.levels.Length;
        int highestUnlocked = PlayerPrefs.GetInt("HighestUnlockedLevel", 0);

        if (totalLevels <= 1)
        {
            scrollRect.verticalNormalizedPosition = 1f;
            return;
        }

        // normalizedPosition: 1 = en üst (Level 1), 0 = en alt (son level)
        // Oyuncunun en son kilidi açık leveli görünür olsun
        float t = 1f - (float)highestUnlocked / (totalLevels - 1);
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(t);
    }
}
