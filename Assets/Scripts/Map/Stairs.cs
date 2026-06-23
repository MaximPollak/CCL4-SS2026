using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class RigidbodyStairWalker : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;

    [Header("Stairs")]
    public float maxStepHeight = 0.35f;
    public float stepCheckDistance = 0.45f;
    public float stepSmooth = 0.08f;

    private Rigidbody rb;
    private bool isGrounded;
    private float horizontalInput;
    private float verticalInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Important for player Rigidbody movement
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    void FixedUpdate()
    {
        CheckGrounded();
        MovePlayer();
        StepClimb();
    }

    void MovePlayer()
    {
        Vector3 moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;
        moveDirection.Normalize();

        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = rb.linearVelocity.y;

        rb.linearVelocity = velocity;
    }

    void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void CheckGrounded()
    {
        if (groundCheck == null)
        {
            isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f, groundLayer);
            return;
        }

        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    void StepClimb()
    {
        if (!isGrounded) return;

        Vector3 moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        if (moveDirection.sqrMagnitude < 0.01f) return;

        moveDirection.Normalize();

        Vector3 lowerRayOrigin = transform.position + Vector3.up * 0.05f;
        Vector3 upperRayOrigin = transform.position + Vector3.up * maxStepHeight;

        bool lowerHit = Physics.Raycast(
            lowerRayOrigin,
            moveDirection,
            out RaycastHit lowerHitInfo,
            stepCheckDistance,
            groundLayer
        );

        if (!lowerHit) return;

        bool upperHit = Physics.Raycast(
            upperRayOrigin,
            moveDirection,
            stepCheckDistance,
            groundLayer
        );

        // Lower ray hits a step, upper ray is free = player can step up
        if (!upperHit)
        {
            rb.position += Vector3.up * stepSmooth;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.05f, transform.forward * stepCheckDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position + Vector3.up * maxStepHeight, transform.forward * stepCheckDistance);
    }
}