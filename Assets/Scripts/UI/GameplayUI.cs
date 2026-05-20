using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameplayUI : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text levelText;
    public TMP_Text scoreText;
    public TMP_Text movesText;
    public TMP_Text hintsText;
    public TMP_Text undosText;

    [Header("Live Stars (HUD - oyun sırasında)")]
    public Image liveStar1;
    public Image liveStar2;
    public Image liveStar3;

    [Header("Star Earned Popup")]
    public TMP_Text starEarnedText;

    [Header("Panels")]
    public GameObject winPanel;
    public GameObject gameOverPanel;

    [Header("Win Panel - Yıldızlar")]
    public Image star1Image;
    public Image star2Image;
    public Image star3Image;

    private static readonly Color StarActiveColor   = new Color(1f, 0.84f, 0f);    // Altın
    private static readonly Color StarInactiveColor = new Color(0.3f, 0.3f, 0.3f); // Gri

    [Header("Combo")]
    public TMP_Text comboText;

    [Header("Screen Flash (opsiyonel)")]
    [Tooltip("Ekranı kaplayan tam şeffaf UI Image — yüksek combo'da yanıp söner")]
    public Image screenFlashImage;

    private Coroutine comboCoroutine;
    private Coroutine flashCoroutine;
    private Coroutine starEarnedCoroutine;
    private int   currentRemainingMoves;
    private float nextPopupTime = 0f; // Sıradaki popup bu zamandan önce başlayamaz

    // ─── Level / Score / Moves ───────────────────────────────────────────────

    public void UpdateLevel(int level)
    {
        if (levelText != null)
            levelText.text = $"Level {level}";
    }

    public void UpdateScore(int currentScore, int targetScore)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {currentScore} / {targetScore}";
    }

    public void UpdateMoves(int remainingMoves)
    {
        currentRemainingMoves = remainingMoves;
        if (movesText != null)
            movesText.text = $"Moves: {remainingMoves}";
    }

    public void ShowMovePreviewCost(int cost)
    {
        if (movesText != null)
            movesText.text = $"Moves: {currentRemainingMoves} <color=red>-{cost}</color>";
    }

    public void HideMovePreviewCost()
    {
        if (movesText != null)
            movesText.text = $"Moves: {currentRemainingMoves}";
    }

    // ─── Hints / Undos ────────────────────────────────────────────────────────

    public void UpdateHints(int remaining)
    {
        if (hintsText != null)
            hintsText.text = $"Hint: {remaining}";
    }

    public void UpdateUndos(int remaining)
    {
        if (undosText != null)
            undosText.text = $"Undo: {remaining}";
    }

    // ─── Live Stars (HUD) ─────────────────────────────────────────────────────

    public void UpdateLiveStars(int stars, LevelData level)
    {
        SetStarColor(liveStar1, stars >= 1);
        SetStarColor(liveStar2, stars >= 2);
        SetStarColor(liveStar3, stars >= 3);
    }

    public void ShowStarEarned(int stars)
    {
        if (starEarnedText == null) return;

        if (starEarnedCoroutine != null)
            StopCoroutine(starEarnedCoroutine);

        starEarnedCoroutine = StartCoroutine(StarEarnedRoutine(stars));
    }

    private IEnumerator StarEarnedRoutine(int stars)
    {
        // Önceki popup bitmeden bekle (combo ile çakışmasın)
        float waitTime = nextPopupTime - Time.time;
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        // Bu popup için süre rezerve et
        float myDuration    = 0.25f + 0.9f + 0.3f + 0.1f; // pop-in + hold + fade + buffer
        nextPopupTime = Time.time + myDuration;

        // Metin — sadece ASCII/Latin karakterler (emoji font bağımlılığı yok)
        string label = stars switch
        {
            1 => "New Star!",
            2 => "2nd Star!",
            3 => "3 Stars! Perfect!",
            _ => "New Star!"
        };

        starEarnedText.text  = label;
        starEarnedText.color = new Color(1f, 0.9f, 0f, 1f);
        starEarnedText.gameObject.SetActive(true);

        // Pop-in (overshoot)
        starEarnedText.transform.localScale = Vector3.zero;
        float timer = 0f;
        while (timer < 0.25f)
        {
            timer += Time.deltaTime;
            float t     = timer / 0.25f;
            float scale = t < 0.7f
                ? Mathf.LerpUnclamped(0f, 1.2f, t / 0.7f)
                : Mathf.LerpUnclamped(1.2f, 1f, (t - 0.7f) / 0.3f);
            starEarnedText.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        starEarnedText.transform.localScale = Vector3.one;

        // Ekranda tut
        yield return new WaitForSeconds(0.9f);

        // Fade out
        timer = 0f;
        while (timer < 0.3f)
        {
            timer += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, timer / 0.3f);
            starEarnedText.color = new Color(1f, 0.9f, 0f, a);
            yield return null;
        }

        starEarnedText.gameObject.SetActive(false);
        starEarnedCoroutine = null;
    }

    // ─── Panels ───────────────────────────────────────────────────────────────

    public void ShowWinPanel(int stars = 1)
    {
        if (winPanel != null)
            winPanel.SetActive(true);

        UpdateStarImages(stars);
    }

    public void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void HidePanels()
    {
        if (winPanel      != null) winPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void UpdateStarImages(int stars)
    {
        SetStarColor(star1Image, stars >= 1);
        SetStarColor(star2Image, stars >= 2);
        SetStarColor(star3Image, stars >= 3);
    }

    private void SetStarColor(Image img, bool active)
    {
        if (img == null) return;
        img.color = active ? StarActiveColor : StarInactiveColor;
    }

    // ─── Combo ────────────────────────────────────────────────────────────────

    public void ShowCombo(int comboMultiplier)
    {
        if (comboMultiplier <= 1 || comboText == null)
            return;

        if (comboCoroutine != null)
            StopCoroutine(comboCoroutine);

        comboCoroutine = StartCoroutine(ShowComboRoutine(comboMultiplier));

        // Combo popup süresi kadar sonraki popup'ı ertele
        float comboDuration = 0.18f + 0.6f + 0.15f; // pop-in + display + buffer
        if (Time.time + comboDuration > nextPopupTime)
            nextPopupTime = Time.time + comboDuration;

        if (comboMultiplier >= 3)
            TriggerScreenFlash(GetComboFlashColor(comboMultiplier));
    }

    private Color GetComboFlashColor(int multiplier)
    {
        if (multiplier >= 5) return new Color(1f, 0.2f, 0.2f, 0.3f);
        if (multiplier >= 4) return new Color(1f, 0.5f, 0f,   0.25f);
        return new Color(1f, 0.9f, 0f, 0.2f);
    }

    private IEnumerator ShowComboRoutine(int comboMultiplier)
    {
        comboText.gameObject.SetActive(true);
        comboText.text  = $"COMBO x{comboMultiplier}!";
        comboText.color = GetComboTextColor(comboMultiplier);

        Vector3 originalScale = Vector3.one;
        comboText.transform.localScale = Vector3.zero;

        float duration = 0.18f;
        float timer    = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            float scale = t < 0.7f
                ? Mathf.LerpUnclamped(0f, 1.15f, t / 0.7f)
                : Mathf.LerpUnclamped(1.15f, 1f, (t - 0.7f) / 0.3f);

            comboText.transform.localScale = originalScale * scale;
            yield return null;
        }

        comboText.transform.localScale = originalScale;
        yield return new WaitForSeconds(0.6f);

        comboText.gameObject.SetActive(false);
        comboCoroutine = null;
    }

    private Color GetComboTextColor(int multiplier)
    {
        if (multiplier >= 5) return new Color(1f, 0.2f, 0.2f);
        if (multiplier >= 4) return new Color(1f, 0.5f, 0f);
        if (multiplier >= 3) return new Color(1f, 0.85f, 0f);
        return new Color(0.9f, 0.9f, 0.9f);
    }

    // ─── Screen Flash ─────────────────────────────────────────────────────────

    private void TriggerScreenFlash(Color color)
    {
        if (screenFlashImage == null) return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(ScreenFlashRoutine(color));
    }

    private IEnumerator ScreenFlashRoutine(Color color)
    {
        screenFlashImage.gameObject.SetActive(true);
        screenFlashImage.color = color;

        float duration  = 0.35f;
        float timer     = 0f;
        float peakAlpha = color.a;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            Color c = color;
            c.a = Mathf.Lerp(peakAlpha, 0f, t);
            screenFlashImage.color = c;

            yield return null;
        }

        screenFlashImage.gameObject.SetActive(false);
        flashCoroutine = null;
    }
}
