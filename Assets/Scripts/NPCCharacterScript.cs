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
    private bool isWalking = false;
    public float hitForce = 150f;
    [HideInInspector] public Vector3 hitPoint = Vector3.zero;

    [Header("Road Cross Settings")]
    public bool roadCrossing = false;
    public float roadCrossEnableDistance = -500f;
    public float roadCrossingProbability = 0.1f;
    public float roadCrossCheckDistance = 5f;
    public float roadCheckForwadOffset = 1f;
    private bool crossingLeftToRight = false;
    private float raycastSideMultiplier = 1.0f;
    private float raycastTimer = 0f;
    private float raycastInterval = 0.2f;
    private bool cachedPathBlocked = false;

    [Header("Drive Settings")]
    public int vehicleType = 0; //0: None, 1: Bike, 2: SportsBike, 3: Texi

    [Header("References")]
    public NPCVehicleController nPCVehicleController;
    private Animator animator;
    public Collider[] bodyColliders;
    public Rigidbody[] bodyRigidBodies;
    public BoxCollider npcTriggerCollider;

    private bool stateUpdated = false;
    private bool rigidBodyActivated = false;
    private CollisionDetector detector;
    private BoxCollider boxCollider;

    void Awake()
    {
        animator = GetComponent<Animator>();

        if (!driving)
        {
            detector = GetComponent<CollisionDetector>();
            boxCollider = GetComponent<BoxCollider>();
        }
    }

    void OnEnable()
    {
        ResetNPC();
    }

    void Update()
    {
        if(rigidBodyActivated) return;

        if (!stateUpdated && driving)
        {
            UpdateDrivingState();
        }
        if(isDead && !rigidBodyActivated && vehicleType != 3)
        {
            RigidBodyActive();
            rigidBodyActivated = true;
        }
        if (!isDead && walking)
        {
            if (roadCrossing)
                RoadCross();
            else
                UpdateWalking();
        }
    }

    void UpdateWalking()
    {
        if (!stateUpdated)
        {
            float randomY = (Random.value > 0.5f) ? 180f : 0f;
            transform.rotation = Quaternion.Euler(0f, randomY, 0f);
            stateUpdated = true;
        }
        raycastTimer += Time.deltaTime;
        if (raycastTimer >= raycastInterval)
        {
            Vector3 origin = transform.position + Vector3.up * 1f;
            cachedPathBlocked = Physics.Raycast(origin, transform.forward, detectionDistance, obstacleLayer);
            raycastTimer = 0f;
        }
        if (cachedPathBlocked)
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
        if (animator == null) return;
        animator.SetInteger("VehicleInt", vehicleType);
        animator.SetBool("Driving", true);
        stateUpdated = true;
    }

    void RoadCross()
    {
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
            stateUpdated = true;
        }
        raycastTimer += Time.deltaTime;
        if (raycastTimer >= raycastInterval)
        {
            Vector3 origin = transform.position + Vector3.up * 1f;
            bool frontBlocked = Physics.Raycast(origin, transform.forward, detectionDistance * 2f, obstacleLayer);

            // Corrected Origin Logic (Local Forward)
            origin = transform.position + Vector3.up * 1f + transform.forward * roadCheckForwadOffset;
            bool sideBlocked = Physics.Raycast(origin, raycastSideMultiplier * transform.right, roadCrossCheckDistance, obstacleLayer);

            cachedPathBlocked = frontBlocked || sideBlocked;
            raycastTimer = 0f;
        }
        if (cachedPathBlocked)
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
            if(npcTriggerCollider != null)
                npcTriggerCollider.enabled = false;
            bodyRigidBodies[0].AddForce(hitPoint.normalized * hitForce, ForceMode.Impulse);
        }
    }

    public void ResetNPC()
    {
        // 1.Reset Ragdoll
        if (isDead)
        {
            isDead = false;
            foreach (var col in bodyColliders) col.enabled = false;
            foreach (var rb in bodyRigidBodies) rb.isKinematic = true;
        }
        // 2. Reset Logic
        stateUpdated = false;
        rigidBodyActivated = false;
        raycastTimer = 0f;

        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool("IsWalking", false);
        }

        isWalking = false;

        // 3. Reset Components (Using Cached references)
        if (walking)
        {
            if (detector != null)
            {
                detector.enabled = true;
                detector.pedestrian = true;
                detector.npcCharacterScript = this;
            }
            if (boxCollider != null) boxCollider.enabled = true;
            if(npcTriggerCollider != null) npcTriggerCollider.enabled = true;

            hitPoint = Vector3.zero;

            // 4. Randomize Road Crossing
            if (transform.position.z < roadCrossEnableDistance)
            {
                roadCrossing = (Random.value <= roadCrossingProbability);
            }
            else
            {
                roadCrossing = false;
            }
        }
    }
}
