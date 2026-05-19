using System.Collections.Generic;
using UnityEngine;

public class PathPreview : MonoBehaviour
{
    [Header("References")]
    public LineRenderer lineRenderer;

    [Header("Visual Settings")]
    public float lineZOffset = -0.2f;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        Hide();
    }

    public void ShowPath(List<GridCell> path)
    {
        if (lineRenderer == null)
        {
            Debug.LogError("PathPreview: LineRenderer eksik!");
            return;
        }

        if (path == null || path.Count == 0)
        {
            Hide();
            return;
        }

        lineRenderer.enabled = true;
        lineRenderer.positionCount = path.Count;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 pos = path[i].transform.position;
            pos.z += lineZOffset;
            lineRenderer.SetPosition(i, pos);
        }
    }

    public void Hide()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }
    }
}