using UnityEngine;

public class GameManagerScript : MonoBehaviour
{
    [System.Serializable]
    public enum Type { Truck, Bus, Car, Cng, Bike, Rickshaw, Barrier }

    // ──────────────────────────────────────────────────────────────
    // ROAD SYSTEM
    // ──────────────────────────────────────────────────────────────
    [System.Serializable]
    public struct Road
    {
        public GameObject roadSegment;
        public Transform endPoint;
    }

    [Header("Road Settings")]
    public Road[] roads;
    public float roadSpawnDistance = 110f;

    public GameObject player;

    private Transform firstRoad;
    private Transform secondRoad;
    private Transform thirdRoad;

    private Transform[] spawnLocations = new Transform[3];
    private int currentRoadIndex = 2;
    private float firstRoadDistance;

    // ──────────────────────────────────────────────────────────────
    // VEHICLE SPAWNING SYSTEM
    // ──────────────────────────────────────────────────────────────
    [System.Serializable]
    public struct VehicleChance
    {
        public Type type;
        public float weight;
    }

    [System.Serializable]
    public struct VehicleGroup
    {
        public Type type;
        public GameObject[] prefabs;
    }

    [System.Serializable]
    public class SpawnPoint
    {
        public Vector2 xRange = new Vector2(-3f, 3f); // random X range per spawn
        public float spawnDistance = 40f;             // Z offset from player
        public float baseCooldown = 3f;               // spawn delay
        public VehicleChance[] chances;               // Taxi 60%, Car 40%, etc.
        public VehicleGroup[] groups;                 // Prefab pools

        [HideInInspector] public float timer;
    }

    [Header("Spawn Points (Follow Player)")]
    public SpawnPoint[] spawnPoints;

    [Header("Collision Check")]
    public Vector3 checkSize = new Vector3(1.2f, 1f, 3f);

    void Start()
    {
        // road initialization
        firstRoad = roads[0].roadSegment.transform;
        secondRoad = roads[1].roadSegment.transform;
        thirdRoad = roads[2].roadSegment.transform;

        spawnLocations[0] = roads[0].endPoint;
        spawnLocations[1] = roads[1].endPoint;
        spawnLocations[2] = roads[2].endPoint;
    }

    void Update()
    {
        HandleRoadSpawning();
        HandleVehicleSpawning();
    }

    void HandleRoadSpawning()
    {
        firstRoadDistance = Vector3.Distance(player.transform.position, firstRoad.position);

        if (firstRoadDistance > roadSpawnDistance)
            SpawnRoad();
    }

    void SpawnRoad()
    {
        firstRoad.position = spawnLocations[currentRoadIndex].position;

        Transform temp = firstRoad;
        firstRoad = secondRoad;
        secondRoad = thirdRoad;
        thirdRoad = temp;

        currentRoadIndex++;
        if (currentRoadIndex > 2)
            currentRoadIndex = 0;
    }

    void HandleVehicleSpawning()
    {
        foreach (var sp in spawnPoints)
        {
            sp.timer += Time.deltaTime;

            // spawn faster the longer the player drives
            float speedFactor = Mathf.Clamp01(player.transform.position.z / 400f);
            float cooldown = Mathf.Lerp(sp.baseCooldown, 0.5f, speedFactor);

            if (sp.timer >= cooldown)
            {
                TrySpawnVehicle(sp);
                sp.timer = 0f;
            }
        }
    }

    void TrySpawnVehicle(SpawnPoint sp)
    {
        // choose type by weighted chance
        Type chosenType = ChooseWeightedType(sp.chances);

        // get prefab from that type
        GameObject prefab = GetPrefabFromType(chosenType, sp.groups);
        if (prefab == null) return;

        // determine spawn position
        float x = Random.Range(sp.xRange.x, sp.xRange.y);
        Vector3 spawnPos = new Vector3(x, 1f, player.transform.position.z + sp.spawnDistance);

        // block if other vehicle present
        if (Physics.CheckBox(spawnPos, checkSize))
            return;

        Instantiate(prefab, spawnPos, Quaternion.identity);
    }

    Type ChooseWeightedType(VehicleChance[] list)
    {
        float total = 0f;
        foreach (var c in list) total += c.weight;

        float r = Random.Range(0, total);
        float running = 0;

        foreach (var c in list)
        {
            running += c.weight;
            if (r <= running) return c.type;
        }
        return list[0].type;
    }

    GameObject GetPrefabFromType(Type type, VehicleGroup[] groups)
    {
        foreach (var g in groups)
        {
            if (g.type == type)
                return g.prefabs[Random.Range(0, g.prefabs.Length)];
        }
        return null;
    }
}
