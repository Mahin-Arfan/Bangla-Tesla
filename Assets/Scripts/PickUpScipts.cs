using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PickUpScipts : MonoBehaviour
{
    public enum PickUpType { Health, Battery }

    [Header("PickUp Settings")]
    public PickUpType pickUpType = PickUpType.Health;
    public float minSpawnGap = 150f;
    public float maxSpawnGap = 400f;
    public float spawnXRange = 4.5f;
    private float nextSpawnZ;
    private LayerMask playerLayer;
    private float onTriggerEnterTime = 0f;
    private float positionCheckTimer = 0f;

    [Header("PickUp Rotation Settings")]
    [SerializeField]
    private float rotationSpeed = 1f;
    [SerializeField]
    private Vector3 rotationVector = new Vector3(0f, 360f, 0f);

    [Header("Directional Arrow Settings")]
    public RectTransform arrowRect;
    public Image iconImage; 
    public Image arrowImage;
    public TextMeshProUGUI textDistance;

    public float pulseScaleMultiplier = 1.2f;
    public float basePulseDuration = 0.5f;
    private Vector3 iconOriginalScale;

    [Header("Dynamic Pulse Speed")]
    public float maxPulseSpeedMultiplier = 4f;
    public float startSpeedingUpDistance = 50f;
    public float maxSpeedDistance = 20f;
    private Tween pulseTween;

    [Header("References")]
    private RickshawHealth rickshawHealthScript;
    private GameManagerScript gameManager;
    private Transform rickshawTransform;

    void Start()
    {
        if (gameManager == null)
        {
            gameManager = GameManagerScript.Instance;
        }
        if(rickshawHealthScript == null)
        {
            rickshawHealthScript = gameManager.player.transform.GetComponent<RickshawHealth>();
        }
        iconOriginalScale = arrowRect.localScale;
        rickshawTransform = rickshawHealthScript.transform;
        playerLayer = LayerMask.GetMask("Player");
        transform.DORotate(rotationVector, rotationSpeed, RotateMode.WorldAxisAdd).SetLoops(-1).SetEase(Ease.Linear);
        RespawnPickup();

        pulseTween = arrowRect.DOScale(iconOriginalScale * pulseScaleMultiplier, basePulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    void Update()
    {
        positionCheckTimer += Time.deltaTime;
        if(positionCheckTimer >= 1f)
        {
            positionCheckTimer = 0f;
            if (transform.position.z > rickshawTransform.position.z + 10f)
            {
                RespawnPickup();
            }
        }
        float distanceZ = Mathf.Abs(rickshawTransform.position.z - transform.position.z);
        if (rickshawTransform != null || arrowRect != null && distanceZ < 100f)
        {
            UpdateRotation();
            UpdateAlpha(distanceZ);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < onTriggerEnterTime + 0.5f) return;

        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            onTriggerEnterTime = Time.time;
            HandlePickup();
        }
    }

    void HandlePickup()
    {
        switch (pickUpType)
        {
            case PickUpType.Health:
                rickshawHealthScript.HealthPickUp();
                break;
            case PickUpType.Battery:
                rickshawHealthScript.BatteryPickUp();
                break;
        }
        RespawnPickup();
    }

    void RespawnPickup()
    {
        float posX = Random.Range(-spawnXRange, spawnXRange);
        float currentGap = Mathf.Lerp(minSpawnGap, maxSpawnGap, gameManager.progress);
        nextSpawnZ = transform.position.z - currentGap;
        transform.position = new Vector3(posX, transform.position.y, nextSpawnZ);
    }

    private void UpdateRotation()
    {
        Vector3 directionToTarget = transform.position - rickshawTransform.position;
        directionToTarget.y = 0;
        float angle = Vector3.SignedAngle(Vector3.back, directionToTarget, Vector3.up);
        arrowRect.localEulerAngles = new Vector3(0, 0, -angle);
    }

    private void UpdateAlpha(float distanceZ)
    {
        float playerZ = rickshawTransform.position.z;
        float targetZ = transform.position.z;
        float targetAlpha = 0f;

        if (playerZ > targetZ)
        {
            targetAlpha = Mathf.InverseLerp(100f, 50f, distanceZ);
        }
        else
        {
            targetAlpha = Mathf.InverseLerp(5f, 0f, distanceZ);
        }
        textDistance.text = Mathf.RoundToInt(distanceZ).ToString() + "m";
        Color currentColor = arrowImage.color;
        Color currentIconColor = iconImage.color;
        currentColor.a = targetAlpha;
        currentIconColor.a = targetAlpha;
        arrowImage.color = currentColor;
        iconImage.color = currentIconColor;
        textDistance.color = currentColor;
        UpdatePulseSpeed(distanceZ);
    }

    private void UpdatePulseSpeed(float distanceZ)
    {
        float speedUpPercentage = Mathf.InverseLerp(startSpeedingUpDistance, maxSpeedDistance, distanceZ);

        float currentSpeed = Mathf.Lerp(1f, maxPulseSpeedMultiplier, speedUpPercentage);

        if (pulseTween != null && pulseTween.IsActive())
        {
            pulseTween.timeScale = currentSpeed;
        }
    }

    void OnDisable()
    {
        arrowRect.DOKill();
    }

    public void GetPlayerReference(Transform player)
    {
        rickshawTransform = player;
        rickshawHealthScript = rickshawTransform.GetComponent<RickshawHealth>();
    }
}
