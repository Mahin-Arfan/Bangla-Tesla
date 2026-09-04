using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [Header("Smooth Settings")]
    public float smoothTime = 0.1f; // Lower = Snappier, Higher = Looser
    public Vector3 offset = new Vector3(0, 3.5f, 2.5f);

    [Header("Rotation Settings")]
    public Vector3 cameraRotationStraight = new Vector3(30f, -180f, 0f);
    public float yOffsetTurnAmount = -30f;
    public float rotationSpeed = 5f;

    [Header("Hit Shake Settings")]
    public float hitShakeIntensity = 0.25f;
    public float shakeDecay = 1f;
    [Tooltip("How fast the camera vibrates.")]
    public float shakeFrequency = 25f;

    [Header("Driving Shake Settings")]
    [Tooltip("The maximum shake intensity when driving at top speed.")]
    public float maxDrivingShake = 0.05f;
    [Tooltip("How fast the camera vibrates from the road.")]
    public float drivingShakeFrequency = 15f;

    [Header("DeadCamera Positions")]
    public Vector3 deadCameraPositionOffset;
    public Vector3 deadCameraRotation;
    public float deadCamSpeed = 2f;

    [Header("References")]
    public Transform cam;
    private Transform playerTransform;

    // Internal Variables
    private Vector3 velocity = Vector3.zero;
    private float turnInput = 0f;
    private float currentHitShake = 0f;
    private float currentSpeedMultiplier = 0f;
    private Vector3 internalPosition;
    private float seedX;    // Random seeds to make X and Y shake differently
    private float seedY;
    private RickshawHealth rickshawHealth;

    void Start()
    {
        if (cam == null) cam = Camera.main.transform;
        if (playerTransform == null)
        {
            if (transform.CompareTag("Player") || transform.GetComponent<Rigidbody>() != null)
            {
                playerTransform = transform;
            }
            else
            {
                Debug.LogError("⚠️ CAMERA SCRIPT: Please assign 'Player Transform' in the Inspector!");
                return;
            }
        }
        rickshawHealth = GetComponent<RickshawHealth>();
        seedX = Random.Range(0f, 100f);
        seedY = Random.Range(0f, 100f);
        internalPosition = playerTransform.position + offset;
    }

    void LateUpdate()
    {
        if (playerTransform == null || !GameManagerScript.Instance.gameStarted) return;
        if (rickshawHealth.isDead)
        {
            HandleDeadCamera();
            return;
        }
        FollowPlayer();
        UpdateCameraRotation(); 
        HandleShakeDecay();
    }

    void UpdateCameraRotation()
    {
        Vector3 targetRotation = cameraRotationStraight;
        targetRotation.y -= turnInput * yOffsetTurnAmount;
        cam.rotation = Quaternion.Slerp(cam.rotation, Quaternion.Euler(targetRotation), Time.deltaTime * rotationSpeed);
    }

    void FollowPlayer()
    {
        Vector3 targetPosition = playerTransform.position + offset;

        internalPosition = Vector3.SmoothDamp(internalPosition, targetPosition, ref velocity, smoothTime);
        Vector3 finalPosition = internalPosition;

        float activeDrivingShake = maxDrivingShake * currentSpeedMultiplier;
        float totalShake = currentHitShake + activeDrivingShake;

        if (totalShake > 0)
        {
            float activeFreq = currentHitShake > 0 ? shakeFrequency : drivingShakeFrequency;

            float xNoise = Mathf.PerlinNoise(seedX + Time.time * activeFreq, 0f) * 2f - 1f;
            float yNoise = Mathf.PerlinNoise(0f, seedY + Time.time * activeFreq) * 2f - 1f;

            Vector3 shakeOffset = new Vector3(xNoise, yNoise, 0f) * totalShake;
            finalPosition += shakeOffset;
        }
        cam.position = finalPosition;
    }

    void HandleShakeDecay()
    {
        if (currentHitShake > 0)
        {
            currentHitShake -= Time.deltaTime * shakeDecay;
            if (currentHitShake < 0) currentHitShake = 0f;
        }
    }

    void HandleDeadCamera()
    {
        cam.position = Vector3.Lerp(cam.position, deadCameraPositionOffset + playerTransform.position, Time.deltaTime * deadCamSpeed);
        cam.rotation = Quaternion.Slerp(cam.rotation, Quaternion.Euler(deadCameraRotation), Time.deltaTime * deadCamSpeed);
    }

    public void TriggerShake(float shakeIntensity)
    {
        currentHitShake = shakeIntensity;
    }
    public void HitTriggerShake()
    {
        currentHitShake = hitShakeIntensity;
    }

    public void SetSteerInput(float input)
    {
        turnInput = input; // -1 to 1
    }

    public void SetSpeedMultiplier(float normalizedSpeed)
    {
        currentSpeedMultiplier = Mathf.Clamp01(normalizedSpeed);
    }
}
