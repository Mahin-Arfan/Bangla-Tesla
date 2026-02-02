using UnityEngine;

public class SpecialRoadScript : MonoBehaviour
{
    public float actionDistance = 50f;
    public float objectDropDistance = 15f;
    public float inactiveDistace = 100f;
    public Rigidbody rigidObject;
    public Transform playerTransform;
    private Animator animator;
    private Collider objectCollider;

    //internals
    float distanceToPlayer;
    Vector3 dropObjectPosition;
    Vector3 dropObjectRotation;
    bool actionStarted = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        objectCollider = rigidObject.GetComponent<Collider>();
        dropObjectPosition = rigidObject.transform.localPosition;
        dropObjectRotation = rigidObject.transform.localEulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        distanceToPlayer = playerTransform.position.z - transform.position.z;
        //Debug.Log("Distance to Player: " + distanceToPlayer);
        if(distanceToPlayer > 0f)
        {
            if (distanceToPlayer < actionDistance)
            {
                StartAction();
            }
        }
        else
        {
            if (Mathf.Abs(distanceToPlayer) > inactiveDistace)
            {
                rigidObject.isKinematic = true;
                rigidObject.transform.localPosition = dropObjectPosition;
                rigidObject.transform.localEulerAngles = dropObjectRotation;
                actionStarted = false;
                objectCollider.enabled = false;
                animator.SetTrigger("Reset");
                this.gameObject.SetActive(false);
                Debug.Log("Special Road Reset");
            }
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
            objectCollider.enabled = true;
            rigidObject.isKinematic = false;
        }
    }
}
