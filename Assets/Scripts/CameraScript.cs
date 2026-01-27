using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [Header("Settings")]
    public Transform cam;
    private Transform playerTransform;

    [Header("Smooth Settings")]
    public float smoothTime = 0.1f; // Lower = Snappier, Higher = Looser
    public Vector3 offset = new Vector3(0, 3.5f, 2.5f);

    [Header("Rotation Settings")]
    public Vector3 cameraRotationStraight = new Vector3(30f, -180f, 0f);
    public float yOffsetTurnAmount = -30f;
    public float rotationSpeed = 5f;

    [Header("Shake Settings")]
    public float shakeIntensity = 0.25f;
    public float shakeDecay = 1f;
    [Tooltip("How fast the camera vibrates.")]
    public float shakeFrequency = 25f;

    // Internal Variables
    private Vector3 velocity = Vector3.zero;
    private float turnInput = 0f;
    private float currentShake = 0f;
    private Vector3 internalPosition;
    private float seedX;    // Random seeds to make X and Y shake differently
    private float seedY;

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
        seedX = Random.Range(0f, 100f); //Initialize seeds for Perlin Noise (random start point)
        seedY = Random.Range(0f, 100f); //Initialize seeds for Perlin Noise (random start point)
        internalPosition = playerTransform.position + offset;
        cam.position = internalPosition;
        cam.rotation = Quaternion.Euler(cameraRotationStraight);
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

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

        if (currentShake > 0)
        {
            float xNoise = Mathf.PerlinNoise(seedX + Time.time * shakeFrequency, 0f) * 2f - 1f;
            float yNoise = Mathf.PerlinNoise(0f, seedY + Time.time * shakeFrequency) * 2f - 1f;

            Vector3 shakeOffset = new Vector3(xNoise, yNoise, 0f) * currentShake;
            finalPosition += shakeOffset;
        }
        cam.position = finalPosition;
    }

    void HandleShakeDecay()
    {
        if (currentShake > 0)
        {
            currentShake -= Time.deltaTime * shakeDecay;
            if (currentShake < 0) currentShake = 0f;
        }
    }

    public void TriggerShake()
    {
        currentShake = shakeIntensity;
    }

    public void SetSteerInput(float input)
    {
        turnInput = input; // -1 to 1
    }
}
