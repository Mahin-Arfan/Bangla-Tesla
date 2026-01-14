using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    public enum WheelPosition { Front, Left, Right }

    [Header("Setup")]
    public bool isNPC = false;

    [Tooltip("Not Needed if isNPC")]
    public WheelPosition position;
    [Tooltip("Not Needed if isNPC")]
    public RickshawHealth healthScript;
    [Tooltip("Needed only if isNPC")]
    public NPCVehicleController npcVehicleController;

    [Header("Collision Layers")]
    public LayerMask obstacleLayer;

    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            Vector3 hitDirection = (transform.position - collision.transform.position).normalized;
            ProcessHit(hitDirection);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & obstacleLayer) != 0)
        {
            Vector3 hitDirection = (transform.position - other.transform.position).normalized;
            ProcessHit(hitDirection);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & obstacleLayer) != 0 && !isNPC)
        {
            Debug.Log("Trigger Stay Detected on " + gameObject.name);
            Vector3 hitDirection = (transform.position - other.transform.position).normalized;
            ProcessHit(hitDirection);
        }
    }

    void ProcessHit(Vector3 hitDirection)
    {
        if (isNPC)
        {
            npcVehicleController.VehicleHit(hitDirection);
        }
        else
        {
            float damage = (position == WheelPosition.Front) ? 100f : 20f;
            healthScript.TakeDamage(damage, position);
        }
    }
}
