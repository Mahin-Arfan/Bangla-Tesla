using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class PlayerRickshawController : MonoBehaviour
{
    [Header("Speed Settings")]
    [HideInInspector] public float baseSpeed = 8f;
    public float startSpeed = 8f;
    public float maxSpeed = 20f;        //maximum speed limit
    public float boostSpeed = 5f;
    public float acceleration = 4f;
    public float currentSpeed;
    [HideInInspector] public bool outOfBattery = false;

    [Header("Brake Settings")]
    public float brakeDeceleration = 5f;
    public float brakeForce = 10f;
    private bool brakePressed = false;
    public bool isBrakeFailed = false;
    public float brakeMeter = 0f;
    public float maxBrakeMeter = 100f;
    public float meterIncreaseRate = 30f;
    public float meterDecreaseRate = 15f; 

    [Header("Steering Settings")]
    public float tiltSensitivity = 1.5f;
    public float steerSpeed = 5f;
    // Influence multipliers for additional accelerometer axes
    public float yTiltInfluence = 0.2f;
    public float zTiltInfluence = 0.0f;
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
    public float maxDamagePullAngle = 5f;
    public float damageWobbleSpeed = 15f;
    public float damageWobbleAmount = 2f;
    float damageBias = 0f;
    float damageWobbleTimer = 0f;

    private Vector3 steerHandleLeft = new Vector3(-42f, -20f, 28f);
    private Vector3 steerHandleNeutral = new Vector3(0.733f, 0.137f, 21.114f);
    private Vector3 steerHandleRight = new Vector3(42f, 20f, 28f);
    float fullSteerX = 4.5f;
    float zeroSteerX = 5.3f;
    float animSteer;
    private float sideCheckTimer = 0f;

    [Header("Engine Sound")]
    public float minPitch = 0f;
    public float maxPitch = 2f;
    private AudioSource rickshawAudioSource;

    [Header("References")]
    public Animator rickshawManAnimator;
    public Transform rickshawSteerHandle;
    public Transform turnCheck;
    public LayerMask obstacleLayer;
    private CameraScript cameraScript;
    private RickshawHealth healthScript;
    private UIScript uiScript;
    private GameManagerScript gameManager;
    [HideInInspector] public bool gameStarted = false;

    private Rigidbody rb;
    private bool isBraking = false;
    public float horizontalInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        cameraScript = GetComponent<CameraScript>();
        healthScript = GetComponent<RickshawHealth>();
        gameManager = GameManagerScript.Instance;
        rickshawAudioSource = GetComponent<AudioSource>();
        uiScript = GameObject.FindWithTag("GameController").GetComponent<UIScript>();
        baseSpeed = startSpeed;
    }

    void Update()
    {
        if(!gameStarted)   return;

        HandleSteeringInput();
        HandleBraking();
        UpdateSteerHandle(animSteer);
        HandleEngineSound();
        UpdateSpeedMeterUI();
        sideCheckTimer += Time.deltaTime;
        damageWobbleTimer += Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (!gameStarted) return;
        ApplyForwardMovement();
        Steer();
    }
    void Steer()
    {
        //input
        float steerInput = Mathf.Clamp(horizontalInput, -1f, 1f);
        float posX = transform.position.x;
        float absX = Mathf.Abs(posX);

        //position Check
        if(posX < -5.6) transform.position = new Vector3(-5.6f, transform.position.y, transform.position.z);
        if(posX > 5.6) transform.position = new Vector3(5.6f, transform.position.y, transform.position.z);

        //side Check
        if (sideCheckTimer >= sideCheckInterval)
        {
            rightBlocked = Physics.Raycast(turnCheck.position, turnCheck.right, turnCheckDistance, obstacleLayer);
            leftBlocked = Physics.Raycast(turnCheck.position, -turnCheck.right, turnCheckDistance, obstacleLayer);
            sideCheckTimer = 0f;
        }

        //Block steering toward obstacle
        if (steerInput > 0f && rightBlocked) steerInput = 0f;
        if (steerInput < 0f && leftBlocked) steerInput = 0f;

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


        //ROTATION
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
            float brakingSpeed = baseSpeed - brakeDeceleration;
            currentSpeed = Mathf.Lerp(currentSpeed, brakingSpeed, brakeForce * Time.fixedDeltaTime);
        }
        else
        {
            float targetSpeed = baseSpeed;
            if (InputManager.Instance.boostPressed) targetSpeed += boostSpeed;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        }

        Vector3 normalForward = transform.forward * currentSpeed;
        rb.linearVelocity = new Vector3(normalForward.x, rb.linearVelocity.y, normalForward.z);
    }

    void HandleBraking()
    {
        if (isBrakeFailed)
        {
            if (isBraking) isBraking = false;
            return;
        }

        brakePressed = InputManager.Instance.brakePressed;

        if (!brakePressed)
        {
            if(isBraking) isBraking = false;
            if (brakeMeter > 0f)
            {
                brakeMeter -= meterDecreaseRate * Time.deltaTime;
            }
        }
        else
        {
            if (!isBraking) isBraking = true;
            brakeMeter += meterIncreaseRate * Time.deltaTime;

            if (brakeMeter >= maxBrakeMeter)
            {
                brakeMeter = maxBrakeMeter;
                isBrakeFailed = true;
                isBraking = false;
            }
        }

        uiScript.BrakeMeterUIUpdate(brakeMeter, isBraking, isBrakeFailed);
    }

    void HandleSteeringInput()
    {
        if (gameManager.tiltSteeringControl)
        {
            Vector3 accel = Input.acceleration;
            float combined = accel.x + accel.y * yTiltInfluence + accel.z * zTiltInfluence;
            float targetTilt = combined * tiltSensitivity;
            targetTilt = Mathf.Clamp(targetTilt, -1f, 1f);
            horizontalInput = Mathf.Lerp(horizontalInput, targetTilt, Time.deltaTime * steerSpeed);
        }
        else
        {
            leftPressed = InputManager.Instance.leftPressed;
            rightPressed = InputManager.Instance.rightPressed;
            if (leftPressed)
                horizontalInput = Mathf.Lerp(horizontalInput, -1f, Time.deltaTime * steerSpeed);
            else if (rightPressed)
                horizontalInput = Mathf.Lerp(horizontalInput, 1f, Time.deltaTime * steerSpeed);
            else
                horizontalInput = Mathf.Lerp(horizontalInput, 0f, Time.deltaTime * steerSpeed);
        }
#if UNITY_EDITOR //KeyboardControl
        /*
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
        */
#endif
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

    void HandleEngineSound()
    {
        if (rickshawAudioSource == null) return;

        float speedPercentage = Mathf.InverseLerp(0f, maxSpeed, currentSpeed);
        rickshawAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, speedPercentage);
        cameraScript.SetSpeedMultiplier(speedPercentage);
    }

    void UpdateSpeedMeterUI()
    {
        if (uiScript != null)
        {
            uiScript.UpdateSpeedUI((int)currentSpeed);
        }
    }
}
