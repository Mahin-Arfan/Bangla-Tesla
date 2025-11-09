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
    public float vehicleSpeed;            // Current speed of the vehicle
    public float minSpeed = 5f;           // Minimum random driving speed
    public float maxSpeed = 15f;          // Maximum random driving speed
    public float acceleration = 25f;        // Acceleration rate
    public float accelerationSmoothness = 1f; // Smoother acceleration
    public float steerAngle = 30f;        // Max steering angle
    public float turnSmoothness = 5f;     // Smoother steering rotation
    public float brakeForce = 2000f;      // Brake power
    public float stopDistanceMultiplier = 1f;
    public Transform driveTarget;
    public bool reverseMechanics = false;

    [Header("Checker Settings")]
    public bool tryOvertake = true;
    public Transform overtakeTarget;
    public BoxCollider rightChecker;
    public BoxCollider leftChecker;
    public GameObject frontChecker;
    public GameObject frontRightchecker;
    public GameObject frontLeftChecker;
    public float frontCheckerDistance = 20f;
    public float overTakeSideClearance = 2f;
    public bool isOvertaking = false;
    public LayerMask vehicleLayer;
    private BoxCollider vehicleBodyCollider;

    [Header("Wheels Setup")]
    public Wheel[] wheels;

    private Rigidbody rb;
    private float currentAcceleration = 0f;
    private float speedLimit = 15f;
    private Vector3 dirToTarget;
    private float absSteerAngle = 0f;
    private float stopDistance = 3f;
    private Transform driveToTarget;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationZ;
        vehicleBodyCollider = GetComponentInChildren<BoxCollider>();
        // Randomize this car's driving speed once
        speedLimit = Random.Range(minSpeed, maxSpeed);
        if (reverseMechanics) { acceleration = -acceleration; }
    }

    void Update()
    {
        UpdateWheelModels();
        ObstacleCheck();
    }

    void FixedUpdate()
    {
        if (driveTarget == null || overtakeTarget == null)
        {
            Debug.LogWarning(transform.name + ": Drive target or overtake target not assigned.");
            return;
        }
        DriveTowardsTarget();
    }

    void DriveTowardsTarget()
    {
        stopDistance = Mathf.Clamp(vehicleSpeed * 0.4f * stopDistanceMultiplier, 4f, 20f);

        if (isOvertaking)
        {
            dirToTarget = overtakeTarget.position - transform.position;
            driveToTarget = overtakeTarget;
            float distance = dirToTarget.magnitude;
            float dotDirection = Vector3.Dot(transform.forward, dirToTarget.normalized);
            if (dotDirection < 0 || distance <= 2f)
            {
                isOvertaking = false;
                driveToTarget = driveTarget;
            }
        }
        else
        {
            dirToTarget = driveTarget.position - transform.position;
            driveToTarget = driveTarget;
            float distance = dirToTarget.magnitude;
            float dotDirection = Vector3.Dot(transform.forward, dirToTarget.normalized);
            // Stop near target
            bool shouldBrake = distance <= stopDistance ||
                       (reverseMechanics ? dotDirection > 0 : dotDirection < 0);
            if (shouldBrake)
            {
                currentAcceleration = 0f;
                ApplyBrakes(true);
                return;
            }
        }

        ApplyBrakes(false);

        //Steering
        Vector3 localTarget = transform.InverseTransformPoint(driveToTarget.position);
        float steerInput = (localTarget.x / localTarget.magnitude);
        float targetSteerAngle = steerInput * steerAngle;

        // Get current actual speed (in m/s)
        vehicleSpeed = rb.linearVelocity.magnitude;

        // --- Adjust speed limit based on steer angle ---
        float adjustedSpeedLimit = speedLimit; // Default
        float adjustedAcceleration = acceleration;
        if (absSteerAngle > 10f)
        {
            // Smoothly reduce from minSpeed at 10° → minSpeed/2 at 30°
            float t = Mathf.InverseLerp(10f, 30f, absSteerAngle);
            adjustedSpeedLimit = Mathf.Lerp(minSpeed, minSpeed / 2f, t);
            adjustedAcceleration = Mathf.Lerp(acceleration, acceleration/2f, t);
        }
        //Smooth acceleration
        currentAcceleration = Mathf.Lerp(currentAcceleration, adjustedAcceleration, accelerationSmoothness * Time.deltaTime);

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
                absSteerAngle = Mathf.Abs(wheel.wheelCollider.steerAngle);
            }

            if (wheel.axel == Axel.Rear)
            {
                if (vehicleSpeed < adjustedSpeedLimit)
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

    void ObstacleCheck()
    {
        RaycastHit hit;
        if (Physics.Raycast(frontChecker.transform.position, frontChecker.transform.forward, out hit, frontCheckerDistance, vehicleLayer))
        {
            // Obstacle detected in front
            if (hit.distance > stopDistance && tryOvertake && !isOvertaking)
            {
                Vector3 hitVehicleSize = GetColliderWorldSize(hit.collider);
                float sideOverTakePosition = hitVehicleSize.x/2 + rightChecker.size.x / 2 + overTakeSideClearance;
                Vector3 rightOvertakePos = new Vector3(hit.point.x + sideOverTakePosition, vehicleBodyCollider.transform.position.y, hit.point.z);
                Vector3 leftOvertakePos = new Vector3(hit.point.x - sideOverTakePosition, vehicleBodyCollider.transform.position.y, hit.point.z);
                TryOvertake(rightOvertakePos, leftOvertakePos);
                Debug.Log(hit.transform.name + " detected, attempting overtake. OverTakePosition:" + overtakeTarget.position);
            }
            else
            {
                ApplyBrakes(true);
            }
            return;
        }
        if (Physics.Raycast(frontRightchecker.transform.position, frontRightchecker.transform.forward, out hit, frontCheckerDistance, vehicleLayer))
        {
            // Obstacle detected in front right
            if (hit.distance > stopDistance && tryOvertake && !isOvertaking)
            {
                Vector3 hitVehicleSize = GetColliderWorldSize(hit.collider);
                float sideOverTakePosition = hitVehicleSize.x / 2 + rightChecker.size.x / 2 + overTakeSideClearance;
                Vector3 rightOvertakePos = new Vector3(hit.point.x + sideOverTakePosition, vehicleBodyCollider.transform.position.y, hit.point.z);
                Vector3 leftOvertakePos = new Vector3(hit.point.x - sideOverTakePosition, vehicleBodyCollider.transform.position.y, hit.point.z);
                TryOvertake(rightOvertakePos, leftOvertakePos);
                Debug.Log(hit.transform.name + " detected, attempting overtake. OverTakePosition:" + overtakeTarget.position);
            }
            else
            {
                ApplyBrakes(true);
            }
            return;
        }
        if (Physics.Raycast(frontLeftChecker.transform.position, frontLeftChecker.transform.forward, out hit, frontCheckerDistance, vehicleLayer))
        {
            // Obstacle detected in front left
            if (hit.distance > stopDistance && tryOvertake && !isOvertaking)
            {
                Vector3 hitVehicleSize = GetColliderWorldSize(hit.collider);
                float sideOverTakePosition = hitVehicleSize.x / 2 + rightChecker.size.x / 2 + overTakeSideClearance;
                Vector3 rightOvertakePos = new Vector3(hit.point.x + sideOverTakePosition, vehicleBodyCollider.transform.position.y, hit.point.z);
                Vector3 leftOvertakePos = new Vector3(hit.point.x - sideOverTakePosition, vehicleBodyCollider.transform.position.y, hit.point.z);
                TryOvertake(rightOvertakePos, leftOvertakePos);
                Debug.Log(hit.transform.name + " detected, attempting overtake. OverTakePosition:" + overtakeTarget.position);
            }
            else
            {
                ApplyBrakes(true);
            }
            return;
        }
    }

    void TryOvertake(Vector3 rightSideOverTakePosition, Vector3 leftSideOverTakePosition)
    {
        // Check right side first
        rightChecker.enabled = true;
        leftChecker.enabled = true;
        if (!Physics.CheckBox(rightChecker.transform.position, rightChecker.size / 2, rightChecker.transform.rotation))
        {
            // Right side is clear
            if (!Physics.CheckBox(rightSideOverTakePosition, vehicleBodyCollider.size / 2, Quaternion.identity))
            {
                // Safe to overtake on the right
                overtakeTarget.position = rightSideOverTakePosition;
                isOvertaking = true;
            }
        }
        else if (!Physics.CheckBox(leftChecker.transform.position, leftChecker.size / 2, leftChecker.transform.rotation))
        {
            // Right side is clear
            if (!Physics.CheckBox(leftSideOverTakePosition, vehicleBodyCollider.size / 2, Quaternion.identity))
            {
                // Safe to overtake on the right
                overtakeTarget.position = leftSideOverTakePosition;
                isOvertaking = true;
            }
        }
        rightChecker.enabled = false;
        leftChecker.enabled = false;
    }

    // Helper: returns an approximate world-space size of the collider.
    Vector3 GetColliderWorldSize(Collider col)
    {
        if (col == null) return Vector3.zero;
        // Simple, general-case: axis-aligned world-space bounds
        Vector3 worldBoundsSize = col.bounds.size;
        // If you specifically want exact BoxCollider dimensions in world space:
        if (col is BoxCollider box)
        {
            // box.size is local; scale by lossyScale to get world size
            Vector3 worldBoxSize = Vector3.Scale(box.size, box.transform.lossyScale);
            return worldBoxSize;
        }
        return worldBoundsSize;
    }

    void OnDrawGizmos()
    {
        // Only show when the game is running or in edit mode
        if (vehicleBodyCollider == null) return;

        // Set gizmo color (red if blocked, green if clear)
        Gizmos.color = Color.green;

        // Optional: visualize blocked state if you’re checking it in Update
#if UNITY_EDITOR
        bool isBlocked = Physics.CheckBox(
            overtakeTarget.position,
            vehicleBodyCollider.size / 2,
            Quaternion.identity
        );
        Gizmos.color = isBlocked ? Color.red : Color.green;
#endif

        // Draw the wireframe box in Scene view
        Gizmos.matrix = Matrix4x4.TRS(overtakeTarget.position, Quaternion.identity, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, vehicleBodyCollider.size);
    }
}
