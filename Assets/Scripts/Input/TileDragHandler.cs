using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileDragHandler : MonoBehaviour
{
    private Tile         tile;
    private GridManager  gridManager;
    private GameManager  gameManager;
    private Pathfinder   pathfinder;
    private PathPreview  pathPreview;
    private AudioManager audioManager;
    private CameraShaker cameraShaker;

    private GridCell   originalCell;
    private Camera     mainCamera;
    private bool       isMoving;
    private GameObject previewTile;

    private void Awake()
    {
        tile         = GetComponent<Tile>();
        mainCamera   = Camera.main;
        gridManager  = FindFirstObjectByType<GridManager>();
        gameManager  = FindFirstObjectByType<GameManager>();
        pathfinder   = FindFirstObjectByType<Pathfinder>();
        pathPreview  = FindFirstObjectByType<PathPreview>();
        audioManager = FindFirstObjectByType<AudioManager>();
        cameraShaker = FindFirstObjectByType<CameraShaker>();
    }

    // ─── Drag events ─────────────────────────────────────────────────────────

    public void BeginDrag()
    {
        // Global kilit: herhangi bir tile hareket ediyorsa yeni drag başlatma
        if (gameManager.IsGameOver || isMoving || gameManager.IsAnyTileMoving)
            return;

        originalCell = tile.CurrentCell;
        tile.SetAlpha(0.35f);

        previewTile = Instantiate(gameObject, transform.position, Quaternion.identity);

        if (previewTile.TryGetComponent<TileDragHandler>(out var ph))  Destroy(ph);
        if (previewTile.TryGetComponent<Collider2D>(out var col))      col.enabled = false;
        if (previewTile.TryGetComponent<Tile>(out var pt))             pt.SetAlpha(0.85f);
    }

    public void Drag(Vector3 worldPosition)
    {
        if (gameManager.IsGameOver || isMoving || previewTile == null || originalCell == null)
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

        if (gameManager.gameplayUI != null)
            gameManager.gameplayUI.ShowMovePreviewCost(path.Count);
    }

    public void EndDrag()
    {
        if (gameManager.IsGameOver || isMoving || originalCell == null)
            return;

        HidePreviewFeedback();

        Vector3 releasePosition = previewTile != null
            ? previewTile.transform.position
            : transform.position;

        if (previewTile != null)
            Destroy(previewTile);

        tile.SetAlpha(1f);

        GridCell targetCell = gridManager.GetClosestCell(releasePosition);

        if (targetCell == null || targetCell == originalCell || targetCell.IsBlocked)
            return;

        List<GridCell> path = pathfinder.FindPath(originalCell, targetCell);

        if (path == null || path.Count == 0)
            return;

        if (!gameManager.HasEnoughMoves(path.Count))
            return;

        // ── Boş hücre: taşı ──────────────────────────────────────────────────
        if (targetCell.IsEmpty)
        {
            gameManager.SaveStateForUndo();
            gameManager.ClearHints();
            StartCoroutine(MoveToCellWithAnimation(targetCell, path));
            return;
        }

        // ── Dolu hücre: merge kontrolü ────────────────────────────────────────
        Tile targetTile = targetCell.CurrentTile;

        bool isBomb = tile.TileType == TileType.Bomb || targetTile.TileType == TileType.Bomb;

        bool canMerge = tile.Level == targetTile.Level
            || tile.TileType       == TileType.Wild
            || targetTile.TileType == TileType.Wild
            || isBomb;

        if (!canMerge)
            return;

        gameManager.SaveStateForUndo();
        gameManager.ClearHints();

        if (isBomb)
            StartCoroutine(BombMergeWithAnimation(targetTile, path, targetCell));
        else
            StartCoroutine(MergeWithAnimation(targetTile, path));
    }

    // ─── Coroutines ──────────────────────────────────────────────────────────

    private IEnumerator MoveToCellWithAnimation(GridCell targetCell, List<GridCell> path)
    {
        isMoving = true;
        gameManager.SetTileMoving(true);

        originalCell.ClearTile();
        tile.SetCellWithoutMoving(targetCell);
        targetCell.SetTile(tile);

        if (audioManager != null) audioManager.PlayMoveSound();

        gameManager.RegisterMoves(path.Count);

        yield return StartCoroutine(tile.MoveAlongPath(path));

        gameManager.SetTileMoving(false);
        isMoving = false;
    }

    private IEnumerator MergeWithAnimation(Tile targetTile, List<GridCell> path)
    {
        isMoving = true;
        gameManager.SetTileMoving(true);

        originalCell.ClearTile();

        yield return StartCoroutine(tile.MoveAlongPath(path));

        bool thisIsWild   = tile.TileType       == TileType.Wild;
        bool targetIsWild = targetTile.TileType  == TileType.Wild;
        bool isWild       = thisIsWild || targetIsWild;

        // ── Hangi tile hayatta kalır? ─────────────────────────────────────────
        // Kural: Wild olmayan tile hayatta kalır (seviye artar), Wild yutulur.
        // İkisi de Wild ise target hayatta kalır (mevcut davranış).
        Tile survivingTile;
        bool destroyThis; // true → gameObject (sürüklenen), false → targetTile

        if (targetIsWild && !thisIsWild)
        {
            // Normal tile → Wild tile: Wild yutulur, Normal tile target hücresine geçer.
            GridCell targetCell = targetTile.CurrentCell;
            targetCell.ClearTile();
            tile.SetCellWithoutMoving(targetCell);
            targetCell.SetTile(tile);
            Destroy(targetTile.gameObject);

            survivingTile = tile;
            destroyThis   = false; // bu tile kalmaya devam eder
        }
        else
        {
            // Wild tile → Normal tile  (veya Wild → Wild)
            // Target tile hayatta kalır, sürüklenen yok edilir.
            survivingTile = targetTile;
            destroyThis   = true;
        }

        // ── Görsel & ses ──────────────────────────────────────────────────────
        survivingTile.IncreaseLevel();
        survivingTile.PlayMergeParticle();

        if (audioManager != null)
        {
            if (isWild) audioManager.PlayWildMergeSound();
            else        audioManager.PlayMergeSound();
        }

        float shakeStrength = 0.03f + survivingTile.Level * 0.01f;
        if (isWild) shakeStrength *= 1.5f;
        if (cameraShaker != null) cameraShaker.Shake(0.08f, shakeStrength);

        yield return StartCoroutine(survivingTile.PlayMergePunch());

        // ── Skor ─────────────────────────────────────────────────────────────
        int baseScore = survivingTile.Level * 100;
        if (isWild) baseScore = Mathf.RoundToInt(baseScore * 1.25f);

        gameManager.SetTileMoving(false);
        gameManager.RegisterMergeResult(path.Count, baseScore, survivingTile.transform.position);
        gameManager.HandleAfterMerge();

        // ── Temizlik ──────────────────────────────────────────────────────────
        if (destroyThis)
        {
            Destroy(gameObject);
        }
        else
        {
            // Sürüklenen tile hayatta kalıyor — isMoving sıfırlanmazsa bir daha sürüklenemez
            isMoving = false;
        }
    }

    private IEnumerator BombMergeWithAnimation(Tile targetTile, List<GridCell> path, GridCell explosionCenter)
    {
        isMoving = true;
        gameManager.SetTileMoving(true);

        originalCell.ClearTile();

        yield return StartCoroutine(tile.MoveAlongPath(path));

        if (audioManager != null) audioManager.PlayBombSound();
        if (cameraShaker != null) cameraShaker.Shake(0.22f, 0.18f);

        // Komşuları patlat (merkez hâlâ targetTile içeriyor, oraya spawn olmaz)
        int explosionScore = gridManager.ExplodeBomb(explosionCenter);

        int baseScore = Mathf.Max(tile.Level, targetTile.Level) * 150;
        int total     = baseScore + explosionScore;

        if (explosionScore > 0)
            FloatingText.Spawn(
                explosionCenter.transform.position + Vector3.up * 0.6f,
                $"BOOM! +{explosionScore}",
                new Color(1f, 0.4f, 0f),
                4f
            );

        // Önce score/spawn kaydet (explosionCenter hâlâ dolu, yanlış spawn olmaz)
        gameManager.SetTileMoving(false);
        gameManager.RegisterMergeResult(path.Count, total, explosionCenter.transform.position);
        gameManager.HandleAfterMerge();

        // Sonra merkezi temizle
        explosionCenter.ClearTile();
        Destroy(targetTile.gameObject);
        Destroy(gameObject);
    }

    // ─── Preview feedback ─────────────────────────────────────────────────────

    private void HidePreviewFeedback()
    {
        if (pathPreview != null)
            pathPreview.Hide();

        if (gameManager != null && gameManager.gameplayUI != null)
            gameManager.gameplayUI.HideMovePreviewCost();
    }

    // ─── Editor input ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnMouseDown()
    {
        BeginDrag();
    }

    private void OnMouseDrag()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z;
        Drag(mainCamera.ScreenToWorldPoint(mousePos));
    }

    private void OnMouseUp()
    {
        EndDrag();
    }
#endif
}
