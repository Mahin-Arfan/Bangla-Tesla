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
    public float minSpeed = 5f;                // min random speed (m/s)
    public float maxSpeed = 15f;               // max random speed (m/s)
    public float acceleration = 25f;           // base acceleration amount (units used as torque multiplier)
    public float accelerationSmoothness = 5f;  // lerp speed for acceleration changes
    public float steerAngle = 30f;             // maximum steering angle (deg)
    public float turnSmoothness = 5f;          // lerp speed for steering
    public float brakeForce = 2000f;           // brake torque
    public float stopDistanceMultiplier = 1f;  // multiplier used to compute stopDistance from speed

    [Header("Obstacle & Overtake")]
    public float frontCheckerDistance = 20f;   // how far the rays check
    public float overTakeSideClearance = 2f;   // extra gap when calculating overtake pos
    public float sideCheckDistance = 3f;
    public bool tryOvertake = true;
    public float checkInterval = 0.2f;         // how often to run obstacle checks (seconds)

    [Header("Random Stops")]
    public bool randomStops = false;
    public float stopPositionX = 0f; // [Range -4 to -3]
    public bool stopping = false;
    public float stopDuration = 5f;
    private float stopTimer = 0f;
    [Range(0f, 100f)]
    public float stopChance = 0;
    public bool shouldStop = false;

    [Header("Reverse Mechanics")]
    public bool reverseMechanics = false;      // if true, the vehicle drives "backwards"

    // runtime
    private Rigidbody rb;
    private float speedLimit;                  // randomly chosen top speed (m/s)
    private float vehicleSpeed;                // current speed (m/s)
    private float currentAcceleration;         // smoothed acceleration value
    private bool isOvertaking = false;
    private GameObject obstacle;               // current obstacle GameObject or null
    private float obstacleDistance = Mathf.Infinity;
    private Vector3 checkSize;          // checkSize for CheckBox (world-space)
    private float overTakeCheckTimer = 0f;
    private float lastCheckTime = 0f;
    private Transform currentDriveTarget;
    private float stopDistance = 5f;
    private Vector3 flatForward;
    private float driveToTargetDistance = 0f;
    public float driveToTargetDot = 0f;

    [Header("References")]
    public Transform temp;                     // optional helper transform (can be null)
    public Transform driveTarget;              // main drive target (set externally)
    public Transform overtakeTarget;           // target used while overtaking
    public Transform frontChecker;             // center ray origin
    public Transform frontRightChecker;        // right ray origin
    public Transform frontLeftChecker;         // left ray origin
    public BoxCollider vehicleBodyCollider;    // used for sizing overtake checks
    public LayerMask vehicleLayer;             // layer mask for raycasts/CheckBox

    [Header("Wheels Setup")]
    public Wheel[] wheels;


    [Header("Temps")]
    // Gizmo storage
    private Vector3 rightOvertakeGizmoPos;
    private Vector3 leftOvertakeGizmoPos;

    private Vector3 rightSideCheckGizmoPos;
    private Vector3 leftSideCheckGizmoPos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        vehicleBodyCollider = GetComponentInChildren<BoxCollider>();
        // Randomize driving speed
        speedLimit = Random.Range(minSpeed, maxSpeed);
        if (reverseMechanics) 
        { 
            acceleration = -acceleration;
            speedLimit = -speedLimit;
            minSpeed = -minSpeed;
            maxSpeed = -maxSpeed;
        }
        if(vehicleBodyCollider == null)
        {
            Debug.LogError(transform.name + ": Vehicle body collider not found!");
        }
        else
        {
            Vector3 fullSize = Vector3.Scale(vehicleBodyCollider.size, vehicleBodyCollider.transform.lossyScale);
            // requested size = (size.x + 0.5, 1, size.z*2)
            Vector3 requested = new Vector3(fullSize.x, 1f, fullSize.z * 3f);
            checkSize = requested * 0.5f;
        }
        currentDriveTarget = driveTarget;
        if (randomStops)
        {
            float randomValue = Random.Range(0f, 100f);
            shouldStop = randomValue <= stopChance;
            if(!shouldStop)
            {
                return;
            }
            float randomStopPosX = Random.Range(stopPositionX + 1, stopPositionX - 1);
            if(randomStopPosX < -4f)
            {
                randomStopPosX = -4f;
            }
            driveTarget.position = new Vector3(randomStopPosX, driveTarget.position.y, transform.position.z - 30f);
            stopping = true;
        }
    }

    void Update()
    {
        UpdateWheelModels();
        lastCheckTime += Time.deltaTime;
        overTakeCheckTimer += Time.deltaTime;
        if (lastCheckTime >= checkInterval)
        {
            ObstacleCheck();
            lastCheckTime = 0f;
        }
        if (stopping)
        {
            RandomStop();
        }
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
        Transform target = isOvertaking ? overtakeTarget : driveTarget;
        if (currentDriveTarget != target)
            currentDriveTarget = target;

        // direction & distance
        Vector3 dirToTarget = currentDriveTarget.position - transform.position;
        driveToTargetDistance = dirToTarget.magnitude;
        driveToTargetDot = Vector3.Dot(transform.forward, dirToTarget.normalized);

        if (isOvertaking && ((reverseMechanics ? driveToTargetDot > 0f : driveToTargetDot < 0f) || driveToTargetDistance <= 0.5f))
        {
            isOvertaking = false;
            currentDriveTarget = driveTarget;
            Debug.LogWarning(transform.name + ": Change Drive to target");
        }

        vehicleSpeed = rb.linearVelocity.magnitude;

        stopDistance = Mathf.Clamp(vehicleSpeed * 0.4f * stopDistanceMultiplier, 1.5f, 20f);
        
        // braking if close or target behind (respects reverseMechanics)
        bool shouldBrake = driveToTargetDistance <= stopDistance || (reverseMechanics ? driveToTargetDot > 0f : driveToTargetDot < 0f);
        if (shouldBrake && !isOvertaking)
        {
            currentAcceleration = 0f;
            ApplyBrakes(true);
            Debug.LogWarning($"{name}: Braking - Close to target or target behind");
            return;
        }
        

        // If we detected an obstacle, scale down allowed speed smoothly:
        float desiredSpeedLimit = speedLimit;
        if (obstacle != null && !float.IsInfinity(obstacleDistance))
        {
            // obstacleDistance in meters; map 1 -> minSpeed, frontCheckerDistance -> speedLimit
            float clampedD = Mathf.Clamp(obstacleDistance, 1f, frontCheckerDistance);
            float t = Mathf.InverseLerp(1f, frontCheckerDistance, clampedD);
            desiredSpeedLimit = Mathf.Lerp(minSpeed, speedLimit, t);
        }

        // Steering: local target
        Vector3 localTarget = transform.InverseTransformPoint(currentDriveTarget.position);
        float steerInput = localTarget.magnitude > 0f ? (localTarget.x / localTarget.magnitude) : 0f;
        float targetSteerAngle = steerInput * steerAngle;

        // compute absolute steer from front wheels (we set steer per front wheel below)
        float absSteer = Mathf.Abs(targetSteerAngle);

        // adjust acceleration & speedLimit when steering hard
        float adjustedAcceleration = acceleration;
        float adjustedSpeedLimit = desiredSpeedLimit;

        if (absSteer > 10f)
        {
            float tSteer = Mathf.InverseLerp(10f, 30f, absSteer);
            adjustedSpeedLimit = Mathf.Lerp(minSpeed, desiredSpeedLimit, tSteer);   // closer = lower
            adjustedAcceleration = Mathf.Lerp(acceleration, acceleration * 0.5f, tSteer);
        }

        // smooth acceleration (use fixedDeltaTime for physics)
        currentAcceleration = Mathf.Lerp(currentAcceleration, adjustedAcceleration, accelerationSmoothness * Time.fixedDeltaTime);


        // apply steering & torque to wheels
        foreach (var w in wheels)
        {
            if (w.wheelCollider == null) continue;

            if (w.axel == Axel.Front)
            {
                float lerpedSteer = Mathf.Lerp(w.wheelCollider.steerAngle, targetSteerAngle, Time.fixedDeltaTime * turnSmoothness);
                w.wheelCollider.steerAngle = lerpedSteer;
            }

            if (w.axel == Axel.Rear)
            {
                // torque only applied if below adjusted speed limit
                if (vehicleSpeed < adjustedSpeedLimit)
                {
                    w.wheelCollider.motorTorque = currentAcceleration * 50f;
                    // ensure brakes not stuck
                    w.wheelCollider.brakeTorque = 0f;
                }
                else if (vehicleSpeed > adjustedSpeedLimit + 5f)
                {
                    // too fast -> brake
                    currentAcceleration = 0f;
                    w.wheelCollider.motorTorque = 0f;
                    w.wheelCollider.brakeTorque = brakeForce;
                }
                else
                {
                    // at or slightly above allowed speed -> cut power, no heavy brake
                    w.wheelCollider.motorTorque = 0f;
                    w.wheelCollider.brakeTorque = 0f;
                }
            }
        }
    }

    void ApplyBrakes(bool apply)
    {
        float brake = apply ? brakeForce : 0f;
        foreach (var w in wheels)
        {
            w.wheelCollider.brakeTorque = brake;
            if (apply) w.wheelCollider.motorTorque = 0f;
        }
    }

    void UpdateWheelModels()
    {
        foreach (var wheel in wheels)
        {
            if (wheel.wheelCollider && wheel.wheelModel)
            {
                wheel.wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
                wheel.wheelModel.transform.SetPositionAndRotation(pos, rot);
            }
        }
    }

    void ObstacleCheck()
    {
        RaycastHit hitCenter, hitRight, hitLeft;
        bool anyHit = false;
        RaycastHit closest = new RaycastHit();

        flatForward = Quaternion.Euler(0f, frontChecker.eulerAngles.y, 0f) * Vector3.forward;

        // center
        if (frontChecker != null && Physics.Raycast(frontChecker.position, flatForward, out hitCenter, frontCheckerDistance, vehicleLayer))
        {
            closest = hitCenter; anyHit = true;
            if (hitCenter.distance <= stopDistance) { ApplyBrakes(true); return; } // immediate contact sanity
        }

        // right
        if (frontRightChecker != null && Physics.Raycast(frontRightChecker.position, frontRightChecker.forward, out hitRight, frontCheckerDistance, vehicleLayer))
        {
            if (!anyHit || hitRight.distance < closest.distance) closest = hitRight;
            anyHit = true;
            if (hitRight.distance <= stopDistance) { ApplyBrakes(true); return; } // immediate contact sanity
        }

        // left
        if (frontLeftChecker != null && Physics.Raycast(frontLeftChecker.position, frontLeftChecker.forward, out hitLeft, frontCheckerDistance, vehicleLayer))
        {
            if (!anyHit || hitLeft.distance < closest.distance) closest = hitLeft;
            anyHit = true;
            if (hitLeft.distance <= stopDistance) { ApplyBrakes(true); return; } // immediate contact sanity
        }

        if (!anyHit)
        {
            // no obstacle
            obstacle = null;
            obstacleDistance = Mathf.Infinity;
            overTakeCheckTimer = 0f;
            ApplyBrakes(false);
            return;
        }

        // assign obstacle & obstacle distance safely
        if (closest.collider != null)
        {
            if (obstacle != closest.collider.gameObject)
            {
                obstacle = closest.collider.gameObject;
                isOvertaking = false;
            }
            obstacleDistance = closest.distance;
        }
        else
        {
            obstacle = null;
            obstacleDistance = Mathf.Infinity;
        }

        // If close enough brake
        if (obstacleDistance <= stopDistance)
        {
            ApplyBrakes(true);
        }
        else
        {
            ApplyBrakes(false);
        }

        // Try overtaking if enabled and not already overtaking
        if (tryOvertake && overTakeCheckTimer > checkInterval && obstacle != null && !shouldStop)
        {
            Debug.LogError("checked");
            overTakeCheckTimer = 0f;
            Vector3 obstacleWorldSize = GetColliderWorldSize(closest.collider);
            float sideOffset = obstacleWorldSize.x * 0.5f + checkSize.x + overTakeSideClearance;

            // right/left positions based on obstacle position and vehicle orientation
            Vector3 rightOvertakePos = new Vector3(closest.point.x - sideOffset, vehicleBodyCollider.transform.position.y, closest.point.z);
            Vector3 leftOvertakePos = new Vector3(closest.point.x + sideOffset, vehicleBodyCollider.transform.position.y, closest.point.z);

            TryOvertakeRightSide(rightOvertakePos, leftOvertakePos);
        }
    }

    void TryOvertakeRightSide(Vector3 rightSideOverTakePosition, Vector3 leftSideOverTakePosition)
    {
        // check the overtake destination first (quick boolean test using layer)
        if (!Physics.CheckBox(rightSideOverTakePosition, checkSize, Quaternion.identity))
        {
            // compute a side-check box position for this vehicle's right side (world aligned Y=1)
            Vector3 checkPos = new Vector3(
                transform.position.x - sideCheckDistance,
                1f,
                vehicleBodyCollider.transform.position.z
            );

            Quaternion checkRot = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);

            if (!Physics.CheckBox(checkPos, checkSize, checkRot))
            {
                // safe to overtake on the right
                overtakeTarget.position = rightSideOverTakePosition;
                overtakeTarget.SetParent(obstacle.transform);
                isOvertaking = true;
            }
            else
            {
                Debug.LogWarning($"{name}: Right side blocked!");
                TryOvertakeLeftSide(leftSideOverTakePosition);
            }
            //temp
            rightOvertakeGizmoPos = rightSideOverTakePosition;
            rightSideCheckGizmoPos = checkPos;
        }
        else
        {
            Debug.LogWarning($"{name}: Right overtake destination blocked");
            TryOvertakeLeftSide(leftSideOverTakePosition);
        }
    }

    void TryOvertakeLeftSide(Vector3 leftSideOverTakePosition)
    {
        if (!Physics.CheckBox(leftSideOverTakePosition,checkSize, Quaternion.identity, vehicleLayer))
        {
            Vector3 checkPos = new Vector3(
                transform.position.x + sideCheckDistance,
                1f,
                vehicleBodyCollider.transform.position.z
            );

            Quaternion checkRot = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);

            if (!Physics.CheckBox(checkPos, checkSize, checkRot))
            {
                overtakeTarget.position = leftSideOverTakePosition;
                overtakeTarget.SetParent(obstacle.transform);
                isOvertaking = true;
            }
            else
            {
                Debug.LogWarning($"{name}: Left side blocked!");
            }
            //temp
            leftOvertakeGizmoPos = leftSideOverTakePosition;
            leftSideCheckGizmoPos = checkPos;
        }
        else
        {
            Debug.LogWarning($"{name}: Left overtake destination blocked");
        }
    }

    Vector3 GetColliderWorldSize(Collider col)
    {
        if (col == null) return Vector3.zero;
        if (col is BoxCollider b)
            return Vector3.Scale(b.size, b.transform.lossyScale);
        return col.bounds.size;
    }

    void RandomStop()
    {
        if(driveToTargetDot < 0f && transform.position.x > -3f)
        {
            driveTarget.position = new Vector3(stopPositionX, driveTarget.position.y, transform.position.z - 10f);
        }
        if(vehicleSpeed < 1f)
        {
            stopTimer += Time.deltaTime;
        }
        if(stopTimer >= stopDuration)
        {
            stopping = false;
            shouldStop = false;
            stopTimer = 0f;
            driveTarget.position = new Vector3(transform.position.x + 2f, driveTarget.position.y, transform.position.z - 1000f);
        }
    }

    // Optional: draw debug rays / boxes in Scene view
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        if (frontChecker) Gizmos.DrawLine(frontChecker.position, frontChecker.position + flatForward * frontCheckerDistance);
        if (frontRightChecker) Gizmos.DrawLine(frontRightChecker.position, frontRightChecker.position + flatForward * frontCheckerDistance);
        if (frontLeftChecker) Gizmos.DrawLine(frontLeftChecker.position, frontLeftChecker.position + flatForward * frontCheckerDistance);
    }

    void OnDrawGizmos()
    {
        if (vehicleBodyCollider == null) return;

        // ----------------------------------------
        // 1. DRAW RIGHT DESTINATION CHECK BOX
        // ----------------------------------------
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = Matrix4x4.TRS(
                rightOvertakeGizmoPos,
                Quaternion.identity,
                Vector3.one
            );
            Gizmos.DrawWireCube(Vector3.zero, checkSize * 2f);
        }

        // ----------------------------------------
        // 2. DRAW LEFT DESTINATION CHECK BOX
        // ----------------------------------------
        Gizmos.color = Color.green;
        Gizmos.matrix = Matrix4x4.TRS(
            leftOvertakeGizmoPos,
            Quaternion.identity,
            Vector3.one
        );
        Gizmos.DrawWireCube(Vector3.zero, checkSize * 2f);

        // ----------------------------------------
        // 3. DRAW RIGHT SIDE CHECK BOX (checkPos)
        // ----------------------------------------
        Gizmos.color = Color.yellow;
        Gizmos.matrix = Matrix4x4.TRS(
            rightSideCheckGizmoPos,
            Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f),
            Vector3.one
        );
        Gizmos.DrawWireCube(Vector3.zero, checkSize * 2f); // *2 because checkSize is half-extents


        // ----------------------------------------
        // 4. DRAW LEFT SIDE CHECK BOX (checkPos)
        // ----------------------------------------
        Gizmos.color = Color.yellow;
        Gizmos.matrix = Matrix4x4.TRS(
            leftSideCheckGizmoPos,
            Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f),
            Vector3.one
        );
        Gizmos.DrawWireCube(Vector3.zero, checkSize * 2f);

        Gizmos.matrix = Matrix4x4.identity;
    }
}
