using UnityEngine;

public class CameraFitter : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;

    [Header("Padding")]
    public float padding = 1.5f;

    [Header("Limits")]
    public float minOrthographicSize = 5f;
    public float maxOrthographicSize = 12f;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    public void FitToGrid(int gridWidth, int gridHeight, float cellStep)
    {
        if (targetCamera == null)
            return;

        float gridWorldWidth = gridWidth * cellStep;
        float gridWorldHeight = gridHeight * cellStep;

        float screenAspect = (float)Screen.width / Screen.height;

        float sizeByHeight = gridWorldHeight / 2f + padding;
        float sizeByWidth = (gridWorldWidth / screenAspect) / 2f + padding;

        float targetSize = Mathf.Max(sizeByHeight, sizeByWidth);
        targetSize = Mathf.Clamp(targetSize, minOrthographicSize, maxOrthographicSize);

        targetCamera.orthographicSize = targetSize;
    }
}