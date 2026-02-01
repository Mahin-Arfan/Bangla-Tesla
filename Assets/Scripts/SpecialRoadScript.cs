using UnityEngine;

public class SpecialRoadScript : MonoBehaviour
{
    public float actionDistance = 50f;
    public float objectDropDistance = 15f;
    public float inactiveDistace = 100f;
    public Rigidbody rigidObject;
    public Transform playerTransform;
    private Animator animator;

    //internals
    float distanceToPlayer;
    Vector3 dropObjectPosition;
    Vector3 dropObjectRotation;
    bool actionStarted = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        dropObjectPosition = rigidObject.transform.position;
        dropObjectRotation = rigidObject.transform.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        distanceToPlayer = playerTransform.position.z - transform.position.z;
        if(distanceToPlayer < actionDistance)
        {
            StartAction();
            return;
        }
        float distanceToPlayerPassed = transform.position.z - playerTransform.position.z;
        Debug.Log("Distance to Player Passed: " + distanceToPlayerPassed);
        if (distanceToPlayerPassed > inactiveDistace)
        {
            rigidObject.isKinematic = true;
            rigidObject.transform.position = dropObjectPosition;
            rigidObject.transform.eulerAngles = dropObjectRotation;
            actionStarted = false;
            animator.SetTrigger("Reset");
            this.gameObject.SetActive(false);
            Debug.Log("Special Road Reset");
        }
    }

    void StartAction()
    {
        if (!actionStarted)
        {
            animator.SetTrigger("Action");
            actionStarted = true;
        }

        if (distanceToPlayer < objectDropDistance && rigidObject.isKinematic)
        {
            rigidObject.isKinematic = false;
        }
    }
}
