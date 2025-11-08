using UnityEngine;

public class NPCVehicleController : MonoBehaviour
{
    public bool reverseMechanics = false;
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
    public float vehicleSpeed;            // Current speed of the vehicle
    public float minSpeed = 5f;           // Minimum random driving speed
    public float maxSpeed = 15f;          // Maximum random driving speed
    public float acceleration = 25f;        // Acceleration rate
    public float accelerationSmoothness = 1f; // Smoother acceleration
    public float steerAngle = 30f;        // Max steering angle
    public float stopDistance = 3f;       // Distance to stop near target
    public float turnSmoothness = 5f;     // Smoother steering rotation
    public float brakeForce = 2000f;      // Brake power
    public float stopDistanceMultiplier = 1f;
    public Transform driveTarget;

    [Header("Wheels Setup")]
    public Wheel[] wheels;

    private Rigidbody rb;
    private float currentAcceleration = 0f;
    private float speedLimit = 15f;
    private Vector3 dirToTarget;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationZ;

        // Randomize this car's driving speed once
        speedLimit = Random.Range(minSpeed, maxSpeed);
        if (reverseMechanics) { acceleration = -acceleration; }
    }

    void FixedUpdate()
    {
        if (driveTarget == null) return;

        DriveTowardsTarget();
        UpdateWheelModels();
    }

    void DriveTowardsTarget()
    {
        dirToTarget = driveTarget.position - transform.position;
        float distance = dirToTarget.magnitude;
        float dotDirection = Vector3.Dot(transform.forward, dirToTarget.normalized);

        stopDistance = Mathf.Clamp(vehicleSpeed * 0.4f * stopDistanceMultiplier, 4f, 20f);

        // Stop near target
        bool shouldBrake = distance <= stopDistance ||
                   (reverseMechanics ? dotDirection > 0 : dotDirection < 0);

        if (shouldBrake)
        {
            currentAcceleration = 0f;
            ApplyBrakes(true);
            return;
        }

        ApplyBrakes(false);

        //Steering
        Vector3 localTarget = transform.InverseTransformPoint(driveTarget.position);
        float steerInput = (localTarget.x / localTarget.magnitude);
        float targetSteerAngle = steerInput * steerAngle;

        // Get current actual speed (in m/s)
        vehicleSpeed = rb.linearVelocity.magnitude;

        //Smooth acceleration
        currentAcceleration = Mathf.Lerp(currentAcceleration, acceleration, accelerationSmoothness * Time.deltaTime);

        // If under max speed, apply torque
        if (reverseMechanics)
        {
            speedLimit = -speedLimit;
        }
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

            if (wheel.axel == Axel.Rear)
            {
                if (vehicleSpeed < speedLimit)
                {
                    // Still below max speed — apply forward torque
                    wheel.wheelCollider.motorTorque = currentAcceleration * 50f;
                }
                else
                {
                    // At or above max speed — cut power
                    wheel.wheelCollider.motorTorque = 0f;
                }
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
