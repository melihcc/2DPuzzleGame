using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileDragHandler : MonoBehaviour
{
    private Tile tile;
    private GridManager gridManager;
    private GameManager gameManager;
    private Pathfinder pathfinder;
    private PathPreview pathPreview;

    private GridCell originalCell;
    private Camera mainCamera;

    private bool isMoving;
    private GameObject previewTile;

    private AudioManager audioManager;

    private CameraShaker cameraShaker;

    private void Awake()
    {
        tile = GetComponent<Tile>();
        mainCamera = Camera.main;

        gridManager = FindFirstObjectByType<GridManager>();
        gameManager = FindFirstObjectByType<GameManager>();
        pathfinder = FindFirstObjectByType<Pathfinder>();
        pathPreview = FindFirstObjectByType<PathPreview>();
        audioManager = FindFirstObjectByType<AudioManager>();
        cameraShaker = FindFirstObjectByType<CameraShaker>();
    }

    public void BeginDrag()
    {
        if (gameManager.IsGameOver || isMoving)
            return;

        originalCell = tile.CurrentCell;

        tile.SetAlpha(0.35f);

        previewTile = Instantiate(gameObject, transform.position, Quaternion.identity);

        TileDragHandler previewDragHandler = previewTile.GetComponent<TileDragHandler>();
        if (previewDragHandler != null)
            Destroy(previewDragHandler);

        Collider2D previewCollider = previewTile.GetComponent<Collider2D>();
        if (previewCollider != null)
            previewCollider.enabled = false;

        Tile previewTileScript = previewTile.GetComponent<Tile>();
        if (previewTileScript != null)
            previewTileScript.SetAlpha(0.85f);
    }

    public void Drag(Vector3 worldPosition)
    {
        if (gameManager.IsGameOver || isMoving || previewTile == null)
            return;

        previewTile.transform.position = worldPosition;

        GridCell targetCell = gridManager.GetClosestCell(worldPosition);

        if (targetCell == null || targetCell == originalCell || targetCell.IsBlocked)
        {
            HidePreviewFeedback();
            return;
        }

        List<GridCell> path = pathfinder.FindPath(originalCell, targetCell);

        if (path == null || path.Count == 0)
        {
            HidePreviewFeedback();
            return;
        }

        if (pathPreview != null)
            pathPreview.ShowPath(path);

        if (gameManager != null && gameManager.gameplayUI != null)
            gameManager.gameplayUI.ShowMovePreviewCost(path.Count);
    }

    public void EndDrag()
    {
        if (gameManager.IsGameOver || isMoving)
            return;

        HidePreviewFeedback();

        Vector3 releasePosition = previewTile != null
            ? previewTile.transform.position
            : transform.position;

        if (previewTile != null)
            Destroy(previewTile);

        tile.SetAlpha(1f);

        GridCell targetCell = gridManager.GetClosestCell(releasePosition);

        if (targetCell == null || targetCell == originalCell)
            return;

        if (targetCell.IsBlocked)
            return;

        List<GridCell> path = pathfinder.FindPath(originalCell, targetCell);

        if (path == null || path.Count == 0)
            return;

        if (!gameManager.HasEnoughMoves(path.Count))
            return;

        if (targetCell.IsEmpty)
        {
            StartCoroutine(MoveToCellWithAnimation(targetCell, path));
            return;
        }

        Tile targetTile = targetCell.CurrentTile;

        if (targetTile.Level == tile.Level)
        {
            StartCoroutine(MergeWithAnimation(targetTile, path));
        }
    }

    private IEnumerator MoveToCellWithAnimation(GridCell targetCell, List<GridCell> path)
    {
        isMoving = true;

        originalCell.ClearTile();
        tile.SetCellWithoutMoving(targetCell);
        targetCell.SetTile(tile);

        gameManager.RegisterMoves(path.Count);

        yield return StartCoroutine(tile.MoveAlongPath(path));

        isMoving = false;
    }

    private IEnumerator MergeWithAnimation(Tile targetTile, List<GridCell> path)
    {
        isMoving = true;

        originalCell.ClearTile();

        yield return StartCoroutine(tile.MoveAlongPath(path));

        targetTile.IncreaseLevel();
        targetTile.PlayMergeParticle();
        if (audioManager != null)
    audioManager.PlayMergeSound();

if (cameraShaker != null)
{
    float shakeStrength = 0.03f + targetTile.Level * 0.01f;
    cameraShaker.Shake(0.08f, shakeStrength);
}
        yield return StartCoroutine(targetTile.PlayMergePunch());

        int scoreToAdd = targetTile.Level * 100;
        gameManager.RegisterMergeResult(path.Count, scoreToAdd);
        gameManager.HandleAfterMerge();

        Destroy(gameObject);
    }

    private void HidePreviewFeedback()
    {
        if (pathPreview != null)
            pathPreview.Hide();

        if (gameManager != null && gameManager.gameplayUI != null)
            gameManager.gameplayUI.HideMovePreviewCost();
    }

#if UNITY_EDITOR
    private void OnMouseDown()
    {
        BeginDrag();
    }

    private void OnMouseDrag()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -mainCamera.transform.position.z;

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);

        Drag(worldPosition);
    }

    private void OnMouseUp()
    {
        EndDrag();
    }
#endif
}