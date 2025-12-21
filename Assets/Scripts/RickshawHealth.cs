using UnityEngine;

public class RickshawHealth : MonoBehaviour
{
    [Header("Stats")]
    public float health = 100f;
    public bool leftWheelDamaged = false;
    public bool rightWheelDamaged = false;

    [Header("Settings")]
    public float hitCooldown = 0.5f; // Prevent multi-hits in 1 frame
    private float lastHitTime;

    public void TakeDamage(float amount, CollisionDetector.WheelPosition part)
    {
        if (Time.time < lastHitTime + hitCooldown) return;

        health -= amount;
        lastHitTime = Time.time;

        if (part == CollisionDetector.WheelPosition.Left) leftWheelDamaged = true;
        if (part == CollisionDetector.WheelPosition.Right) rightWheelDamaged = true;

        if (health <= 0) Die();
    }

    void Die()
    {
        //this.gameObject.SetActive(false);
        Debug.Log("Game Over");
    }
}
