using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    public enum WheelPosition { Front, Left, Right }

    [Header("Setup")]
    public bool isNPC = false;
    [HideInInspector] public bool pedestrian = false;

    [Tooltip("Not Needed if isNPC")]
    public WheelPosition position;
    [Tooltip("Not Needed if isNPC")]
    public RickshawHealth healthScript;
    [Tooltip("Needed only if isNPC")]
    public NPCVehicleController npcVehicleController;

    [Header("Collision Layers")]
    public LayerMask obstacleLayer;

    [HideInInspector] public NPCCharacterScript npcCharacterScript;
    private float onTriggerEnterTime = 0f;
    private float onTriggerStayTime = 0f;
    private float hitCooldown = 0.75f;

    void Awake()
    {
        if (!isNPC)
        {
            healthScript = GetComponentInParent<RickshawHealth>();
        }
        else if (pedestrian && npcCharacterScript == null)
        {
            npcCharacterScript = GetComponent<NPCCharacterScript>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < onTriggerEnterTime + hitCooldown) return;

        if (((1 << other.gameObject.layer) & obstacleLayer) != 0)
        {
            onTriggerEnterTime = Time.time;
            HandleCollision(other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (Time.time < onTriggerEnterTime + hitCooldown || Time.time < onTriggerStayTime + hitCooldown)
        {
            return;
        }

        if (((1 << other.gameObject.layer) & obstacleLayer) != 0 && !isNPC)
        {
            onTriggerStayTime = Time.time;
            HandleCollision(other);
        }
    }

    void HandleCollision(Collider other)
    {
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        int hitLayer = other.gameObject.layer;
        bool shouldSpawnVisuals = true;

        if (other.TryGetComponent(out CollisionDetector otherVehicle))
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
        ProcessHit(hitPoint, hitLayer);
    }
    void ProcessHit(Vector3 hitPoint, int hitLayer)
    {
        if (isNPC)
        {
            if(pedestrian)
            {
                npcCharacterScript.isDead = true;
                npcCharacterScript.hitPoint = hitPoint;
            }
            else
            {
                npcVehicleController.VehicleHit(hitPoint, hitLayer);
            }
        }
        else if(healthScript != null)
        {
            healthScript.TakeDamage(hitLayer, position);
        }
    }

    void SpawnImpactEffects(Vector3 spawnPosition)
    {
        if (UIPoolManager.Instance == null) return;
        Vector3 randomOffset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(0.2f, 0.5f),
                Random.Range(-0.5f, 0.5f)
        );

        UIPoolManager.Instance.SpawnRandomEffect(spawnPosition + randomOffset);
    }
}
