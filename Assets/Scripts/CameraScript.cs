using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [Header("Settings")]
    public Transform cam;
    private Vector3 velocity = Vector3.zero;
    public float smoothTime = 0.1f; //(lower = snappier, higher = looser)

    [Header("Position Settings")]
    public Vector3 offset = new Vector3(5f, 4.2f, 3f);
    public float followSpeed = 10f;

    [Header("Rotation Settings")]
    public Vector3 cameraRotationStraight = new Vector3(40f, -180f, 0f);
    public float yOffsetTurnAmount = 3f;
    public float rotationSpeed = 5f;

    [Header("Shake Settings")]
    public float shakeIntensity = 0.5f;
    public float shakeDecay = 5f;

    private float turnInput = 0f;
    private float currentShake = 0f;
    private Transform playerTransform;

    void Start()
    {
        if (cam == null) Debug.LogError("Camera Transform not assigned in CameraScript.");
        if (playerTransform  == null) playerTransform = transform;
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
        // SmoothDamp is much smoother than Lerp for following Physics objects
        Vector3 smoothedPosition = Vector3.SmoothDamp(cam.transform.position, targetPosition, ref velocity, smoothTime);
        if (currentShake > 0)
        {
            smoothedPosition += Random.insideUnitSphere * currentShake;
        }
        cam.transform.position = smoothedPosition;
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
