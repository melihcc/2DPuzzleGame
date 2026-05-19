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

    [Header("Star Thresholds")]
    [Tooltip("2 yıldız için gereken skor (0 = devre dışı)")]
    public int starThreshold2;
    [Tooltip("3 yıldız için gereken skor (0 = devre dışı)")]
    public int starThreshold3;

    [Header("Player Aids")]
    [Tooltip("Level başına ipucu kullanım hakkı")]
    public int hintCount = 3;
    [Tooltip("Level başına geri alma hakkı")]
    public int undoCount = 2;

    [Header("Initial Tiles")]
    public InitialTileData[] initialTiles;

    [Header("Blocked Cells")]
    public BlockedCellData[] blockedCells;

    [Header("Spawn Settings")]
    public bool enableAutoSpawn;
    public int spawnAfterMergeCount = 1;
    public int[] spawnableTileLevels;
    [Tooltip("Auto-spawn'da çıkabilecek özel tile tipleri (boş bırakılırsa sadece Normal)")]
    public TileType[] spawnableTileTypes;
}
