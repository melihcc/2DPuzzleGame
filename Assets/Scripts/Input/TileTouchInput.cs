using UnityEngine;
using UnityEngine.InputSystem;

public class TileTouchInput : MonoBehaviour
{
    private Camera mainCamera;

    private TileDragHandler currentDraggedTile;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Touchscreen.current == null)
            return;

        var primaryTouch = Touchscreen.current.primaryTouch;

        if (primaryTouch.press.wasPressedThisFrame)
        {
            HandleTouchStart(primaryTouch.position.ReadValue());
        }

        if (primaryTouch.press.isPressed)
        {
            HandleTouchMove(primaryTouch.position.ReadValue());
        }

        if (primaryTouch.press.wasReleasedThisFrame)
        {
            HandleTouchEnd();
        }
    }

    private void HandleTouchStart(Vector2 screenPosition)
    {
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z)
        );

        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);

        if (hit.collider == null)
            return;

        TileDragHandler dragHandler = hit.collider.GetComponent<TileDragHandler>();

        if (dragHandler == null)
            return;

        currentDraggedTile = dragHandler;
        currentDraggedTile.BeginDrag();
    }

    private void HandleTouchMove(Vector2 screenPosition)
    {
        if (currentDraggedTile == null)
            return;

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z)
        );

        currentDraggedTile.Drag(worldPosition);
    }

    private void HandleTouchEnd()
    {
        if (currentDraggedTile == null)
            return;

        currentDraggedTile.EndDrag();
        currentDraggedTile = null;
    }
}