using TMPro;
using UnityEngine;
using System.Collections;

public class GameplayUI : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text levelText;
    public TMP_Text scoreText;
    public TMP_Text movesText;

    [Header("Panels")]
    public GameObject winPanel;
    public GameObject gameOverPanel;

    [Header("Combo")]
    public TMP_Text comboText;

    private Coroutine comboCoroutine;

    private int currentRemainingMoves;

    public void UpdateLevel(int level)
    {
        levelText.text = $"Level {level}";
    }

    public void UpdateScore(int currentScore, int targetScore)
    {
        scoreText.text = $"Score: {currentScore} / {targetScore}";
    }

    public void UpdateMoves(int remainingMoves)
    {
        currentRemainingMoves = remainingMoves;
        movesText.text = $"Moves: {remainingMoves}";
    }

    public void ShowMovePreviewCost(int cost)
    {
        movesText.text = $"Moves: {currentRemainingMoves} <color=red>-{cost}</color>";
    }

    public void HideMovePreviewCost()
    {
        movesText.text = $"Moves: {currentRemainingMoves}";
    }

    public void ShowWinPanel()
    {
        winPanel.SetActive(true);
    }

    public void ShowGameOverPanel()
    {
        gameOverPanel.SetActive(true);
    }

    public void HidePanels()
    {
        winPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }
    public void ShowCombo(int comboMultiplier)
    {
        if (comboMultiplier <= 1)
            return;

        if (comboText == null)
            return;

        if (comboCoroutine != null)
            StopCoroutine(comboCoroutine);

        comboCoroutine = StartCoroutine(ShowComboRoutine(comboMultiplier));
    }

    private IEnumerator ShowComboRoutine(int comboMultiplier)
    {
        comboText.gameObject.SetActive(true);
        comboText.text = $"COMBO x{comboMultiplier}!";

        Vector3 originalScale = Vector3.one;
        comboText.transform.localScale = Vector3.zero;

        float duration = 0.15f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            comboText.transform.localScale = Vector3.Lerp(
                Vector3.zero,
                originalScale,
                t
            );

            yield return null;
        }

        comboText.transform.localScale = originalScale;

        yield return new WaitForSeconds(0.5f);

        comboText.gameObject.SetActive(false);
        comboCoroutine = null;
    }
}