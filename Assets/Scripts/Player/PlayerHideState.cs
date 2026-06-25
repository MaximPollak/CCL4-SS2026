using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerHideState : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FirstPersonMovement movement;
    [SerializeField] private FirstPersonLook look;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform cameraTransform;

    [Header("Hiding")]
    [SerializeField] private bool disableMovementWhileHidden = true;
    [SerializeField] private bool disableLookWhileHidden = true;
    [SerializeField] private bool snapViewToHidingPoint = true;
    [SerializeField] private bool useHidePointAsCameraView = true;

    [Header("Transitions")]
    [FormerlySerializedAs("enterDelay")]
    [SerializeField] private float enterTransitionDuration = 0.35f;
    [FormerlySerializedAs("exitDelay")]
    [SerializeField] private float exitTransitionDuration = 0.2f;

    [Header("Hidden Overlay")]
    [SerializeField] private Image hiddenOverlay;
    [SerializeField] private Color hiddenOverlayColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private float overlayFadeSpeed = 8f;

    [Header("Wwise Locker Audio")]
    [SerializeField] private string lockerEvent = "Play_locker_closing";
    [SerializeField] private string breathingStartEvent = "Play_breathing_in_locker";
    [SerializeField] private string breathingStopEvent = "Stop_breathing_in_locker";

    public bool IsHidden { get; private set; }
    public bool IsTransitioning { get; private set; }
    public HidingSpot CurrentHidingSpot { get; private set; }
    private bool isBreathingLoopPlaying;
    private float overlayAlphaTarget;

    private void Reset()
    {
        AssignReferences();
    }

    private void OnValidate()
    {
        AssignReferences();

        enterTransitionDuration = Mathf.Max(0f, enterTransitionDuration);
        exitTransitionDuration = Mathf.Max(0f, exitTransitionDuration);
        overlayFadeSpeed = Mathf.Max(0f, overlayFadeSpeed);
    }

    private void Awake()
    {
        AssignReferences();
        SetOverlayAlpha(0f);
    }

    private void Update()
    {
        UpdateHiddenOverlay();
    }

    public void EnterHidingSpot(HidingSpot hidingSpot, Transform hidePoint, Transform viewPoint)
    {
        if (hidingSpot == null || IsHidden || IsTransitioning)
        {
            return;
        }

        StartCoroutine(EnterHidingSpotRoutine(hidingSpot, hidePoint, viewPoint));
    }

    public void ExitHidingSpot(Transform exitPoint, Transform viewPoint)
    {
        if (!IsHidden || IsTransitioning)
        {
            return;
        }

        StartCoroutine(ExitHidingSpotRoutine(exitPoint, viewPoint));
    }

    private void AssignReferences()
    {
        if (movement == null)
        {
            movement = GetComponent<FirstPersonMovement>();
        }

        if (look == null)
        {
            look = GetComponent<FirstPersonLook>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (cameraTransform == null)
        {
            Camera playerCamera = GetComponentInChildren<Camera>();

            if (playerCamera != null)
            {
                cameraTransform = playerCamera.transform;
            }
        }

        if (hiddenOverlay == null)
        {
            GameObject overlayObject = GameObject.Find("HiddenOverlay");

            if (overlayObject != null)
            {
                hiddenOverlay = overlayObject.GetComponent<Image>();
            }
        }
    }

    private System.Collections.IEnumerator EnterHidingSpotRoutine(
        HidingSpot hidingSpot,
        Transform hidePoint,
        Transform viewPoint
    )
    {
        IsTransitioning = true;
        CurrentHidingSpot = hidingSpot;
        PlayLockerSound();

        SetMovementEnabled(false);
        SetLookEnabled(false);
        overlayAlphaTarget = hiddenOverlayColor.a;

        yield return MoveToPointRoutine(
            hidePoint,
            viewPoint,
            useHidePointAsCameraView,
            false,
            enterTransitionDuration
        );

        StartBreathingLoop();
        IsHidden = true;
        IsTransitioning = false;
    }

    private System.Collections.IEnumerator ExitHidingSpotRoutine(
        Transform exitPoint,
        Transform viewPoint
    )
    {
        IsTransitioning = true;
        HidingSpot previousHidingSpot = CurrentHidingSpot;
        overlayAlphaTarget = 0f;

        StopBreathingLoop();
        PlayLockerSound();

        yield return MoveToPointRoutine(
            exitPoint,
            viewPoint,
            false,
            false,
            exitTransitionDuration
        );

        SetMovementEnabled(true);
        SetLookEnabled(true);

        CurrentHidingSpot = null;
        IsHidden = false;
        IsTransitioning = false;

        if (previousHidingSpot != null)
        {
            previousHidingSpot.ClearOccupant(this);
        }
    }

    private System.Collections.IEnumerator MoveToPointRoutine(
        Transform targetPoint,
        Transform viewPoint,
        bool alignCameraToPoint,
        bool flattenViewPitch,
        float duration
    )
    {
        if (targetPoint == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            MoveToPoint(targetPoint, viewPoint, alignCameraToPoint, flattenViewPitch);
            yield break;
        }

        if (viewPoint == null)
        {
            viewPoint = targetPoint;
        }

        bool controllerWasEnabled = characterController != null && characterController.enabled;

        if (controllerWasEnabled)
        {
            characterController.enabled = false;
        }

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = GetBodyRotation(viewPoint.rotation);
        Vector3 targetPosition = GetTargetBodyPosition(
            targetPoint,
            targetRotation,
            alignCameraToPoint
        );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPosition, targetPosition, easedT);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, easedT);

            yield return null;
        }

        transform.position = targetPosition;
        ApplyLookRotation(viewPoint.rotation, flattenViewPitch);

        if (controllerWasEnabled)
        {
            characterController.enabled = true;
        }
    }

    private void MoveToPoint(
        Transform targetPoint,
        Transform viewPoint,
        bool alignCameraToPoint,
        bool flattenViewPitch
    )
    {
        if (targetPoint == null)
        {
            return;
        }

        bool controllerWasEnabled = characterController != null && characterController.enabled;

        if (controllerWasEnabled)
        {
            characterController.enabled = false;
        }

        Quaternion targetRotation = viewPoint != null ? viewPoint.rotation : targetPoint.rotation;
        transform.position = GetTargetBodyPosition(
            targetPoint,
            GetBodyRotation(targetRotation),
            alignCameraToPoint
        );
        ApplyLookRotation(targetRotation, flattenViewPitch);

        if (controllerWasEnabled)
        {
            characterController.enabled = true;
        }
    }

    private void StartBreathingLoop()
    {
        if (isBreathingLoopPlaying)
        {
            return;
        }

        AkUnitySoundEngine.PostEvent(breathingStartEvent, gameObject);
        isBreathingLoopPlaying = true;
    }

    private void StopBreathingLoop()
    {
        if (!isBreathingLoopPlaying)
        {
            return;
        }

        AkUnitySoundEngine.PostEvent(breathingStopEvent, gameObject);
        isBreathingLoopPlaying = false;
    }

    private void PlayLockerSound()
    {
        uint eventId = AkUnitySoundEngine.PostEvent(lockerEvent, gameObject);

        if (eventId == 0)
        {
            Debug.LogError("Locker sound event not found or SoundBank not loaded: " + lockerEvent);
        }
    }

    private Vector3 GetTargetBodyPosition(
        Transform targetPoint,
        Quaternion targetBodyRotation,
        bool alignCameraToPoint
    )
    {
        Vector3 targetPosition = targetPoint.position;

        if (alignCameraToPoint && cameraTransform != null)
        {
            Vector3 localCameraOffset = transform.InverseTransformPoint(cameraTransform.position);
            targetPosition -= targetBodyRotation * localCameraOffset;
        }

        return targetPosition;
    }

    private Quaternion GetBodyRotation(Quaternion lookRotation)
    {
        Vector3 eulerAngles = lookRotation.eulerAngles;
        return Quaternion.Euler(0f, eulerAngles.y, 0f);
    }

    private void ApplyLookRotation(Quaternion lookRotation, bool flattenViewPitch)
    {
        if (snapViewToHidingPoint && look != null)
        {
            if (flattenViewPitch)
            {
                look.SnapYawTo(lookRotation);
            }
            else
            {
                look.SnapLookTo(lookRotation);
            }
        }
        else
        {
            transform.rotation = GetBodyRotation(lookRotation);
        }
    }

    private void SetMovementEnabled(bool isEnabled)
    {
        if (!disableMovementWhileHidden || movement == null)
        {
            return;
        }

        movement.enabled = isEnabled;
    }

    private void SetLookEnabled(bool isEnabled)
    {
        if (!disableLookWhileHidden || look == null)
        {
            return;
        }

        look.enabled = isEnabled;
    }

    private void UpdateHiddenOverlay()
    {
        if (hiddenOverlay == null)
        {
            return;
        }

        Color currentColor = hiddenOverlay.color;
        float newAlpha = Mathf.MoveTowards(
            currentColor.a,
            overlayAlphaTarget,
            overlayFadeSpeed * Time.deltaTime
        );

        hiddenOverlay.color = new Color(
            hiddenOverlayColor.r,
            hiddenOverlayColor.g,
            hiddenOverlayColor.b,
            newAlpha
        );
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (hiddenOverlay == null)
        {
            return;
        }

        hiddenOverlay.color = new Color(
            hiddenOverlayColor.r,
            hiddenOverlayColor.g,
            hiddenOverlayColor.b,
            alpha
        );
    }

    private void OnDisable()
    {
        StopBreathingLoop();
    }
}
