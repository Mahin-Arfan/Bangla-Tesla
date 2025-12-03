using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class PlayerRickshawController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float forwardSpeed = 10f;
    public float maxSpeed = 20f;
    public float acceleration = 5f;
    public float steerSpeed = 5f;
    public float maxTurnAngle = 45f;
    public float sideCheckDistance = 1f;

    private Rigidbody rb;
    private float currentSpeed;
    private float horizontalInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        currentSpeed = forwardSpeed;
    }

    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        MoveAndSteer();
    }

    void FixedUpdate()
    {
        
    }

    void MoveAndSteer()
    {
        // Forward movement
        currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.fixedDeltaTime);
        Vector3 forwardMove = transform.forward * currentSpeed * Time.fixedDeltaTime;

        // Side collision checks
        bool canTurnLeft = !Physics.Raycast(rb.position, -transform.right, sideCheckDistance);
        bool canTurnRight = !Physics.Raycast(rb.position, transform.right, sideCheckDistance);

        float steer = horizontalInput;

        // Block rotation if collider on that side
        if ((steer < 0 && !canTurnLeft) || (steer > 0 && !canTurnRight))
            steer = 0;

        // Rotate: 180 ± maxTurnAngle
        float targetY = 180f + steer * maxTurnAngle;
        Quaternion targetRot = Quaternion.Euler(0f, targetY, 0f);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, steerSpeed * Time.fixedDeltaTime));

        // Apply forward movement
        rb.MovePosition(rb.position + forwardMove);
    }
}
