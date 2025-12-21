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
    public float sideCheckInterval = 0.2f;
    public float turnCheckDistance = 0.5f;

    private Vector3 steerHandleLeft = new Vector3(-42f, -20f, 28f);
    private Vector3 steerHandleNeutral = new Vector3(0.733f, 0.137f, 21.114f);
    private Vector3 steerHandleRight = new Vector3(42f, 20f, 28f);
    float fullSteerX = 3.5f;
    float zeroSteerX = 5.3f;
    float animSteer;
    private float sideCheckTimer = 0f;

    [Header("References")]
    public Animator rickshawManAnimator;
    public Transform rickshawSteerHandle;
    public Transform turnCheck;
    public LayerMask obstacleLayer;

    private Rigidbody rb;
    private float brakeTimer = 0f;
    private float brakeCooldownTimer = 0f;
    private bool isBraking = false;
    private float horizontalInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        //rb.collisionDetectionMode |= CollisionDetectionMode.Continuous;

        currentSpeed = baseSpeed;
    }

    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        HandleBraking();
        UpdateSteerHandle(animSteer);
        sideCheckTimer += Time.deltaTime;
    }

    void FixedUpdate()
    {
        ApplyForwardMovement();
        Steer();
    }

    void Steer()
    {
        // INPUT
        float steerInput = Mathf.Clamp(horizontalInput, -1f, 1f);
        float posX = transform.position.x;
        float absX = Mathf.Abs(posX);

        //Side Check
        bool leftBlocked = false;
        bool rightBlocked = false;
        if (sideCheckTimer >= sideCheckInterval)
        {
            rightBlocked = Physics.Raycast(turnCheck.position, turnCheck.right, turnCheckDistance, obstacleLayer);

            leftBlocked = Physics.Raycast(turnCheck.position, -turnCheck.right, turnCheckDistance, obstacleLayer);
            sideCheckTimer = 0f;
        }

        // Block steering only toward obstacle
        if (steerInput > 0f && rightBlocked) steerInput = 0f;
        if (steerInput < 0f && leftBlocked) steerInput = 0f;
        if(rightBlocked || leftBlocked)
        {
            Debug.Log("Right Blocked: " + rightBlocked + " | Left Blocked: " + leftBlocked);
        }

        // POSITION-BASED STEER LIMIT
        float steerMultiplier = 1f;

        if (absX > fullSteerX)
        {
            float t = Mathf.InverseLerp(zeroSteerX, fullSteerX, absX);
            t = Mathf.Clamp01(t);

            if (posX < 0f && steerInput > 0f)
                steerMultiplier = t;

            else if (posX > 0f && steerInput < 0f)
                steerMultiplier = t;
        }

        float finalSteer = steerInput * steerMultiplier;

        // ROTATION (PHYSICS)
        float targetY = 180f + finalSteer * maxTurnAngle;
        Quaternion targetRot = Quaternion.Euler(0f, targetY, 0f);

        rb.MoveRotation(
            Quaternion.Slerp(
                rb.rotation,
                targetRot,
                steerSpeed * Time.fixedDeltaTime
            )
        );

        // VISUAL + ANIMATION STEERING
        float deltaFromForward =
            Mathf.Abs(Mathf.DeltaAngle(180f, rb.rotation.eulerAngles.y));

        bool atMaxSteer = deltaFromForward >= (maxTurnAngle - 5f);

        if (atMaxSteer)
        {
            animSteer = Mathf.Lerp(rickshawManAnimator.GetFloat("Steer"), 0f, steerSpeed * Time.fixedDeltaTime);
        }
        else
        {
            animSteer = Mathf.Lerp(rickshawManAnimator.GetFloat("Steer"), finalSteer, steerSpeed * Time.fixedDeltaTime);
        }
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

        rickshawManAnimator.SetFloat("Steer", animSteer);
    }
}
