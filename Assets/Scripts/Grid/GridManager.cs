using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("References")]
    public LevelManager levelManager;
    public GameObject cellPrefab;
    public GameObject tilePrefab;

    [Header("Camera")]
    public CameraFitter cameraFitter;

    [Header("Grid Settings")]
    public float cellSize = 1f;
    public float cellSpacing = 0.1f;

    [Header("Snap Settings")]
    public float maxSnapDistance = 0.8f;

    private GridCell[,] cells;

    public void BuildLevel()
    {
        ClearGrid();
        CreateGrid();
        ApplyBlockedCells();
        SpawnInitialTiles();

        if (cameraFitter != null)
            cameraFitter.FitToGrid(levelManager.GridWidth, levelManager.GridHeight, cellSize + cellSpacing);
    }

    private void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        cells = null;
    }

    private void CreateGrid()
    {
        int width = levelManager.GridWidth;
        int height = levelManager.GridHeight;

        cells = new GridCell[width, height];

        float step = cellSize + cellSpacing;

        float totalWidth = width * step;
        float totalHeight = height * step;

        Vector2 startPosition = new Vector2(
            -totalWidth / 2f + step / 2f,
            -totalHeight / 2f + step / 2f
        );

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 spawnPosition = startPosition + new Vector2(
                    x * step,
                    y * step
                );

                GameObject cellObject = Instantiate(
                    cellPrefab,
                    spawnPosition,
                    Quaternion.identity,
                    transform
                );

                GridCell cell = cellObject.GetComponent<GridCell>();
                cell.Setup(x, y);

                cells[x, y] = cell;
            }
        }
    }

    private void ApplyBlockedCells()
    {
        foreach (BlockedCellData blockedData in levelManager.CurrentLevel.blockedCells)
        {
            GridCell cell = GetCell(blockedData.x, blockedData.y);

            if (cell == null)
            {
                Debug.LogWarning($"Invalid blocked cell position: {blockedData.x}, {blockedData.y}");
                continue;
            }

            cell.SetCellType(CellType.Blocked);
        }
    }

    private void SpawnInitialTiles()
    {
        foreach (InitialTileData tileData in levelManager.CurrentLevel.initialTiles)
        {
            SpawnTile(tileData.x, tileData.y, tileData.level);
        }
    }

        public void SpawnTile(int x, int y, int level)
    {
        GridCell cell = GetCell(x, y);

        if (cell == null)
        {
            Debug.LogWarning($"Invalid tile position: {x}, {y}");
            return;
        }

        if (cell.IsBlocked)
        {
            Debug.LogWarning($"Cannot spawn tile on blocked cell: {x}, {y}");
            return;
        }

        if (!cell.IsEmpty)
        {
            Debug.LogWarning($"Cell already has tile: {x}, {y}");
            return;
        }

        GameObject tileObject = Instantiate(
            tilePrefab,
            cell.transform.position,
            Quaternion.identity,
            transform
        );

        Tile tile = tileObject.GetComponent<Tile>();
        tile.Setup(level, cell);

        cell.SetTile(tile);

        StartCoroutine(tile.PlaySpawnAnimation());
    }

    public bool SpawnRandomTile(int level)
{
    List<GridCell> emptyCells = new List<GridCell>();

    for (int x = 0; x < cells.GetLength(0); x++)
    {
        for (int y = 0; y < cells.GetLength(1); y++)
        {
            GridCell cell = cells[x, y];

            if (cell == null)
                continue;

            if (cell.CanAcceptTile)
                emptyCells.Add(cell);
        }
    }

    if (emptyCells.Count == 0)
        return false;

    GridCell randomCell = emptyCells[Random.Range(0, emptyCells.Count)];
    SpawnTile(randomCell.X, randomCell.Y, level);

    return true;
}

    public GridCell GetCell(int x, int y)
    {
        if (x < 0 || y < 0)
            return null;

        if (x >= cells.GetLength(0) || y >= cells.GetLength(1))
            return null;

        return cells[x, y];
    }

    public GridCell[,] GetAllCells()
    {
        return cells;
    }

    public GridCell GetClosestCell(Vector3 worldPosition)
    {
        GridCell closestCell = null;
        float closestDistance = Mathf.Infinity;

        for (int x = 0; x < cells.GetLength(0); x++)
        {
            for (int y = 0; y < cells.GetLength(1); y++)
            {
                float distance = Vector3.Distance(worldPosition, cells[x, y].transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCell = cells[x, y];
                }
            }
        }

        if (closestDistance > maxSnapDistance)
            return null;

        return closestCell;
    }

    public List<GridCell> GetNeighbors(GridCell cell)
    {
        List<GridCell> neighbors = new List<GridCell>();

        int x = cell.X;
        int y = cell.Y;

        GridCell right = GetCell(x + 1, y);
        GridCell left = GetCell(x - 1, y);
        GridCell up = GetCell(x, y + 1);
        GridCell down = GetCell(x, y - 1);

        if (right != null)
            neighbors.Add(right);

        if (left != null)
            neighbors.Add(left);

        if (up != null)
            neighbors.Add(up);

        if (down != null)
            neighbors.Add(down);

        return neighbors;
    }
}