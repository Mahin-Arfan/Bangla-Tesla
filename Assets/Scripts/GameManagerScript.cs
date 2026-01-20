using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class GameManagerScript : MonoBehaviour
{
    [System.Serializable]
    public enum Type { Truck, Bus, Car, Cng, Bike, Rickshaw, Barrier, Pedestrian}

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

    [System.Serializable]
    public struct VehicleGroup
    {
        public Type type;
        public GameObject[] prefabs;
        public Vector2 xRange;
        public float weight;
        [HideInInspector] public float bonusWeight;
        public float weightIncrement;
        public float maxBonusWeight;
        public Vector3 spawnCheckSize;
    }

    [Header("Vehicles")]
    public VehicleGroup[] vehicleGroups;
    private int[] vehicleGroupIndex;

    [Header("Vehicle Spawn Settings")]
    public float spawnRate = 2f; // base spawn rate
    public float startSpawnRate = 2f;
    public float maxSpawnRate = 10f;
    public Vector3 spawnLocation;
    public float baseSpawnTime = 3f;
    public float spawnDistance = 80f;
    public float spawnTimer;
    private float recycleTimer;

    [Header("Pedestrians")]
    public GameObject[] pedestrianPrefabs;
    public float pedestrianSpawnRate = 1f;
    public int maxActivePedestrians = 10; // New Limit just for people
    public float pedestrianXOffset = 4.5f;
    public float pedestrianYOffset = 0f;

    private float pedestrianTimer;

    [Header("Pooling")]
    public float recycleDistance = 90f;
    public int maxActiveVehicles = 35;

    private Dictionary<GameObject, Queue<GameObject>> prefabPool = new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<GameObject, Type> activeVehicles = new Dictionary<GameObject, Type>();
    private Dictionary<GameObject, Type> activePedestrians = new Dictionary<GameObject, Type>();
    private List<GameObject> toRecycle = new List<GameObject>();

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
        HandlePedestrianSpawning();
        recycleTimer += Time.deltaTime;
        if (recycleTimer > 0.25f)
        {
            RecycleOldVehicles();
            recycleTimer = 0f;
        }
        if (gameStarted && !gameInitiaded)
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

    void HandleVehicleSpawning()
    {
        spawnTimer += spawnRate * Time.deltaTime;
        if (spawnTimer >= baseSpawnTime)
        {
            TrySpawnVehicle();
        }
    }

    void HandlePedestrianSpawning()
    {
        pedestrianTimer += Time.deltaTime;
        if (pedestrianTimer >= pedestrianSpawnRate)
        {
            TrySpawnPedestrian();
            pedestrianTimer = 0f;
        }
    }

    void TrySpawnVehicle()
    {
        if (activeVehicles.Count >= maxActiveVehicles)
            return;

        int chosenGroupIndex = ChooseWeightedTypeIndex();
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

    void TrySpawnPedestrian()
    {
        if (activePedestrians.Count >= maxActivePedestrians || pedestrianPrefabs.Length == 0)
            return;

        float sideMultiplier = (Random.value > 0.5f) ? 1f : -1f; // 1. Pick Side
        float spawnX = sideMultiplier * pedestrianXOffset;

        Vector3 spawnPos = new Vector3(spawnX, pedestrianYOffset, player.transform.position.z + spawnDistance);

        GameObject prefab = pedestrianPrefabs[Random.Range(0, pedestrianPrefabs.Length)];
        GameObject ped = GetFromPool(prefab);

        ped.transform.position = spawnPos;

        ped.SetActive(true);

        activePedestrians[ped] = Type.Pedestrian;
    }

    GameObject GetFromPool(GameObject prefab)
    {
        if (!prefabPool.ContainsKey(prefab))
            prefabPool[prefab] = new Queue<GameObject>();

        if (prefabPool[prefab].Count > 0)
            return prefabPool[prefab].Dequeue();

        GameObject obj = Instantiate(prefab);
        Debug.Log("Instantiating new object for prefab: " + prefab.name);
        PooledVehicle pv = obj.AddComponent<PooledVehicle>();
        pv.prefabSource = prefab;
        pv.controller = obj.GetComponent<NPCVehicleController>();

        return obj;
    }

    void PrewarmPool(int maxInstantiate)
    {
        foreach (var group in vehicleGroups)    //For Vehicles
        {
            int prefabCount = group.prefabs.Length;
            int countToInstantiate = Mathf.Min(prefabCount, maxInstantiate);

            for (int i = 0; i < countToInstantiate; i++)
            {
                CreateAndEnqueue(group.prefabs[i]);
            }
        }

        if (pedestrianPrefabs != null)  //For Pedestrians
        {
            foreach (GameObject prefab in pedestrianPrefabs)
            {
                for (int i = 0; i < 5; i++)
                {
                    CreateAndEnqueue(prefab);
                }
            }
        }
    }

    void CreateAndEnqueue(GameObject prefab)
    {
        if (!prefabPool.ContainsKey(prefab))
            prefabPool[prefab] = new Queue<GameObject>();

        GameObject obj = Instantiate(prefab);
        obj.SetActive(false);

        PooledVehicle pv = obj.AddComponent<PooledVehicle>();
        pv.prefabSource = prefab;

        pv.controller = obj.GetComponent<NPCVehicleController>();

        prefabPool[prefab].Enqueue(obj);
    }


    void RecycleOldVehicles()
    {
        toRecycle.Clear();
        Vector3 playerPos = player.transform.position;

        foreach (var pair in activeVehicles) // For Vehicles
        {
            GameObject v = pair.Key;

            if (!v.activeSelf) continue;

            Vector3 vPos = v.transform.position;
            NPCVehicleController npcScript = v.GetComponent<PooledVehicle>().controller;
            if (npcScript != null)
            {
                if (npcScript.idleTime > 10f)
                {
                    toRecycle.Add(v);
                    continue;
                }
            }
            if (!gameStarted && !gameOver && (vPos.x > 2.5f && vPos.z > 17f))
            {
                toRecycle.Add(v);
                continue;
            }

            bool outOfZRange = Mathf.Abs(vPos.z - playerPos.z) > recycleDistance;
            bool outOfYRange = vPos.y > 3f || vPos.y < -3f;
            bool rotatedWrong = Vector3.Angle(v.transform.forward, Vector3.forward) < 100f;

            if (outOfZRange || outOfYRange || rotatedWrong)
            {
                toRecycle.Add(v);
            }
        }

        foreach (GameObject v in toRecycle)
        {
            ReturnToPool(v);
            activeVehicles.Remove(v);
        }
        toRecycle.Clear();

        foreach (var pair in activePedestrians) //For Pedestrians
        {
            GameObject p = pair.Key;
            if (!p.activeSelf) continue;

            bool outOfZRange = Mathf.Abs(p.transform.position.z - playerPos.z) > recycleDistance;

            if (outOfZRange)
            {
                toRecycle.Add(p);
            }
        }

        // Apply Pedestrian Recycle
        foreach (GameObject p in toRecycle)
        {
            ReturnToPool(p);
            activePedestrians.Remove(p);
        }
    }

    void ReturnToPool(GameObject obj)
    {
        PooledVehicle pv = obj.GetComponent<PooledVehicle>();
        if (pv != null && pv.prefabSource != null)
        {
            if (!prefabPool.ContainsKey(pv.prefabSource))
                prefabPool[pv.prefabSource] = new Queue<GameObject>();

            obj.SetActive(false);
            prefabPool[pv.prefabSource].Enqueue(obj);
        }
        else
        {
            obj.SetActive(false); // Just hide if something is wrong
        }
    }

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
                vehicleGroups[i].bonusWeight = 0f; // Reset bonus for selected type
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
