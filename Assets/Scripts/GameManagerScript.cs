using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class GameManagerScript : MonoBehaviour
{
    public static GameManagerScript Instance { get; private set; }

    [System.Serializable]
    public enum Type { Truck, Bus, Car, Cng, Bike, Rickshaw, Crane, Barrier, WrongSides, Pedestrian}

    [System.Serializable]
    public struct Road
    {
        public GameObject roadSegment;
        public Transform endPoint;
    }

    [System.Serializable]
    public struct SpecialRoad
    {
        public GameObject roadSegment;
        public Transform endPoint;
    }

    [Header("Game Settings")]
    public int score = 0;
    public float maxDificultyScore = 1000f;
    [HideInInspector] public float progress = 0f;
    public bool gameStarted = false;
    public bool gameOver = false;
    private bool gameInitiaded = false;
    private bool gameOverInitiaded = false;

    [Header("Road Settings")]
    public Road[] roads;
    public SpecialRoad[] specialRoads;
    public float roadSpawnDistance = 110f;
    public float specialRoadSpawnDistance = 500f;

    private Transform firstRoad;
    private Transform secondRoad;
    private Transform thirdRoad;

    private Vector3 roadSpawnLocation;
    private int currentRoadIndex = 2;
    private float firstRoadDistance;
    private float specialRoadSpawnLocation = 0f;
    private int specialRoadIndex = 0;

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
        public int prewarmPerPrefeb;
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

    [Header("Pedestrians")]
    public float pedestrianSpawnRate = 1f;
    public int maxActivePedestrians = 10; // New Limit just for people
    public Vector2 pedestrianXOffset = new Vector2(7f, 10.5f);
    public float pedestrianYOffset = 0f;
    public float padestrianSpawnDistance = 35f;
    public GameObject[] pedestrianPrefabs;
    private int pedestrianPrefabIndex = 0;
    private float pedestrianTimer;

    [Header("Pooling")]
    public float vehicleRecycleDistance = 90f;
    public float pedestrianRecycleDistance = 40f;
    public int maxActiveVehicles = 35;
    private float recycleTimer;

    private Dictionary<GameObject, Queue<GameObject>> prefabPool = new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<GameObject, Type> activeVehicles = new Dictionary<GameObject, Type>();
    private Dictionary<GameObject, Type> activePedestrians = new Dictionary<GameObject, Type>();
    private List<GameObject> toRecycle = new List<GameObject>();

    [Header("References")]
    public GameObject player;
    private PlayerRickshawController playerController;
    private UIScript uIScript;
    //temp
    public Vector3 gizmosSpawnPos;
    public Vector3 gizmosSpawnSize;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = false;
    }
    void Start()
    {
        if(player == null) player = GameObject.FindGameObjectWithTag("Player");
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

        roadSpawnLocation = roads[2].endPoint.position;

        for(int i = 0; i<specialRoads.Length; i++)
        {
            specialRoads[i].roadSegment.SetActive(false);
        }

        vehicleGroupIndex = new int[vehicleGroups.Length];
        for (int i = 0; i < vehicleGroupIndex.Length; i++)
            vehicleGroupIndex[i] = 0;

        PrewarmPool();
    }

    void Update()
    {
        score = (int)Mathf.Abs(player.transform.position.z);
        UpdateDifficulty();
        HandleRoadSpawning();
        HandleVehicleSpawning();
        HandlePedestrianSpawning();
        recycleTimer += Time.deltaTime;
        if (recycleTimer > 0.25f)
        {
            //RecycleOldVehicles(); //temp
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
        float specialRoadDistance = specialRoadSpawnLocation - player.transform.position.z;
        if (specialRoadDistance > specialRoadSpawnDistance)
        {
            SpawnSpecialRoad();
            return;
        }
        firstRoadDistance = Vector3.Distance(player.transform.position, firstRoad.position);
        if (firstRoadDistance > roadSpawnDistance)
            SpawnRoad();
    }

    void SpawnRoad()
    {
        firstRoad.position = roadSpawnLocation;

        Transform temp = firstRoad;
        firstRoad = secondRoad;
        secondRoad = thirdRoad;
        thirdRoad = temp;

        currentRoadIndex++;
        if (currentRoadIndex >= roads.Length)
            currentRoadIndex = 0;
        roadSpawnLocation = roads[currentRoadIndex].endPoint.position;
    }

    void SpawnSpecialRoad()
    {
        specialRoads[specialRoadIndex].roadSegment.transform.position = roadSpawnLocation;
        specialRoads[specialRoadIndex].roadSegment.SetActive(true);
        specialRoadSpawnLocation = roadSpawnLocation.z;
        roadSpawnLocation = specialRoads[specialRoadIndex].endPoint.position;
        specialRoadIndex++;
        if (specialRoadIndex >= specialRoads.Length)
            specialRoadIndex = 0;
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
        if(chosenGroup.type == Type.Barrier || chosenGroup.type == Type.WrongSides)
        {
            if (!gameStarted || gameOver) return;
            int spawnSide = Random.value > 0.5f ? 1 : -1;
            for (int i = 0; i < 2; i++)   // try 2 times max
            {
                spawnPos = new Vector3(5.2f * spawnSide, 1f, player.transform.position.z - spawnDistance);
                if (!Physics.CheckBox(spawnPos, chosenGroup.spawnCheckSize, Quaternion.identity))
                {
                    foundFreeSpot = true;
                    break;
                }
                spawnSide *= -1;
            }
        }
        else
        {
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
                    spawnPos = new Vector3(spawnX, 1f, 30f);
                }

                if (!Physics.CheckBox(spawnPos, chosenGroup.spawnCheckSize, Quaternion.identity))
                {
                    foundFreeSpot = true;
                    break;
                }
            }
        }
        if (!foundFreeSpot)
        {
            return;
        }

        GameObject chosenVehicle = GetVehiclePrefab(chosenGroupIndex);
        if(chosenVehicle == null)   return;

        GameObject vehicle = GetFromPool(chosenVehicle);

        if(chosenGroup.type == Type.Barrier)
        {
            spawnPos.y = 0f;
            if (spawnPos.x > 0)
            {
                vehicle.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            }
            else
            {
                vehicle.transform.rotation = Quaternion.Euler(-90f, 180f, 0f);
            }
        }
        vehicle.transform.position = spawnPos;
        vehicle.SetActive(true);
        activeVehicles[vehicle] = chosenGroup.type;
        spawnTimer = 0f;
        IncreaseBonusForUnselected(chosenGroup);
    }

    void TrySpawnPedestrian()
    {
        if (activePedestrians.Count >= maxActivePedestrians || pedestrianPrefabs.Length == 0)
            return;

        float sideMultiplier = (Random.value > 0.5f) ? 1f : -1f; // 1. Pick Side
        float spawnX = sideMultiplier * Random.Range(pedestrianXOffset.x, pedestrianXOffset.y);
        Vector3 spawnPos = Vector3.zero;
        if (gameStarted && !gameOver)
        {
            spawnPos = new Vector3(spawnX, pedestrianYOffset, player.transform.position.z - padestrianSpawnDistance);
        }
        else
        {
            float zPositionMultiplier = (Random.value > 0.5f) ? 1f : -1f;
            spawnPos = new Vector3(spawnX, pedestrianYOffset, player.transform.position.z + (zPositionMultiplier * padestrianSpawnDistance));
        }

        GameObject prefab = pedestrianPrefabs[pedestrianPrefabIndex];
        pedestrianPrefabIndex++;
        if (pedestrianPrefabIndex >= pedestrianPrefabs.Length)
        {
            pedestrianPrefabIndex = 0;
        }
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

    void PrewarmPool()
    {
        foreach (var group in vehicleGroups)    //For Vehicles
        {
            int prefabCount = group.prefabs.Length;

            for (int i = 0; i < prefabCount; i++)
            {
                for(int j = 0; j < group.prewarmPerPrefeb; j++)
                    CreateAndEnqueue(group.prefabs[i]);
            }
        }

        if (pedestrianPrefabs != null)  //For Pedestrians
        {
            foreach (GameObject prefab in pedestrianPrefabs)
            {
                CreateAndEnqueue(prefab);
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

    public void RecycleSingleVehicle(GameObject vehicleToRecycle)
    {
        if (activeVehicles.ContainsKey(vehicleToRecycle))
        {
            ReturnToPool(vehicleToRecycle);
            activeVehicles.Remove(vehicleToRecycle);
        }
    }

    public void RecycleSinglePedestrian(GameObject pedestrianToRecycle)
    {
        if (activePedestrians.ContainsKey(pedestrianToRecycle))
        {
            ReturnToPool(pedestrianToRecycle);
            activePedestrians.Remove(pedestrianToRecycle);
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

    void UpdateDifficulty()
    {
        progress = Mathf.Clamp01(score / maxDificultyScore);
        progress = Mathf.SmoothStep(0f, 1f, progress);

        playerController.baseSpeed = Mathf.Lerp(playerController.startSpeed, playerController.maxSpeed, progress);
        spawnRate = Mathf.Lerp(startSpawnRate, maxSpawnRate, progress);
    }

    void StartGame()
    {
        playerController.gameStarted = true;
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
