using UnityEngine;

public class PlayerHideState : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FirstPersonMovement movement;
    [SerializeField] private FirstPersonLook look;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform cameraTransform;

    [Header("Hiding")]
    [SerializeField] private bool disableMovementWhileHidden = true;
    [SerializeField] private bool snapViewToHidingPoint = true;
    [SerializeField] private bool useHidePointAsCameraView = true;

    public bool IsHidden { get; private set; }
    public HidingSpot CurrentHidingSpot { get; private set; }

    private void Reset()
    {
        AssignReferences();
    }

    private void OnValidate()
    {
        AssignReferences();
    }

    private void Awake()
    {
        AssignReferences();
    }

    public void EnterHidingSpot(HidingSpot hidingSpot, Transform hidePoint, Transform viewPoint)
    {
        if (hidingSpot == null || IsHidden)
        {
            return;
        }

        CurrentHidingSpot = hidingSpot;
        IsHidden = true;

        MoveToPoint(hidePoint, viewPoint, useHidePointAsCameraView, false);
        SetMovementEnabled(false);
    }

    public void ExitHidingSpot(Transform exitPoint, Transform viewPoint)
    {
        if (!IsHidden)
        {
            return;
        }

        HidingSpot previousHidingSpot = CurrentHidingSpot;

        MoveToPoint(exitPoint, viewPoint, false, false);
        SetMovementEnabled(true);

        CurrentHidingSpot = null;
        IsHidden = false;

        if (previousHidingSpot != null)
        {
            previousHidingSpot.ClearOccupant(this);
        }
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

        if (viewPoint == null)
        {
            viewPoint = targetPoint;
        }

        bool controllerWasEnabled = characterController != null && characterController.enabled;

        if (controllerWasEnabled)
        {
            characterController.enabled = false;
        }

        if (snapViewToHidingPoint && look != null)
        {
            if (flattenViewPitch)
            {
                look.SnapYawTo(viewPoint.rotation);
            }
            else
            {
                look.SnapLookTo(viewPoint.rotation);
            }
        }
        else
        {
            transform.rotation = viewPoint.rotation;
        }

        Vector3 targetPosition = targetPoint.position;

        if (alignCameraToPoint && cameraTransform != null)
        {
            Vector3 cameraOffset = cameraTransform.position - transform.position;
            targetPosition -= cameraOffset;
        }

        transform.position = targetPosition;

        if (controllerWasEnabled)
        {
            characterController.enabled = true;
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
}
