using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    private GridManager gridManager;

    private void Awake()
    {
        gridManager = FindFirstObjectByType<GridManager>();
    }

    public List<GridCell> FindPath(GridCell startCell, GridCell targetCell)
    {
        Queue<GridCell> frontier = new Queue<GridCell>();
        Dictionary<GridCell, GridCell> cameFrom = new Dictionary<GridCell, GridCell>();

        frontier.Enqueue(startCell);
        cameFrom[startCell] = null;

        while (frontier.Count > 0)
        {
            GridCell currentCell = frontier.Dequeue();

            if (currentCell == targetCell)
                break;

            foreach (GridCell neighbor in gridManager.GetNeighbors(currentCell))
            {
                if (neighbor.IsBlocked)
                    continue;

                if (!neighbor.IsEmpty && neighbor != targetCell)
                    continue;

                if (cameFrom.ContainsKey(neighbor))
                    continue;

                frontier.Enqueue(neighbor);
                cameFrom[neighbor] = currentCell;
            }
        }

        if (!cameFrom.ContainsKey(targetCell))
            return null;

        return ReconstructPath(startCell, targetCell, cameFrom);
    }

    private List<GridCell> ReconstructPath(
        GridCell startCell,
        GridCell targetCell,
        Dictionary<GridCell, GridCell> cameFrom)
    {
        List<GridCell> path = new List<GridCell>();

        GridCell currentCell = targetCell;

        while (currentCell != startCell)
        {
            path.Add(currentCell);
            currentCell = cameFrom[currentCell];
        }

        path.Reverse();

        return path;
    }
}