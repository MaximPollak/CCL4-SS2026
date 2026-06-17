using UnityEngine;

public class GridNode
{
    public bool walkable;
    public Vector3 worldPosition;

    public int gridX;
    public int gridY;

    public int gCost;
    public int hCost;

    public GridNode parentNode;

    public int fCost
    {
        get { return gCost + hCost; }
    }

    public GridNode(bool walkable, Vector3 worldPosition, int gridX, int gridY)
    {
        this.walkable = walkable;
        this.worldPosition = worldPosition;
        this.gridX = gridX;
        this.gridY = gridY;

        gCost = int.MaxValue;
        hCost = 0;
        parentNode = null;
    }
}