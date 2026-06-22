using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonLook : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference lookAction;

    [Header("References")]
    [SerializeField] private Transform cameraHolder;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;

    private float verticalRotation;

    public Transform CameraHolder => cameraHolder;

    private void OnEnable()
    {
        lookAction.action.Enable();
    }

    private void OnDisable()
    {
        lookAction.action.Disable();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        LookAround();
    }

    private void LookAround()
    {
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);

        cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    public void SnapLookTo(Quaternion worldRotation)
    {
        Vector3 eulerAngles = worldRotation.eulerAngles;

        transform.rotation = Quaternion.Euler(0f, eulerAngles.y, 0f);

        float pitch = NormalizeAngle(eulerAngles.x);
        verticalRotation = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }

    public void SnapYawTo(Quaternion worldRotation, float pitch = 0f)
    {
        Vector3 eulerAngles = worldRotation.eulerAngles;

        transform.rotation = Quaternion.Euler(0f, eulerAngles.y, 0f);
        verticalRotation = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            return angle - 360f;
        }

        return angle;
    }
}
