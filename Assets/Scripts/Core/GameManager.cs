using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public LevelManager levelManager;
    public GridManager gridManager;
    public Pathfinder pathfinder;
    public GameplayUI gameplayUI;

    [Header("Runtime Values")]
    public int currentScore;
    public int remainingMoves;

    [Header("Combo")]
    public int comboCount;
    public int maxComboTime = 5;

    private float lastMergeTime;

    private int mergeCounter;

    public bool IsGameOver { get; private set; }

    private void Start()
    {
        StartLevel();
    }

    private void StartLevel()
    {
        comboCount = 0;
        lastMergeTime = -999f;
        mergeCounter = 0;
        currentScore = 0;
        remainingMoves = levelManager.MoveLimit;
        IsGameOver = false;

        gameplayUI.HidePanels();

        gridManager.BuildLevel();

        gameplayUI.UpdateLevel(levelManager.CurrentLevelIndex);
        gameplayUI.UpdateScore(currentScore, levelManager.TargetScore);
        gameplayUI.UpdateMoves(remainingMoves);
    }

    public void RetryLevel()
    {
        levelManager.ReloadCurrentLevel();
        StartLevel();
    }

    public void NextLevel()
    {
        levelManager.LoadNextLevel();
        StartLevel();
    }

    public void ResetProgressToFirstLevel()
    {
        PlayerPrefs.SetInt("CurrentLevelArrayIndex", 0);
        PlayerPrefs.Save();

        levelManager.LoadLevel(0);
        StartLevel();
    }
    public int GetComboMultiplier()
    {
        if (Time.time - lastMergeTime <= maxComboTime)
        {
            comboCount++;
        }
        else
        {
            comboCount = 1;
        }

        lastMergeTime = Time.time;

        return comboCount;
    }
    public bool HasEnoughMoves(int moveCost)
    {
        return remainingMoves >= moveCost;
    }

    public void RegisterMove()
    {
        RegisterMoves(1);
    }

    public void RegisterMoves(int moveAmount)
    {
        if (IsGameOver)
            return;

        remainingMoves -= moveAmount;

        if (remainingMoves < 0)
            remainingMoves = 0;

        gameplayUI.UpdateMoves(remainingMoves);

        CheckGameState();
    }

    public void AddScore(int amount)
    {
        if (IsGameOver)
            return;

        currentScore += amount;

        gameplayUI.UpdateScore(currentScore, levelManager.TargetScore);

        CheckGameState();
    }

    public void RegisterMergeResult(int moveCost, int scoreAmount)
    {
        if (IsGameOver)
            return;

        int comboMultiplier = GetComboMultiplier();
        int comboBonus = 0;

        if (comboMultiplier > 1)
            comboBonus = (comboMultiplier - 1) * 50;

        int finalScore = scoreAmount + comboBonus;

        remainingMoves -= moveCost;

        if (remainingMoves < 0)
            remainingMoves = 0;

        currentScore += finalScore;

        gameplayUI.UpdateMoves(remainingMoves);
        gameplayUI.UpdateScore(currentScore, levelManager.TargetScore);

        if (comboMultiplier > 1)
        {
            Debug.Log($"Combo x{comboMultiplier}! Bonus +{comboBonus}");

            if (gameplayUI != null)
                gameplayUI.ShowCombo(comboMultiplier);
        }

        CheckGameState();
    }

    private void CheckGameState()
{
    if (currentScore >= levelManager.TargetScore)
    {
        WinLevel();
        return;
    }

    if (remainingMoves <= 0)
    {
        LoseLevel();
        return;
    }

    
        if (!HasAnyPossibleMerge())
        {
            LoseLevel();
            return;
        }
    
}

    public void HandleAfterMerge()
{
    if (!levelManager.CurrentLevel.enableAutoSpawn)
        return;

    mergeCounter++;

    if (mergeCounter < levelManager.CurrentLevel.spawnAfterMergeCount)
        return;

    mergeCounter = 0;

    int spawnLevel = GetRandomSpawnLevel();

    bool spawned = gridManager.SpawnRandomTile(spawnLevel);

    if (!spawned)
    {
        Debug.Log("No empty cell available for auto spawn.");
    }
}

private int GetRandomSpawnLevel()
{
    int[] levels = levelManager.CurrentLevel.spawnableTileLevels;

    if (levels == null || levels.Length == 0)
        return 1;

    return levels[Random.Range(0, levels.Length)];
}

    private bool HasAnyPossibleMerge()
    {
        GridCell[,] cells = gridManager.GetAllCells();

        for (int x = 0; x < cells.GetLength(0); x++)
        {
            for (int y = 0; y < cells.GetLength(1); y++)
            {
                GridCell cell = cells[x, y];

                if (cell == null || cell.IsBlocked || cell.IsEmpty)
                    continue;

                Tile tile = cell.CurrentTile;

                for (int tx = 0; tx < cells.GetLength(0); tx++)
                {
                    for (int ty = 0; ty < cells.GetLength(1); ty++)
                    {
                        GridCell targetCell = cells[tx, ty];

                        if (targetCell == null || targetCell.IsBlocked || targetCell.IsEmpty)
                            continue;

                        if (targetCell == cell)
                            continue;

                        Tile targetTile = targetCell.CurrentTile;

                        if (targetTile.Level == tile.Level)
                            return true;
                    }
                }
            }
        }

        return false;
    }

    private void WinLevel()
    {
        IsGameOver = true;
        gameplayUI.ShowWinPanel();
    }

    private void LoseLevel()
    {
        IsGameOver = true;
        gameplayUI.ShowGameOverPanel();
    }
}