using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [Header("Settings")]
    public Transform cam;

    [Header("Position Settings")]
    public Vector3 offset = new Vector3(5f, 4.2f, 3f);
    public float followSpeed = 10f;

    [Header("Rotation Settings")]
    public Vector3 cameraRotationStraight = new Vector3(40f, -180f, 0f);
    public float yOffsetTurnAmount = 3f;
    public float rotationSpeed = 5f;

    private float turnInput = 0f;
    private Transform playerTransform;

    void Start()
    {
        if (cam == null) cam = transform;
        if(playerTransform  == null) playerTransform = transform;
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        FollowPlayer();
        UpdateCameraRotation();
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
        cam.transform.position = Vector3.Lerp(cam.transform.position, targetPosition, Time.deltaTime * followSpeed);
    }

    public void SetSteerInput(float input)
    {
        turnInput = input; // -1 to 1
    }
}
