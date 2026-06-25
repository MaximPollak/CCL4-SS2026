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
    [SerializeField] private bool disablePaperPickupUntilBribed = true;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string snapTriggerName = "Snap";
    [SerializeField] private string liftArmsTriggerName = "LiftArms";
    [SerializeField] private string isBribedBoolName = "IsBribed";
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
        player.Inventory.ClearItem();
        Log("Coin was consumed: " + requiredCoinItemId);

        if (animator != null && !string.IsNullOrWhiteSpace(liftArmsTriggerName))
        {
            if (!string.IsNullOrWhiteSpace(snapTriggerName))
            {
                animator.ResetTrigger(snapTriggerHash);
            }

            animator.SetTrigger(liftArmsTriggerHash);
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

        if (animator != null && !string.IsNullOrWhiteSpace(snapTriggerName))
        {
            animator.SetTrigger(snapTriggerHash);
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
        if (animator != null && !string.IsNullOrWhiteSpace(isBribedBoolName))
        {
            animator.SetBool(isBribedBoolHash, isBribed);
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

        if (paperCanBePickedUp)
        {
            Log("Paper pickup became available.");
        }
        else
        {
            Log("Paper pickup is blocked until crab is bribed.");
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
        snapTriggerHash = Animator.StringToHash(snapTriggerName);
        liftArmsTriggerHash = Animator.StringToHash(liftArmsTriggerName);
        isBribedBoolHash = Animator.StringToHash(isBribedBoolName);
    }

    private void Log(string message)
    {
        if (printDebugLogs)
        {
            Debug.Log("CrabGuard: " + message, this);
        }
    }
}
