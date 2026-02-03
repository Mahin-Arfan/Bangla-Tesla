using UnityEngine;

public class SpecialRoadScript : MonoBehaviour
{
    public enum EnvironmentTypes { CraneWork, MetroRail }

    public EnvironmentTypes environmentType;
    [Header("Settings")]
    public float actionDistance = 50f;
    public float objectDropDistance = 15f;
    public float inactiveDistace = 100f;

    public Rigidbody[] rigidObjects;
    public Transform playerTransform;
    private Animator animator;
    private Collider[] objectColliders;

    //internals
    float distanceToPlayer;
    Vector3 dropObjectPosition;
    Vector3 dropObjectRotation;
    int actionObjectIndex = 0;
    bool actionStarted = false;
    bool objectDropped = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        objectColliders = new Collider[rigidObjects.Length];
        if (environmentType == EnvironmentTypes.CraneWork)
        {
            objectColliders[0] = rigidObjects[0].GetComponent<Collider>();
            dropObjectPosition = rigidObjects[0].transform.localPosition;
            dropObjectRotation = rigidObjects[0].transform.localEulerAngles;
        }
        else
        {
            for(int i = 0; i < rigidObjects.Length; i++)
            {
                objectColliders[i] = rigidObjects[i].GetComponent<Collider>();
            }
        }
    }

    void Update()
    {
        distanceToPlayer = playerTransform.position.z - transform.position.z;
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
                rigidObjects[actionObjectIndex].isKinematic = true;
                rigidObjects[actionObjectIndex].transform.localPosition = dropObjectPosition;
                rigidObjects[actionObjectIndex].transform.localEulerAngles = dropObjectRotation;
                objectColliders[actionObjectIndex].enabled = false;
                animator.enabled = false;
                actionStarted = false;
                objectDropped = false;
                this.gameObject.SetActive(false);
            }
        }
    }

    void StartAction()
    {
        if (!actionStarted)
        {
            animator.enabled = true;
            animator.SetTrigger("Action");
            actionStarted = true;
        }
        if (!objectDropped && distanceToPlayer < objectDropDistance)
        {
            if(environmentType == EnvironmentTypes.CraneWork)
            {
                actionObjectIndex = 0;
            }
            else
            {
                actionObjectIndex = Random.Range(0, rigidObjects.Length);
                dropObjectPosition = rigidObjects[actionObjectIndex].transform.localPosition;
                dropObjectRotation = rigidObjects[actionObjectIndex].transform.localEulerAngles;
            }
            objectColliders[actionObjectIndex].enabled = true;
            rigidObjects[actionObjectIndex].isKinematic = false;
            if (environmentType == EnvironmentTypes.MetroRail)
            {
                float randomXForce = rigidObjects[actionObjectIndex].transform.position.x < 0 ? 0.5f : -0.5f;
                Vector3 dropForce = new Vector3(randomXForce, 0f, 0f).normalized;
                float hitForce = Random.Range(5, 15f);
                rigidObjects[actionObjectIndex].AddForce(dropForce * hitForce, ForceMode.Impulse);
            }
            objectDropped = true;
        }
    }
}
