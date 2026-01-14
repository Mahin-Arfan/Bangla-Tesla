using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

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

    [Header("Game Settings")]
    public int score = 0;
    public float maxDificultyScore = 1000f;
    private float progress = 0f;
    public bool gameStarted = false;
    public bool gameOver = false;
    private bool gameInitiaded = false;
    private bool gameOverInitiaded = false;

    [Header("Road Settings")]
    public Road[] roads;
    public float roadSpawnDistance = 110f;

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
    public float startSpawnRate = 2f;
    public float maxSpawnRate = 10f;
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


    [Header("References")]
    public GameObject player;
    private PlayerRickshawController playerController;
    public UIScript uIScript;
    //temp
    public Vector3 gizmosSpawnPos;
    public Vector3 gizmosSpawnSize;

    void Awake()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = false;
    }
    void Start()
    {
        playerController = player.GetComponent<PlayerRickshawController>();
        uIScript = GetComponent<UIScript>();
        if (playerController == null)
        {
            Debug.LogError("PlayerRickshawController not found on player!");
        }
        if(uIScript == null)
        {
            Debug.LogError("UIScript not found on GameManagerScript!");
        }
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

        PrewarmPool(5);
    }

    void Update()
    {
        score = (int)Mathf.Abs(player.transform.position.z);
        UpdateDifficulty(score);
        HandleRoadSpawning();
        HandleVehicleSpawning();
        RecycleOldVehicles();
        if(gameStarted && !gameInitiaded)
        {
            StartGame();
        }
        if(gameOver && !gameOverInitiaded)
        {
            GameOver();
        }
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
        //if (!gameStarted && chosenGroupIndex == 0) return;
        VehicleGroup chosenGroup = vehicleGroups[chosenGroupIndex];

        bool foundFreeSpot = false;
        Vector3 spawnPos = Vector3.zero;
        for (int i = 0; i < 6; i++)   // try 6 times max
        {
            float spawnX = Random.Range(chosenGroup.xRange.x, chosenGroup.xRange.y);

            if (gameStarted && !gameOver)
            {
                spawnPos = new Vector3(spawnX, 1f, player.transform.position.z - spawnDistance);
            }
            else
            {
                if (spawnX > 2.5f) spawnX = 2.5f;
                spawnPos = new Vector3(spawnX, 1f, player.transform.position.z + spawnDistance);
            }

            if (!Physics.CheckBox(spawnPos, chosenGroup.spawnCheckSize, Quaternion.identity))
            {
                foundFreeSpot = true;
                break;
            }
        }
        // ❌ If all attempts failed → skip this spawn safely
        if (!foundFreeSpot)
        {
            return;
        }

        GameObject chosenVehicle = GetVehiclePrefab(chosenGroupIndex);
        if(chosenVehicle == null)   return;

        GameObject vehicle = GetFromPool(chosenVehicle);

        vehicle.transform.position = spawnPos;
        NPCVehicleController npcController = vehicle.transform.GetComponent<NPCVehicleController>();
        if(npcController != null)
        {
            vehicle.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            if (gameStarted)
            {
                npcController.currentMaxSpeed = Mathf.Lerp(npcController.minSpeed, npcController.maxSpeed, progress);
                npcController.currentStopDecision = npcController.randomStops;
                npcController.vehicleCanBeDamaged = true;
            }
            else
            {
                npcController.currentMaxSpeed = npcController.maxSpeed;
                npcController.currentStopDecision = false;
                npcController.vehicleCanBeDamaged = false;
            }
        }
        vehicle.SetActive(true);
        if(npcController != null) npcController.ResetNPC();
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

    void PrewarmPool(int maxInstantiate)
    {
        foreach (var group in vehicleGroups)
        {
            int prefabCount = group.prefabs.Length;

            // Number to instantiate = MIN(prefabs, maxInstantiate)
            int countToInstantiate = Mathf.Min(prefabCount, maxInstantiate);

            for (int i = 0; i < countToInstantiate; i++)
            {
                GameObject prefab = group.prefabs[i];

                if (!prefabPool.ContainsKey(prefab))
                    prefabPool[prefab] = new Queue<GameObject>();

                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);

                PooledVehicle pv = obj.AddComponent<PooledVehicle>();
                pv.prefabSource = prefab;

                prefabPool[prefab].Enqueue(obj);
            }
        }
    }


    void RecycleOldVehicles()
    {
        List<GameObject> toRecycle = new List<GameObject>();

        foreach (var pair in activeVehicles)
        {
            GameObject v = pair.Key;

            if (!v.activeSelf) continue;

            Vector3 playerPos = player.transform.position;
            Vector3 vPos = v.transform.position;
            NPCVehicleController npcScript = v.GetComponent<NPCVehicleController>();
            if (!gameStarted && !gameOver && (vPos.x > 2.5f && vPos.z > 17f) || npcScript.idleTime > 10f)
            {
                toRecycle.Add(v);
                continue;
            }

            bool outOfZRange = Mathf.Abs(vPos.z - playerPos.z) > recycleDistance;
            bool outOfYRange = vPos.y > 3f || vPos.y < -3f;
            bool rotatedWrong = Vector3.Angle(v.transform.forward, Vector3.forward) < 100f;
            float angle = Vector3.Angle(v.transform.forward, Vector3.forward);
            if (rotatedWrong)
            {
                Debug.Log("Recycling vehicle due to wrong rotation: " + v.name + "Rotated: " + angle);
            }

            if (outOfZRange || outOfYRange || rotatedWrong)
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

    void UpdateDifficulty(int score)
    {
        progress = Mathf.Clamp01(score / maxDificultyScore);
        progress = Mathf.SmoothStep(0f, 1f, progress);

        playerController.baseSpeed = Mathf.Lerp(playerController.startSpeed, playerController.maxSpeed, progress);
        spawnRate = Mathf.Lerp(startSpawnRate, maxSpawnRate, progress);
    }

    void StartGame()
    {
        playerController.enabled = true;
        gameInitiaded = true;
    }

    void GameOver()
    {
        gameOverInitiaded = true;
        uIScript.endMenuUI.SetActive(true);
    }

#if UNITY_EDITOR
    //temp gizmos
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;


        // ----------------------------------------
        // 4. DRAW LEFT SIDE CHECK BOX (checkPos)
        // ----------------------------------------
        Gizmos.color = Color.yellow;
        Gizmos.matrix = Matrix4x4.TRS(
            gizmosSpawnPos,
            Quaternion.identity,
            Vector3.one
        );
        Gizmos.DrawWireCube(Vector3.zero, gizmosSpawnSize * 2f);

        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}
