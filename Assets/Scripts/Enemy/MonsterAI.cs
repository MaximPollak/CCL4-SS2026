using UnityEngine;

[RequireComponent(typeof(MonsterPathFollower))]
[RequireComponent(typeof(MonsterVision))]
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
    public float roamLookAroundAngle = 45f;
    public float roamLookAroundTurnSpeed = 45f;
    public float roamLookDirectionPauseTime = 0.35f;

    [Header("Movement Speeds")]
    public float roamingSpeed = 2f;
    public float chasingSpeed = 3.2f;
    public float searchingSpeed = 2.3f;

    [Header("Chase")]
    public float catchDistance = 1.1f;
    public float loseSightDelay = 0.6f;
    
    [Header("Wwise Death Audio")]
    [SerializeField] private string deathEvent = "Play_death";
    [SerializeField] private float deathSoundDelay = 3f;

    [Header("Search")]
    public float searchDuration = 3f;

    [Header("Search Vision Boost")]
    public bool useSearchVisionBoost = true;
    public float searchViewAngle = 140f;
    public float searchViewDistance = 10f;

    [Header("Search Look Around")]
    public bool lookAroundWhileSearching = true;
    public float searchLookAroundAngle = 75f;
    public float searchLookAroundTurnSpeed = 80f;
    public float searchLookDirectionPauseTime = 0.4f;

    [Header("Debug")]
    public MonsterState currentState = MonsterState.Idle;
    public bool printDebugLogs = true;

    private int currentRoamIndex = -1;

    private float waitTimer;
    private float searchTimer;

    private bool isWaitingAtRoamPoint;
    private int roamLookPhase;
    private float roamLookPauseTimer;
    private Quaternion roamLookStartRotation;
    private bool isRoamLookPausing;

    private Vector3 lastKnownPlayerPosition;
    private bool hasLastKnownPlayerPosition;
    private float lastTimePlayerSeen = Mathf.NegativeInfinity;

    private int searchLookPhase;
    private float searchLookPauseTimer;
    private Quaternion searchLookStartRotation;
    private bool isSearchLookPausing;
    private bool hasStartedSearchLook;
    private bool hasCompletedSearchLookCycle;
    private bool hasStartedSearchAtCurrentPosition;

    private float normalViewAngle;
    private float normalViewDistance;
    private PlayerHideState playerHideState;

    private void Reset()
    {
        AssignReferences(true);
    }

    private void OnValidate()
    {
        AssignReferences(false);

        waitAtRoamPointTime = Mathf.Max(0f, waitAtRoamPointTime);
        roamLookAroundAngle = Mathf.Clamp(roamLookAroundAngle, 0f, 180f);
        roamLookAroundTurnSpeed = Mathf.Max(0f, roamLookAroundTurnSpeed);
        roamLookDirectionPauseTime = Mathf.Max(0f, roamLookDirectionPauseTime);
        roamingSpeed = Mathf.Max(0f, roamingSpeed);
        chasingSpeed = Mathf.Max(0f, chasingSpeed);
        searchingSpeed = Mathf.Max(0f, searchingSpeed);
        catchDistance = Mathf.Max(0f, catchDistance);
        loseSightDelay = Mathf.Max(0f, loseSightDelay);
        searchDuration = Mathf.Max(0f, searchDuration);
        searchViewAngle = Mathf.Clamp(searchViewAngle, 0f, 360f);
        searchViewDistance = Mathf.Max(0f, searchViewDistance);
        searchLookAroundAngle = Mathf.Clamp(searchLookAroundAngle, 0f, 180f);
        searchLookAroundTurnSpeed = Mathf.Max(0f, searchLookAroundTurnSpeed);
        searchLookDirectionPauseTime = Mathf.Max(0f, searchLookDirectionPauseTime);
    }

    private void Awake()
    {
        AssignReferences(true);
    }

    private void AssignReferences(bool includeSceneSearch)
    {
        if (pathFollower == null)
        {
            pathFollower = GetComponent<MonsterPathFollower>();
        }

        if (vision == null)
        {
            vision = GetComponent<MonsterVision>();
        }

        if (player == null && includeSceneSearch)
        {
            PlayerInteraction playerInteraction = FindFirstObjectByType<PlayerInteraction>();

            if (playerInteraction != null)
            {
                player = playerInteraction.transform;
            }
        }

        if (player != null && (playerHideState == null || playerHideState.transform != player))
        {
            playerHideState = player.GetComponent<PlayerHideState>();
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
            lastTimePlayerSeen = Time.time;

            if (currentState != MonsterState.ChasingPlayer)
            {
                ChangeState(MonsterState.ChasingPlayer);
            }
        }
        else
        {
            if (currentState == MonsterState.ChasingPlayer)
            {
                if (Time.time - lastTimePlayerSeen < loseSightDelay)
                {
                    return;
                }

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
            if (!isWaitingAtRoamPoint)
            {
                StartRoamPointWait();
            }

            bool finishedLooking = LookAroundAtRoamPoint();

            waitTimer += Time.deltaTime;

            if (waitTimer >= waitAtRoamPointTime && finishedLooking)
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

        if (playerHideState != null && playerHideState.IsHidden)
        {
            return;
        }

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

        if (pathFollower.LastPathRequestFailed || pathFollower.IsStuck)
        {
            StartSearchAtCurrentPosition();
        }

        if (!pathFollower.HasReachedDestination)
        {
            return;
        }

        LookAroundWhileSearching();

        searchTimer += Time.deltaTime;

        bool canFinishSearch =
            !lookAroundWhileSearching
            || searchLookAroundAngle <= 0f
            || searchLookAroundTurnSpeed <= 0f
            || hasCompletedSearchLookCycle;

        if (searchTimer >= searchDuration && canFinishSearch)
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
        isWaitingAtRoamPoint = false;

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
        ResetSearchLook();
        hasStartedSearchAtCurrentPosition = false;

        if (pathFollower == null)
        {
            return;
        }

        pathFollower.SetDestination(lastKnownPlayerPosition);

        if (pathFollower.LastPathRequestFailed)
        {
            StartSearchAtCurrentPosition();
        }
    }

    private void EnterCaughtPlayer()
    {
        if (pathFollower != null)
        {
            pathFollower.StopMoving();
        }

        Debug.Log("Player caught!");
        StartCoroutine(PlayDeathSoundAfterDelay());
    }

    private System.Collections.IEnumerator PlayDeathSoundAfterDelay()
    {
        yield return new WaitForSeconds(deathSoundDelay);

        AkUnitySoundEngine.PostEvent(deathEvent, gameObject);
    }

    private void GoToNextRoamPoint()
    {
        if (roamPoints == null || roamPoints.Length == 0)
        {
            return;
        }

        waitTimer = 0f;
        isWaitingAtRoamPoint = false;

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

        if (searchLookAroundAngle <= 0f || searchLookAroundTurnSpeed <= 0f)
        {
            return;
        }

        if (!hasStartedSearchLook)
        {
            hasStartedSearchLook = true;
            searchLookStartRotation = transform.rotation;
            searchLookPhase = 0;
            searchLookPauseTimer = 0f;
            isSearchLookPausing = false;
        }

        bool completedLookCycle = RunLookPattern(
            ref searchLookPhase,
            ref searchLookPauseTimer,
            ref isSearchLookPausing,
            searchLookStartRotation,
            searchLookAroundAngle,
            searchLookAroundTurnSpeed,
            searchLookDirectionPauseTime,
            true
        );

        if (completedLookCycle)
        {
            hasCompletedSearchLookCycle = true;
        }
    }

    private void StartRoamPointWait()
    {
        isWaitingAtRoamPoint = true;
        waitTimer = 0f;
        roamLookPhase = 0;
        roamLookPauseTimer = 0f;
        isRoamLookPausing = false;
        roamLookStartRotation = transform.rotation;
    }

    private bool LookAroundAtRoamPoint()
    {
        if (!lookAroundAtRoamPoint)
        {
            return true;
        }

        if (roamLookAroundAngle <= 0f || roamLookAroundTurnSpeed <= 0f)
        {
            return true;
        }

        return RunLookPattern(
            ref roamLookPhase,
            ref roamLookPauseTimer,
            ref isRoamLookPausing,
            roamLookStartRotation,
            roamLookAroundAngle,
            roamLookAroundTurnSpeed,
            roamLookDirectionPauseTime,
            false
        );
    }

    private bool RunLookPattern(
        ref int phase,
        ref float pauseTimer,
        ref bool isPausing,
        Quaternion startRotation,
        float lookAngle,
        float turnSpeed,
        float directionPauseTime,
        bool loop
    )
    {
        if (phase >= 3)
        {
            if (!loop)
            {
                return true;
            }

            phase = 0;
            pauseTimer = 0f;
            isPausing = false;
        }

        float targetYaw = GetLookPatternYaw(phase, lookAngle);
        Quaternion targetRotation = startRotation * Quaternion.Euler(0f, targetYaw, 0f);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );

        if (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
        {
            return false;
        }

        if (!isPausing)
        {
            isPausing = true;
            pauseTimer = 0f;
        }

        pauseTimer += Time.deltaTime;

        if (pauseTimer < directionPauseTime)
        {
            return false;
        }

        phase++;
        pauseTimer = 0f;
        isPausing = false;

        return phase >= 3;
    }

    private float GetLookPatternYaw(int phase, float lookAngle)
    {
        if (phase == 0)
        {
            return -lookAngle;
        }

        if (phase == 1)
        {
            return lookAngle;
        }

        return 0f;
    }

    private void ResetSearchLook()
    {
        searchLookPhase = 0;
        searchLookPauseTimer = 0f;
        isSearchLookPausing = false;
        hasStartedSearchLook = false;
        hasCompletedSearchLookCycle = false;
    }

    private void StartSearchAtCurrentPosition()
    {
        if (hasStartedSearchAtCurrentPosition)
        {
            return;
        }

        hasStartedSearchAtCurrentPosition = true;

        if (pathFollower != null)
        {
            pathFollower.StopMoving();
        }

        if (printDebugLogs)
        {
            Debug.Log("Monster could not reach search point, searching current position.");
        }
    }
}
