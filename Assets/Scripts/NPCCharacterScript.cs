using UnityEngine;

public class NPCCharacterScript : MonoBehaviour
{
    [Header("NPC Settings")]
    public bool driving = true;
    public float walkSpeed = 2.0f;
    public int vehicleType = 0;
    public bool isDead = false;

    private bool drivingStateUpdated = false;
    private bool rigidBodyActivated = false;

    public Collider[] bodyColliders;
    public Rigidbody[] bodyRigidBodies;

    [Header("References")]
    public NPCVehicleController nPCVehicleController;
    private Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Animator component not found on NPCCharacterScript GameObject.");
        }
        if (nPCVehicleController == null)
        {
            Debug.LogWarning("NPCVehicleController component not found on NPCCharacterScript GameObject.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(driving & !drivingStateUpdated)
        {
            UpdateDrivingState();
        }
        if(isDead && !rigidBodyActivated)
        {
            RigidBodyActive();
            rigidBodyActivated = true;
        }
    }

    void UpdateDrivingState()
    {
        animator.SetInteger("VehicleInt", vehicleType);
        animator.SetBool("Driving", true);
        drivingStateUpdated = true;
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
}
