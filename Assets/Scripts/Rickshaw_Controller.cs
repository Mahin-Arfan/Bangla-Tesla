using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class PlayerRickshawController : MonoBehaviour
{
    [Header("Speed Settings")]
    [HideInInspector] public float baseSpeed = 8f;         // always moves forward
    public float startSpeed = 8f;
    public float maxSpeed = 20f;        // maximum speed limit
    public float boostSpeed = 5f;        // extra speed
    public float acceleration = 4f;      // smooth speed change
    public float currentSpeed;
    private bool boostPressed = false;
    [HideInInspector] public bool outOfBattery = false;

    [Header("Brake Settings")]
    public float breakDeceleration = 5f;
    public float brakeForce = 10f;
    public float maxBrakeTime = 1f;      // how long brakes works
    public float brakeCooldown = 2f;     // cooldown after brake
    private bool brakePressed = false;

    [Header("Steering Settings")]
    public float tiltSensitivity = 2.0f;
    public float steerSpeed = 5f;
    public float maxTurnAngle = 45f;
    public float sideCheckDistance = 1f;
    public float sideCheckInterval = 0.2f;
    public float turnCheckDistance = 0.5f;
    public float maxTurnMinusVisual = 20f;
    private bool leftPressed = false;
    private bool rightPressed = false;
    bool leftBlocked = false;
    bool rightBlocked = false;

    [Header("Damage Effects")]
    public float maxDamagePullAngle = 5f; // The pull rotation
    public float damageWobbleSpeed = 15f;  // How fast it shakes
    public float damageWobbleAmount = 2f;  // How violent the shake is
    float damageBias = 0f;
    float damageWobbleTimer = 0f;

    private Vector3 steerHandleLeft = new Vector3(-42f, -20f, 28f);
    private Vector3 steerHandleNeutral = new Vector3(0.733f, 0.137f, 21.114f);
    private Vector3 steerHandleRight = new Vector3(42f, 20f, 28f);
    float fullSteerX = 4.5f;
    float zeroSteerX = 5.3f;
    float animSteer;
    private float sideCheckTimer = 0f;

    [Header("References")]
    public Animator rickshawManAnimator;
    public Transform rickshawSteerHandle;
    public Transform turnCheck;
    public LayerMask obstacleLayer;
    private CameraScript cameraScript;
    private RickshawHealth healthScript;
    [HideInInspector] public bool gameStarted = false;

    private Rigidbody rb;
    private float brakeTimer = 0f;
    private float brakeCooldownTimer = 0f;
    private bool isBraking = false;
    public float horizontalInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        cameraScript = GetComponent<CameraScript>();
        healthScript = GetComponent<RickshawHealth>();
        baseSpeed = startSpeed;
    }

    void Update()
    {
        if(!gameStarted)   return;
        if (leftPressed)
            horizontalInput = Mathf.Lerp(horizontalInput, -1f, Time.deltaTime * steerSpeed);
        else if (rightPressed)
            horizontalInput = Mathf.Lerp(horizontalInput, 1f, Time.deltaTime * steerSpeed);
        else
            horizontalInput = Mathf.Lerp(horizontalInput, 0f, Time.deltaTime * steerSpeed);

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.W))
        {
            BoostButtonDown();
        }
        if(Input.GetKeyUp(KeyCode.W))
        {
            BoostButtonUp();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            BreakButtonDown();
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            BreakButtonUp();
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            LeftButtonDown();
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            LeftButtonUp();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            RightButtonDown();
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            RightButtonUp();
        }

