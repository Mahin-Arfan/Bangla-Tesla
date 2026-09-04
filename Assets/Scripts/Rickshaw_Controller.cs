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
    [HideInInspector] public bool isBrakeFailed = false;
    [HideInInspector] public float brakeMeter = 0f;
    private float maxBrakeMeter = 100f;
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

    [Header("Tilt Settings")]
    public float tiltingTendency = 1.5f;
    public float tiltMeter = 0f;
    public float maxTiltMeter = 100f;
    public float maxTiltAngle = 35f;
    public float baseRecoveryRate = 10f;
    public float recoveryAcceleration = 150f;
    private float currentRecoveryRate;
    public float tiltSlamShakeIntensity = 0.15f;
    public Transform rickshawVisualModel;
    private float currentVisualTiltZ = 0f;
    private float previousSteerInput = 0f;
    private bool isFlipped = false;
    private bool wasHighlyTilted = false;

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

    [Header("Brake Sound")]
    public float brakeMinPitch = 0f;
    public float brakeMaxPitch = 2f;
    public float brakeVolume = 0.5f;
    private AudioSource brakeSound;
    private AudioSource brakeSoundSource;

    [Header("References")]
    public Animator rickshawManAnimator;
    public Transform rickshawSteerHandle;
    public Transform turnCheck;
    public LayerMask obstacleLayer;

    //Internal References
    private CameraScript cameraScript;
    private RickshawHealth healthScript;
    private UIScript uiScript;
    private GameManagerScript gameManager;
    [HideInInspector] public bool gameStarted = false;
    private Rigidbody rb;
    [HideInInspector] public bool isBraking = false;
    [HideInInspector] public float horizontalInput;
    [HideInInspector] public bool forHire = true;
    [HideInInspector] public NPCCharacterScript passengerCharacterScript;

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

        float deltaFromForward = Mathf.Abs(Mathf.DeltaAngle(180f, rb.rotation.eulerAngles.y));

        HandleInstability(finalSteer);

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
            if (brakeSound != null) ReleaseBrakeAudio();
            return;
        }

        brakePressed = InputManager.Instance.brakePressed;

        if (!brakePressed)
        {
            if(isBraking) isBraking = false;
            ReleaseBrakeAudio();
            if (brakeMeter > 0f)
            {
                brakeMeter -= meterDecreaseRate * Time.deltaTime;
            }
        }
        else
        {
            if (!isBraking) isBraking = true;
            brakeMeter += meterIncreaseRate * Time.deltaTime;
            HandleBrakeSound(brakeMeter, maxBrakeMeter);
            if (brakeMeter >= maxBrakeMeter)
            {
                brakeMeter = maxBrakeMeter;
                isBrakeFailed = true;
                isBraking = false;
                brakeSoundSource = AudioManager.Instance.RequestGameAudioClip(AudioManager.Instance.brakeSnapSoundClip, transform, 0.8f, 1f, 0f, false);
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
    }

    void HandleInstability(float steerInput)
    {
        if (isFlipped)
        {
            ApplyVisualTilt(steerInput);
            return;
        }

        float steerDelta = Mathf.Abs(steerInput - previousSteerInput);
        float hardTurnFactor = Mathf.Abs(steerInput) > 0.8f ? Mathf.Abs(steerInput) : 0f;
        float speedFactor = currentSpeed / maxSpeed;

        // Base meter increase
        float meterIncrease = (steerDelta * 60f + hardTurnFactor * 15f) * speedFactor * tiltingTendency * Time.deltaTime;

        // Braking reduces the tilting rate by damping the increase
        if (isBraking)
        {
            meterIncrease *= 0.3f;
        }

        if (Mathf.Abs(steerInput) > 0.1f)
        {
            tiltMeter += meterIncrease;
            currentRecoveryRate = baseRecoveryRate; // Reset recovery rate while steering
        }
        else
        {
            // Snappy recovery: Rate increases over time to simulate gravity
            currentRecoveryRate += recoveryAcceleration * Time.deltaTime;
            tiltMeter -= currentRecoveryRate * Time.deltaTime;
        }

        tiltMeter = Mathf.Clamp(tiltMeter, 0f, maxTiltMeter);
        previousSteerInput = steerInput;

        ApplyVisualTilt(steerInput);
    }

    void ApplyVisualTilt(float steerInput)
    {
        // Determine target rotation (if steerInput is 0, maintain previous direction for the fall)
        float activeSteer = steerInput != 0f ? steerInput : previousSteerInput;
        float targetTiltZ = (tiltMeter / maxTiltMeter) * maxTiltAngle * Mathf.Sign(activeSteer);

        // Lerp visuals
        currentVisualTiltZ = Mathf.Lerp(currentVisualTiltZ, targetTiltZ, Time.deltaTime * 12f);
        float absTiltZ = Mathf.Abs(currentVisualTiltZ);

        // Map Y position based on Z rotation thresholds
        float targetY = 0.1f;
        if (absTiltZ <= 15f)
            targetY = Mathf.Lerp(0.1f, 0.2f, absTiltZ / 15f);
        else
            targetY = Mathf.Lerp(0.2f, 0.37f, (absTiltZ - 15f) / (maxTiltAngle - 15f));

        if (rickshawVisualModel != null)
        {
            rickshawVisualModel.localPosition = new Vector3(rickshawVisualModel.localPosition.x, targetY, rickshawVisualModel.localPosition.z);
            rickshawVisualModel.localRotation = Quaternion.Euler(rickshawVisualModel.localRotation.eulerAngles.x, rickshawVisualModel.localRotation.eulerAngles.y, currentVisualTiltZ);
        }

        // --- Slam Detection ---
        if (absTiltZ > 15f)
        {
            wasHighlyTilted = true; // Rickshaw is significantly in the air
        }
        else if (absTiltZ < 1f && wasHighlyTilted)
        {
            // Rickshaw has slammed back down to ~0
            if (cameraScript != null) cameraScript.TriggerShake(tiltSlamShakeIntensity);
            wasHighlyTilted = false;
        }

        // Critical Flip Check
        if (absTiltZ >= (maxTiltAngle - 0.5f) && !isFlipped) TriggerFlip();
    }

    void TriggerFlip()
    {
        isFlipped = true;
        currentSpeed = 0f;
        if(rickshawVisualModel.rotation.z > 0f)
        {
            rickshawManAnimator.SetTrigger("Flipped_Left");
        }
        else
        {
            rickshawManAnimator.SetTrigger("Flipped_Right");
        }
        StartCoroutine(FlipSlamRoutine());
        //rickshawManAnimator.SetTrigger("rickshawFlip");

        // Transition to Game Over (You may want to use an Animation Event or Coroutine here)
        
    }

    private System.Collections.IEnumerator FlipSlamRoutine()
    {
        if (rickshawVisualModel == null) yield break;

        // Capture the side we were tilting towards when the flip occurred
        float flipDirection = Mathf.Sign(currentVisualTiltZ);
        float targetZ = 80f * flipDirection;
        float targetY = 0.46f;

        Vector3 startPos = rickshawVisualModel.localPosition;
        float startZ = currentVisualTiltZ;

        float t = 0f;
        float slamSpeed = 4f; // Adjust this higher for a faster slam, lower for slower

        while (t < 1f)
        {
            t += Time.deltaTime * slamSpeed;

            // Use an Ease-In curve (t * t) to make it feel like it's accelerating into the ground
            float easeT = Mathf.Clamp01(t * t);

            float newY = Mathf.Lerp(startPos.y, targetY, easeT);
            currentVisualTiltZ = Mathf.LerpAngle(startZ, targetZ, easeT);

            rickshawVisualModel.localPosition = new Vector3(startPos.x, newY, startPos.z);
            rickshawVisualModel.localRotation = Quaternion.Euler(
                rickshawVisualModel.localRotation.eulerAngles.x,
                rickshawVisualModel.localRotation.eulerAngles.y,
                currentVisualTiltZ
            );

            yield return null;
        }

        // Ensure it sets perfectly to the target values at the very end
        rickshawVisualModel.localPosition = new Vector3(startPos.x, targetY, startPos.z);
        rickshawVisualModel.localRotation = Quaternion.Euler(
            rickshawVisualModel.localRotation.eulerAngles.x,
            rickshawVisualModel.localRotation.eulerAngles.y,
            targetZ
        );

        if (cameraScript != null) cameraScript.TriggerShake(0.2f);
        AudioManager.Instance.PlayCrash(transform.position);
        AudioManager.Instance.RequestGameAudioClip(AudioManager.Instance.batteryEmptyClip, transform, 1f, 1f, 0f, false);
        healthScript.Die();
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

    void HandleBrakeSound(float brakeMeter, float maxBrakeMeter)
    {
        if (brakeSound == null && AudioManager.Instance != null)
        {
            brakeSound = AudioManager.Instance.RequestGameAudioClip(AudioManager.Instance.brakeSoundClip, transform, brakeVolume, 1f, 0f, true);
        }
        if (brakeSound.isPlaying)
        {
            float brakePercentage = Mathf.InverseLerp(0f, maxBrakeMeter, brakeMeter);
            brakeSound.pitch = Mathf.Lerp(brakeMinPitch, brakeMaxPitch, brakePercentage);
        }
    }
    void ReleaseBrakeAudio()
    {
        if (brakeSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.ReturnAudioSource(brakeSound);
            brakeSound = null;
        }
    }

    void UpdateSpeedMeterUI()
    {
        if (uiScript != null)
        {
            uiScript.UpdateSpeedUI((int)currentSpeed);
        }
    }
}
