using UnityEngine;

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

    [Header("Drive Settings")]
    public int vehicleType = 0; //0: None, 1: Bike, 2: SportsBike, 3: Texi

    [Header("References")]
    public NPCVehicleController nPCVehicleController;
    private Animator animator;
    public Collider[] bodyColliders;
    public Rigidbody[] bodyRigidBodies;

    private bool stateUpdated = false;
    private bool rigidBodyActivated = false;

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
    }

    // Update is called once per frame
    void Update()
    {
        if(!stateUpdated && driving)
        {
            UpdateDrivingState();
        }
        if(isDead && !rigidBodyActivated && vehicleType != 3)
        {
            RigidBodyActive();
            rigidBodyActivated = true;
        }
        if(!isDead && walking)
        {
            UpdateWalking();
        }
    }

    void UpdateWalking()
    {
        if(!stateUpdated)
        {
            if(Random.value > 0.5f)
            {
                transform.Rotate(0f, 180f, 0f);
            }
            else
            {
                transform.Rotate(0f, 0f, 0f);
            }
            stateUpdated = true;
        }
        Vector3 origin = transform.position + Vector3.up * 0.5f;
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
    }
}
