using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Item Settings")]
    [SerializeField] private string itemId = "Screwdriver";
    [SerializeField] private ItemSize itemSize = ItemSize.Medium;

    [Header("Wwise Drop Audio")]
    [SerializeField] private string dropSoundEvent = "Play_dropped_item";

    [Header("Held Visual Override")]
    [SerializeField] private bool overrideHeldPosition = false;
    [SerializeField] private bool overrideHeldRotation = false;
    [SerializeField] private Vector3 heldLocalPosition = new Vector3(0.45f, -0.35f, 0.75f);
    [SerializeField] private Vector3 heldLocalRotation = new Vector3(15f, -25f, 10f);
    [SerializeField] private float heldScaleMultiplier = 1f;

    [Header("Drop Physics")]
    [SerializeField] private bool usePhysicsOnDrop = false;
    [SerializeField] private bool addRigidbodyIfMissing = true;
    [SerializeField] private float upwardDropForce = 0.6f;
    [SerializeField] private float spinForce = 0f;

    [Header("World Pickup Physics")]
    [SerializeField] private bool makeWorldCollidersTriggers = true;
    [SerializeField] private bool keepWorldRigidbodyKinematic = true;

    [Header("Scene Persistence")]
    [SerializeField] private bool persistScenePickupState = true;

    [Header("Safe Drop Animation")]
    [SerializeField] private bool animateSafeDrop = true;
    [SerializeField] private float safeDropDuration = 0.22f;
    [SerializeField] private float safeDropArcHeight = 0.25f;
    [SerializeField] private float safeDropForwardSpin = 35f;
    [SerializeField] private bool centerVisualOnDropPoint = true;
    [SerializeField] private bool snapSafeDropToGround = true;
    [SerializeField] private float safeDropGroundClearance = 0.03f;
    [SerializeField] private float safeDropGroundRayHeight = 1.2f;
    [SerializeField] private float safeDropGroundRayDistance = 3f;

    [Header("Debug")]
    [SerializeField] private bool printDropDebugLogs = false;

    public string ItemId => itemId;
    public ItemSize Size => itemSize;
    public bool OverrideHeldPosition => overrideHeldPosition;
    public bool OverrideHeldRotation => overrideHeldRotation;
    public Vector3 HeldLocalPosition => heldLocalPosition;
    public Vector3 HeldLocalRotation => heldLocalRotation;
    public float HeldScaleMultiplier => heldScaleMultiplier;

    private void Awake()
    {
        if (!persistScenePickupState)
        {
            return;
        }

        string scenePickupKey = GetScenePickupKey();

        if (GameState.Instance.IsScenePickupConsumed(scenePickupKey))
        {
            gameObject.SetActive(false);
        }
    }

    public void PrepareForWorldSpawn(bool printDebugLogs = false)
    {
        ApplyWorldPickupPhysicsState(printDebugLogs);
    }

    public void DisableScenePickupPersistence()
    {
        persistScenePickupState = false;
    }

    public void Interact(PlayerInteraction player)
    {
        if (player.Inventory == null)
        {
            Debug.LogWarning("Player has no inventory.");
            return;
        }

        GameState.Instance.RemoveDroppedWorldItem(
            SceneManager.GetActiveScene().name,
            itemId,
            transform.position
        );

        RuntimeSpawnedItem runtimeSpawnedItem = GetComponentInParent<RuntimeSpawnedItem>();

        if (runtimeSpawnedItem != null)
        {
            runtimeSpawnedItem.MarkPickedUp();
        }
        else if (persistScenePickupState)
        {
            GameState.Instance.MarkScenePickupConsumed(GetScenePickupKey());
        }

        player.Inventory.PickUpItem(this, player.PlayerCamera.transform);

        ReadableNoteOverlay readableNoteOverlay = GetComponentInChildren<ReadableNoteOverlay>(true);

        if (readableNoteOverlay != null)
        {
            readableNoteOverlay.Show();
        }
    }

    public void DropFromHand(Vector3 forwardDirection, Vector3 originalScale, float plopForce)
    {
        DropFromHand(
            transform.position,
            forwardDirection,
            originalScale,
            plopForce,
            null,
            0f
        );
    }

    public void DropFromHand(
        Vector3 dropPosition,
        Vector3 forwardDirection,
        Vector3 originalScale,
        float plopForce,
        Collider[] playerColliders,
        float ignorePlayerCollisionDuration,
        bool printExtraDebugLogs = false
    )
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 startScale = transform.localScale;

        gameObject.SetActive(true);
        transform.SetParent(null, true);

        Collider[] itemColliders = GetComponentsInChildren<Collider>();
        bool shouldPrintDebug = printDropDebugLogs || printExtraDebugLogs;
        Vector3 centeredDropPosition = centerVisualOnDropPoint
            ? GetTransformPositionForVisualCenter(dropPosition, originalScale)
            : dropPosition;

        if (!usePhysicsOnDrop && animateSafeDrop)
        {
            StartCoroutine(AnimateSafeDrop(
                startPosition,
                centeredDropPosition,
                startRotation,
                startScale,
                originalScale,
                itemColliders,
                playerColliders,
                shouldPrintDebug
            ));

            return;
        }

        transform.position = centeredDropPosition;
        transform.localScale = originalScale;

        foreach (Collider itemCollider in itemColliders)
        {
            itemCollider.enabled = true;
        }

        ApplyWorldPickupPhysicsState(shouldPrintDebug);

        PlayDropSound();

        IgnorePlayerCollisions(
            itemColliders,
            playerColliders,
            true,
            usePhysicsOnDrop ? ignorePlayerCollisionDuration : 0f,
            shouldPrintDebug
        );

        Rigidbody itemRigidbody = GetComponent<Rigidbody>();

        if (itemRigidbody == null && addRigidbodyIfMissing && usePhysicsOnDrop)
        {
            itemRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        if (itemRigidbody != null)
        {
            itemRigidbody.isKinematic = !usePhysicsOnDrop;
            itemRigidbody.linearVelocity = Vector3.zero;
            itemRigidbody.angularVelocity = Vector3.zero;
            itemRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (usePhysicsOnDrop)
            {
                itemRigidbody.AddForce(
                    (forwardDirection + Vector3.up * upwardDropForce).normalized * plopForce,
                    ForceMode.Impulse
                );

                if (spinForce > 0f)
                {
                    itemRigidbody.AddTorque(
                        Random.insideUnitSphere * spinForce,
                        ForceMode.Impulse
                    );
                }
            }
        }

        if (shouldPrintDebug)
        {
            Debug.Log(
                "Drop debug | item: " + itemId
                + " | position: " + transform.position
                + " | requested visual center: " + dropPosition
                + " | scale: " + transform.localScale
                + " | item colliders: " + itemColliders.Length
                + " | player colliders: " + (playerColliders == null ? 0 : playerColliders.Length)
                + " | rb: " + (itemRigidbody == null ? "none" : itemRigidbody.GetType().Name)
                + " | physics drop: " + usePhysicsOnDrop
                + " | plop force: " + plopForce
            );
        }
    }

    private void PlayDropSound()
{
    if (string.IsNullOrWhiteSpace(dropSoundEvent))
    {
        return;
    }

    uint eventId = AkUnitySoundEngine.PostEvent(dropSoundEvent, gameObject);

    if (eventId == 0)
    {
        Debug.LogError("Drop sound event not found or SoundBank not loaded: " + dropSoundEvent);
    }
}

    private IEnumerator AnimateSafeDrop(
        Vector3 startPosition,
        Vector3 endPosition,
        Quaternion startRotation,
        Vector3 startScale,
        Vector3 endScale,
        Collider[] itemColliders,
        Collider[] playerColliders,
        bool shouldPrintDebug
    )
    {
        Quaternion endRotation = startRotation;
        Vector3 groundedEndPosition = snapSafeDropToGround
            ? GetGroundedDropPosition(endPosition, endRotation, endScale)
            : endPosition;

        Rigidbody itemRigidbody = GetComponent<Rigidbody>();

        if (itemRigidbody != null)
        {
            itemRigidbody.isKinematic = true;
            itemRigidbody.linearVelocity = Vector3.zero;
            itemRigidbody.angularVelocity = Vector3.zero;
        }

        foreach (Collider itemCollider in itemColliders)
        {
            itemCollider.enabled = false;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, safeDropDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            float arc = Mathf.Sin(t * Mathf.PI) * safeDropArcHeight;

            transform.position = Vector3.Lerp(startPosition, groundedEndPosition, easedT) + Vector3.up * arc;
            transform.localScale = Vector3.Lerp(startScale, endScale, easedT);
            transform.rotation =
                Quaternion.Euler(Vector3.right * safeDropForwardSpin * t)
                * startRotation;

            yield return null;
        }

        transform.position = groundedEndPosition;
        transform.rotation = endRotation;
        transform.localScale = endScale;

        foreach (Collider itemCollider in itemColliders)
        {
            itemCollider.enabled = true;
        }

        ApplyWorldPickupPhysicsState(shouldPrintDebug);

        PlayDropSound();

        IgnorePlayerCollisions(itemColliders, playerColliders, true, 0f, shouldPrintDebug);

        if (shouldPrintDebug)
        {
            Debug.Log(
                "Drop debug | item: " + itemId
                + " | safe animated drop finished"
                + " | position: " + transform.position
                + " | visual center: " + GetVisualBoundsCenter()
                + " | scale: " + transform.localScale
                + " | item colliders: " + itemColliders.Length
                + " | player colliders: " + (playerColliders == null ? 0 : playerColliders.Length)
            );
        }
    }

    private Vector3 GetGroundedDropPosition(
        Vector3 desiredPosition,
        Quaternion desiredRotation,
        Vector3 desiredScale
    )
    {
        Vector3 originalPosition = transform.position;
        Quaternion originalRotation = transform.rotation;
        Vector3 originalScale = transform.localScale;

        transform.position = desiredPosition;
        transform.rotation = desiredRotation;
        transform.localScale = desiredScale;

        Bounds visualBounds = GetVisualBounds();
        Vector3 rayStart = visualBounds.center + Vector3.up * safeDropGroundRayHeight;
        Vector3 groundedPosition = desiredPosition;

        if (Physics.Raycast(
            rayStart,
            Vector3.down,
            out RaycastHit floorHit,
            safeDropGroundRayDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        ))
        {
            float bottomOffset = visualBounds.min.y - transform.position.y;
            groundedPosition.y = floorHit.point.y + safeDropGroundClearance - bottomOffset;
        }

        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = originalScale;

        return groundedPosition;
    }

    private Vector3 GetTransformPositionForVisualCenter(Vector3 desiredVisualCenter, Vector3 finalScale)
    {
        Vector3 originalPosition = transform.position;
        Vector3 originalScale = transform.localScale;

        transform.position = desiredVisualCenter;
        transform.localScale = finalScale;

        Vector3 visualOffset = GetVisualBoundsCenter() - transform.position;

        transform.position = originalPosition;
        transform.localScale = originalScale;

        return desiredVisualCenter - visualOffset;
    }

    private Vector3 GetVisualBoundsCenter()
    {
        return GetVisualBounds().center;
    }

    private Bounds GetVisualBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        bool hasBounds = false;
        Bounds visualBounds = new Bounds(transform.position, Vector3.zero);

        foreach (Renderer itemRenderer in renderers)
        {
            if (itemRenderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                visualBounds = itemRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                visualBounds.Encapsulate(itemRenderer.bounds);
            }
        }

        if (hasBounds)
        {
            return visualBounds;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider itemCollider in colliders)
        {
            if (itemCollider == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                visualBounds = itemCollider.bounds;
                hasBounds = true;
            }
            else
            {
                visualBounds.Encapsulate(itemCollider.bounds);
            }
        }

        return hasBounds ? visualBounds : new Bounds(transform.position, Vector3.zero);
    }

    private void IgnorePlayerCollisions(
        Collider[] itemColliders,
        Collider[] playerColliders,
        bool shouldIgnore,
        float restoreDelay,
        bool printDebugLogs
    )
    {
        if (itemColliders == null || playerColliders == null)
        {
            return;
        }

        foreach (Collider itemCollider in itemColliders)
        {
            if (itemCollider == null)
            {
                continue;
            }

            foreach (Collider playerCollider in playerColliders)
            {
                if (playerCollider == null || itemCollider == playerCollider)
                {
                    continue;
                }

                Physics.IgnoreCollision(itemCollider, playerCollider, shouldIgnore);
            }
        }

        if (shouldIgnore && restoreDelay > 0f)
        {
            StartCoroutine(RestorePlayerCollisionsAfterDelay(
                itemColliders,
                playerColliders,
                restoreDelay,
                printDebugLogs
            ));
        }
    }

    private IEnumerator RestorePlayerCollisionsAfterDelay(
        Collider[] itemColliders,
        Collider[] playerColliders,
        float delay,
        bool printDebugLogs
    )
    {
        yield return new WaitForSeconds(delay);

        float waitedTime = delay;

        while (IsOverlappingAnyPlayerCollider(itemColliders, playerColliders))
        {
            if (printDebugLogs)
            {
                Debug.Log(
                    "Drop debug | item: " + itemId
                    + " still overlaps player, keeping player collision ignored."
                );
            }

            yield return new WaitForSeconds(0.1f);
            waitedTime += 0.1f;
        }

        IgnorePlayerCollisions(itemColliders, playerColliders, false, 0f, false);

        if (printDebugLogs)
        {
            Debug.Log(
                "Drop debug | item: " + itemId
                + " restored player collisions after " + waitedTime.ToString("0.00") + "s."
            );
        }
    }

    private bool IsOverlappingAnyPlayerCollider(Collider[] itemColliders, Collider[] playerColliders)
    {
        if (itemColliders == null || playerColliders == null)
        {
            return false;
        }

        foreach (Collider itemCollider in itemColliders)
        {
            if (itemCollider == null || !itemCollider.enabled)
            {
                continue;
            }

            foreach (Collider playerCollider in playerColliders)
            {
                if (playerCollider == null || !playerCollider.enabled)
                {
                    continue;
                }

                if (itemCollider.bounds.Intersects(playerCollider.bounds))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ApplyWorldPickupPhysicsState(bool printDebugLogs)
    {
        Rigidbody itemRigidbody = GetComponent<Rigidbody>();

        if (itemRigidbody != null && keepWorldRigidbodyKinematic && !usePhysicsOnDrop)
        {
            itemRigidbody.isKinematic = true;
            itemRigidbody.useGravity = false;
            itemRigidbody.linearVelocity = Vector3.zero;
            itemRigidbody.angularVelocity = Vector3.zero;
            itemRigidbody.Sleep();
        }

        Collider[] itemColliders = GetComponentsInChildren<Collider>();

        foreach (Collider itemCollider in itemColliders)
        {
            if (itemCollider == null)
            {
                continue;
            }

            itemCollider.enabled = true;

            if (makeWorldCollidersTriggers && !usePhysicsOnDrop)
            {
                itemCollider.isTrigger = true;
            }
        }

        if (printDebugLogs)
        {
            Debug.Log(
                "PickupItem world physics | item: " + itemId
                + " | trigger colliders: " + (makeWorldCollidersTriggers && !usePhysicsOnDrop)
                + " | kinematic rb: " + (itemRigidbody != null && itemRigidbody.isKinematic)
                + " | colliders: " + itemColliders.Length,
                this
            );
        }
    }

    private string GetScenePickupKey()
    {
        string sceneName = gameObject.scene.IsValid()
            ? gameObject.scene.name
            : SceneManager.GetActiveScene().name;

        return sceneName + "/" + GetHierarchyPath(transform);
    }

    private string GetHierarchyPath(Transform itemTransform)
    {
        if (itemTransform == null)
        {
            return "";
        }

        string path = itemTransform.name;
        Transform parent = itemTransform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}
