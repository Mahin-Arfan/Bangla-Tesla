using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class PlayerRickshawController : MonoBehaviour
{
    [Header("Speed Settings")]
    public float baseSpeed = 8f;         // always moves forward
    public float boostSpeed = 5f;        // extra speed when pressing W
    public float acceleration = 4f;      // smooth speed change
    public float currentSpeed;

    [Header("Brake Settings")]
    public float brakeForce = 10f;
    public float maxBrakeTime = 1f;      // how long S key works
    public float brakeCooldown = 2f;     // cooldown after brake

    [Header("Steering Settings")]
    public float steerSpeed = 5f;
    public float maxTurnAngle = 45f;
    public float sideCheckDistance = 1f;

    private Vector3 steerHandleLeft = new Vector3(-42f, -20f, 28f);
    private Vector3 steerHandleNeutral = new Vector3(0.733f, 0.137f, 21.114f);
    private Vector3 steerHandleRight = new Vector3(42f, 20f, 28f);

    [Header("References")]
    public Animator rickshawManAnimator;
    public Transform rickshawSteerHandle;

    private Rigidbody rb;
    private float brakeTimer = 0f;
    private float brakeCooldownTimer = 0f;
    private bool isBraking = false;
    private float horizontalInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        currentSpeed = baseSpeed;
    }

    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        MoveAndSteer();
        HandleBraking();
    }

    void FixedUpdate()
    {
        ApplyForwardMovement();
    }

    void MoveAndSteer()
    {

        // Side collision checks
        bool canTurnLeft = !Physics.Raycast(rb.position, -transform.right, sideCheckDistance);
        bool canTurnRight = !Physics.Raycast(rb.position, transform.right, sideCheckDistance);
        bool canGoForward = !Physics.Raycast(rb.position, transform.forward, sideCheckDistance);

        float steer = Mathf.Clamp(horizontalInput, -1f, 1f);

        // Block rotation if collider on that side
        if ((steer < 0 && !canTurnLeft) || (steer > 0 && !canTurnRight))
            steer = 0;

        float currentY = transform.rotation.eulerAngles.y;
        // how far we are rotated from straight (180°)
        float deltaFromForward = Mathf.DeltaAngle(180f, currentY);
        // true when near max turn
        bool atMaxSteer =
            Mathf.Abs(deltaFromForward) >= (maxTurnAngle - 10f);
        float finalSteer = steer;
        if (atMaxSteer)
        {
            // Smoothly return visuals + animation to neutral
            finalSteer = Mathf.Lerp(
                rickshawManAnimator.GetFloat("Steer"),
                0f,
                steerSpeed * Time.deltaTime
            );
        }
        // Animator
        rickshawManAnimator.SetFloat("Steer", finalSteer);
        // Visual handle
        UpdateSteerHandle(finalSteer);

        // Rotate: 180 ± maxTurnAngle
        float targetY = 180f + steer * maxTurnAngle;
        Quaternion targetRot = Quaternion.Euler(0f, targetY, 0f);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, steerSpeed * Time.fixedDeltaTime));
    }

    void ApplyForwardMovement()
    {
        if (isBraking)
        {
            // apply braking
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, brakeForce * Time.fixedDeltaTime);
        }
        else
        {
            // normal acceleration toward base + boost
            float targetSpeed = baseSpeed;

            // boost applied?
            if (Input.GetKey(KeyCode.W))
                targetSpeed += boostSpeed;

            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        }

        Vector3 forward = transform.forward * currentSpeed;
        rb.linearVelocity = new Vector3(forward.x, rb.linearVelocity.y, forward.z);
    }

    void HandleBraking()
    {
        if (brakeCooldownTimer > 0f)
            brakeCooldownTimer -= Time.deltaTime;

        if (Input.GetKey(KeyCode.S) && brakeCooldownTimer <= 0f)
        {
            isBraking = true;
            brakeTimer += Time.deltaTime;

            if (brakeTimer >= maxBrakeTime)
            {
                isBraking = false;
                brakeCooldownTimer = brakeCooldown;
            }
        }
        else
        {
            isBraking = false;
            brakeTimer = 0f;
        }
    }

    void UpdateSteerHandle(float steer)
    {
        Quaternion targetRotation;

        if (steer < 0f)
        {
            targetRotation = Quaternion.Euler(
                Vector3.Lerp(
                    steerHandleNeutral,
                    steerHandleRight,
                    Mathf.Abs(steer)
                )
            );
        }
        else if (steer > 0f)
        {
            targetRotation = Quaternion.Euler(
                Vector3.Lerp(
                    steerHandleNeutral,
                    steerHandleLeft,
                    steer
                )
            );
        }
        else
        {
            targetRotation = Quaternion.Euler(steerHandleNeutral);
        }

        rickshawSteerHandle.localRotation = Quaternion.Slerp(
            rickshawSteerHandle.localRotation,
            targetRotation,
            steerSpeed * 4 * Time.deltaTime
        );
    }
}
