using UnityEngine;

public class HidingSpot : MonoBehaviour, IInteractable
{
    [Header("Player Points")]
    [SerializeField] private Transform hidePoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform viewPoint;

    [Header("Debug")]
    [SerializeField] private bool printDebugLogs = true;

    public bool IsOccupied => occupant != null;

    private PlayerHideState occupant;

    private void Reset()
    {
        AssignDefaultPoints();
    }

    private void OnValidate()
    {
        AssignDefaultPoints();
    }

    public void Interact(PlayerInteraction player)
    {
        if (player == null)
        {
            return;
        }

        PlayerHideState hideState = player.HideState;

        if (hideState == null)
        {
            hideState = player.GetComponent<PlayerHideState>();
        }

        if (hideState == null)
        {
            Debug.LogWarning("Player needs a PlayerHideState component to use hiding spots.");
            return;
        }

        if (hideState.IsHidden)
        {
            if (hideState.CurrentHidingSpot == this)
            {
                hideState.ExitHidingSpot(GetExitPoint(), GetViewPoint());
            }

            return;
        }

        if (IsOccupied)
        {
            return;
        }

        occupant = hideState;
        hideState.EnterHidingSpot(this, GetHidePoint(), GetViewPoint());

        if (printDebugLogs)
        {
            Debug.Log("Player entered hiding spot: " + name);
        }
    }

    public void ClearOccupant(PlayerHideState hideState)
    {
        if (occupant != hideState)
        {
            return;
        }

        occupant = null;

        if (printDebugLogs)
        {
            Debug.Log("Player exited hiding spot: " + name);
        }
    }

    private Transform GetHidePoint()
    {
        if (hidePoint != null)
        {
            return hidePoint;
        }

        return transform;
    }

    private Transform GetExitPoint()
    {
        if (exitPoint != null)
        {
            return exitPoint;
        }

        return transform;
    }

    private Transform GetViewPoint()
    {
        if (viewPoint != null)
        {
            return viewPoint;
        }

        return GetHidePoint();
    }

    private void AssignDefaultPoints()
    {
        if (hidePoint == null)
        {
            hidePoint = transform.Find("HidePoint");
        }

        if (exitPoint == null)
        {
            exitPoint = transform.Find("ExitPoint");
        }

        if (viewPoint == null)
        {
            viewPoint = transform.Find("ViewPoint");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform usedHidePoint = GetHidePoint();
        Transform usedExitPoint = GetExitPoint();
        Transform usedViewPoint = GetViewPoint();

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(usedHidePoint.position, 0.25f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(usedExitPoint.position, 0.25f);
        Gizmos.DrawLine(usedHidePoint.position, usedExitPoint.position);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            usedHidePoint.position,
            usedHidePoint.position + usedViewPoint.forward * 0.75f
        );
    }
}
