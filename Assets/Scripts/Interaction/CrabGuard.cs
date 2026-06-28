using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CrabGuard : MonoBehaviour, IInteractable
{
    [Header("State")]
    [SerializeField] private string crabId = "CrabGuard";
    [SerializeField] private string requiredCoinItemId = "Coin";

    [Header("Guarded Paper")]
    [SerializeField] private GameObject guardedPaperObject;
    [SerializeField] private PickupItem guardedPaperPickup;
    [SerializeField] private Collider[] guardedPaperColliders;
    [SerializeField] private Collider[] crabBlockingCollidersToDisableAfterBribe;
    [SerializeField] private bool disablePaperPickupUntilBribed = true;

    [Header("Coin Result")]
    [SerializeField] private GameObject[] coinObjectsToDisableAfterBribe;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string snapTriggerName = "Snap";
    [SerializeField] private string liftArmsTriggerName = "LiftArms";
    [SerializeField] private string isBribedBoolName = "IsBribed";
    [SerializeField] private bool playAnimationStatesDirectly = true;
    [SerializeField] private string snapStateName = "Armature_001|Snap";
    [SerializeField] private string liftArmsStateName = "Armature_001|LiftArms";
    [SerializeField] private float snapAnimationLockTime = 0.75f;
    [SerializeField] private float liftArmsUnlockDelay = 1.25f;

    [Header("Debug")]
    [SerializeField] private bool printDebugLogs = true;

    private bool isBribed;
    private bool isSnapPlaying;
    private bool isLiftInProgress;

    private int snapTriggerHash;
    private int liftArmsTriggerHash;
    private int isBribedBoolHash;
    private bool hasSnapTriggerParameter;
    private bool hasLiftArmsTriggerParameter;
    private bool hasIsBribedBoolParameter;

    private void Reset()
    {
        AssignReferences();
    }

    private void Awake()
    {
        AssignReferences();
        CacheAnimatorHashes();

        isBribed = GameState.Instance.IsCrabBribed(crabId);
        ApplyBribedState();
    }

    public void Interact(PlayerInteraction player)
    {
        Log("Player interacted with crab.");

        if (isBribed)
        {
            Log("Crab is already bribed. Paper pickup is available.");
            return;
        }

        if (isLiftInProgress)
        {
            Log("Crab lift animation is already in progress.");
            return;
        }

        if (player == null || player.Inventory == null)
        {
            Debug.LogWarning("CrabGuard cannot check coin because player inventory is missing.", this);
            return;
        }

        bool playerHasCoin = player.Inventory.HasItem(requiredCoinItemId);
        Log("Player has coin: " + playerHasCoin);

        if (!playerHasCoin)
        {
            TryPlaySnapAnimation();
            return;
        }

        StartCoroutine(BribeCrabRoutine(player));
    }

    private IEnumerator BribeCrabRoutine(PlayerInteraction player)
    {
        isLiftInProgress = true;

        GameState.Instance.MarkItemConsumed(requiredCoinItemId);
        if (player.Inventory.ConsumeHeldItem(requiredCoinItemId))
        {
            Log("Coin was consumed and deactivated: " + requiredCoinItemId);
        }
        else
        {
            Log("Coin consume requested, but the player was no longer holding: " + requiredCoinItemId);
        }

        DisableBribedCoinObjects();

        if (animator != null)
        {
            if (!string.IsNullOrWhiteSpace(snapTriggerName) && hasSnapTriggerParameter)
            {
                animator.ResetTrigger(snapTriggerHash);
            }

            TrySetAnimatorBool(isBribedBoolHash, hasIsBribedBoolParameter, true);
            TrySetAnimatorTrigger(liftArmsTriggerHash, hasLiftArmsTriggerParameter);
            TryPlayAnimatorState(liftArmsStateName);
            Log("Lift arms animation triggered.");
        }

        yield return new WaitForSeconds(Mathf.Max(0f, liftArmsUnlockDelay));

        isBribed = true;
        GameState.Instance.MarkCrabBribed(crabId);
        ApplyBribedState();

        isLiftInProgress = false;
        Log("Crab is now bribed.");
    }

    private void TryPlaySnapAnimation()
    {
        if (isSnapPlaying || isLiftInProgress)
        {
            return;
        }

        if (animator != null)
        {
            TrySetAnimatorTrigger(snapTriggerHash, hasSnapTriggerParameter);
            TryPlayAnimatorState(snapStateName);
            Log("Snap animation triggered.");
        }

        StartCoroutine(SnapLockRoutine());
    }

    private IEnumerator SnapLockRoutine()
    {
        isSnapPlaying = true;
        yield return new WaitForSeconds(Mathf.Max(0f, snapAnimationLockTime));
        isSnapPlaying = false;
    }

    private void ApplyBribedState()
    {
        if (animator != null)
        {
            TrySetAnimatorBool(isBribedBoolHash, hasIsBribedBoolParameter, isBribed);

            if (isBribed)
            {
                TryPlayAnimatorState(liftArmsStateName);
            }
        }

        if (isBribed)
        {
            DisableBribedCoinObjects();
        }

        bool paperCanBePickedUp = isBribed || !disablePaperPickupUntilBribed;

        if (guardedPaperPickup != null)
        {
            guardedPaperPickup.enabled = paperCanBePickedUp;
        }

        if (guardedPaperColliders != null)
        {
            foreach (Collider paperCollider in guardedPaperColliders)
            {
                if (paperCollider != null)
                {
                    paperCollider.enabled = paperCanBePickedUp;
                }
            }
        }

        if (crabBlockingCollidersToDisableAfterBribe != null)
        {
            foreach (Collider blockingCollider in crabBlockingCollidersToDisableAfterBribe)
            {
                if (blockingCollider != null)
                {
                    // The crab can still be interacted with, but its note-blocking box collider opens.
                    blockingCollider.enabled = !isBribed;
                }
            }
        }

        if (paperCanBePickedUp)
        {
            Log("Paper pickup became available.");
        }
        else
        {
            Log("Paper pickup is blocked until crab is bribed.");
        }
    }

    private void DisableBribedCoinObjects()
    {
        if (coinObjectsToDisableAfterBribe == null)
        {
            return;
        }

        foreach (GameObject coinObject in coinObjectsToDisableAfterBribe)
        {
            if (coinObject != null)
            {
                coinObject.SetActive(false);
            }
        }
    }

    private void AssignReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (guardedPaperObject == null && guardedPaperPickup != null)
        {
            guardedPaperObject = guardedPaperPickup.gameObject;
        }

        if (guardedPaperObject != null && guardedPaperPickup == null)
        {
            guardedPaperPickup = guardedPaperObject.GetComponentInChildren<PickupItem>(true);
        }

        if (guardedPaperObject != null && (guardedPaperColliders == null || guardedPaperColliders.Length == 0))
        {
            guardedPaperColliders = guardedPaperObject.GetComponentsInChildren<Collider>(true);
        }
    }

    private void CacheAnimatorHashes()
    {
        hasSnapTriggerParameter = false;
        hasLiftArmsTriggerParameter = false;
        hasIsBribedBoolParameter = false;

        snapTriggerHash = Animator.StringToHash(snapTriggerName);
        liftArmsTriggerHash = Animator.StringToHash(liftArmsTriggerName);
        isBribedBoolHash = Animator.StringToHash(isBribedBoolName);

        if (animator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == snapTriggerName && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                hasSnapTriggerParameter = true;
            }
            else if (parameter.name == liftArmsTriggerName && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                hasLiftArmsTriggerParameter = true;
            }
            else if (parameter.name == isBribedBoolName && parameter.type == AnimatorControllerParameterType.Bool)
            {
                hasIsBribedBoolParameter = true;
            }
        }
    }

    private void TrySetAnimatorTrigger(int parameterHash, bool hasParameter)
    {
        if (animator != null && hasParameter)
        {
            animator.SetTrigger(parameterHash);
        }
    }

    private void TrySetAnimatorBool(int parameterHash, bool hasParameter, bool value)
    {
        if (animator != null && hasParameter)
        {
            animator.SetBool(parameterHash, value);
        }
    }

    private void TryPlayAnimatorState(string stateName)
    {
        if (!playAnimationStatesDirectly || animator == null || string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        int stateHash = Animator.StringToHash(stateName);
        int baseLayerStateHash = Animator.StringToHash("Base Layer." + stateName);

        if (animator.HasState(0, stateHash))
        {
            animator.Play(stateHash, 0, 0f);
        }
        else if (animator.HasState(0, baseLayerStateHash))
        {
            animator.Play(baseLayerStateHash, 0, 0f);
        }
        else
        {
            Log("Animator state not found: " + stateName);
        }
    }

    private void Log(string message)
    {
        if (printDebugLogs)
        {
            Debug.Log("CrabGuard: " + message, this);
        }
    }
}
