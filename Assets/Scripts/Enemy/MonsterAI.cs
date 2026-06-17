using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    public enum MonsterState
    {
        Idle,
        Roaming,
        ChasingPlayer,
        SearchingLastSeenPosition,
        CaughtPlayer
    }

    [Header("References")]
    public MonsterPathFollower pathFollower;
    public MonsterVision vision;
    public Transform player;

    [Header("Roaming")]
    public Transform[] roamPoints;
    public float waitAtRoamPointTime = 1.5f;
    public bool chooseRandomRoamPoint = false;

    [Header("Roam Look Around")]
    public bool lookAroundAtRoamPoint = true;
    public float roamLookAroundTurnSpeed = 45f;
    public float roamLookDirectionChangeInterval = 0.8f;

    [Header("Movement Speeds")]
    public float roamingSpeed = 2f;
    public float chasingSpeed = 3.2f;
    public float searchingSpeed = 2.3f;

    [Header("Chase")]
    public float catchDistance = 1.1f;

    [Header("Search")]
    public float searchDuration = 3f;

    [Header("Search Vision Boost")]
    public bool useSearchVisionBoost = true;
    public float searchViewAngle = 140f;
    public float searchViewDistance = 10f;

    [Header("Search Look Around")]
    public bool lookAroundWhileSearching = true;
    public float searchLookAroundTurnSpeed = 80f;

    [Header("Debug")]
    public MonsterState currentState = MonsterState.Idle;
    public bool printDebugLogs = true;

    private int currentRoamIndex = -1;

    private float waitTimer;
    private float searchTimer;

    private float roamLookDirectionTimer;
    private int roamLookDirection = 1;

    private Vector3 lastKnownPlayerPosition;
    private bool hasLastKnownPlayerPosition;

    private float normalViewAngle;
    private float normalViewDistance;

    private void Awake()
    {
        if (pathFollower == null)
        {
            pathFollower = GetComponent<MonsterPathFollower>();
        }

        if (vision == null)
        {
            vision = GetComponent<MonsterVision>();
        }
    }

    private void Start()
    {
        currentState = MonsterState.Idle;

        if (vision != null && vision.target == null)
        {
            vision.target = player;
        }

        if (vision != null)
        {
            normalViewAngle = vision.viewAngle;
            normalViewDistance = vision.viewDistance;
        }

        ChangeState(MonsterState.Roaming);
    }

    private void Update()
    {
        if (currentState == MonsterState.CaughtPlayer)
        {
            return;
        }

        CheckVisionPriority();

        switch (currentState)
        {
            case MonsterState.Roaming:
                HandleRoaming();
                break;

            case MonsterState.ChasingPlayer:
                HandleChasingPlayer();
                break;

            case MonsterState.SearchingLastSeenPosition:
                HandleSearchingLastSeenPosition();
                break;
        }
    }

    private void CheckVisionPriority()
    {
        if (vision == null)
        {
            return;
        }

        Vector3 seenPosition;

        if (vision.CanSeeTarget(out seenPosition))
        {
            lastKnownPlayerPosition = seenPosition;
            hasLastKnownPlayerPosition = true;

            if (currentState != MonsterState.ChasingPlayer)
            {
                ChangeState(MonsterState.ChasingPlayer);
            }
        }
        else
        {
            if (currentState == MonsterState.ChasingPlayer)
            {
                if (hasLastKnownPlayerPosition)
                {
                    ChangeState(MonsterState.SearchingLastSeenPosition);
                }
                else
                {
                    ChangeState(MonsterState.Roaming);
                }
            }
        }
    }

    private void HandleRoaming()
    {
        if (roamPoints == null || roamPoints.Length == 0)
        {
            return;
        }

        if (pathFollower == null)
        {
            return;
        }

        if (!pathFollower.HasPath && pathFollower.HasReachedDestination)
        {
            LookAroundAtRoamPoint();

            waitTimer += Time.deltaTime;

            if (waitTimer >= waitAtRoamPointTime)
            {
                GoToNextRoamPoint();
            }
        }
    }

    private void HandleChasingPlayer()
    {
        if (player == null || pathFollower == null)
        {
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= catchDistance)
        {
            ChangeState(MonsterState.CaughtPlayer);
        }
    }

    private void HandleSearchingLastSeenPosition()
    {
        if (pathFollower == null)
        {
            return;
        }

        if (!pathFollower.HasReachedDestination)
        {
            return;
        }

        LookAroundWhileSearching();

        searchTimer += Time.deltaTime;

        if (searchTimer >= searchDuration)
        {
            hasLastKnownPlayerPosition = false;
            ChangeState(MonsterState.Roaming);
        }
    }

    private void ChangeState(MonsterState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;

        if (printDebugLogs)
        {
            Debug.Log("Monster state changed to: " + currentState);
        }

        switch (currentState)
        {
            case MonsterState.Roaming:
                EnterRoaming();
                break;

            case MonsterState.ChasingPlayer:
                EnterChasingPlayer();
                break;

            case MonsterState.SearchingLastSeenPosition:
                EnterSearchingLastSeenPosition();
                break;

            case MonsterState.CaughtPlayer:
                EnterCaughtPlayer();
                break;
        }
    }

    private void EnterRoaming()
    {
        ApplyNormalVision();
        SetMonsterSpeed(roamingSpeed);

        waitTimer = 0f;
        searchTimer = 0f;

        GoToNextRoamPoint();
    }

    private void EnterChasingPlayer()
    {
        ApplyNormalVision();
        SetMonsterSpeed(chasingSpeed);

        if (pathFollower == null || player == null)
        {
            return;
        }

        pathFollower.SetTarget(player, true);
    }

    private void EnterSearchingLastSeenPosition()
    {
        ApplySearchVisionBoost();
        SetMonsterSpeed(searchingSpeed);

        searchTimer = 0f;

        if (pathFollower == null)
        {
            return;
        }

        pathFollower.SetDestination(lastKnownPlayerPosition);
    }

    private void EnterCaughtPlayer()
    {
        if (pathFollower != null)
        {
            pathFollower.StopMoving();
        }

        Debug.Log("Player caught!");
    }

    private void GoToNextRoamPoint()
    {
        if (roamPoints == null || roamPoints.Length == 0)
        {
            return;
        }

        waitTimer = 0f;

        roamLookDirectionTimer = 0f;
        roamLookDirection = Random.value > 0.5f ? 1 : -1;

        if (chooseRandomRoamPoint)
        {
            currentRoamIndex = Random.Range(0, roamPoints.Length);
        }
        else
        {
            currentRoamIndex++;

            if (currentRoamIndex >= roamPoints.Length)
            {
                currentRoamIndex = 0;
            }
        }

        Transform roamPoint = roamPoints[currentRoamIndex];

        if (roamPoint != null && pathFollower != null)
        {
            if (printDebugLogs)
            {
                Debug.Log("Going to roam point: " + roamPoint.name);
            }

            pathFollower.SetDestination(roamPoint.position);
        }
    }

    private void SetMonsterSpeed(float newSpeed)
    {
        if (pathFollower == null)
        {
            return;
        }

        pathFollower.SetMoveSpeed(newSpeed);
    }

    private void ApplyNormalVision()
    {
        if (vision == null)
        {
            return;
        }

        vision.viewAngle = normalViewAngle;
        vision.viewDistance = normalViewDistance;
    }

    private void ApplySearchVisionBoost()
    {
        if (vision == null)
        {
            return;
        }

        if (!useSearchVisionBoost)
        {
            return;
        }

        vision.viewAngle = searchViewAngle;
        vision.viewDistance = searchViewDistance;
    }

    private void LookAroundWhileSearching()
    {
        if (!lookAroundWhileSearching)
        {
            return;
        }

        transform.Rotate(
            Vector3.up,
            searchLookAroundTurnSpeed * Time.deltaTime
        );
    }

    private void LookAroundAtRoamPoint()
    {
        if (!lookAroundAtRoamPoint)
        {
            return;
        }

        roamLookDirectionTimer += Time.deltaTime;

        if (roamLookDirectionTimer >= roamLookDirectionChangeInterval)
        {
            roamLookDirectionTimer = 0f;
            roamLookDirection *= -1;
        }

        transform.Rotate(
            Vector3.up,
            roamLookDirection * roamLookAroundTurnSpeed * Time.deltaTime
        );
    }
}