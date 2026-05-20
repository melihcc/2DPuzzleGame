using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Level Select sahnesini yönetir.
/// Candy Crush tarzı zigzag düzeninde level node'larını oluşturur.
/// </summary>
public class LevelSelectManager : MonoBehaviour
{
    [Header("References")]
    public LevelManager levelManager;
    public Transform    nodesContainer;  // ScrollView > Viewport > Content
    public GameObject   levelNodePrefab; // LevelNode prefab'ı

    [Header("Layout")]
    [Tooltip("Node'lar arasındaki dikey boşluk (px)")]
    public float nodeSpacingY  = 160f;
    [Tooltip("Zigzag için sağa/sola offset (px)")]
    public float zigzagOffsetX = 200f;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        BuildMap();
    }

    // ─── Map builder ─────────────────────────────────────────────────────────

    private void BuildMap()
    {
        if (levelManager == null || levelNodePrefab == null || nodesContainer == null)
        {
            Debug.LogError("LevelSelectManager: Eksik referans! levelManager, levelNodePrefab veya nodesContainer atanmamış.");
            return;
        }

        int totalLevels     = levelManager.levels.Length;
        int highestUnlocked = PlayerPrefs.GetInt("HighestUnlockedLevel", 0);

        // Content yüksekliğini node sayısına göre ayarla
        RectTransform contentRect = nodesContainer.GetComponent<RectTransform>();
        if (contentRect != null)
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalLevels * nodeSpacingY + 100f);

        for (int i = 0; i < totalLevels; i++)
        {
            GameObject nodeObj = Instantiate(levelNodePrefab, nodesContainer);

            // Zigzag: çift index → sol, tek → sağ
            float xOffset = (i % 2 == 0) ? -zigzagOffsetX : zigzagOffsetX;
            // En üstten aşağıya doğru
            float yOffset = -(i * nodeSpacingY);

            RectTransform rt = nodeObj.GetComponent<RectTransform>();
            if (rt != null)
                rt.anchoredPosition = new Vector2(xOffset, yOffset);

            int  stars    = PlayerPrefs.GetInt($"Stars_{i}", 0);
            bool unlocked = i <= highestUnlocked;

            LevelNode node = nodeObj.GetComponent<LevelNode>();
            if (node != null)
                node.Setup(i, stars, unlocked);
        }

        // Scroll'u en alta al (son kazanılan level görünsün)
        ScrollToCurrentLevel(highestUnlocked, totalLevels);
    }

    private void ScrollToCurrentLevel(int highestUnlocked, int totalLevels)
    {
        ScrollRect scrollRect = GetComponentInChildren<ScrollRect>();
        if (scrollRect == null) return;

        // 0 = en üst, 1 = en alt (Unity ScrollRect'te normalizedPosition.y tersine çalışır)
        float t = totalLevels <= 1 ? 1f : 1f - (float)highestUnlocked / (totalLevels - 1);
        // Biraz padding ekle ki hedef node tam ortaya gelsin
        t = Mathf.Clamp01(t);
        scrollRect.verticalNormalizedPosition = t;
    }
}
