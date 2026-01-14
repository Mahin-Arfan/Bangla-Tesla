using UnityEngine;

public class RickshawHealth : MonoBehaviour
{
    [Header("Stats")]
    public float health = 100f;
    public bool isDead = false;
    public bool leftWheelDamaged = false;
    public bool rightWheelDamaged = false;

    [Header("Settings")]
    public float hitCooldown = 0.5f; // Prevent multi-hits in 1 frame
    private float lastHitTime = 0f;

    [Header("Wheel References")]
    public Transform leftWheelTransform;
    public Transform rightWheelTransform;

    [Header("Jiggle Settings")]
    public float jiggleSpeed = 20f;

    [Header("References")]
    public Transform baseCollider;
    public Transform frontWheelCollider;
    public Animator rickshawManAnimator;
    public Collider[] rickshawManColliders;
    public Rigidbody[] rickshawManRigidBodies;
    public GameObject colliders;
    public GameManagerScript gameManagerScript;

    void Update()
    {
        if(!isDead) ApplyWheelJiggle();
    }

    public void TakeDamage(float amount, CollisionDetector.WheelPosition part)
    {
        if (Time.time < lastHitTime + hitCooldown || isDead) return;
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
        isDead = true;
        gameManagerScript.gameOver = true;
        frontWheelCollider.GetComponent<BoxCollider>().enabled = true;
        baseCollider.GetComponent<BoxCollider>().enabled = true;
        colliders.SetActive(false);
        leftWheelTransform.GetComponent<BoxCollider>().enabled = true;
        rightWheelTransform.GetComponent<BoxCollider>().enabled = true;
        GetComponent<PlayerRickshawController>().enabled = false;
        GetComponent<CapsuleCollider>().enabled = false;
        GetComponent<BoxCollider>().enabled = false;
        frontWheelCollider.GetComponent<Rigidbody>().isKinematic = false;
        baseCollider.GetComponent<Rigidbody>().isKinematic = false;
        leftWheelTransform.GetComponent<Rigidbody>().isKinematic = false;
        rightWheelTransform.GetComponent<Rigidbody>().isKinematic = false;
        rickshawManAnimator.enabled = false;
        GetComponent<Rigidbody>().isKinematic = true;
        foreach (var col in rickshawManColliders)
        {
            col.enabled = true;
        }
        foreach(var rb in rickshawManRigidBodies)
        {
            rb.isKinematic = false;
        }
    }
}
