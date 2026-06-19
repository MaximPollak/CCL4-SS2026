using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridManager))]
public class AStarPathfinder : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;

    [Header("Debug")]
    public bool showPathOnGrid = true;

    private void Reset()
    {
        AssignReferences(true);
    }

    private void OnValidate()
    {
        AssignReferences(false);
    }

    private void Awake()
    {
        AssignReferences(true);
    }

    private void AssignReferences(bool includeSceneSearch)
    {
        if (gridManager == null)
        {
            gridManager = GetComponent<GridManager>();
        }

        if (gridManager == null && includeSceneSearch)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }
    }

    public List<GridNode> FindPath(Vector3 startWorldPosition, Vector3 targetWorldPosition)
    {
        List<GridNode> emptyPath = new List<GridNode>();

        if (gridManager == null)
        {
            Debug.LogWarning("AStarPathfinder: No GridManager assigned.");
            return emptyPath;
        }

        GridNode startNode = gridManager.FindClosestWalkableNode(startWorldPosition);
        GridNode targetNode = gridManager.FindClosestWalkableNode(targetWorldPosition);

        if (startNode == null || targetNode == null)
        {
            Debug.LogWarning("AStarPathfinder: Could not find valid start or target node.");
            return emptyPath;
        }

        gridManager.ResetNodeCosts();

        List<GridNode> openSet = new List<GridNode>();
        HashSet<GridNode> closedSet = new HashSet<GridNode>();

        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);
        startNode.parentNode = null;

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            GridNode currentNode = openSet[0];

            for (int i = 1; i < openSet.Count; i++)
            {
                bool hasLowerFCost = openSet[i].fCost < currentNode.fCost;
                bool hasSameFCostButLowerHCost =
                    openSet[i].fCost == currentNode.fCost &&
                    openSet[i].hCost < currentNode.hCost;

                if (hasLowerFCost || hasSameFCostButLowerHCost)
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                List<GridNode> finalPath = RetracePath(startNode, targetNode);

                if (showPathOnGrid)
                {
                    gridManager.SetCurrentPath(finalPath);
                }

                return finalPath;
            }

            foreach (GridNode neighbour in gridManager.GetNeighbours(currentNode))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                {
                    continue;
                }

                int newMovementCostToNeighbour =
                    currentNode.gCost + GetDistance(currentNode, neighbour);

                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parentNode = currentNode;

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                }
            }
        }

        Debug.LogWarning("AStarPathfinder: No path found.");

        if (showPathOnGrid)
        {
            gridManager.SetCurrentPath(emptyPath);
        }

        return emptyPath;
    }

    private List<GridNode> RetracePath(GridNode startNode, GridNode endNode)
    {
        List<GridNode> path = new List<GridNode>();

        GridNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parentNode;

            if (currentNode == null)
            {
                Debug.LogWarning("AStarPathfinder: Path retrace failed.");
                return new List<GridNode>();
            }
        }

        path.Reverse();

        return path;
    }

    private int GetDistance(GridNode nodeA, GridNode nodeB)
    {
        int distanceX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distanceY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (distanceX > distanceY)
        {
            return 14 * distanceY + 10 * (distanceX - distanceY);
        }

        return 14 * distanceX + 10 * (distanceY - distanceX);
    }
}
