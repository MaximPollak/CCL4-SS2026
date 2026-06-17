using UnityEngine;

public class MonsterVision : MonoBehaviour
{
    [Header("References")]
    public Transform target;

    [Header("Vision Settings")]
    public float viewDistance = 8f;
    public float viewAngle = 80f;
    public float eyeHeight = 1.5f;
    public float targetHeight = 1f;

    [Header("Close Awareness")]
    public float closeAwarenessDistance = 1.5f;

    [Header("Line Of Sight")]
    public LayerMask obstacleMask;

    [Header("Debug")]
    public bool drawGizmos = true;

    public bool CanSeeTarget(out Vector3 seenPosition)
    {
        seenPosition = Vector3.zero;

        if (target == null)
        {
            return false;
        }

        Vector3 eyePosition = GetEyePosition();
        Vector3 targetPosition = GetTargetLookPosition();

        Vector3 directionToTarget = targetPosition - eyePosition;
        float distanceToTarget = directionToTarget.magnitude;

        if (distanceToTarget > viewDistance)
        {
            return false;
        }

        bool isVeryClose = distanceToTarget <= closeAwarenessDistance;

        if (!isVeryClose)
        {
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

            if (angleToTarget > viewAngle / 2f)
            {
                return false;
            }
        }

        if (!HasLineOfSight(eyePosition, targetPosition, distanceToTarget))
        {
            return false;
        }

        seenPosition = target.position;
        return true;
    }

    private bool HasLineOfSight(Vector3 from, Vector3 to, float distance)
    {
        Vector3 direction = (to - from).normalized;

        bool blocked = Physics.Raycast(
            from,
            direction,
            distance,
            obstacleMask
        );

        return !blocked;
    }

    private Vector3 GetEyePosition()
    {
        return transform.position + Vector3.up * eyeHeight;
    }

    private Vector3 GetTargetLookPosition()
    {
        return target.position + Vector3.up * targetHeight;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(eyePosition, viewDistance);

        Vector3 leftDirection = Quaternion.Euler(0f, -viewAngle / 2f, 0f) * transform.forward;
        Vector3 rightDirection = Quaternion.Euler(0f, viewAngle / 2f, 0f) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(eyePosition, eyePosition + leftDirection * viewDistance);
        Gizmos.DrawLine(eyePosition, eyePosition + rightDirection * viewDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(eyePosition, closeAwarenessDistance);

        if (target != null)
        {
            Vector3 targetPosition = target.position + Vector3.up * targetHeight;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(eyePosition, targetPosition);
        }
    }
}