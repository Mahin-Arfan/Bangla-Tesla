
using UnityEngine;
using UnityEngine.UI;

public class RickshawHealth : MonoBehaviour
{
    [Header("Stats")]
    public bool invincible = false;
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
    private bool dieCausedByBattery = false;

    [Header("Battery Settings")]
    [Tooltip("How many meters can it go with full battery?")]
    public float initialRangeInMeters = 250f;
    public float currentBattery;
    private float drainCoefficient;

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
    public Slider healthSlider;
    public Slider easeHealthSlider;
    public Slider batterySlider;

    // Internal References
    private GameManagerScript gameManagerScript;
    private CameraScript cameraScript;
    private PlayerRickshawController playerRickshawController;
    private BoxCollider frontWheelBoxCollider;
    private BoxCollider baseBoxCollider;
    private BoxCollider leftWheelBoxCollider;
    private BoxCollider rightWheelBoxCollider;
    private BoxCollider rickshawBoxCollider;
    private CapsuleCollider rickshawCapsuleCollider;
    private Rigidbody frontWheelRigidbody;
    private Rigidbody baseRigidbody;
    private Rigidbody leftWheelRigidbody;
    private Rigidbody rightWheelRigidbody;
    private Rigidbody rb;
    int pedestrianHitScore = 150;
    int bikeHitScore = 100;
    int vehicleHitScore = 50;

    void Start()
    {
        gameManagerScript = GameManagerScript.Instance;
        cameraScript = GetComponent<CameraScript>();
        rb = GetComponent<Rigidbody>();
        playerRickshawController = GetComponent<PlayerRickshawController>();
        frontWheelBoxCollider = frontWheelCollider.GetComponent<BoxCollider>();
        baseBoxCollider = baseCollider.GetComponent<BoxCollider>();
        leftWheelBoxCollider = leftWheelTransform.GetComponent<BoxCollider>();
        rightWheelBoxCollider = rightWheelTransform.GetComponent<BoxCollider>();
        rickshawBoxCollider = GetComponent<BoxCollider>();
        rickshawCapsuleCollider = GetComponent<CapsuleCollider>();
        frontWheelRigidbody = frontWheelCollider.GetComponent<Rigidbody>();
        baseRigidbody = baseCollider.GetComponent<Rigidbody>();
        leftWheelRigidbody = leftWheelTransform.GetComponent<Rigidbody>();
        rightWheelRigidbody = rightWheelTransform.GetComponent<Rigidbody>();
        currentBattery = maxBattery;
        drainCoefficient = maxBattery / initialRangeInMeters;
        pedestrianHitScore = gameManagerScript.pedestrianHitScore;
        bikeHitScore = gameManagerScript.bikeHitScore;
        vehicleHitScore = gameManagerScript.vehicleHitScore;
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
        if (playerRickshawController.outOfBattery && playerRickshawController.currentSpeed < 0.1f && !isDead)
        {
            dieCausedByBattery = true;
            Die();
        }
        if (!playerRickshawController.gameStarted) return;
        ApplyWheelJiggle();
        if (currentBattery > 0 && playerRickshawController.enabled)
        {
            UpdateBatteryHealth();
            batterySlider.value = currentBattery;
        }
    }

    public void TakeDamage(int hitLayer, CollisionDetector.WheelPosition wheelPos)
    {
        if(invincible) return; //Temp
        if (Time.time < lastHitTime + hitCooldown || isDead || !playerRickshawController.gameStarted) return;

        AudioManager.Instance.PlayCrash(transform.position);
        float damageToApply = 0f;

        if (wheelPos == CollisionDetector.WheelPosition.Front)
        {
            if (((1 << hitLayer) & pedestrianLayer) != 0)
            {
                damageToApply = 20f;
                gameManagerScript.AddScore(pedestrianHitScore);
            }
            else if (((1 << hitLayer) & BikeLayer) != 0)
            {
                damageToApply = 50f;
                gameManagerScript.AddScore(bikeHitScore);
            }
            else
            {
                damageToApply = 100f;
                gameManagerScript.AddScore(vehicleHitScore);
            }
        }
        else
        {
            if (((1 << hitLayer) & pedestrianLayer) != 0)
            {
                damageToApply = 10f;
                gameManagerScript.AddScore(pedestrianHitScore);
            }
            else if (((1 << hitLayer) & BikeLayer) != 0)
            {
                damageToApply = 15f;
                gameManagerScript.AddScore(bikeHitScore);
            }
            else
            {
                damageToApply = 20f;
                gameManagerScript.AddScore(vehicleHitScore);
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

        if (health <= 0)
        { 
            dieCausedByBattery = false;
            Die(); 
        }
    }

    void UpdateBatteryHealth()
    {
        float drainAmount = playerRickshawController.currentSpeed * drainCoefficient * Time.deltaTime;
        currentBattery -= drainAmount;
        if (currentBattery <= 0)
        {
            currentBattery = 0;
            if (!playerRickshawController.outOfBattery)
            {
                playerRickshawController.outOfBattery = true;
                //Play a "Power Down" sound here
            }
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
        if(playerRickshawController.outOfBattery)
        {
            playerRickshawController.outOfBattery = false;
            //Play a "Power Up" sound here
        }
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
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        gameManagerScript.gameOver = true;
        colliders.SetActive(false);
        playerRickshawController.enabled = false;

        if (!dieCausedByBattery) 
        {
            frontWheelBoxCollider.enabled = true;
            baseBoxCollider.enabled = true;
            leftWheelBoxCollider.enabled = true;
            rightWheelBoxCollider.enabled = true;
            rickshawCapsuleCollider.enabled = false;
            rickshawBoxCollider.enabled = false;
            frontWheelRigidbody.isKinematic = false;
            baseRigidbody.isKinematic = false;
            leftWheelRigidbody.isKinematic = false;
            rightWheelRigidbody.isKinematic = false;
            rickshawManAnimator.enabled = false;
            foreach (var col in rickshawManColliders)
            {
                col.enabled = true;
            }
            foreach (var rb in rickshawManRigidBodies)
            {
                rb.isKinematic = false;
            }
        }
    }
}
