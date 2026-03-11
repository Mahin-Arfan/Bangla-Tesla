using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.Image;

public class NPCCharacterScript : MonoBehaviour
{
    [Header("NPC Settings")]
    public bool driving = true;
    public bool walking = false;
    public bool salesman = false;
    public bool police = false;
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
    public float roadCrossingProbability = 0.1f;
    public float roadCrossCheckDistance = 5f;
    public float roadCheckForwadOffset = 1f;
    private bool crossingLeftToRight = false;
    private float raycastSideMultiplier = 1.0f;
    private float raycastTimer = 0f;
    private float raycastInterval = 0.2f;
    private bool cachedPathBlocked = false;

    [Header("Police Settings")]
    public float chaseDistance = 10f;
    public float xPositionLimit = 3.4f;
    public float xPositionOffset = 0.5f;
    public float closestDistance = 1f;
    public Vector3 spineRotationOffset;
    public Transform spineTransform;
    private bool policeChaseStart = false;
    private Transform playerTransform;

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
    private Transform salesmanParentStall;
    private CollisionDetector detector;
    private BoxCollider boxCollider;
    private float recycleTimer = 0f;

    //animations hashes
    private int policeHash = Animator.StringToHash("Police");
    private int crossingRoadHash = Animator.StringToHash("CrossingRoad");
    private int roadCrossingSideHash = Animator.StringToHash("RoadCrossingSide");

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerTransform = GameManagerScript.Instance.player.transform;
        if (!driving)
        {
            detector = GetComponent<CollisionDetector>();
            boxCollider = GetComponent<BoxCollider>();
        }
        if (salesman)
        {
            salesmanParentStall = transform.parent.transform;
        }
    }

    void OnEnable()
    {
        ResetNPC();
    }

    void Update()
    {
        recycleTimer += Time.deltaTime;
        if (recycleTimer > 0.5f && !police)
        {
            CheckIfShouldRecycle();
            recycleTimer = 0f;
        }
        if (rigidBodyActivated) return;

        if(isDead && !rigidBodyActivated && vehicleType != 3)
        {
            RigidBodyActive();
            rigidBodyActivated = true;
            return;
        }
        if(walking)
        {
            if (roadCrossing)
                RoadCross();
            else
                UpdateWalking();
        }else if (police)
        {
            Police();
        }
        else if (!stateUpdated && salesman)
        {
            UpdateSalesman();
        }
        else if (!stateUpdated && driving)
        {
            UpdateDrivingState();
        }
    }
    private void OnAnimatorIK(int layerIndex)
    {
        if (police && policeChaseStart && animator != null)
        {
            animator.SetLookAtWeight(1f, 0.6f, 0.8f, 0f, 0.5f);

            animator.SetLookAtPosition(playerTransform.position);
        }
        else if (animator != null)
        {
            animator.SetLookAtWeight(0f);
        }
    }

    void Police()
    {
        float zDistance = Mathf.Abs(transform.position.z - playerTransform.position.z);
        if (zDistance <= chaseDistance)
        {
            if (!policeChaseStart)
            {
                animator.SetBool(policeHash, true);
                policeChaseStart = true;
            }
            if (zDistance > closestDistance)
            {
                float targetX = playerTransform.position.x;
                if (transform.position.x >= 0)
                {
                    targetX = Mathf.Clamp(targetX, xPositionLimit, 6f);
                }
                else
                {
                    targetX = Mathf.Clamp(targetX, -6f, -xPositionLimit);
                }
                float currentX = transform.position.x;
                if (Mathf.Abs(targetX - currentX) > xPositionOffset)
                {
                    float newX = Mathf.MoveTowards(currentX, targetX, walkSpeed * Time.deltaTime);
                    Vector3 newPos = transform.position;
                    newPos.x = newX;
                    transform.position = newPos;

                    if (targetX > currentX)
                    {
                        animator.SetInteger(roadCrossingSideHash, 1);
                    }
                    else
                    {
                        animator.SetInteger(roadCrossingSideHash, -1);
                    }
                }
                else
                {
                    animator.SetInteger(roadCrossingSideHash, 0);
                }
            }
            else
            {
                if (!stateUpdated)
                {
                    animator.SetBool(crossingRoadHash, true);
                    stateUpdated = true;
                }
            }
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

    void UpdateSalesman()
    {
        if(!GameManagerScript.Instance.gameStarted)
        {
            salesmanParentStall.gameObject.SetActive(false);
            return;
        }
        if (transform.position.x > 0f)
        {
            Vector3 stallPosition = salesmanParentStall.position;
            stallPosition.x = 6.3f;
            salesmanParentStall.position = stallPosition;
            salesmanParentStall.rotation = Quaternion.identity;
        }
        else
        {
            Vector3 stallPosition = salesmanParentStall.position;
            stallPosition.x = -6.3f;
            salesmanParentStall.position = stallPosition;
            salesmanParentStall.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
        stateUpdated = true;
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
                animator.SetInteger(roadCrossingSideHash, 1);
                raycastSideMultiplier = 1f;
            }
            else
            {
                transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                crossingLeftToRight = false;
                animator.SetInteger(roadCrossingSideHash, -1);
                raycastSideMultiplier = -1f;
            }
            animator.SetBool(crossingRoadHash, true);
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
            animator.SetInteger(roadCrossingSideHash, 0);
            animator.SetBool(crossingRoadHash, false);
        }
        else if (!crossingLeftToRight && transform.position.x > 7f)
        {
            roadCrossing = false;
            stateUpdated = false;
            animator.SetInteger(roadCrossingSideHash, 0);
            animator.SetBool(crossingRoadHash, false);
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
            raycastTimer = 0f;

            // 4. Randomize Road Crossing
            if (GameManagerScript.Instance.progress >= 0.5f)
            {
                roadCrossing = (Random.value <= roadCrossingProbability);
            }
            else
            {
                roadCrossing = false;
            }
        }
        else if(police)
        {
            if (detector != null)
            {
                detector.enabled = true;
                detector.pedestrian = true;
                detector.npcCharacterScript = this;
            }
            if (boxCollider != null) boxCollider.enabled = true;
            if (npcTriggerCollider != null) npcTriggerCollider.enabled = true;
            transform.rotation = Quaternion.identity;
            hitPoint = Vector3.zero;
            policeChaseStart = false;
        }
        else if (salesman)
        {
            detector.enabled = true;
            detector.pedestrian = true;
            detector.npcCharacterScript = this;
            if (boxCollider != null) boxCollider.enabled = true;
            if (npcTriggerCollider != null) npcTriggerCollider.enabled = true;
            hitPoint = Vector3.zero;
        }
    }

    void CheckIfShouldRecycle()
    {
        Vector3 playerPos = playerTransform.position;
        Vector3 cPos = transform.position;
        bool outOfZRange = false;

        //Pre-game logic
        if (!GameManagerScript.Instance.gameStarted)
        {
            outOfZRange = Mathf.Abs(cPos.z - playerPos.z) > GameManagerScript.Instance.pedestrianRecycleDistance;
        }
        else
        {
            outOfZRange = cPos.z - playerPos.z > 5f;
        }
        //Trigger Recycle
        if (outOfZRange)
        {
            if(salesman)
                GameManagerScript.Instance.RecycleSinglePedestrian(salesmanParentStall.gameObject);
            else
                GameManagerScript.Instance.RecycleSinglePedestrian(gameObject);
        }
    }
}
