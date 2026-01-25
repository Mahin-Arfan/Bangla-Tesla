using UnityEngine;
using static UnityEngine.UI.Image;

public class NPCCharacterScript : MonoBehaviour
{
    [Header("NPC Settings")]
    public bool driving = true;
    public bool walking = false;
    public bool salesman = false;
    public bool isDead = false;

    [Header("Walk Settings")]
    public float walkSpeed = 2.0f;
    public float detectionDistance = 1.5f;
    public LayerMask obstacleLayer;
    public bool isWalking = false;

    [Header("Road Cross Settings")]
    public bool roadCrossing = false;
    public float roadCrossEnableDistance = -500f;
    public float roadCrossingProbability = 0.1f;
    public float roadCrossCheckDistance = 5f;
    public float roadCheckForwadOffset = 1f;
    private bool crossingLeftToRight = false;
    private float raycastSideMultiplier = 1.0f;

    [Header("Drive Settings")]
    public int vehicleType = 0; //0: None, 1: Bike, 2: SportsBike, 3: Texi

    [Header("References")]
    public NPCVehicleController nPCVehicleController;
    private Animator animator;
    public Collider[] bodyColliders;
    public Rigidbody[] bodyRigidBodies;

    public bool stateUpdated = false;
    private bool rigidBodyActivated = false;
    private CollisionDetector detector;
    private BoxCollider boxCollider;

