using DG.Tweening;
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
    private float positionCheckTimer = 0f;

    [Header("PickUp Rotation Settings")]
    [SerializeField]
    private float rotationSpeed = 1f;
    [SerializeField]
    private Vector3 rotationVector = new Vector3(0f, 360f, 0f);

    [Header("References")]
    private RickshawHealth rickshawHealthScript;
    private GameManagerScript gameManager;
    public Transform rickshawTransform;

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
        rickshawTransform = rickshawHealthScript.transform;
        playerLayer = LayerMask.GetMask("Player");
        transform.DORotate(rotationVector, rotationSpeed, RotateMode.WorldAxisAdd).SetLoops(-1).SetEase(Ease.Linear);
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
        float currentGap = Mathf.Lerp(minSpawnGap, maxSpawnGap, gameManager.progress);
        nextSpawnZ = transform.position.z - currentGap;
        transform.position = new Vector3(posX, transform.position.y, nextSpawnZ);
    }

    public void GetPlayerReference(Transform player)
    {
        rickshawTransform = player;
        rickshawHealthScript = rickshawTransform.GetComponent<RickshawHealth>();
    }
}
