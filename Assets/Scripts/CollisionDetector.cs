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
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            ProcessHit();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & obstacleLayer) != 0)
        {
            ProcessHit();
        }
    }

    void ProcessHit()
    {
        float damage = (position == WheelPosition.Front) ? 100f : 20f;
        healthScript.TakeDamage(damage, position);
    }
}
