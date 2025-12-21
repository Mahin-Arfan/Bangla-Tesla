using Unity.VisualScripting;
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

    [Header("Wheel References")]
    public Transform leftWheelTransform;
    public Transform rightWheelTransform;

    [Header("Jiggle Settings")]
    public float jiggleSpeed = 20f;

    void Update()
    {
        ApplyWheelJiggle();
    }

    public void TakeDamage(float amount, CollisionDetector.WheelPosition part)
    {
        if (Time.time < lastHitTime + hitCooldown) return;

        health -= amount;
        lastHitTime = Time.time;

        if (part == CollisionDetector.WheelPosition.Left) leftWheelDamaged = true;
        if (part == CollisionDetector.WheelPosition.Right) rightWheelDamaged = true;

        if (health <= 0) Die();
    }

    void ApplyWheelJiggle()
    {
        if (health >= 100 && !leftWheelDamaged && !rightWheelDamaged) return;

        float damagePercent = Mathf.InverseLerp(100f, 20f, health);

        float maxJiggle = Mathf.Lerp(0f, 12f, damagePercent);

        float jiggleOffset = Mathf.Sin(Time.time * jiggleSpeed) * (maxJiggle / 2f);

        if (leftWheelDamaged)
        {
            ApplyRotation(leftWheelTransform, jiggleOffset);
        }

        if (rightWheelDamaged)
        {
            ApplyRotation(rightWheelTransform, jiggleOffset);
        }
    }
    void ApplyRotation(Transform wheel, float offset)
    {
        Vector3 rot = wheel.localEulerAngles;
        rot.y = offset;
        wheel.localEulerAngles = rot;
    }

    void Die()
    {
        Debug.Log("Game Over");
    }
}
