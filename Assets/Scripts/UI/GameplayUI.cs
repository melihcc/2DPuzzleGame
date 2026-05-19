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

    [Header("Panels")]
    public GameObject winPanel;
    public GameObject gameOverPanel;

    [Header("Win Panel")]
    [Tooltip("Win panel'deki yıldız metni (örn: ★★☆)")]
    public TMP_Text starsText;
    [Tooltip("Win panel'deki skor özet metni")]
    public TMP_Text winScoreText;

    [Header("Combo")]
    public TMP_Text comboText;

    [Header("Screen Flash (opsiyonel)")]
    [Tooltip("Ekranı kaplayan tam şeffaf UI Image — yüksek combo'da yanıp söner")]
    public Image screenFlashImage;

    private Coroutine comboCoroutine;
    private Coroutine flashCoroutine;
    private int currentRemainingMoves;

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

    // ─── Panels ───────────────────────────────────────────────────────────────

    public void ShowWinPanel(int stars = 1)
    {
        if (winPanel != null)
            winPanel.SetActive(true);

        if (starsText != null)
            starsText.text = BuildStarString(stars);
    }

    public void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void HidePanels()
    {
        if (winPanel     != null) winPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private string BuildStarString(int stars)
    {
        return stars switch
        {
            1 => "★☆☆",
            2 => "★★☆",
            3 => "★★★",
            _ => "☆☆☆"
        };
    }

    // ─── Combo ────────────────────────────────────────────────────────────────

    public void ShowCombo(int comboMultiplier)
    {
        if (comboMultiplier <= 1 || comboText == null)
            return;

        if (comboCoroutine != null)
            StopCoroutine(comboCoroutine);

        comboCoroutine = StartCoroutine(ShowComboRoutine(comboMultiplier));

        // Yüksek combo'da ekran flaşı
        if (comboMultiplier >= 3)
            TriggerScreenFlash(GetComboFlashColor(comboMultiplier));
    }

    private Color GetComboFlashColor(int multiplier)
    {
        if (multiplier >= 5) return new Color(1f, 0.2f, 0.2f, 0.3f); // kırmızı
        if (multiplier >= 4) return new Color(1f, 0.5f, 0f,   0.25f); // turuncu
        return new Color(1f, 0.9f, 0f, 0.2f);                          // sarı
    }

    private IEnumerator ShowComboRoutine(int comboMultiplier)
    {
        comboText.gameObject.SetActive(true);
        comboText.text = $"COMBO x{comboMultiplier}!";
        comboText.color = GetComboTextColor(comboMultiplier);

        Vector3 originalScale = Vector3.one;
        comboText.transform.localScale = Vector3.zero;

        float duration = 0.18f;
        float timer    = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            // Overshoot
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
        if (screenFlashImage == null)
            return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(ScreenFlashRoutine(color));
    }

    private IEnumerator ScreenFlashRoutine(Color color)
    {
        screenFlashImage.gameObject.SetActive(true);
        screenFlashImage.color = color;

        float duration = 0.35f;
        float timer    = 0f;
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