#endif
        HandleBraking();
        UpdateSteerHandle(animSteer);
        sideCheckTimer += Time.deltaTime;
        damageWobbleTimer += Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (!gameStarted) return;
        ApplyForwardMovement();
        Steer();
    }
    public void LeftButtonDown() { leftPressed = true; }
    public void LeftButtonUp() { leftPressed = false; }
    public void RightButtonDown() { rightPressed = true; }
    public void RightButtonUp() { rightPressed = false; }
    public void BoostButtonUp() { boostPressed = false; }
    public void BoostButtonDown() { boostPressed = true; }
    public void BreakButtonUp() { brakePressed = false; }
    public void BreakButtonDown() { brakePressed = true; }


    void Steer()
    {
        // INPUT
        float steerInput = Mathf.Clamp(horizontalInput, -1f, 1f);
        float posX = transform.position.x;
        float absX = Mathf.Abs(posX);

        //PositionCheck
        if(posX < -5.6) transform.position = new Vector3(-5.6f, transform.position.y, transform.position.z);
        if(posX > 5.6) transform.position = new Vector3(5.6f, transform.position.y, transform.position.z);

        //Side Check
        if (sideCheckTimer >= sideCheckInterval)
        {
            rightBlocked = Physics.Raycast(turnCheck.position, turnCheck.right, turnCheckDistance, obstacleLayer);
            leftBlocked = Physics.Raycast(turnCheck.position, -turnCheck.right, turnCheckDistance, obstacleLayer);
            sideCheckTimer = 0f;
        }

        // Block steering only toward obstacle
        if (steerInput > 0f && rightBlocked) steerInput = 0f;
        if (steerInput < 0f && leftBlocked) steerInput = 0f;

        // POSITION-BASED STEER LIMIT
        float steerMultiplier = 1f;

        if (absX > fullSteerX)
        {
            float t = Mathf.InverseLerp(zeroSteerX, fullSteerX, absX);
            t = Mathf.Clamp01(t);

            if (posX < 0f && steerInput > 0f)   steerMultiplier = t;
            else if (posX > 0f && steerInput < 0f)  steerMultiplier = t;
        }

        float finalSteer = steerInput * steerMultiplier;

        //Wobble Effect when damaged
        if (healthScript != null && damageWobbleTimer > 0.2f)
        {
            float leftDamage = Mathf.Clamp01(1f - (healthScript.leftWheelHealth / 100f));
            float rightDamage = Mathf.Clamp01(1f - (healthScript.rightWheelHealth / 100f));

            float damageDiff = rightDamage - leftDamage;
            float constantPull = damageDiff * maxDamagePullAngle;
            float shakeIntensity = Mathf.Max(leftDamage, rightDamage);

            float totalShake = 0f;
            if (currentSpeed > 1f && shakeIntensity > 0f)
            {
                float vibration = Mathf.Sin(Time.time * damageWobbleSpeed) * damageWobbleAmount * shakeIntensity;
                float randomDrift = 0f;
                if (leftDamage > 0.5f && rightDamage > 0.5f)
                {
                    randomDrift = (Mathf.PerlinNoise(Time.time * 2f, 0f) - 0.5f) * 2f;
                    randomDrift *= 5f;
                }

                totalShake = vibration + randomDrift;
            }
            damageBias = constantPull + totalShake;
            damageWobbleTimer = 0f;
        }


        // ROTATION (PHYSICS)
        float targetY = 180f + (finalSteer * maxTurnAngle) + damageBias;
        Quaternion targetRot = Quaternion.Euler(0f, targetY, 0f);

        if (cameraScript != null) cameraScript.SetSteerInput(finalSteer);

        float speedFactor = Mathf.Clamp01(currentSpeed / baseSpeed);
        float dynamicSteerSpeed = steerSpeed * speedFactor;
        dynamicSteerSpeed = Mathf.Max(dynamicSteerSpeed, 0.5f);

        rb.MoveRotation(
            Quaternion.Slerp(
                rb.rotation,
                targetRot,
                dynamicSteerSpeed * Time.fixedDeltaTime
            )
        );

        // VISUAL + ANIMATION STEERING
        float deltaFromForward =
            Mathf.Abs(Mathf.DeltaAngle(180f, rb.rotation.eulerAngles.y));

        bool atMaxSteer = deltaFromForward >= (maxTurnAngle - maxTurnMinusVisual);

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
        if (outOfBattery)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, 0.5f * Time.fixedDeltaTime);
            Vector3 forward = transform.forward * currentSpeed;
            rb.linearVelocity = new Vector3(forward.x, rb.linearVelocity.y, forward.z);
            return;
        }

        if (isBraking)
        {
            float brakingSpeed = baseSpeed - breakDeceleration;
            currentSpeed = Mathf.Lerp(currentSpeed, brakingSpeed, brakeForce * Time.fixedDeltaTime);
        }
        else
        {
            float targetSpeed = baseSpeed;
            if (boostPressed) targetSpeed += boostSpeed;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        }

        Vector3 normalForward = transform.forward * currentSpeed;
        rb.linearVelocity = new Vector3(normalForward.x, rb.linearVelocity.y, normalForward.z);
    }

    void HandleBraking()
    {
        if (brakeCooldownTimer > 0f)
            brakeCooldownTimer -= Time.deltaTime;

        if (brakePressed && brakeCooldownTimer <= 0f)
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
