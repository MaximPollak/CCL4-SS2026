using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonMovement : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference crouchAction;

    [Header("Movement")]
    [SerializeField] private float movementSpeed = 4f;
    [SerializeField] private float gravity = -20f;

    [Header("Crouch")]
    [SerializeField] private bool allowCrouch = true;
    [SerializeField] private bool holdToCrouch = false;
    [SerializeField] private float crouchHeight = 1.05f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float crouchCameraHeight = 1.1f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private LayerMask standUpObstacleMask = ~0;

    private CharacterController characterController;
    private Vector3 verticalVelocity;
    private Transform cameraHolder;
    private float standingHeight;
    private float standingSpeed;
    private float standingCameraHeight;
    private float controllerBottomOffset;
    private bool wantsToCrouch;
    private InputAction runtimeCrouchAction;
    private readonly Collider[] standUpHits = new Collider[8];

    public bool IsCrouching { get; private set; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        FirstPersonLook firstPersonLook = GetComponent<FirstPersonLook>();
        cameraHolder = firstPersonLook != null ? firstPersonLook.CameraHolder : null;

        standingHeight = characterController.height;
        standingSpeed = movementSpeed;
        standingCameraHeight = cameraHolder != null ? cameraHolder.localPosition.y : standingHeight;
        controllerBottomOffset = characterController.center.y - characterController.height * 0.5f;
    }

    private void OnValidate()
    {
        movementSpeed = Mathf.Max(0f, movementSpeed);
        gravity = Mathf.Min(0f, gravity);
        crouchHeight = Mathf.Max(0.4f, crouchHeight);
        crouchSpeed = Mathf.Max(0f, crouchSpeed);
        crouchCameraHeight = Mathf.Max(0.1f, crouchCameraHeight);
        crouchTransitionSpeed = Mathf.Max(0f, crouchTransitionSpeed);
    }

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.Enable();
        }

        InputAction activeCrouchAction = GetCrouchAction();

        if (activeCrouchAction != null)
        {
            activeCrouchAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.Disable();
        }

        InputAction activeCrouchAction = GetCrouchAction();

        if (activeCrouchAction != null)
        {
            activeCrouchAction.Disable();
        }
    }

    private void Update()
    {
        HandleCrouchInput();
        UpdateCrouchState();
        MovePlayer();
        ApplyGravity();
    }

    private void MovePlayer()
    {
        if (moveAction == null || moveAction.action == null)
        {
            return;
        }

        Vector2 input = moveAction.action.ReadValue<Vector2>();

        Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
        moveDirection.Normalize();

        float currentSpeed = IsCrouching ? crouchSpeed : standingSpeed;
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }

        verticalVelocity.y += gravity * Time.deltaTime;

        characterController.Move(verticalVelocity * Time.deltaTime);
    }

    private void HandleCrouchInput()
    {
        if (!allowCrouch)
        {
            wantsToCrouch = false;
            return;
        }

        if (holdToCrouch)
        {
            wantsToCrouch = IsCrouchHeld();
            return;
        }

        if (WasCrouchPressedThisFrame())
        {
            wantsToCrouch = !wantsToCrouch;
        }
    }

    private void UpdateCrouchState()
    {
        bool shouldCrouch = wantsToCrouch;

        if (!shouldCrouch && IsCrouching && !CanStandUp())
        {
            shouldCrouch = true;
            wantsToCrouch = true;
        }

        IsCrouching = shouldCrouch;

        float targetHeight = IsCrouching ? crouchHeight : standingHeight;
        float targetCameraHeight = IsCrouching ? crouchCameraHeight : standingCameraHeight;

        if (crouchTransitionSpeed <= 0f)
        {
            ApplyControllerHeight(targetHeight);
            ApplyCameraHeight(targetCameraHeight);
            return;
        }

        float newHeight = Mathf.MoveTowards(
            characterController.height,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime
        );

        ApplyControllerHeight(newHeight);

        if (cameraHolder != null)
        {
            float newCameraHeight = Mathf.MoveTowards(
                cameraHolder.localPosition.y,
                targetCameraHeight,
                crouchTransitionSpeed * Time.deltaTime
            );

            ApplyCameraHeight(newCameraHeight);
        }
    }

    private void ApplyControllerHeight(float newHeight)
    {
        characterController.height = newHeight;
        characterController.center = new Vector3(
            characterController.center.x,
            controllerBottomOffset + newHeight * 0.5f,
            characterController.center.z
        );
    }

    private void ApplyCameraHeight(float newHeight)
    {
        if (cameraHolder == null)
        {
            return;
        }

        cameraHolder.localPosition = new Vector3(
            cameraHolder.localPosition.x,
            newHeight,
            cameraHolder.localPosition.z
        );
    }

    private bool CanStandUp()
    {
        float radius = Mathf.Max(0.01f, characterController.radius - characterController.skinWidth);
        Vector3 bottom = transform.position + Vector3.up * (controllerBottomOffset + radius);
        Vector3 top = transform.position + Vector3.up * (controllerBottomOffset + standingHeight - radius);

        int hitCount = Physics.OverlapCapsuleNonAlloc(
            bottom,
            top,
            radius,
            standUpHits,
            standUpObstacleMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = standUpHits[i];

            if (hit == null)
            {
                continue;
            }

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private bool WasCrouchPressedThisFrame()
    {
        InputAction activeCrouchAction = GetCrouchAction();

        if (activeCrouchAction != null)
        {
            return activeCrouchAction.WasPressedThisFrame();
        }

        return Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame;
    }

    private bool IsCrouchHeld()
    {
        InputAction activeCrouchAction = GetCrouchAction();

        if (activeCrouchAction != null)
        {
            return activeCrouchAction.IsPressed();
        }

        return Keyboard.current != null && Keyboard.current.cKey.isPressed;
    }

    private InputAction GetCrouchAction()
    {
        if (crouchAction != null && crouchAction.action != null)
        {
            return crouchAction.action;
        }

        if (runtimeCrouchAction != null)
        {
            return runtimeCrouchAction;
        }

        if (moveAction == null || moveAction.action == null || moveAction.action.actionMap == null)
        {
            return null;
        }

        runtimeCrouchAction = moveAction.action.actionMap.FindAction("Crouch", false);
        return runtimeCrouchAction;
    }
}
