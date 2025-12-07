using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManagerScript : MonoBehaviour
{
    [System.Serializable]
    public enum Type { Truck, Bus, Car, Cng, Bike, Rickshaw, Barrier }

    // ───────────────────────── ROAD SYSTEM ─────────────────────────
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

    // ───────────────────────── VEHICLE SPAWNING ─────────────────────────
    [System.Serializable]
    public struct VehicleGroup
    {
        public Type type;
        public GameObject[] prefabs;
        public Vector2 xRange; // random X range per spawn
        public float weight;
        [HideInInspector] public float bonusWeight;
        public float weightIncrement;
        public float maxBonusWeight;
        public Vector3 spawnCheckSize;
    }

    [Header("Vehicles")]
    public VehicleGroup[] vehicleGroups;
    private int[] vehicleGroupIndex;

    [Header("Spawn Settings")]
    public float spawnRate = 2f; // base spawn rate
    public Vector3 spawnLocation;
    public float baseSpawnTime = 3f;
    public float spawnDistance = 80f; // distance in front of player to spawn
    public float spawnTimer;

    [Header("Pooling")]
    public float recycleDistance = 80f;
    public int maxActiveVehicles = 35;

    private Dictionary<GameObject, Queue<GameObject>> prefabPool =
        new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<GameObject, Type> activeVehicles =
            new Dictionary<GameObject, Type>();


    void Start()
    {
        // road initialization
        firstRoad = roads[0].roadSegment.transform;
        secondRoad = roads[1].roadSegment.transform;
        thirdRoad = roads[2].roadSegment.transform;

        spawnLocations[0] = roads[0].endPoint;
        spawnLocations[1] = roads[1].endPoint;
        spawnLocations[2] = roads[2].endPoint;

        vehicleGroupIndex = new int[vehicleGroups.Length];
        for (int i = 0; i < vehicleGroupIndex.Length; i++)
            vehicleGroupIndex[i] = 0;
    }

    void Update()
    {
        HandleRoadSpawning();
        HandleVehicleSpawning();
        RecycleOldVehicles();
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

    // ───────────────────────── VEHICLE SPAWN ─────────────────────────
    void HandleVehicleSpawning()
    {
        spawnTimer += spawnRate * Time.deltaTime;
        if (spawnTimer >= baseSpawnTime)
        {
            TrySpawnVehicle();
        }
    }

    void TrySpawnVehicle()
    {
        if (activeVehicles.Count >= maxActiveVehicles)
            return;

        int chosenGroupIndex = ChooseWeightedTypeIndex();
        GameObject chosenVehicle = GetVehiclePrefab(chosenGroupIndex);
        if(chosenVehicle == null)   return;

        GameObject vehicle = GetFromPool(chosenVehicle);
        VehicleGroup chosenGroup = vehicleGroups[chosenGroupIndex];
        bool foundFreeSpot = false;
        Vector3 spawnPos = Vector3.zero;

        for (int i = 0; i < 6; i++)   // try 6 times max
        {
            float spawnX = Random.Range(chosenGroup.xRange.x, chosenGroup.xRange.y);

            spawnPos = new Vector3(
                spawnX,
                1f,
                player.transform.position.z - spawnDistance
            );

            if (!Physics.CheckBox(spawnPos, chosenGroup.spawnCheckSize, Quaternion.identity))
            {
                foundFreeSpot = true;
                Debug.LogError("Spawned " + i + "th try.");
                break;
            }
        }

        // ❌ If all attempts failed → skip this spawn safely
        if (!foundFreeSpot)
        {
            Debug.LogError("Failed to spawn " + chosenGroup.type + " after max attempts.");
            return;
        }

        vehicle.transform.position = spawnPos;
        NPCVehicleController npcController = vehicle.transform.GetComponent<NPCVehicleController>();
        if(npcController != null)
        {
            vehicle.transform.rotation = npcController.reverseMechanics ? Quaternion.Euler(0f, 0f, 0f) : Quaternion.Euler(0f, 180f, 0f);
        }
        vehicle.SetActive(true);
        activeVehicles[vehicle] = chosenGroup.type;
        spawnTimer = 0f;
        IncreaseBonusForUnselected(chosenGroup);
    }

    // ───────────────────────── OBJECT POOL ─────────────────────────
    GameObject GetFromPool(GameObject prefab)
    {
        if (!prefabPool.ContainsKey(prefab))
            prefabPool[prefab] = new Queue<GameObject>();

        if (prefabPool[prefab].Count > 0)
            return prefabPool[prefab].Dequeue();

        GameObject obj = Instantiate(prefab);

        PooledVehicle pv = obj.AddComponent<PooledVehicle>();
        pv.prefabSource = prefab;

        return obj;
    }

    void RecycleOldVehicles()
    {
        List<GameObject> toRecycle = new List<GameObject>();

        foreach (var pair in activeVehicles)
        {
            GameObject v = pair.Key;

            if (!v.activeSelf) continue;

            // ✅ Recycle if far BEHIND player
            if (v.transform.position.z > player.transform.position.z + recycleDistance)
            {
                toRecycle.Add(v);
            }
        }

        foreach (GameObject vehicle in toRecycle)
        {
            PooledVehicle pv = vehicle.GetComponent<PooledVehicle>();

            if (pv == null || pv.prefabSource == null)
            {
                vehicle.SetActive(false);
                activeVehicles.Remove(vehicle);
                continue;
            }

            if (!prefabPool.ContainsKey(pv.prefabSource))
                prefabPool[pv.prefabSource] = new Queue<GameObject>();

            vehicle.SetActive(false);
            prefabPool[pv.prefabSource].Enqueue(vehicle);
            activeVehicles.Remove(vehicle);
        }
    }

    // ───────────────────────── UTILS ─────────────────────────
    int ChooseWeightedTypeIndex()
    {
        float totalWeight = 0f;
        foreach (var vehicle in vehicleGroups) totalWeight += vehicle.weight + vehicle.bonusWeight;

        float selectedWeight = Random.Range(0, totalWeight);
        float running = 0f;

        for (int i = 0; i < vehicleGroups.Length; i++)
        {
            running += vehicleGroups[i].weight + vehicleGroups[i].bonusWeight;

            if (selectedWeight <= running)
            {
                // ✅ RESET bonus for selected type
                Debug.LogError("Chosen Vehicle Type: " + vehicleGroups[i].type + "Weight: " + (vehicleGroups[i].weight + vehicleGroups[i].bonusWeight));
                vehicleGroups[i].bonusWeight = 0f;
                return i;
            }
        }

        return 0;
    }

    void IncreaseBonusForUnselected(VehicleGroup selected)
    {
        for (int i = 0; i < vehicleGroups.Length; i++)
        {
            if (vehicleGroups[i].type == selected.type)
                continue;

            vehicleGroups[i].bonusWeight += vehicleGroups[i].weightIncrement;
            vehicleGroups[i].bonusWeight = Mathf.Clamp(
                vehicleGroups[i].bonusWeight,
                0f,
                vehicleGroups[i].maxBonusWeight
            );
        }
    }

    GameObject GetVehiclePrefab(int chosenGroupIndex)
    {
        if (vehicleGroups[chosenGroupIndex].prefabs.Length == 0)
            return null;

        GameObject vehiclePrefeb = vehicleGroups[chosenGroupIndex].prefabs[vehicleGroupIndex[chosenGroupIndex]];

        if (vehicleGroups[chosenGroupIndex].prefabs.Length - 1 > vehicleGroupIndex[chosenGroupIndex])
        {
            vehicleGroupIndex[chosenGroupIndex]++;
        }
        else
        {
            vehicleGroupIndex[chosenGroupIndex] = 0;
        } 
        return vehiclePrefeb;
    }
}
