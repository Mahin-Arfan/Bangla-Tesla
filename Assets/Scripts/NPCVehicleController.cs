using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

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
    public float vehicleSpeed;                // current speed (m/s)
    public float minSpeed = 5f;                // min random speed (m/s)
    public float maxSpeed = 15f;               // max random speed (m/s)
    private float speedLimit;
    public float acceleration = 25f;           // base acceleration amount (units used as torque multiplier)
    public float accelerationSmoothness = 5f;  // lerp speed for acceleration changes
    private float steerAngle = 30f;             // maximum steering angle (deg)
    public float turnSmoothness = 5f;          // lerp speed for steering
    public float brakeForce = 2000f;           // brake torque
    public float stopDistanceMultiplier = 1f;  // multiplier used to compute stopDistance from speed
    private bool isBraking = false;

    [Header("Obstacle & Overtake")]
    public bool tryOvertake = true;
    public float frontCheckerDistance = 20f;   // how far the rays check
    public float overTakeSideClearance = 2f;   // extra gap when calculating overtake pos
    public float overTakeSideClearanceY = 0f;
    public float sideCheckDistance = 3f;
    public float checkInterval = 0.2f;         // how often to run obstacle checks (seconds)
    public float overTakingTendency = 3f;      // Lower = more likely to overtake
    private Vector3 overTakeLocalOffset = Vector3.zero;
    public float overTakeCompleteDisctance = 1f;
    private bool frontCheckHit = false;
    private NPCVehicleController otherNPC = null;

    [Header("Random Stops")]
    public bool randomStops = false;
    public float stopPositionX = 0f; // [Range -4 to -3]
    private bool stopping = false;
    public float stopDuration = 5f;
    public float minimumStopDistance = 3f;
    [Range(0f, 100f)]
    public float stopChance = 0; //needs reset
    public float nextStopCheckTime = 5f;
    private bool shouldStop = false;
    private float rightCheckTimer = 0f;
    private bool leftSideClearForStop = true;
    private float stopTimer = 0f;
    private float stopCheckTimer = 0f;

    [Header("Vehicle Mechanics")]
    public bool reverseMechanics = false;      // if true, the vehicle drives "backwards"
    public bool lockXRotation = false;         // if true, freezes X rotation on Rigidbody
    public bool lockZRotation  = false;          // if true, freezes Z rotation on Rigidbody
    public bool lockYPosition = true;         // if true, freezes Y position on Rigidbody

    // runtime
    private Rigidbody rb;              // randomly chosen top speed (m/s)
    private float currentAcceleration;         // smoothed acceleration value
    private bool isOvertaking = false;
    private GameObject obstacle;               // current obstacle GameObject or null
    private float obstacleDistance = Mathf.Infinity;
    private Collider obstacleCollider = null;
    private Transform obstacleTransform = null;
    private Vector3 checkSize;          // checkSize for CheckBox (world-space)
    private float overTakeCheckTimer = 0f;
    private float lastCheckTime = 0f;
    private Vector3 currentDriveTarget;
    private float stopDistance = 5f;
    private Vector3 flatForward;
    private float driveToTargetDistance = 0f;
    private float driveToTargetDot = 0f;
    private float driveToTargetCheck = 5f;
    private float initialYPosition;

    [Header("References")]
    private Vector3 driveTarget;              // main drive target
    private Vector3 overtakeTarget;           // target used while overtaking
    public Transform frontChecker;             // center ray origin
    public Transform frontRightChecker;        // right ray origin
    public Transform frontLeftChecker;         // left ray origin
    public Transform groundChecker;           // ground check origin
    public BoxCollider vehicleBodyCollider;    // used for sizing overtake checks
    public LayerMask vehicleLayer;             // layer mask for raycasts/CheckBox

    [Header("Wheels Setup")]
    public Wheel[] wheels;


    [Header("Temps")]
    // Gizmo storage
    private Vector3 rightOvertakeGizmoPos;
    private Vector3 leftOvertakeGizmoPos;
    private Vector3 hitPos;
    private Vector3 rightSideCheckGizmoPos;
    private Vector3 leftSideCheckGizmoPos;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if(lockXRotation) rb.constraints |= RigidbodyConstraints.FreezeRotationX;
        if(lockYPosition) rb.constraints |= RigidbodyConstraints.FreezePositionY;
        if(lockZRotation) rb.constraints |= RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        initialYPosition = transform.position.y;
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
            Vector3 requested = new Vector3(fullSize.x, 1f, fullSize.z * overTakingTendency);
            checkSize = requested * 0.5f;
        }
        driveTarget = new Vector3(transform.position.x, transform.position.y, transform.position.z - 1000f);
        currentDriveTarget = driveTarget;
        if (randomStops)
        {
            stopCheckTimer = Random.Range(0f, nextStopCheckTime);
        }
    }

    void Update()
    {
        UpdateWheelModels();
        lastCheckTime += Time.deltaTime;
        overTakeCheckTimer += Time.deltaTime;
        driveToTargetCheck += Time.deltaTime;
        if (lastCheckTime >= checkInterval)
        {
            ObstacleCheck();
            GroundCheck();
            lastCheckTime = 0f;
        }
        if (randomStops)
        {
            stopCheckTimer += Time.deltaTime;
            if (stopCheckTimer >= nextStopCheckTime && !stopping)
            {
                stopCheckTimer = 0f;
                float randomValue = Random.Range(0f, 100f);
                shouldStop = randomValue <= stopChance;
                if (shouldStop)
                {
                    driveTarget = new Vector3(stopPositionX, transform.position.y, transform.position.z - 30f);
                    stopChance = stopChance * 0.5f; // reduce chance for next time
                    stopping = true;
                }
            }
        }
        if (isOvertaking && obstacle != null)
        {
            overtakeTarget = obstacleTransform.position + overTakeLocalOffset;
        }
        if (stopping)
        {
            RandomStop();
        }
        if(!isOvertaking && !stopping && driveToTargetCheck > 10f)
        {
            driveTarget += new Vector3(0f, 0f, -300f);
        }
    }

    void FixedUpdate()
    {
        DriveTowardsTarget();
    }

    void DriveTowardsTarget()
    {
        Vector3 target = isOvertaking ? overtakeTarget : driveTarget;
        if (currentDriveTarget != target)
            currentDriveTarget = target;

        // direction & distance
        Vector3 dirToTarget = currentDriveTarget - transform.position;
        driveToTargetDistance = dirToTarget.magnitude;
        driveToTargetDot = Vector3.Dot(transform.forward, dirToTarget.normalized);

        vehicleSpeed = rb.linearVelocity.magnitude;
        float minimumStopDistance = isOvertaking ? 1.5f : 3f;
        stopDistance = Mathf.Clamp(vehicleSpeed * 0.4f * stopDistanceMultiplier, minimumStopDistance, 20f);
        
        // braking if close or target behind (respects reverseMechanics)
        bool shouldBrake = driveToTargetDistance <= stopDistance || (reverseMechanics ? driveToTargetDot > 0f : driveToTargetDot < 0f);
        if (shouldBrake && !isOvertaking)
        {
            currentAcceleration = 0f;
            if(!isBraking)  ApplyBrakes(true);
            Debug.LogWarning($"{name}: Braking - Close to target or target behind");
            return;
        }


        if (isOvertaking && ((reverseMechanics ? driveToTargetDot > 0f : driveToTargetDot < 0f) ||
            driveToTargetDistance <= overTakeCompleteDisctance || obstacleDistance > frontCheckerDistance))
        {
            isOvertaking = false;
            obstacle = null;
            obstacleDistance = Mathf.Infinity;
            obstacleCollider = null;
            obstacleTransform = null;
            overTakeCheckTimer = 0f;
            currentDriveTarget = driveTarget;
            Debug.LogWarning(transform.name + ": Change Drive to target");
        }

        float desiredSpeedLimit = speedLimit;
        if (obstacle != null && !float.IsInfinity(obstacleDistance) && frontCheckHit && !stopping && leftSideClearForStop)
        {
            float clampedD = Mathf.Clamp(obstacleDistance, 1f, frontCheckerDistance);
            float t = Mathf.InverseLerp(1f, frontCheckerDistance, clampedD);
            desiredSpeedLimit = Mathf.Lerp(minSpeed, speedLimit, t);
        }

        Vector3 localTarget = transform.InverseTransformPoint(currentDriveTarget);
        float steerInput = localTarget.magnitude > 0f ? (localTarget.x / localTarget.magnitude) : 0f;
        float targetSteerAngle = steerInput * steerAngle;
        if (reverseMechanics) targetSteerAngle = -targetSteerAngle;

        float absSteer = Mathf.Abs(targetSteerAngle);

        float adjustedAcceleration = acceleration;
        float adjustedSpeedLimit = leftSideClearForStop ? desiredSpeedLimit : 1f;

        if (absSteer > 10f)
        {
            float tSteer = Mathf.InverseLerp(10f, 30f, absSteer);
            adjustedSpeedLimit = Mathf.Lerp(minSpeed, desiredSpeedLimit, tSteer);   // closer = lower
            adjustedAcceleration = Mathf.Lerp(acceleration, acceleration * 0.5f, tSteer);
        }

        currentAcceleration = Mathf.Lerp(currentAcceleration, adjustedAcceleration, accelerationSmoothness * Time.fixedDeltaTime);

        if(isBraking) return;

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
        if (apply)
        {
            isBraking = true;
        }else
        {
            isBraking = false;
        }
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
        frontCheckHit = false;
        RaycastHit closest = new RaycastHit();

        flatForward = Quaternion.Euler(0f, frontChecker.eulerAngles.y, 0f) * Vector3.forward;

        // center
        if (frontChecker != null && Physics.Raycast(frontChecker.position, flatForward, out hitCenter, frontCheckerDistance, vehicleLayer))
        {
            closest = hitCenter; frontCheckHit = true;
            if (hitCenter.distance <= stopDistance) { ApplyBrakes(true); return; } // immediate contact sanity
        }

        // right
        if (frontRightChecker != null && Physics.Raycast(frontRightChecker.position, frontRightChecker.forward, out hitRight, frontCheckerDistance, vehicleLayer))
        {
            if (!frontCheckHit || hitRight.distance < closest.distance) closest = hitRight;
            frontCheckHit = true;
            if (hitRight.distance <= stopDistance) { ApplyBrakes(true); return; } // immediate contact sanity
        }

        // left
        if (frontLeftChecker != null && Physics.Raycast(frontLeftChecker.position, frontLeftChecker.forward, out hitLeft, frontCheckerDistance, vehicleLayer))
        {
            if (!frontCheckHit || hitLeft.distance < closest.distance) closest = hitLeft;
            frontCheckHit = true;
            if (hitLeft.distance <= stopDistance) { ApplyBrakes(true); return; } // immediate contact sanity
        }

        if (!frontCheckHit && !isOvertaking)
        {
            obstacle = null;
            obstacleDistance = Mathf.Infinity;
            overTakeCheckTimer = 0f;
            ApplyBrakes(false);
            return;
        }

        // assign obstacle & obstacle distance safely
        if (closest.collider != null)
        {
            GameObject newObstacle = closest.collider.gameObject;
            //temp
            hitPos = closest.point;
            //temp end
            if (obstacle != newObstacle)
            {
                obstacle = newObstacle;
                obstacleCollider = closest.collider;
                obstacleTransform = obstacle.transform;
                otherNPC = obstacle.GetComponentInParent<NPCVehicleController>();
                isOvertaking = false;
            }

            obstacleDistance = closest.distance;
        }
        else if (!isOvertaking)
        {
            obstacle = null;
            obstacleDistance = Mathf.Infinity;
            return;   // <- unnecessary to continue if no obstacle
        }

        // If close enough brake
        if (obstacleDistance <= stopDistance)
        {
            ApplyBrakes(true);
            return;
        }

        ApplyBrakes(false);

        //If obstacle is already faster — no overtake needed
        if (otherNPC != null && otherNPC.vehicleSpeed > speedLimit)
        {
            return;
        }

        // Try overtaking if enabled and not already overtaking
        if (tryOvertake && obstacle != null && !shouldStop && frontCheckHit && overTakeCheckTimer > checkInterval)
        {
            overTakeCheckTimer = 0f;
            Vector3 obstacleWorldSize = GetColliderWorldSize(obstacleCollider);
            float sideOffset = obstacleWorldSize.x * 0.5f + checkSize.x + overTakeSideClearance;

            // right/left positions based on obstacle position and vehicle orientation
            Vector3 rightOvertakePos = new Vector3(obstacleTransform.position.x - sideOffset, vehicleBodyCollider.transform.position.y + overTakeSideClearanceY, obstacleTransform.position.z + obstacleWorldSize.x * 0.5f);
            Vector3 leftOvertakePos = new Vector3(obstacleTransform.position.x + sideOffset, vehicleBodyCollider.transform.position.y + overTakeSideClearanceY, obstacleTransform.position.z + obstacleWorldSize.x * 0.5f);
            //temp
            rightOvertakeGizmoPos = rightOvertakePos;
            leftOvertakeGizmoPos = leftOvertakePos;
            //temp End
            // Decide best lane logically
            bool rightBlockedByRoad = rightOvertakePos.x < -5f;
            bool leftBlockedByRoad = leftOvertakePos.x > 5f;
            if (rightBlockedByRoad)
            {
                TryOvertakeLeftSide(leftOvertakePos);
            }
            else if (leftBlockedByRoad)
            {
                TryOvertakeRightSide(rightOvertakePos, leftOvertakePos, false);
            }
            else
            {
                // both available → prefer right but fallback left if needed
                TryOvertakeRightSide(rightOvertakePos, leftOvertakePos, true);
            }
        }
    }

    void TryOvertakeRightSide(Vector3 rightSideOverTakePosition, Vector3 leftSideOverTakePosition, bool leftSideCheck)
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
                overTakeLocalOffset = rightSideOverTakePosition - obstacleTransform.position;
                isOvertaking = true;
            }
            else
            {
                Debug.LogWarning($"{name}: Right side blocked!");
                if (leftSideCheck) TryOvertakeLeftSide(leftSideOverTakePosition);
            }
            //temp
            rightSideCheckGizmoPos = checkPos;
        }
        else
        {
            Debug.LogWarning($"{name}: Right overtake destination blocked");
            if (leftSideCheck) TryOvertakeLeftSide(leftSideOverTakePosition);
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
                overTakeLocalOffset = leftSideOverTakePosition - obstacleTransform.position;
                isOvertaking = true;
            }
            else
            {
                Debug.LogWarning($"{name}: Left side blocked!");
            }
            //temp
            leftSideCheckGizmoPos = checkPos;
        }
        else
        {
            Debug.LogWarning($"{name}: Left overtake destination blocked");
        }
    }

    void GroundCheck()
    {
        if (groundChecker == null) return;
        RaycastHit hit;
        if (!Physics.Raycast(groundChecker.position, Vector3.down, out hit, 1f))
        {
            // not grounded — apply simple gravity
            transform.position = new Vector3(transform.position.x, initialYPosition + 0.5f, transform.position.z);
            if (reverseMechanics)
            {
                transform.rotation = Quaternion.identity;
            }
            else
            {
                transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
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
        rightCheckTimer += Time.deltaTime;
        if (driveToTargetDot < 0f && transform.position.x > 3f)
        {
            driveTarget = new Vector3(stopPositionX, transform.position.y, transform.position.z - 10f);
        }
        if(rightCheckTimer >= checkInterval)
        {
            rightCheckTimer = 0f;
            leftSideCheckForStop();
        }
        if (vehicleSpeed < 1f && driveToTargetDistance <= minimumStopDistance)
        {
            stopTimer += Time.deltaTime;
        }
        if(stopTimer >= stopDuration)
        {
            stopping = false;
            shouldStop = false;
            stopTimer = 0f;
            leftSideClearForStop = true;
            driveTarget = new Vector3(transform.position.x + 2f, transform.position.y, transform.position.z - 1000f);
        }
    }

    void leftSideCheckForStop()
    {
        Vector3 checkPos = new Vector3(
                transform.position.x + sideCheckDistance,
                1f,
                vehicleBodyCollider.transform.position.z
            );

        Quaternion checkRot = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);

        bool vehicleDetected = Physics.CheckBox(
        checkPos,
        checkSize,
        checkRot,
        vehicleLayer,
        QueryTriggerInteraction.Ignore
        );

        if (!vehicleDetected)
        {
            leftSideClearForStop = true;
        }
        else
        {
            Debug.LogError("Left side not clear for stopping");
            leftSideClearForStop = false;
        }
        //temp
        leftSideCheckGizmoPos = checkPos;
    }

    // Optional: draw debug rays / boxes in Scene view
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        if (frontChecker) Gizmos.DrawLine(frontChecker.position, frontChecker.position + flatForward * frontCheckerDistance);
        if (frontRightChecker) Gizmos.DrawLine(frontRightChecker.position, frontRightChecker.position + flatForward * frontCheckerDistance);
        if (frontLeftChecker) Gizmos.DrawLine(frontLeftChecker.position, frontLeftChecker.position + flatForward * frontCheckerDistance);
        if(frontCheckHit)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(hitPos, 0.3f);
        }
        if (Application.isPlaying && isOvertaking)
        {
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(
                overtakeTarget,
                Quaternion.identity,
                Vector3.one
            );
            Gizmos.DrawWireCube(Vector3.zero, checkSize);
        }

        Gizmos.color = Color.green;
        Gizmos.matrix = Matrix4x4.TRS(
            leftOvertakeGizmoPos,
            Quaternion.identity,
            Vector3.one
        );
        Gizmos.DrawWireCube(Vector3.zero, checkSize * 2f);
        Gizmos.color = Color.green;
        Gizmos.matrix = Matrix4x4.TRS(
            rightOvertakeGizmoPos,
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
