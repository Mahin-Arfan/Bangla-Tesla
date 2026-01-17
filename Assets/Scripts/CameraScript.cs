using UnityEngine;
using Unity.Cinemachine;

public class CameraScript : MonoBehaviour
{
    [Header("Settings")]
    public CinemachineCamera virtualCamera;
    public float maxTiltAngle = 5f;  // Maximum angle in degrees
    public float smoothTime = 5f;    // How fast it tilts

    private float currentSteerInput = 0f;

    void Update()
    {
        if (virtualCamera == null) return;

        // 1. Calculate target tilt
        float targetTilt = -currentSteerInput * maxTiltAngle;

        // 2. Get the current Lens settings (It's a struct now!)
        LensSettings lens = virtualCamera.Lens;

        // 3. Modify the Dutch (Roll)
        lens.Dutch = Mathf.Lerp(lens.Dutch, targetTilt, Time.deltaTime * smoothTime);

        // 4. Apply the modified Lens settings back to the camera
        virtualCamera.Lens = lens;
    }

    public void SetSteerInput(float input)
    {
        // input should be between -1 (Left) and 1 (Right)
        currentSteerInput = input;
    }
}
