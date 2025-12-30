using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    public enum WheelPosition { Front, Left, Right }

    [Header("Setup")]
    public WheelPosition position;
    public RickshawHealth healthScript;

    [Header("Collision Layers")]
    public LayerMask obstacleLayer;

    private void OnCollisionEnter(Collision collision)
    {
        Debug.LogWarning("Collision detected with " + collision.gameObject.name);
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            Debug.LogError("Processing hit for " + gameObject.name);
            ProcessHit();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.LogWarning("Collision detected with " + other.gameObject.name);
        if (((1 << other.gameObject.layer) & obstacleLayer) != 0)
        {
            Debug.LogError("Processing hit for " + gameObject.name);
            ProcessHit();
        }
    }

    void ProcessHit()
    {
        float damage = (position == WheelPosition.Front) ? 100f : 20f;
        healthScript.TakeDamage(damage, position);
    }
}
