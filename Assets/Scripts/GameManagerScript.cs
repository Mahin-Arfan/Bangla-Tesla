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
        public Vector2 xRange; // random X range per spawn
        public float weight;
        public float bonusWeight;
        [HideInInspector]
        public int chosenVehicleIndex;
    }

    [Header("Vehicles")]
    public VehicleGroup[] vehicleGroups;

    [Header("Spawn Settings")]
    public float spawnRate = 2f; // base spawn rate
    public Vector3 spawnLocation;
    public float baseSpawnTime = 3f;
    public float spawnDistance = 80f; // distance in front of player to spawn
    public float spawnTimer;

    [Header("Collision Check")]
    private Vector3 checkSize = new Vector3(1.2f, 1f, 3f);

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

        VehicleGroup chosenGroup = ChooseWeightedType();
        GameObject chosenVehicle = GetVehiclePrefab(chosenGroup);
        if(chosenVehicle == null)   return;

        float spawnPositionX = Random.Range(chosenGroup.xRange.x, chosenGroup.xRange.y);

        // ✅ SPAWN IN FRONT (PLAYER MOVES -Z)
        Vector3 spawnPos = new Vector3(
            spawnPositionX,
            1f,
            player.transform.position.z - spawnDistance
        );

        GameObject vehicle = GetFromPool(chosenVehicle);

        BoxCollider col = vehicle.transform.GetComponent<NPCVehicleController>().vehicleBodyCollider;
        Debug.LogError(col);
        if (col == null) return;

        checkSize = col.size;
        checkSize.z += 10f;

        if (Physics.CheckBox(spawnPos, checkSize))
        {
            spawnTimer = 0f;   // ✅ reset even if blocked
            return;
        }

        vehicle.transform.position = spawnPos;
        vehicle.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        vehicle.SetActive(true);
        Debug.LogError("Spawned Vehicle: " + chosenGroup.type.ToString());
        activeVehicles[vehicle] = chosenGroup.type;
        spawnTimer = 0f;
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
    VehicleGroup ChooseWeightedType()
    {
        float totalWeight = 0f;
        foreach (var vehicle in vehicleGroups) totalWeight += vehicle.weight + vehicle.bonusWeight;

        float selectedWeight = Random.Range(0, totalWeight);
        float running = 0f;
        int index = 0;

        foreach (var vehicle in vehicleGroups)
        {
            running += vehicle.weight + vehicle.bonusWeight;
            if (selectedWeight <= running) return vehicleGroups[index];
            index++;
        }

        return vehicleGroups[0];
    }

    GameObject GetVehiclePrefab(VehicleGroup chosenGroup)
    {
        if (chosenGroup.prefabs.Length > 0)
        {
            GameObject vehiclePrefeb = chosenGroup.prefabs[chosenGroup.chosenVehicleIndex];
            if(chosenGroup.chosenVehicleIndex < chosenGroup.prefabs.Length - 1)
                chosenGroup.chosenVehicleIndex++;
            else
                chosenGroup.chosenVehicleIndex = 0;
            return vehiclePrefeb;
        }
        return null;
    }
}
