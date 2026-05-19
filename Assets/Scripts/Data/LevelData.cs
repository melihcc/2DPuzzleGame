using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Puzzle/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public int levelIndex;

    [Header("Grid Settings")]
    public int gridWidth;
    public int gridHeight;

    [Header("Game Rules")]
    public int moveLimit;
    public int targetScore;

    [Header("Initial Tiles")]
    public InitialTileData[] initialTiles;

    [Header("Blocked Cells")]
    public BlockedCellData[] blockedCells;

    [Header("Spawn Settings")]
    public bool enableAutoSpawn;
    public int spawnAfterMergeCount = 1;
    public int[] spawnableTileLevels;
}