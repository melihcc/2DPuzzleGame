using UnityEngine;

public class GridCell : MonoBehaviour
{
    [Header("Visual")]
    public SpriteRenderer spriteRenderer;

    public int X { get; private set; }
    public int Y { get; private set; }

    public CellType CellType { get; private set; } = CellType.Normal;

    public Tile CurrentTile { get; private set; }

    public bool IsEmpty => CurrentTile == null;
    public bool IsBlocked => CellType == CellType.Blocked;
    public bool CanAcceptTile => !IsBlocked && IsEmpty;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Setup(int x, int y)
    {
        X = x;
        Y = y;

        SetCellType(CellType.Normal);

        gameObject.name = $"Cell_{x}_{y}";
    }

    public void SetCellType(CellType cellType)
    {
        CellType = cellType;
        UpdateVisual();
    }

    public void SetTile(Tile tile)
    {
        if (IsBlocked)
            return;

        CurrentTile = tile;
    }

    public void ClearTile()
    {
        CurrentTile = null;
    }

    private void UpdateVisual()
    {
        if (spriteRenderer == null)
            return;

        if (CellType == CellType.Blocked)
        {
            spriteRenderer.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        }
        else
        {
            spriteRenderer.color = new Color(0.78f, 0.78f, 0.78f, 1f);
        }
    }
}