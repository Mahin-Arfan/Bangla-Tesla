using System.Collections.Generic;
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
    }

    [System.Serializable]
    public class SpawnPoint
    {
        public Vector2 xRange = new Vector2(-3f, 3f); // random X range per spawn
        public float spawnDistance = 40f;             // Z offset from player // distance IN FRONT (toward -Z)
        public float baseCooldown = 3f;               // spawn delay
        public VehicleChance[] vehicleSpawnChances;               // Taxi 60%, Car 40%, etc.

        [HideInInspector] public float timer;
    }

    [Header("Vehicles")]
    public VehicleGroup[] vehicleGroups;                 // Prefab pools

    [Header("Spawn Points (Follow Player)")]
    public SpawnPoint[] spawnPoints;

    [Header("Collision Check")]
    public Vector3 checkSize = new Vector3(1.2f, 1f, 3f);

    [Header("Pooling")]
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

        foreach (var sp in spawnPoints)
            sp.timer = sp.baseCooldown;
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
        float speedFactor = Mathf.Clamp01(Mathf.Abs(player.transform.position.z) / 400f);

        foreach (var sp in spawnPoints)
        {
            sp.timer += Time.deltaTime;
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
        if (activeVehicles.Count >= maxActiveVehicles)
            return;

        Type chosenType = ChooseWeightedType(sp.vehicleSpawnChances);
        GameObject prefab = GetPrefabFromType(chosenType, vehicleGroups);
        if (prefab == null) return;

        float x = Random.Range(sp.xRange.x, sp.xRange.y);

        // ✅ SPAWN IN FRONT (PLAYER MOVES -Z)
        Vector3 spawnPos = new Vector3(
            x,
            1f,
            player.transform.position.z - sp.spawnDistance
        );

        if (Physics.CheckBox(spawnPos, checkSize))
            return;

        GameObject vehicle = GetFromPool(prefab);
        vehicle.transform.position = spawnPos;
        vehicle.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        vehicle.SetActive(true);

        activeVehicles[vehicle] = chosenType;
        /*
        GameObject vehicle = GetFromPool(prefab);
        BoxCollider col = vehicle.transform.GetComponent<NPCVehicleController>().vehicleBodyCollider;
        Debug.LogError(col);
        if (col == null) return;
        checkSize = col.size * 0.5f;
        checkSize.z += 10f; // extra buffer

        if (Physics.CheckBox(spawnPos, checkSize))
            return;

        vehicle.transform.position = spawnPos;
        vehicle.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        vehicle.SetActive(true);

        activeVehicles[vehicle] = chosenType;
        */
    }

    // ───────────────────────── OBJECT POOL ─────────────────────────
    GameObject GetFromPool(GameObject prefab)
    {
        if (!prefabPool.ContainsKey(prefab))
            prefabPool[prefab] = new Queue<GameObject>();

        if (prefabPool[prefab].Count > 0)
            return prefabPool[prefab].Dequeue();

        return Instantiate(prefab);
    }

    void RecycleOldVehicles()
    {
        List<GameObject> toRecycle = new List<GameObject>();

        foreach (var v in activeVehicles.Keys)
        {
            if (!v.activeSelf) continue;

            // ✅ Vehicle moved far BEHIND the player
            if (v.transform.position.z > player.transform.position.z + recycleDistance)
            {
                toRecycle.Add(v);
            }
            // ✅ Safety clean-up
            else if (Mathf.Abs(v.transform.position.z - player.transform.position.z) > 150f)
            {
                toRecycle.Add(v);
            }
        }

        foreach (var vehicle in toRecycle)
        {
            Type type = activeVehicles[vehicle];
            activeVehicles.Remove(vehicle);

            GameObject prefab = GetPrefabFromType(type, vehicleGroups);
            prefabPool[prefab].Enqueue(vehicle);

            vehicle.SetActive(false);
        }
    }

    // ───────────────────────── UTILS ─────────────────────────
    Type ChooseWeightedType(VehicleChance[] list)
    {
        float total = 0f;
        foreach (var c in list) total += c.weight;

        float r = Random.Range(0, total);
        float running = 0f;

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
            if (g.type == type && g.prefabs.Length > 0)
                return g.prefabs[Random.Range(0, g.prefabs.Length)];
        }
        return null;
    }
}
