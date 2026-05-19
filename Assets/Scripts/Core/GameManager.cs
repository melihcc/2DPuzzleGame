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
    public float maxComboTime = 5f;

    public bool IsGameOver { get; private set; }

    // ─── Private state ────────────────────────────────────────────────────────

    private float lastMergeTime;
    private int   mergeCounter;

    private int remainingHints;
    private int remainingUndos;

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

    public void ResetProgressToFirstLevel()
    {
        PlayerPrefs.SetInt("CurrentLevelArrayIndex", 0);
        PlayerPrefs.Save();
        levelManager.LoadLevel(0);
        StartLevel();
    }

    // ─── Combo ───────────────────────────────────────────────────────────────

    public int GetComboMultiplier()
    {
        if (Time.time - lastMergeTime <= maxComboTime)
            comboCount++;
        else
            comboCount = 1;

        lastMergeTime = Time.time;
        return comboCount;
    }

    // ─── Move / Score registration ────────────────────────────────────────────

    public bool HasEnoughMoves(int moveCost) => remainingMoves >= moveCost;

    public void RegisterMove()   => RegisterMoves(1);

    public void RegisterMoves(int moveAmount)
    {
        if (IsGameOver) return;

        remainingMoves -= moveAmount;
        if (remainingMoves < 0) remainingMoves = 0;

        gameplayUI.UpdateMoves(remainingMoves);
        CheckGameState();
    }

    public void AddScore(int amount)
    {
        if (IsGameOver) return;

        currentScore += amount;
        gameplayUI.UpdateScore(currentScore, levelManager.TargetScore);
        CheckGameState();
    }

    public void RegisterMergeResult(int moveCost, int scoreAmount, Vector3 mergeWorldPos)
    {
        if (IsGameOver) return;

        int comboMultiplier = GetComboMultiplier();

        // Gerçek çarpan: x1, x2, x3...
        int finalScore = scoreAmount * comboMultiplier;

        remainingMoves -= moveCost;
        if (remainingMoves < 0) remainingMoves = 0;

        currentScore += finalScore;

        gameplayUI.UpdateMoves(remainingMoves);
        gameplayUI.UpdateScore(currentScore, levelManager.TargetScore);

        // Floating score text
        Color textColor = comboMultiplier > 1
            ? new Color(1f, 0.85f, 0f)
            : Color.white;

        string label = comboMultiplier > 1
            ? $"+{finalScore} x{comboMultiplier}!"
            : $"+{finalScore}";

        FloatingText.Spawn(mergeWorldPos, label, textColor);

        if (comboMultiplier > 1)
            gameplayUI.ShowCombo(comboMultiplier);

        CheckGameState();
    }

    // Overload for backward compat with callers that don't have world pos yet
    public void RegisterMergeResult(int moveCost, int scoreAmount)
    {
        RegisterMergeResult(moveCost, scoreAmount, Vector3.zero);
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

        bool spawned = gridManager.SpawnRandomTile(spawnLevel, spawnType);

        if (!spawned)
            Debug.Log("No empty cell available for auto spawn.");
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
        if (remainingHints <= 0 || IsGameOver)
            return;

        ClearHints();

        (GridCell source, GridCell target) = FindBestMergePair();

        if (source == null)
        {
            Debug.Log("Hint: Merge pair bulunamadı.");
            return;
        }

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
        GridCell bestSource = null;
        GridCell bestTarget = null;
        int  bestLevel      = -1;
        int  bestPathLen    = int.MaxValue;

        for (int x = 0; x < allCells.GetLength(0); x++)
        {
            for (int y = 0; y < allCells.GetLength(1); y++)
            {
                GridCell cell = allCells[x, y];
                if (cell == null || cell.IsBlocked || cell.IsEmpty) continue;

                Tile tile = cell.CurrentTile;

                for (int tx = 0; tx < allCells.GetLength(0); tx++)
                {
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
                            bestLevel   = mergeLevel;
                            bestPathLen = path.Count;
                            bestSource  = cell;
                            bestTarget  = targetCell;
                        }
                    }
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
        if (!undoSnapshot.HasValue || remainingUndos <= 0 || IsGameOver)
            return;

        ClearHints();

        GameSnapshot snap = undoSnapshot.Value;

        gridManager.RespawnFromStates(snap.tiles);

        currentScore  = snap.score;
        remainingMoves = snap.moves;
        comboCount    = snap.combo;
        lastMergeTime = snap.lastMergeTime;
        mergeCounter  = snap.mergeCounter;

        undoSnapshot = null;
        remainingUndos--;

        gameplayUI.UpdateScore(currentScore, levelManager.TargetScore);
        gameplayUI.UpdateMoves(remainingMoves);
        gameplayUI.UpdateUndos(remainingUndos);
    }

    // ─── Game state check ─────────────────────────────────────────────────────

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

    private bool HasAnyPossibleMerge()
    {
        GridCell[,] allCells = gridManager.GetAllCells();

        bool hasSpecialTile = false;
        int  totalTiles     = 0;

        for (int x = 0; x < allCells.GetLength(0); x++)
        {
            for (int y = 0; y < allCells.GetLength(1); y++)
            {
                GridCell cell = allCells[x, y];
                if (cell == null || cell.IsBlocked || cell.IsEmpty) continue;

                totalTiles++;

                TileType t = cell.CurrentTile.TileType;
                if (t == TileType.Wild || t == TileType.Bomb)
                    hasSpecialTile = true;
            }
        }

        // Wild/Bomb her tile ile merge olabilir
        if (hasSpecialTile && totalTiles >= 2)
            return true;

        // Normal kontrol: aynı level'dan iki tile
        for (int x = 0; x < allCells.GetLength(0); x++)
        {
            for (int y = 0; y < allCells.GetLength(1); y++)
            {
                GridCell cell = allCells[x, y];
                if (cell == null || cell.IsBlocked || cell.IsEmpty) continue;
                if (cell.CurrentTile.TileType != TileType.Normal) continue;

                Tile tile = cell.CurrentTile;

                for (int tx = 0; tx < allCells.GetLength(0); tx++)
                {
                    for (int ty = 0; ty < allCells.GetLength(1); ty++)
                    {
                        GridCell targetCell = allCells[tx, ty];
                        if (targetCell == null || targetCell.IsBlocked || targetCell.IsEmpty) continue;
                        if (targetCell == cell) continue;
                        if (targetCell.CurrentTile.TileType != TileType.Normal) continue;

                        if (targetCell.CurrentTile.Level == tile.Level)
                            return true;
                    }
                }
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
        SaveStars(stars);

        gameplayUI.ShowWinPanel(stars);
    }

    private void LoseLevel()
    {
        IsGameOver = true;
        ClearHints();
        gameplayUI.ShowGameOverPanel();
    }

    private int CalculateStars()
    {
        LevelData level = levelManager.CurrentLevel;

        if (level.starThreshold3 > 0 && currentScore >= level.starThreshold3)
            return 3;

        if (level.starThreshold2 > 0 && currentScore >= level.starThreshold2)
            return 2;

        return 1;
    }

    private void SaveStars(int stars)
    {
        string key    = $"Stars_{levelManager.CurrentLevelArrayIndex}";
        int    saved  = PlayerPrefs.GetInt(key, 0);

        if (stars > saved)
        {
            PlayerPrefs.SetInt(key, stars);
            PlayerPrefs.Save();
        }
    }
}
