using UnityEngine;

public class RotatePropeller : MonoBehaviour
{
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private bool useUnscaledTime = true;

    private void Update()
    {
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        transform.Rotate(rotationAxis * rotationSpeed * deltaTime, Space.Self);
    }
}