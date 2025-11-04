using UnityEngine;

public class NPCVehicleController : MonoBehaviour
{
    [System.Serializable]
    public enum Axel { Front, Rear }

    [System.Serializable]
    public struct Wheel
    {
        public GameObject wheelModel;
        public WheelCollider wheelCollider;
        public Axel axel;
    }

    [Header("Driving Settings")]
    public float minSpeed = 5f;           // Minimum random driving speed
    public float maxSpeed = 15f;          // Maximum random driving speed
    public float steerAngle = 30f;        // Max steering angle
    public float stopDistance = 3f;       // Distance to stop near target
    public float turnSmoothness = 5f;     // Smoother steering rotation
    public float brakeForce = 2000f;      // Brake power
    public Transform driveTarget;

    [Header("Wheels Setup")]
    public Wheel[] wheels;

    private float currentSpeed;           // Chosen random speed
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Randomize this car's driving speed once
        currentSpeed = Random.Range(minSpeed, maxSpeed);
    }

    void FixedUpdate()
    {
        if (driveTarget == null) return;

        DriveTowardsTarget();
        UpdateWheelModels();
    }

    void DriveTowardsTarget()
    {
        Vector3 dirToTarget = driveTarget.position - transform.position;
        float distance = dirToTarget.magnitude;

        // Stop near target
        if (distance <= stopDistance)
        {
            ApplyBrakes(true);
            return;
        }

        ApplyBrakes(false);

        // Local direction to target for steering
        Vector3 localTarget = transform.InverseTransformPoint(driveTarget.position);
        float steerInput = (localTarget.x / localTarget.magnitude);
        float targetSteerAngle = steerInput * steerAngle;

        // Apply steering only to front wheels
        foreach (var wheel in wheels)
        {
            if (wheel.axel == Axel.Front)
            {
                wheel.wheelCollider.steerAngle = Mathf.Lerp(
                    wheel.wheelCollider.steerAngle,
                    targetSteerAngle,
                    Time.deltaTime * turnSmoothness
                );
            }

            // Apply torque to rear wheels
            if (wheel.axel == Axel.Rear)
            {
                wheel.wheelCollider.motorTorque = currentSpeed * 50f; // Adjust multiplier if needed
            }
        }
    }

    void ApplyBrakes(bool apply)
    {
        float brake = apply ? brakeForce : 0f;

        foreach (var wheel in wheels)
        {
            wheel.wheelCollider.brakeTorque = brake;
        }
    }

    void UpdateWheelModels()
    {
        foreach (var wheel in wheels)
        {
            if (wheel.wheelCollider && wheel.wheelModel)
            {
                Vector3 pos;
                Quaternion rot;
                wheel.wheelCollider.GetWorldPose(out pos, out rot);
                wheel.wheelModel.transform.position = pos;
                wheel.wheelModel.transform.rotation = rot;
            }
        }
    }
}