    void OnEnable()
    {
        ResetNPC();
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Animator component not found on NPCCharacterScript GameObject.");
            gameObject.SetActive(false);
        }
        if (driving && nPCVehicleController == null)
        {
            Debug.LogWarning("NPCVehicleController component not found on NPCCharacterScript GameObject.");
            gameObject.SetActive(false);
        }
        if(!driving && walking)
        {
            detector = GetComponent<CollisionDetector>();
            if(detector != null)
            {
                boxCollider = GetComponent<BoxCollider>();
                boxCollider.enabled = true;
                detector.enabled = true;
                detector.pedestrian = true;
                detector.npcCharacterScript = this;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(rigidBodyActivated)
        {
            return;
        }
        if (!stateUpdated && driving)
        {
            UpdateDrivingState();
        }
        if(isDead && !rigidBodyActivated && vehicleType != 3)
        {
            RigidBodyActive();
            rigidBodyActivated = true;
        }
        if(!isDead && walking && !roadCrossing)
        {
            UpdateWalking();
        }
        if(!isDead && roadCrossing)
        {
            RoadCross();
        }
    }

    void UpdateWalking()
    {
        Debug.Log("NPC Walking: " + transform.name);
        if (!stateUpdated)
        {
            if(Random.value > 0.5f)
            {
                transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
            Debug.Log("NPC Walking Direction: " + transform.rotation.y + transform.name);
            stateUpdated = true;
        }
        Vector3 origin = transform.position + Vector3.up * 1f;
        bool pathBlocked = Physics.Raycast(origin, transform.forward, detectionDistance, obstacleLayer);
        if (pathBlocked)
        {

            if (isWalking)
            {
                isWalking = false;
                if (animator) animator.SetBool("IsWalking", false);
            }
        }
        else
        {
            if (!isWalking)
            {
                isWalking = true;
                if (animator) animator.SetBool("IsWalking", true);
            }
            transform.Translate(Vector3.forward * walkSpeed * Time.deltaTime);
        }
    }

    void UpdateDrivingState()
    {
        if (animator == null)
        {
            Debug.LogWarning("No Animator on " + gameObject.name);
            return;
        }
        animator.SetInteger("VehicleInt", vehicleType);
        animator.SetBool("Driving", true);
        stateUpdated = true;
    }

    void RoadCross()
    {
        Debug.Log("NPC Road Crossing: " + transform.name);
        if (!stateUpdated)
        {
            if (transform.position.x > 0f)
            {
                transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                crossingLeftToRight = true;
                raycastSideMultiplier = 1f;
            }
            else
            {
                transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                crossingLeftToRight = false;
                raycastSideMultiplier = -1f;
            }
            Debug.Log("NPC Road Crossing Direction: " + transform.rotation.y + transform.name);
            stateUpdated = true;
        }
        Vector3 origin = transform.position + Vector3.up * 1f;
        bool pathBlocked1 = Physics.Raycast(origin, transform.forward, detectionDistance * 2f, obstacleLayer);
        origin = transform.position + Vector3.up * 1f + transform.forward * roadCheckForwadOffset;
        bool pathBlocked2 = Physics.Raycast(origin, raycastSideMultiplier * transform.right, roadCrossCheckDistance, obstacleLayer);
        if (pathBlocked1 || pathBlocked2)
        {
            if (isWalking)
            {
                isWalking = false;
                if (animator) animator.SetBool("IsWalking", false);
            }
        }
        else if(!pathBlocked1 && !pathBlocked2)
        {
            if (!isWalking)
            {
                isWalking = true;
                if (animator) animator.SetBool("IsWalking", true);
            }
            transform.Translate(Vector3.forward * walkSpeed * Time.deltaTime);
        }
        if (crossingLeftToRight && transform.position.x < -7f)
        {
            roadCrossing = false;
            stateUpdated = false;
        }
        else if (!crossingLeftToRight && transform.position.x > 7f)
        {
            roadCrossing = false;
            stateUpdated = false;
        }
    }

    void RigidBodyActive()
    {
        animator.enabled = false;
        foreach (var col in bodyColliders)
        {
            col.enabled = true;
        }
        foreach (var rb in bodyRigidBodies)
        {
            rb.isKinematic = false;
        }
        if (walking)
        {
            boxCollider.enabled = false;
            detector.enabled = false;
        }
    }

    public void ResetNPC()
    {
        if (isDead)
        {
            isDead = false;
            foreach (var col in bodyColliders)
            {
                col.enabled = false;
            }
            foreach (var rb in bodyRigidBodies)
            {
                rb.isKinematic = true;
            }
        }
        stateUpdated = false;
        rigidBodyActivated = false;
        if(animator != null)
        {
            animator.enabled = true;
        }
        else
        {
            animator = GetComponent<Animator>();
            animator.enabled = true;
        }
        animator.SetBool("IsWalking", false);
        isWalking = false;
        if (!driving && walking)
        {
            if(detector == null) detector = GetComponent<CollisionDetector>();

            if (detector != null)
            {
                boxCollider = GetComponent<BoxCollider>();
                boxCollider.enabled = true;
                detector.enabled = true;
                detector.pedestrian = true;
                if(detector.npcCharacterScript == null)
                    detector.npcCharacterScript = this;
            }
        }
        if (walking && transform.position.z < roadCrossEnableDistance)
        {
            if (Random.value <= roadCrossingProbability)
            {
                roadCrossing = true;
            }
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // ----------------------------------------------------
        // 1. VISUALIZE FORWARD CHECK (PathBlocked1)
        // ----------------------------------------------------
        Vector3 origin1 = transform.position + Vector3.up * 1f;
        Vector3 dir1 = transform.forward;

        // Check if it hits anything to decide color
        bool hit1 = Physics.Raycast(origin1, dir1, detectionDistance, obstacleLayer);
        Gizmos.color = hit1 ? Color.red : Color.green;

        // Draw the Ray
        Gizmos.DrawRay(origin1, dir1 * detectionDistance);


        // ----------------------------------------------------
        // 2. VISUALIZE SIDE CHECK (PathBlocked2)
        // ----------------------------------------------------
        // NOTE: I used your exact math, but check the tip below regarding Vector3.forward!
        Vector3 origin2 = transform.position + Vector3.up * 1f + transform.forward * roadCheckForwadOffset;
        Vector3 dir2 = transform.right * raycastSideMultiplier; // Assuming multiplier is 1 or -1

        bool hit2 = Physics.Raycast(origin2, dir2, roadCrossCheckDistance, obstacleLayer);
        Gizmos.color = hit2 ? Color.red : Color.yellow;

        Gizmos.DrawRay(origin2, dir2 * roadCrossCheckDistance);
    }
#endif
}
