using UnityEngine;
using System.Collections;

public class NPCCharacterScript : MonoBehaviour
{
    [Header("NPC Settings")]
    public bool driving = true;
    public bool walking = false;
    public bool salesman = false;
    public bool police = false;
    public bool male = true;
    public bool isDead = false;
    private Vector3 commentPosition = new Vector3(-1f, 2.45f, 0f);

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
    public AudioClip[] policeWhistleClips;
    public GameObject policeSignUI;
    AudioSource policeAudioSource;

    [Header("Salesman Settings")]
    public int salesmanAnim = 0;
    public AudioClip salesmanAudioClip;
    private bool salesmanAudioPlayed = false;
    public Transform salesmanParentStall;
    private AudioSource salesmanAudioSource;

    [Header("Passenger Settings")]
    public float passengerCallDistance = 20f;
    public int angryCommentInterval = 5;
    private bool inRickshaw = false;
    private bool isAngry = false;
    private bool isGettingOut = false;
    private bool waving = false;
    private float dropPoint = 0f;
    private float angryAnimationLength = 1.02f;
    private float angryTimer = 0f;
    bool passengerCallPlayed = false;
    bool dropCallPlayer = false;
    int angryCallInterval = 0;

    [HideInInspector] public bool isPassenger = false;
    [HideInInspector] public PlayerRickshawController playerRickshaw;

    [Header("Drive Settings")]
    public int vehicleType = 0; //0: None, 1: Bike, 2: SportsBike, 3: Texi
    public bool dieOnCollision = true;

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
    private AudioSource deadAudioSource;
    private float recycleTimer = 0f;

    //animations hashes
    private int policeHash = Animator.StringToHash("Police");
    private int crossingRoadHash = Animator.StringToHash("CrossingRoad");
    private int roadCrossingSideHash = Animator.StringToHash("RoadCrossingSide");

    void Awake()
    {
        animator = GetComponent<Animator>();
        detector = GetComponent<CollisionDetector>();
        boxCollider = GetComponent<BoxCollider>();
        if (salesman)
        {
            salesmanParentStall = transform.parent.transform;
            salesmanAudioSource = gameObject.AddComponent<AudioSource>();
            salesmanAudioSource.clip = salesmanAudioClip;
            salesmanAudioSource.volume = 0.5f;
            salesmanAudioSource.loop = true;
            salesmanAudioSource.spatialBlend = 1.0f; 
            salesmanAudioSource.minDistance = 2.0f;
            salesmanAudioSource.maxDistance = 20.0f;
            salesmanAudioSource.rolloffMode = AudioRolloffMode.Linear;
        }
    }

    void OnEnable()
    {
        GameManagerScript.OnPlayerChanged += UpdatePlayerReference;

        if (GameManagerScript.Instance != null && GameManagerScript.Instance.player != null)
        {
            playerTransform = GameManagerScript.Instance.player.transform;
        }

        ResetNPC();
    }
    void OnDisable()
    {
        GameManagerScript.OnPlayerChanged -= UpdatePlayerReference;
        isPassenger = false;
    }

