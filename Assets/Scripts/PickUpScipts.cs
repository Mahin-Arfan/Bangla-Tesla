using UnityEngine;

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

    [Header("References")]
    public GameManagerScript gameManager;
    public RickshawHealth rickshawHealthScript;

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
        playerLayer = LayerMask.GetMask("Player");
        RespawnPickup();
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
        float currentProgress = (gameManager != null) ? gameManager.progress : 0f;
        float currentGap = Mathf.Lerp(minSpawnGap, maxSpawnGap, currentProgress);
        nextSpawnZ = transform.position.z - currentGap;
        transform.position = new Vector3(posX, transform.position.y, nextSpawnZ);
    }
}
