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
            SpawnImpactEffects(hitDirection);
            ProcessHit(hitDirection);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & obstacleLayer) != 0)
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);

            bool shouldSpawnVisuals = true;
            CollisionDetector otherVehicle = other.GetComponent<CollisionDetector>();
            if (otherVehicle != null)
            {
                if (GetInstanceID() < otherVehicle.GetInstanceID())
                {
                    shouldSpawnVisuals = false;
                }
            }
            if (shouldSpawnVisuals)
            {
                SpawnImpactEffects(hitPoint);
            }
            ProcessHit(hitPoint);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & obstacleLayer) != 0 && !isNPC)
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            bool shouldSpawnVisuals = true;
            CollisionDetector otherVehicle = other.GetComponent<CollisionDetector>();
            if (otherVehicle != null)
            {
                if (GetInstanceID() < otherVehicle.GetInstanceID())
                {
                    shouldSpawnVisuals = false;
                }
            }
            if (shouldSpawnVisuals)
            {
                SpawnImpactEffects(hitPoint);
            }
            ProcessHit(hitPoint);
        }
    }

    void ProcessHit(Vector3 hitPoint)
    {
        if (isNPC)
        {
            npcVehicleController.VehicleHit(hitPoint);
        }
        else
        {
            float damage = (position == WheelPosition.Front) ? 100f : 20f;
            healthScript.TakeDamage(damage, position);
        }
    }

    void SpawnImpactEffects(Vector3 spawnPosition)
    {
        Vector3 randomOffset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(0.2f, 0.5f),
                Random.Range(-0.5f, 0.5f)
            );

        // ONE LINE OF CODE - No drag and drop needed here
        UIPoolManager.Instance.SpawnRandomEffect(spawnPosition + randomOffset);
    }
}