    void Update()
    {
        recycleTimer += Time.deltaTime;
        if (recycleTimer > 0.5f && !police && !driving)
        {
            CheckIfShouldRecycle();
            recycleTimer = 0f;
        }
        if (rigidBodyActivated) return;

        if(isDead && !rigidBodyActivated && dieOnCollision)
        {
            RigidBodyActive();
            rigidBodyActivated = true;
            return;
        }

        if (isPassenger)
        {
            UpdatePassengerLogic();
            return;
        }
        if(walking)
        {
            if (roadCrossing)
                RoadCross();
            else
                UpdateWalking();
        }
        else if (police)
        {
            Police();
        }
        else if (salesman)
        {
            UpdateSalesmanAudio();
            if (!stateUpdated)
            {
                UpdateSalesman();
            }
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
                if (policeSignUI != null)
                {
                    policeSignUI.SetActive(true);
                    int randomIndex = Random.Range(0, policeWhistleClips.Length);
                    policeAudioSource = AudioManager.Instance.Play3DVoice(policeWhistleClips[randomIndex], transform.position);
                }
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
        animator.SetBool("Stall", true);
        animator.SetInteger("StallAnim", salesmanAnim);
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

    void UpdateSalesmanAudio()
    {
        float zDistance = Mathf.Abs(transform.position.z - playerTransform.position.z);
        if(zDistance > 20f)
        {
            if(salesmanAudioPlayed)
            {
                salesmanAudioPlayed = false;
                salesmanAudioSource.Pause();
            }
        }
        else
        {
            if (!salesmanAudioPlayed)
            {
                salesmanAudioPlayed = true;
                if (salesmanAudioSource.time > 0f) salesmanAudioSource.UnPause();
                else salesmanAudioSource.Play();
            }
        }
    }

    void UpdateDrivingState()
    {
        if (animator == null) return;
        animator.SetInteger("VehicleInt", vehicleType);
        animator.SetBool("Driving", true);
        boxCollider.enabled = false;
        if (npcTriggerCollider != null) npcTriggerCollider.enabled = false;
        detector.enabled = false;
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

    void UpdatePassengerLogic()
    {
        if (!inRickshaw)                
        {
            if (!playerRickshaw.forHire)
            {
                animator.SetBool("Passenger_Waving", false);
                waving = false;
                isPassenger = false;
                walking = true;
                stateUpdated = false;
                return;
            }
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (!waving)
            {
                animator.SetBool("Passenger_Waving", true);
                waving = true;
            }
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            directionToPlayer.y = 0f;
            transform.rotation = Quaternion.LookRotation(directionToPlayer);
            if(distanceToPlayer < passengerCallDistance && !passengerCallPlayed)
            {
                NPCCommentManager.Instance.PlayPassengerCallComment(transform, commentPosition);
                passengerCallPlayed = true;
            }
            if (distanceToPlayer < 2f && playerRickshaw.isBraking && playerRickshaw.brakeMeter > 20f && playerRickshaw.forHire)
            {
                StartCoroutine(GetInRickshaw());
            }
        }
        else if (!isGettingOut)
        {
            //In the Rickshaw
            float playerZ = playerTransform.position.z;

            if (!isAngry && (playerZ < dropPoint - 50f))
            {
                isAngry = true;
                animator.SetTrigger("Passenger_angry");
                angryTimer = angryAnimationLength;
            }

            // Apply Steering Sabotage
            if (isAngry)
            {
                angryTimer -= Time.deltaTime;
                if (angryTimer <= 0f)
                {
                    playerRickshaw.horizontalInput = Random.value > 0.5f ? 0.6f : -0.6f;
                    angryTimer = angryAnimationLength;
                    angryCallInterval--;
                    if (angryCallInterval <= 0)
                    {
                        NPCCommentManager.Instance.PlayPassengerAngryComment(transform, commentPosition);
                        angryCallInterval = angryCommentInterval;
                    }
                }
            }

            // Check for DropOff
            if (playerZ <=dropPoint)
            {
                if (!dropCallPlayer)
                {
                    NPCCommentManager.Instance.PlayPassengerDropComment(transform, commentPosition);
                    dropCallPlayer = true;
                }
                if(playerRickshaw.isBraking && playerRickshaw.brakeMeter > 20f)
                {
                    bool atSideOfRoad = playerTransform.position.x <= -4.5f || playerTransform.position.x >= 4.5f;
                    if (atSideOfRoad)
                    {
                        StartCoroutine(GetOutRickshaw());
                    }
                }
            }
        }
    }

    private IEnumerator GetInRickshaw()
    {
        inRickshaw = true;
        playerRickshaw.forHire = false;
        playerRickshaw.passengerCharacterScript = this;

        // Parent and reset position
        detector.enabled = false;
        boxCollider.enabled = false;
        npcTriggerCollider.enabled = false;
        transform.SetParent(playerRickshaw.rickshawVisualModel);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        // Trigger animation
        animator.SetFloat("RoadSide", playerTransform.position.x);
        animator.SetTrigger("Passenger_In");

        waving = false;
        animator.SetBool("Passenger_Waving", false);

        // Assign random drop point
        dropPoint = playerTransform.position.z - Random.Range(200f, 300f);
        float dropPointX = transform.position.x > 0 ? 8f : -8f;
        Vector3 dropPointPosition = new Vector3(dropPointX, 2.2f, dropPoint);
        UIPoolManager.Instance.PlacePassengerDropPoint(dropPointPosition);

        yield return null;
    }

    private IEnumerator GetOutRickshaw()
    {
        isGettingOut = true;
        animator.SetFloat("RoadSide", playerTransform.position.x);
        animator.SetTrigger("Passenger_Out");
        UIPoolManager.Instance.passengerDropPointUI.SetActive(false);

        yield return new WaitForSeconds(0.75f);

        transform.SetParent(null);
        Vector3 dropPos = transform.position;
        dropPos.y = 0.3f;
        transform.position = dropPos;
        transform.rotation = Quaternion.identity;

        if (!isAngry)
        {
            GameManagerScript.Instance.SuccessfulPassengerDropOff();
        }
        detector.enabled = true;
        boxCollider.enabled = true;
        npcTriggerCollider.enabled = true;
        playerRickshaw.forHire = true;
        isPassenger = false;
        inRickshaw = false;
        isAngry = false;
        isGettingOut = false;
        walking = true;
        isPassenger = false;
        angryCallInterval = 0;
        dropCallPlayer = false;
        passengerCallPlayed = false;
    }

    void RigidBodyActive()
    {
        animator.enabled = false;
        bool shouldPlayDeadAudio = Mathf.Abs(transform.position.z - playerTransform.position.z) < AudioManager.Instance.audioTriggerDistance;
        if (shouldPlayDeadAudio)
        {
            deadAudioSource = AudioManager.Instance.RequestDeadVoiceClip(transform.position, male);
        }

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
        }
        else if (police && policeAudioSource != null)
        {
            AudioManager.Instance.ReturnAudioSource(policeAudioSource);
            policeAudioSource = null;
        }
        if (hitPoint != Vector3.zero)
            bodyRigidBodies[0].AddForce(hitPoint.normalized * hitForce, ForceMode.Impulse);
    }

    public void ResetNPC()
    {
        //Reset Ragdoll
        if (isDead)
        {
            isDead = false;
            foreach (var col in bodyColliders) col.enabled = false;
            foreach (var rb in bodyRigidBodies) rb.isKinematic = true;
        }
        //Reset Logic
        stateUpdated = false;
        rigidBodyActivated = false;

        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool("IsWalking", false);
        }

        isWalking = false;

        //Reset Components
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

            //Reset Passenger Logic
            inRickshaw = false;
            isAngry = false;
            isGettingOut = false;
            waving = false;
            angryCallInterval = 0;
            dropCallPlayer = false;
            passengerCallPlayed = false;

            hitPoint = Vector3.zero;
            raycastTimer = 0f;

            //Randomize Road Crossing
            if (GameManagerScript.Instance.progress >= GameManagerScript.Instance.pedestrianRoadCrossProbability)
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
        float playertoNPCDistance = Mathf.Abs(cPos.z - playerPos.z);
        //Pre-game logic
        if (!GameManagerScript.Instance.gameStarted || GameManagerScript.Instance.gameOver)
        {
            outOfZRange = playertoNPCDistance > GameManagerScript.Instance.pedestrianRecycleDistance;
        }
        else
        {
            if (playerPos.z > cPos.z)
            {
                outOfZRange = playertoNPCDistance > GameManagerScript.Instance.pedestrianRecycleDistance;
            }
            else
            {
                outOfZRange = playertoNPCDistance > 20f;
            }
        }
        //Trigger Recycle
        if (outOfZRange)
        {
            if(salesman)
                GameManagerScript.Instance.RecycleSingleStall(salesmanParentStall.gameObject);
            else
                GameManagerScript.Instance.RecycleSinglePedestrian(gameObject);

            if (deadAudioSource != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.ReturnAudioSource(deadAudioSource);
                deadAudioSource = null;
            }
        }
    }

    private void UpdatePlayerReference(Transform newPlayerTransform)
    {
        playerTransform = newPlayerTransform;
    }
}
