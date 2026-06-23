using UnityEngine;

public class MenuObjectPassBy : MonoBehaviour
{
    private const float MinimumUsefulDistance = 0.01f;

    [Header("Waypoints")]
    [SerializeField] private Transform[] waypoints;

    [Header("Movement")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float arriveDistance = 0.1f;
    [SerializeField] private bool pingPong = false;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Rotation")]
    [SerializeField] private bool faceMovementDirection = true;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("Debug")]
    [SerializeField] private bool printDebugLogs = true;
    [SerializeField] private bool drawPathGizmos = true;

    private Vector3[] pathPositions;
    private int currentWaypointIndex;
    private int direction = 1;

    private void Start()
    {
        pathPositions = BuildPathPositions();

        if (!HasMovementPath())
        {
            Debug.LogError("MenuObjectPassBy needs at least two assigned waypoints on " + gameObject.name);
            return;
        }

        currentWaypointIndex = 0;
        transform.position = pathPositions[currentWaypointIndex];
        AdvanceTarget();

        if (printDebugLogs)
        {
            Debug.Log($"{name} menu path started with {pathPositions.Length} points.", this);
        }
    }

    private void Update()
    {
        if (!HasMovementPath())
        {
            return;
        }

        Vector3 targetPosition = pathPositions[currentWaypointIndex];
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        Vector3 moveDirection = targetPosition - transform.position;

        RotateTowardsMovement(moveDirection, deltaTime);

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) <= arriveDistance)
        {
            AdvanceTarget();
        }
    }

    private bool HasMovementPath()
    {
        return pathPositions != null && pathPositions.Length >= 2;
    }

    private Vector3[] BuildPathPositions()
    {
        if (waypoints == null || waypoints.Length < 2)
        {
            return null;
        }

        Vector3[] positions = new Vector3[waypoints.Length];

        for (int i = 0; i < waypoints.Length; i++)
        {
            Transform waypoint = waypoints[i];

            if (waypoint == null)
            {
                Debug.LogError($"{name} has an empty waypoint slot at index {i}.", this);
                return null;
            }

            WarnIfWaypointIsChild(waypoint);
            positions[i] = waypoint.position;
        }

        WarnIfPathIsTooShort(positions);
        return positions;
    }

    private void AdvanceTarget()
    {
        int waypointCount = pathPositions.Length;

        if (!pingPong)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypointCount;
            return;
        }

        if (currentWaypointIndex == waypointCount - 1)
        {
            direction = -1;
        }
        else if (currentWaypointIndex == 0)
        {
            direction = 1;
        }

        currentWaypointIndex += direction;
    }

    private void RotateTowardsMovement(Vector3 moveDirection, float deltaTime)
    {
        if (!faceMovementDirection || moveDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * deltaTime
        );
    }

    private void WarnIfWaypointIsChild(Transform waypoint)
    {
        if (!printDebugLogs || waypoint == null || !waypoint.IsChildOf(transform))
        {
            return;
        }

        Debug.LogWarning(
            $"{waypoint.name} is a child of {name}. Put menu path points outside the moving submarine object.",
            this
        );
    }

    private void WarnIfPathIsTooShort(Vector3[] positions)
    {
        if (!printDebugLogs || positions == null || positions.Length < 2)
        {
            return;
        }

        for (int i = 1; i < positions.Length; i++)
        {
            if (Vector3.Distance(positions[i - 1], positions[i]) > MinimumUsefulDistance)
            {
                return;
            }
        }

        Debug.LogWarning($"{name} menu path points are almost in the same position.", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawPathGizmos)
        {
            return;
        }

        if (waypoints == null || waypoints.Length < 2)
        {
            return;
        }

        Gizmos.color = Color.cyan;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(waypoints[i].position, 0.25f);

            int nextIndex = i + 1;

            if (nextIndex < waypoints.Length && waypoints[nextIndex] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[nextIndex].position);
            }
            else if (!pingPong && waypoints.Length > 2 && waypoints[0] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[0].position);
            }
        }
    }
}
