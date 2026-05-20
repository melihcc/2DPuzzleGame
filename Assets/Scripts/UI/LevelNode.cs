using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Level Select ekranındaki her bir level düğmesini temsil eder.
/// LevelSelectManager tarafından Setup() ile başlatılır.
/// </summary>
public class LevelNode : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text levelNumberText;
    public Image    star1;
    public Image    star2;
    public Image    star3;
    public Button   button;
    public Image    lockOverlay;   // Kilitli levellarda üstüne gelecek karartma/kilit ikonu

    private static readonly Color StarActiveColor   = new Color(1f, 0.84f, 0f);    // Altın
    private static readonly Color StarInactiveColor = new Color(0.25f, 0.25f, 0.25f); // Koyu gri

    private int levelIndex;

    // ─── Setup ───────────────────────────────────────────────────────────────

    public void Setup(int index, int stars, bool unlocked)
    {
        levelIndex = index;

        if (levelNumberText != null)
            levelNumberText.text = (index + 1).ToString();

        SetStarColor(star1, stars >= 1);
        SetStarColor(star2, stars >= 2);
        SetStarColor(star3, stars >= 3);

        if (lockOverlay != null)
            lockOverlay.gameObject.SetActive(!unlocked);

        if (button != null)
            button.interactable = unlocked;
    }

    // ─── Click ───────────────────────────────────────────────────────────────

    public void OnClick()
    {
        PlayerPrefs.SetInt("CurrentLevelArrayIndex", levelIndex);
        PlayerPrefs.Save();
        SceneLoader.LoadGameplay();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void SetStarColor(Image img, bool active)
    {
        if (img == null) return;
        img.color = active ? StarActiveColor : StarInactiveColor;
    }
}
