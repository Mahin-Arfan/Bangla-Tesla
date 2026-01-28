
using UnityEngine;
using UnityEngine.UI;

public class RickshawHealth : MonoBehaviour
{
    [Header("Stats")]
    public float health = 100f;
    public float maxBattery = 100f;
    public bool isDead = false;
    public bool leftWheelDamaged = false;
    public bool rightWheelDamaged = false;

    [Header("Settings")]
    public float hitCooldown = 0.5f;
    public float healthDrainSpeed = 0.025f;
    private float lastHitTime = 0f;
    public float leftWheelHealth = 100f;
    public float rightWheelHealth = 100f;
    public LayerMask pedestrianLayer;
    public LayerMask BikeLayer;
    public LayerMask obstacleLayer;

    [Header("Battery Settings")]
    [Tooltip("How many meters can the it go with full battery?")]
    public float initialRangeInMeters = 250f;
    public float currentBattery;
    public float drainCoefficient;

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
    private PlayerRickshawController playerRickshawController;
    public Slider healthSlider;
    public Slider easeHealthSlider;
    public Slider batterySlider;
    private CameraScript cameraScript;

    void Start()
    {
        cameraScript = GetComponent<CameraScript>();
        playerRickshawController = GetComponent<PlayerRickshawController>();
        currentBattery = maxBattery;
        drainCoefficient = maxBattery / initialRangeInMeters;
    }

    void Update()
    {
        if(healthSlider.value != health)
        {
            healthSlider.value = health;
        }
        if(healthSlider.value != easeHealthSlider.value)
        {
            easeHealthSlider.value = Mathf.Lerp(easeHealthSlider.value, health, Time.deltaTime * healthDrainSpeed);
        }
        if (isDead) return;
        if (!playerRickshawController.gamgeStarted) return;
        ApplyWheelJiggle();
        if (currentBattery > 0 && playerRickshawController.enabled)
        {
            UpdateBatteryHealth();
            batterySlider.value = currentBattery;
        }
    }

    public void TakeDamage(int hitLayer, CollisionDetector.WheelPosition wheelPos)
    {
        if (Time.time < lastHitTime + hitCooldown || isDead) return;

        float damageToApply = 0f;

        if (wheelPos == CollisionDetector.WheelPosition.Front)
        {
            if (((1 << hitLayer) & pedestrianLayer) != 0)
            {
                damageToApply = 20f;
            }
            else if (((1 << hitLayer) & BikeLayer) != 0)
            {
                damageToApply = 50f;
            }
            else
            {
                damageToApply = 100f;
            }
        }
        else
        {
            if (((1 << hitLayer) & pedestrianLayer) != 0)
            {
                damageToApply = 10f;
            }
            else
            {
                damageToApply = 20f;
            }

            if (wheelPos == CollisionDetector.WheelPosition.Left)
            {
                leftWheelDamaged = true;
                leftWheelHealth -= damageToApply;
            }
            else if (wheelPos == CollisionDetector.WheelPosition.Right)
            {
                rightWheelDamaged = true;
                rightWheelHealth -= damageToApply;
            }
        }

        health -= damageToApply;
        lastHitTime = Time.time;

        if (cameraScript != null) cameraScript.TriggerShake();

        if (health <= 0) Die();
    }

    void UpdateBatteryHealth()
    {
        float drainAmount = playerRickshawController.currentSpeed * drainCoefficient * Time.deltaTime;
        currentBattery -= drainAmount;
        if (currentBattery <= 0)
        {
            currentBattery = 0;
            Debug.Log("Battery Empty! at - " + transform.position.z);
        }
    }

    public void HealthPickUp()
    {
        if(isDead) return;
        health = 100f;
        leftWheelHealth = 100f;
        rightWheelHealth = 100f;
    }

    public void BatteryPickUp()
    {
        if(isDead) return;
        currentBattery = maxBattery;
    }

    void ApplyWheelJiggle()
    {
        if (health >= 100 && !leftWheelDamaged && !rightWheelDamaged) return;

        if (leftWheelDamaged)
        {
            ApplyRotation(leftWheelTransform, leftWheelHealth);
        }

        if (rightWheelDamaged)
        {
            ApplyRotation(rightWheelTransform, rightWheelHealth);
        }
    }
    void ApplyRotation(Transform wheel, float wheelHealth)
    {
        float damagePercent = Mathf.InverseLerp(100f, 50f, wheelHealth);
        float maxJiggle = Mathf.Lerp(0f, 12f, damagePercent);
        float jiggleOffset = Mathf.Sin(Time.time * jiggleSpeed) * (maxJiggle / 2f);
        Vector3 rot = wheel.localEulerAngles;
        rot.y = jiggleOffset;
        wheel.localEulerAngles = rot;
    }

    void Die()
    {
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
