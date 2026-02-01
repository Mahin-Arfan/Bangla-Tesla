using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

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
    private float distanceProgress = 0f;

    [Header("References")]
    public GameManagerScript gameManager;
    public RickshawHealth rickshawHealthScript;
    private Transform rickshawTransform;

    void Start()
    {
        if (gameManager == null)
        {
            gameManager = GameObject.FindGameObjectWithTag("GameController").transform.GetComponent<GameManagerScript>();
        }
        if(rickshawHealthScript == null)
        {
            rickshawHealthScript = GameObject.FindGameObjectWithTag("Player").transform.GetComponent<RickshawHealth>();
        }
        rickshawTransform = rickshawHealthScript.transform;
        playerLayer = LayerMask.GetMask("Player");
        RespawnPickup();
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
        distanceProgress = Mathf.Clamp01(gameManager.score / gameManager.maxDificultyScore * 2);
        distanceProgress = Mathf.SmoothStep(0f, 1f, distanceProgress);
        float currentGap = Mathf.Lerp(minSpawnGap, maxSpawnGap, distanceProgress);
        nextSpawnZ = transform.position.z - currentGap;
        transform.position = new Vector3(posX, transform.position.y, nextSpawnZ);
    }
}
