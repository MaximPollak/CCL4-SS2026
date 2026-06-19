using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MonsterPathFollower : MonoBehaviour
{
    [Header("References")]
    public AStarPathfinder pathfinder;
    public Transform target;

    [Header("Movement")]
    [SerializeField] private float currentMoveSpeed = 2f;
    public float turnSpeed = 8f;
    public float waypointReachDistance = 0.2f;

    [Header("Repathing")]
    public bool followTargetContinuously = false;
    public float repathInterval = 0.5f;
    public float targetMoveThreshold = 0.5f;

    [Header("Gravity")]
    public float gravity = -9.81f;

    public bool HasReachedDestination { get; private set; }
    public bool HasPath { get; private set; }

    private CharacterController characterController;

    private List<Vector3> pathPoints = new List<Vector3>();
    private int currentPathIndex;

    private Vector3 currentDestination;
    private float nextRepathTime;
    private float verticalVelocity;

    private void Reset()
    {
        AssignReferences(true);
        ApplyCharacterControllerDefaults();
    }

    private void OnValidate()
    {
        AssignReferences(false);

        currentMoveSpeed = Mathf.Max(0f, currentMoveSpeed);
        turnSpeed = Mathf.Max(0f, turnSpeed);
        waypointReachDistance = Mathf.Max(0.05f, waypointReachDistance);
        repathInterval = Mathf.Max(0.05f, repathInterval);
        targetMoveThreshold = Mathf.Max(0.05f, targetMoveThreshold);
    }

    private void Awake()
    {
        AssignReferences(true);
    }

    private void AssignReferences(bool includeSceneSearch)
    {
        characterController = GetComponent<CharacterController>();

        if (pathfinder == null && includeSceneSearch)
        {
            pathfinder = FindFirstObjectByType<AStarPathfinder>();
        }
    }

    private void ApplyCharacterControllerDefaults()
    {
        if (characterController == null)
        {
            return;
        }

        characterController.radius = 0.3f;
        characterController.height = 1.8f;
        characterController.center = new Vector3(0f, 0.9f, 0f);
    }

    private void Update()
    {
        HandleRepathing();
        FollowPath();
    }

    private void HandleRepathing()
    {
        if (target == null)
        {
            return;
        }

        if (!followTargetContinuously)
        {
            return;
        }

        if (Time.time < nextRepathTime)
        {
            return;
        }

        float targetDistanceFromCurrentDestination =
            Vector3.Distance(target.position, currentDestination);

        if (targetDistanceFromCurrentDestination >= targetMoveThreshold)
        {
            RequestPath(target.position);
        }
        else
        {
            nextRepathTime = Time.time + repathInterval;
        }
    }

    public void SetDestination(Vector3 worldDestination)
    {
        target = null;
        followTargetContinuously = false;
        RequestPath(worldDestination);
    }

    public void SetTarget(Transform newTarget, bool continuouslyFollow)
    {
        target = newTarget;
        followTargetContinuously = continuouslyFollow;

        if (target != null)
        {
            RequestPath(target.position);
        }
    }

    public void StopMoving()
    {
        target = null;
        followTargetContinuously = false;

        pathPoints.Clear();
        currentPathIndex = 0;

        HasPath = false;
        HasReachedDestination = true;
    }

    private void RequestPath(Vector3 worldDestination)
    {
        if (pathfinder == null)
        {
            Debug.LogWarning("MonsterPathFollower: No AStarPathfinder assigned.");
            return;
        }

        currentDestination = worldDestination;
        nextRepathTime = Time.time + repathInterval;

        List<GridNode> nodePath = pathfinder.FindPath(transform.position, worldDestination);

        pathPoints.Clear();

        foreach (GridNode node in nodePath)
        {
            pathPoints.Add(node.worldPosition);
        }

        currentPathIndex = 0;
        HasPath = pathPoints.Count > 0;

        float distanceToDestination = Vector3.Distance(transform.position, worldDestination);
        HasReachedDestination = !HasPath && distanceToDestination <= waypointReachDistance * 2f;
    }

    private void FollowPath()
    {
        ApplyGravity();

        if (!HasPath)
        {
            Move(Vector3.zero);
            return;
        }

        if (currentPathIndex >= pathPoints.Count)
        {
            HasPath = false;
            HasReachedDestination = true;
            Move(Vector3.zero);
            return;
        }

        Vector3 nextPoint = pathPoints[currentPathIndex];

        Vector3 flatTargetPosition = new Vector3(
            nextPoint.x,
            transform.position.y,
            nextPoint.z
        );

        Vector3 directionToTarget = flatTargetPosition - transform.position;
        directionToTarget.y = 0f;

        if (directionToTarget.magnitude <= waypointReachDistance)
        {
            currentPathIndex++;
            return;
        }

        Vector3 horizontalMovement = directionToTarget.normalized * currentMoveSpeed;

        RotateTowards(horizontalMovement);
        Move(horizontalMovement);
    }

    public void SetMoveSpeed(float newSpeed)
    {
        currentMoveSpeed = newSpeed;
    }

    private void RotateTowards(Vector3 movementDirection)
    {
        if (movementDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(movementDirection);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1f;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void Move(Vector3 horizontalMovement)
    {
        Vector3 finalMovement =
            horizontalMovement
            + Vector3.up * verticalVelocity;

        characterController.Move(finalMovement * Time.deltaTime);
    }
}
