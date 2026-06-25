using UnityEngine;

[DisallowMultipleComponent]
public class MonsterAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MonsterAI monsterAI;
    [SerializeField] private MonsterPathFollower pathFollower;
    [SerializeField] private Animator animator;

    [Header("Animator Parameters")]
    [SerializeField] private string isMovingParameter = "IsMoving";
    [SerializeField] private string isSearchingParameter = "IsSearching";
    [SerializeField] private string isLookingAroundParameter = "IsLookingAround";
    [SerializeField] private string isChasingParameter = "IsChasing";
    [SerializeField] private string speedParameter = "Speed";

    [Header("Movement Detection")]
    [SerializeField] private float movingSpeedThreshold = 0.05f;
    [SerializeField] private bool printDebugWarnings = true;

    [SerializeField] private float stopMovingDelay = 0.15f;
private float lastMovingTime;

    private Vector3 lastPosition;
    private bool hasLastPosition;
    private RuntimeAnimatorController cachedController;

    private int isMovingHash;
    private int isSearchingHash;
    private int isLookingAroundHash;
    private int isChasingHash;
    private int speedHash;

    private bool hasIsMovingParameter;
    private bool hasIsSearchingParameter;
    private bool hasIsLookingAroundParameter;
    private bool hasIsChasingParameter;
    private bool hasSpeedParameter;

    private void Reset()
    {
        AssignReferences();
    }

    private void Awake()
    {
        AssignReferences();
        CacheAnimatorParameters();
    }

    private void OnEnable()
    {
        AssignReferences();
        Transform movementTransform = monsterAI != null ? monsterAI.transform : transform;
        lastPosition = movementTransform.position;
        hasLastPosition = true;
    }

    private void LateUpdate()
    {
        if (animator == null || monsterAI == null)
        {
            AssignReferences();
        }

        if (animator == null || monsterAI == null)
        {
            return;
        }

        if (animator.runtimeAnimatorController != cachedController)
        {
            CacheAnimatorParameters();
        }

        float horizontalSpeed = GetHorizontalSpeed();
        bool isCaught = monsterAI.currentState == MonsterAI.MonsterState.CaughtPlayer;
        bool isChasing = monsterAI.currentState == MonsterAI.MonsterState.ChasingPlayer;
        bool isSearching = monsterAI.currentState == MonsterAI.MonsterState.SearchingLastSeenPosition;
        bool isActuallyMoving = !isCaught && horizontalSpeed >= movingSpeedThreshold;

if (isActuallyMoving)
{
    lastMovingTime = Time.time;
}

bool isMoving = !isCaught && Time.time - lastMovingTime <= stopMovingDelay;
        bool isLookingAround = isSearching && !isMoving && HasReachedSearchPoint();

        animator.speed = isChasing ? 1.6f : 1f;

        // The Animator must have matching parameters for these calls to affect transitions.
        SetBoolIfAvailable(hasIsMovingParameter, isMovingHash, isMoving);
        SetBoolIfAvailable(hasIsSearchingParameter, isSearchingHash, isSearching);
        SetBoolIfAvailable(hasIsLookingAroundParameter, isLookingAroundHash, isLookingAround);
        SetBoolIfAvailable(hasIsChasingParameter, isChasingHash, isChasing);
        SetFloatIfAvailable(hasSpeedParameter, speedHash, horizontalSpeed);
    }

    private void AssignReferences()
    {
        if (monsterAI == null)
        {
            monsterAI = GetComponent<MonsterAI>();
        }

        if (monsterAI == null)
        {
            monsterAI = GetComponentInParent<MonsterAI>();
        }

        if (pathFollower == null)
        {
            pathFollower = GetComponent<MonsterPathFollower>();
        }

        if (pathFollower == null)
        {
            pathFollower = GetComponentInParent<MonsterPathFollower>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            animator = GetComponentInParent<Animator>();
        }
    }

    private void CacheAnimatorParameters()
    {
        hasIsMovingParameter = false;
        hasIsSearchingParameter = false;
        hasIsLookingAroundParameter = false;
        hasIsChasingParameter = false;
        hasSpeedParameter = false;
        cachedController = animator != null ? animator.runtimeAnimatorController : null;

        if (animator == null)
        {
            return;
        }

        if (animator.runtimeAnimatorController == null && printDebugWarnings)
        {
            Debug.LogWarning(
                "MonsterAnimationController found an Animator, but it has no Animator Controller assigned.",
                this
            );
        }

        isMovingHash = Animator.StringToHash(isMovingParameter);
        isSearchingHash = Animator.StringToHash(isSearchingParameter);
        isLookingAroundHash = Animator.StringToHash(isLookingAroundParameter);
        isChasingHash = Animator.StringToHash(isChasingParameter);
        speedHash = Animator.StringToHash(speedParameter);

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == isMovingParameter && parameter.type == AnimatorControllerParameterType.Bool)
            {
                hasIsMovingParameter = true;
            }
            else if (parameter.name == isSearchingParameter && parameter.type == AnimatorControllerParameterType.Bool)
            {
                hasIsSearchingParameter = true;
            }
            else if (parameter.name == isLookingAroundParameter && parameter.type == AnimatorControllerParameterType.Bool)
            {
                hasIsLookingAroundParameter = true;
            }
            else if (parameter.name == isChasingParameter && parameter.type == AnimatorControllerParameterType.Bool)
            {
                hasIsChasingParameter = true;
            }
            else if (parameter.name == speedParameter && parameter.type == AnimatorControllerParameterType.Float)
            {
                hasSpeedParameter = true;
            }
        }

        if (printDebugWarnings)
        {
            WarnMissingParameter(hasIsMovingParameter, isMovingParameter);
            WarnMissingParameter(hasIsSearchingParameter, isSearchingParameter);
            WarnMissingParameter(hasIsLookingAroundParameter, isLookingAroundParameter);
        }
    }

    private float GetHorizontalSpeed()
    {
        if (!hasLastPosition || Time.deltaTime <= 0f)
        {
            Transform initialTransform = monsterAI != null ? monsterAI.transform : transform;
            lastPosition = initialTransform.position;
            hasLastPosition = true;
            return 0f;
        }

        Transform movementTransform = monsterAI != null ? monsterAI.transform : transform;
        Vector3 movement = movementTransform.position - lastPosition;
        movement.y = 0f;
        lastPosition = movementTransform.position;

        return movement.magnitude / Time.deltaTime;
    }

    private bool HasReachedSearchPoint()
    {
        return pathFollower == null
            || pathFollower.HasReachedDestination
            || !pathFollower.HasPath;
    }

    private void SetBoolIfAvailable(bool hasParameter, int parameterHash, bool value)
    {
        if (hasParameter)
        {
            animator.SetBool(parameterHash, value);
        }
    }

    private void SetFloatIfAvailable(bool hasParameter, int parameterHash, float value)
    {
        if (hasParameter)
        {
            animator.SetFloat(parameterHash, value);
        }
    }

    private void WarnMissingParameter(bool hasParameter, string parameterName)
    {
        if (hasParameter || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        Debug.LogWarning(
            "MonsterAnimationController could not find Animator parameter '" +
            parameterName +
            "'. Add it to the assigned Animator Controller or update the parameter name.",
            this
        );
    }
}
