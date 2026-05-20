using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public LevelManager levelManager;
    public GridManager  gridManager;
    public Pathfinder   pathfinder;
    public GameplayUI   gameplayUI;

    [Header("Runtime Values")]
    public int currentScore;
    public int remainingMoves;

    [Header("Combo")]
    public int   comboCount;
    public float maxComboTime       = 1.5f;
    public int   maxComboMultiplier = 3;

    public bool IsGameOver      { get; private set; }
    public bool IsAnyTileMoving { get; private set; }

    public void SetTileMoving(bool moving) { IsAnyTileMoving = moving; }

    // ─── Private state ────────────────────────────────────────────────────────

    private float lastMergeTime;
    private int   mergeCounter;
    private int   remainingHints;
    private int   remainingUndos;
    private int   lastShownStars = 0;

    private Tile hintSourceTile;
    private Tile hintTargetTile;

    private struct GameSnapshot
    {
        public List<GridManager.TileSnapshot> tiles;
        public int   score;
        public int   moves;
        public int   combo;
        public float lastMergeTime;
        public int   mergeCounter;
    }

    private GameSnapshot? undoSnapshot;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        StartLevel();
    }

    private void StartLevel()
    {
        comboCount     = 0;
        lastMergeTime  = -999f;
        mergeCounter   = 0;
        currentScore   = 0;
        remainingMoves = levelManager.MoveLimit;
        IsGameOver     = false;
        undoSnapshot   = null;
        lastShownStars = 0;

        remainingHints = levelManager.CurrentLevel.hintCount;
        remainingUndos = levelManager.CurrentLevel.undoCount;

        ClearHints();

        gameplayUI.HidePanels();
        gridManager.BuildLevel();

        gameplayUI.UpdateLevel(levelManager.CurrentLevelIndex);
        gameplayUI.UpdateScore(currentScore, levelManager.TargetScore);
        gameplayUI.UpdateMoves(remainingMoves);
        gameplayUI.UpdateHints(remainingHints);
        gameplayUI.UpdateUndos(remainingUndos);
        gameplayUI.UpdateLiveStars(0, levelManager.CurrentLevel);
    }

    // ─── Level flow ───────────────────────────────────────────────────────────

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

    public void GoToLevelSelect()
    {
        SceneLoader.LoadLevelSelect();
    }

    // ─── Combo ───────────────────────────────────────────────────────────────

    public int GetComboMultiplier()
    {
        if (Time.time - lastMergeTime <= maxComboTime)
            comboCount = Mathf.Min(comboCount + 1, maxComboMultiplier);
        else
            comboCount = 1;

        lastMergeTime = Time.time;
        return comboCount;
    }

    // ─── Move / Score registration ────────────────────────────────────────────

    public bool HasEnoughMoves(int moveCost) => remainingMoves >= moveCost;

    public void RegisterMove() => RegisterMoves(1);

    public void RegisterMoves(int moveAmount)
    {
        if (IsGameOver) return;

        remainingMoves -= moveAmount;
        if (remainingMoves < 0) remainingMoves = 0;

        gameplayUI.UpdateMoves(remainingMoves);
        CheckGameState();
    }

    public void RegisterMergeResult(int moveCost, int scoreAmount, Vector3 mergeWorldPos)
    {
        if (IsGameOver) return;

        int   comboMultiplier = GetComboMultiplier();
        float bonusRate       = (comboMultiplier - 1) * 0.25f;
        int   finalScore      = Mathf.RoundToInt(scoreAmount * (1f + bonusRate));

        remainingMoves -= moveCost;
        if (remainingMoves < 0) remainingMoves = 0;

        currentScore += finalScore;

        gameplayUI.UpdateMoves(remainingMoves);
        gameplayUI.UpdateScore(currentScore, levelManager.TargetScore);

        // Floating score
        Color  textColor = comboMultiplier > 1 ? new Color(1f, 0.85f, 0f) : Color.white;
        string label     = comboMultiplier > 1 ? $"+{finalScore} x{comboMultiplier}!" : $"+{finalScore}";
        FloatingText.Spawn(mergeWorldPos, label, textColor);

        if (comboMultiplier > 1)
            gameplayUI.ShowCombo(comboMultiplier);

        // Canlı yıldız güncellemesi
        UpdateLiveStars();

        CheckGameState();
    }

    public void RegisterMergeResult(int moveCost, int scoreAmount)
        => RegisterMergeResult(moveCost, scoreAmount, Vector3.zero);

    // ─── Live stars ───────────────────────────────────────────────────────────

    private void UpdateLiveStars()
    {
        int stars = CalculateStars();
        gameplayUI.UpdateLiveStars(stars, levelManager.CurrentLevel);

        if (stars > lastShownStars)
        {
            lastShownStars = stars;
            gameplayUI.ShowStarEarned(stars);
        }
    }

    // ─── Finish (oyuncu manuel bitirir) ──────────────────────────────────────

    public void FinishLevel()
    {
        if (IsGameOver) return;

        if (currentScore >= levelManager.TargetScore)
            WinLevel();
        else
            LoseLevel();
    }

    // ─── Auto-spawn ───────────────────────────────────────────────────────────

    public void HandleAfterMerge()
    {
        if (!levelManager.CurrentLevel.enableAutoSpawn)
            return;

        mergeCounter++;

        if (mergeCounter < levelManager.CurrentLevel.spawnAfterMergeCount)
            return;

        mergeCounter = 0;

        int      spawnLevel = GetRandomSpawnLevel();
        TileType spawnType  = GetRandomSpawnType();

        if (!gridManager.SpawnRandomTile(spawnLevel, spawnType))
            Debug.Log("No empty cell for auto spawn.");
    }

    private int GetRandomSpawnLevel()
    {
        int[] levels = levelManager.CurrentLevel.spawnableTileLevels;
        if (levels == null || levels.Length == 0) return 1;
        return levels[Random.Range(0, levels.Length)];
    }

    private TileType GetRandomSpawnType()
    {
        TileType[] types = levelManager.CurrentLevel.spawnableTileTypes;
        if (types == null || types.Length == 0) return TileType.Normal;
        return types[Random.Range(0, types.Length)];
    }

    // ─── Hint ─────────────────────────────────────────────────────────────────

    public void UseHint()
    {
        if (remainingHints <= 0 || IsGameOver) return;

        ClearHints();
        (GridCell source, GridCell target) = FindBestMergePair();

        if (source == null) return;

        remainingHints--;
        gameplayUI.UpdateHints(remainingHints);

        hintSourceTile = source.CurrentTile;
        hintTargetTile = target.CurrentTile;

        hintSourceTile.StartHintPulse();
        hintTargetTile.StartHintPulse();
    }

    public void ClearHints()
    {
        if (hintSourceTile != null) { hintSourceTile.StopHintPulse(); hintSourceTile = null; }
        if (hintTargetTile != null) { hintTargetTile.StopHintPulse(); hintTargetTile = null; }
    }

    private (GridCell, GridCell) FindBestMergePair()
    {
        GridCell[,] allCells = gridManager.GetAllCells();
        GridCell bestSource = null, bestTarget = null;
        int bestLevel = -1, bestPathLen = int.MaxValue;

        for (int x = 0; x < allCells.GetLength(0); x++)
        for (int y = 0; y < allCells.GetLength(1); y++)
        {
            GridCell cell = allCells[x, y];
            if (cell == null || cell.IsBlocked || cell.IsEmpty) continue;
            Tile tile = cell.CurrentTile;

            for (int tx = 0; tx < allCells.GetLength(0); tx++)
            for (int ty = 0; ty < allCells.GetLength(1); ty++)
            {
                GridCell targetCell = allCells[tx, ty];
                if (targetCell == null || targetCell.IsBlocked || targetCell.IsEmpty) continue;
                if (targetCell == cell) continue;

                Tile targetTile = targetCell.CurrentTile;
                bool canMerge = tile.Level == targetTile.Level
                    || tile.TileType       == TileType.Wild
                    || targetTile.TileType == TileType.Wild
                    || tile.TileType       == TileType.Bomb
                    || targetTile.TileType == TileType.Bomb;

                if (!canMerge) continue;

                List<GridCell> path = pathfinder.FindPath(cell, targetCell);
                if (path == null || path.Count == 0) continue;
                if (!HasEnoughMoves(path.Count)) continue;

                int mergeLevel = Mathf.Max(tile.Level, targetTile.Level);
                if (mergeLevel > bestLevel || (mergeLevel == bestLevel && path.Count < bestPathLen))
                {
                    bestLevel = mergeLevel; bestPathLen = path.Count;
                    bestSource = cell; bestTarget = targetCell;
                }
            }
        }

        return (bestSource, bestTarget);
    }

    // ─── Undo ─────────────────────────────────────────────────────────────────

    public void SaveStateForUndo()
    {
        undoSnapshot = new GameSnapshot
        {
            tiles         = gridManager.GetAllTileStates(),
            score         = currentScore,
            moves         = remainingMoves,
            combo         = comboCount,
            lastMergeTime = lastMergeTime,
            mergeCounter  = mergeCounter
        };
    }

    public void UndoLastMove()
    {
        if (!undoSnapshot.HasValue || remainingUndos <= 0 || IsGameOver) return;

        ClearHints();
        GameSnapshot snap = undoSnapshot.Value;

        gridManager.RespawnFromStates(snap.tiles);
        currentScore   = snap.score;
        remainingMoves = snap.moves;
        comboCount     = snap.combo;
        lastMergeTime  = snap.lastMergeTime;
        mergeCounter   = snap.mergeCounter;

        undoSnapshot = null;
        remainingUndos--;

        gameplayUI.UpdateScore(currentScore, levelManager.TargetScore);
        gameplayUI.UpdateMoves(remainingMoves);
        gameplayUI.UpdateUndos(remainingUndos);
        UpdateLiveStars();
    }

    // ─── Game state check ─────────────────────────────────────────────────────

    private void CheckGameState()
    {
        // Skor yeterli olsa bile oyun BİTMEZ — oyuncu devam edebilir veya "Bitir" der
        // Oyun sadece hamle bitince veya merge kalmayınca biter

        if (remainingMoves <= 0)
        {
            if (currentScore >= levelManager.TargetScore)
                WinLevel();
            else
                LoseLevel();
            return;
        }

        if (!HasAnyPossibleMerge())
        {
            if (currentScore >= levelManager.TargetScore)
                WinLevel();
            else
                LoseLevel();
            return;
        }
    }

    private bool HasAnyPossibleMerge()
    {
        GridCell[,] allCells = gridManager.GetAllCells();
        bool hasSpecialTile = false;
        int  totalTiles     = 0;

        for (int x = 0; x < allCells.GetLength(0); x++)
        for (int y = 0; y < allCells.GetLength(1); y++)
        {
            GridCell cell = allCells[x, y];
            if (cell == null || cell.IsBlocked || cell.IsEmpty) continue;
            totalTiles++;
            TileType t = cell.CurrentTile.TileType;
            if (t == TileType.Wild || t == TileType.Bomb) hasSpecialTile = true;
        }

        if (hasSpecialTile && totalTiles >= 2) return true;

        for (int x = 0; x < allCells.GetLength(0); x++)
        for (int y = 0; y < allCells.GetLength(1); y++)
        {
            GridCell cell = allCells[x, y];
            if (cell == null || cell.IsBlocked || cell.IsEmpty) continue;
            if (cell.CurrentTile.TileType != TileType.Normal) continue;
            Tile tile = cell.CurrentTile;

            for (int tx = 0; tx < allCells.GetLength(0); tx++)
            for (int ty = 0; ty < allCells.GetLength(1); ty++)
            {
                GridCell targetCell = allCells[tx, ty];
                if (targetCell == null || targetCell.IsBlocked || targetCell.IsEmpty) continue;
                if (targetCell == cell) continue;
                if (targetCell.CurrentTile.TileType != TileType.Normal) continue;
                if (targetCell.CurrentTile.Level == tile.Level) return true;
            }
        }

        return false;
    }

    // ─── Win / Lose ───────────────────────────────────────────────────────────

    private void WinLevel()
    {
        IsGameOver = true;
        ClearHints();

        int stars = CalculateStars();
        SaveProgress(stars);

        gameplayUI.ShowWinPanel(stars);
    }

    private void LoseLevel()
    {
        IsGameOver = true;
        ClearHints();
        gameplayUI.ShowGameOverPanel();
    }

    // ─── Stars & Progress ─────────────────────────────────────────────────────

    public int CalculateStars()
    {
        LevelData level = levelManager.CurrentLevel;

        if (level.starThreshold3 > 0 && currentScore >= level.starThreshold3) return 3;
        if (level.starThreshold2 > 0 && currentScore >= level.starThreshold2) return 2;
        if (currentScore >= level.targetScore) return 1;
        return 0;
    }

    private void SaveProgress(int stars)
    {
        int idx = levelManager.CurrentLevelArrayIndex;

        // Yıldız kaydet (sadece daha iyiyse)
        string key   = $"Stars_{idx}";
        int    saved = PlayerPrefs.GetInt(key, 0);
        if (stars > saved)
            PlayerPrefs.SetInt(key, stars);

        // Sonraki leveli aç
        if (stars > 0)
        {
            int highest = PlayerPrefs.GetInt("HighestUnlockedLevel", 0);
            int next    = idx + 1;
            if (next > highest)
                PlayerPrefs.SetInt("HighestUnlockedLevel", next);
        }

        PlayerPrefs.Save();
    }
}
