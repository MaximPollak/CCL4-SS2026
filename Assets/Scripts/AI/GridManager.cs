using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector2 gridWorldSize = new Vector2(20f, 20f);
    [Min(0.05f)]
    public float nodeRadius = 0.5f;

    [Header("Obstacle Detection")]
    public LayerMask obstacleMask;
    [Min(0f)]
    public float obstacleCheckHeight = 0.5f;
    [Range(0.1f, 1f)]
    public float obstacleCheckRadiusMultiplier = 0.6f;
    public bool includeTriggerObstacles = true;

    [Header("Movement Rules")]
    public bool allowDiagonals = true;
    public bool preventDiagonalCornerCutting = true;

    [Header("Debug")]
    public bool drawGrid = true;
    public bool drawOnlyWhenSelected = false;

    private GridNode[,] grid;

    private float nodeDiameter;
    private int gridSizeX;
    private int gridSizeY;

    private List<GridNode> currentPath;

    private void Reset()
    {
        ApplyDefaultSettings();
    }

    private void OnValidate()
    {
        ApplyDefaultSettings();
    }

    private void Awake()
    {
        ApplyDefaultSettings();
        CreateGrid();
    }

    private void ApplyDefaultSettings()
    {
        if (gridWorldSize.x <= 0f || gridWorldSize.y <= 0f)
        {
            gridWorldSize = new Vector2(20f, 20f);
        }

        nodeRadius = Mathf.Max(0.05f, nodeRadius);
        obstacleCheckHeight = Mathf.Max(0f, obstacleCheckHeight);
        obstacleCheckRadiusMultiplier = Mathf.Clamp(obstacleCheckRadiusMultiplier, 0.1f, 1f);

        if (obstacleMask.value == 0)
        {
            int wallMask = LayerMask.GetMask("Wall");

            if (wallMask != 0)
            {
                obstacleMask = wallMask;
            }
        }
    }

    public void CreateGrid()
    {
        nodeDiameter = nodeRadius * 2f;

        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        grid = new GridNode[gridSizeX, gridSizeY];

        Vector3 worldBottomLeft =
            transform.position
            - Vector3.right * gridWorldSize.x / 2f
            - Vector3.forward * gridWorldSize.y / 2f;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint =
                    worldBottomLeft
                    + Vector3.right * (x * nodeDiameter + nodeRadius)
                    + Vector3.forward * (y * nodeDiameter + nodeRadius);

                bool blocked = Physics.CheckSphere(
                    GetObstacleCheckPosition(worldPoint),
                    GetObstacleCheckRadius(),
                    obstacleMask,
                    GetObstacleTriggerInteraction()
                );

                bool walkable = !blocked;

                grid[x, y] = new GridNode(walkable, worldPoint, x, y);
            }
        }
    }

    public GridNode GetNodeFromWorldPosition(Vector3 worldPosition)
    {
        float percentX =
            (worldPosition.x + gridWorldSize.x / 2f - transform.position.x)
            / gridWorldSize.x;

        float percentY =
            (worldPosition.z + gridWorldSize.y / 2f - transform.position.z)
            / gridWorldSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[x, y];
    }

    public GridNode FindClosestWalkableNode(Vector3 worldPosition, int searchRadius = 5)
    {
        GridNode startNode = GetNodeFromWorldPosition(worldPosition);

        if (startNode.walkable)
        {
            return startNode;
        }

        GridNode closestNode = null;
        float closestDistance = Mathf.Infinity;

        for (int xOffset = -searchRadius; xOffset <= searchRadius; xOffset++)
        {
            for (int yOffset = -searchRadius; yOffset <= searchRadius; yOffset++)
            {
                int checkX = startNode.gridX + xOffset;
                int checkY = startNode.gridY + yOffset;

                if (checkX < 0 || checkX >= gridSizeX || checkY < 0 || checkY >= gridSizeY)
                {
                    continue;
                }

                GridNode node = grid[checkX, checkY];

                if (!node.walkable)
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(node.worldPosition - worldPosition);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNode = node;
                }
            }
        }

        return closestNode;
    }

    public List<GridNode> GetNeighbours(GridNode node)
    {
        List<GridNode> neighbours = new List<GridNode>();

        for (int xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                if (xOffset == 0 && yOffset == 0)
                {
                    continue;
                }

                bool isDiagonal = xOffset != 0 && yOffset != 0;

                if (isDiagonal && !allowDiagonals)
                {
                    continue;
                }

                int checkX = node.gridX + xOffset;
                int checkY = node.gridY + yOffset;

                if (checkX < 0 || checkX >= gridSizeX || checkY < 0 || checkY >= gridSizeY)
                {
                    continue;
                }

                if (isDiagonal && preventDiagonalCornerCutting)
                {
                    GridNode horizontalNeighbour = grid[node.gridX + xOffset, node.gridY];
                    GridNode verticalNeighbour = grid[node.gridX, node.gridY + yOffset];

                    if (!horizontalNeighbour.walkable || !verticalNeighbour.walkable)
                    {
                        continue;
                    }
                }

                GridNode neighbour = grid[checkX, checkY];

                if (IsMovementBetweenNodesBlocked(node, neighbour))
                {
                    continue;
                }

                neighbours.Add(neighbour);
            }
        }

        return neighbours;
    }

    private bool IsMovementBetweenNodesBlocked(GridNode fromNode, GridNode toNode)
    {
        Vector3 start = GetObstacleCheckPosition(fromNode.worldPosition);
        Vector3 end = GetObstacleCheckPosition(toNode.worldPosition);
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return false;
        }

        return Physics.SphereCast(
            start,
            GetObstacleCheckRadius(),
            direction.normalized,
            out _,
            distance,
            obstacleMask,
            GetObstacleTriggerInteraction()
        );
    }

    private QueryTriggerInteraction GetObstacleTriggerInteraction()
    {
        return includeTriggerObstacles
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;
    }

    private Vector3 GetObstacleCheckPosition(Vector3 worldPosition)
    {
        return worldPosition + Vector3.up * obstacleCheckHeight;
    }

    private float GetObstacleCheckRadius()
    {
        return nodeRadius * obstacleCheckRadiusMultiplier;
    }

    public void ResetNodeCosts()
    {
        foreach (GridNode node in grid)
        {
            node.gCost = int.MaxValue;
            node.hCost = 0;
            node.parentNode = null;
        }
    }

    public void SetCurrentPath(List<GridNode> path)
    {
        currentPath = path;
    }

    private void OnDrawGizmos()
    {
        if (!drawGrid || drawOnlyWhenSelected)
        {
            return;
        }

        DrawGridGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGrid || !drawOnlyWhenSelected)
        {
            return;
        }

        DrawGridGizmos();
    }

    private void DrawGridGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(gridWorldSize.x, 0.1f, gridWorldSize.y)
        );

        if (grid == null)
        {
            return;
        }

        HashSet<GridNode> pathNodes = null;

        if (currentPath != null)
        {
            pathNodes = new HashSet<GridNode>(currentPath);
        }

        foreach (GridNode node in grid)
        {
            if (pathNodes != null && pathNodes.Contains(node))
            {
                Gizmos.color = Color.cyan;
            }
            else if (node.walkable)
            {
                Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
            }
            else
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.7f);
            }

            Gizmos.DrawCube(
                node.worldPosition,
                new Vector3(nodeDiameter - 0.05f, 0.05f, nodeDiameter - 0.05f)
            );
        }
    }
}
