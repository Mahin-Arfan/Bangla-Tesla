using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManagerScript : MonoBehaviour
{
    public static GameManagerScript Instance { get; private set; }

    [System.Serializable]
    public enum Type { Truck, Bus, Car, Cng, Bike, Rickshaw, Crane, Barrier, WrongSides, Pedestrian, Stall}

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
    private int score = 0;
    public int highScore = 0;
    public float maxDificultyScore = 1000f;
    private int pedestrianHitScore = 150;
    private int vehicleHitScore = 50;
    private int bikeHitScore = 100;
    private int passengerDropOffScore = 200;
    [HideInInspector] public float progress = 0f;
    [HideInInspector] public bool gameStarted = false;
    [HideInInspector] public bool gameOver = false;
    [HideInInspector] public bool tiltSteeringControl = true;

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
    public float startSpawnRate = 2f;
    public float maxSpawnRate = 10f;
    public float baseSpawnTime = 3f;
    public float spawnDistance = 80f;
    public float menuSpawnPositionZ = 20f;
    private float spawnTimer;
    private float spawnRate = 2f; // base spawn rate

    [Header("Pedestrians")]
    public float pedestrianSpawnRate = 1f;
    public int maxActivePedestrians = 10;
    public Vector2 pedestrianXOffset = new Vector2(7f, 10.5f);
    public float pedestrianYOffset = 0f;
    public float padestrianSpawnDistance = 35f;
    public float pedestrianRoadCrossProbability = 0.25f;
    public GameObject[] pedestrianPrefabs;
    private int pedestrianPrefabIndex = 0;
    private float pedestrianTimer;

    [Header("Stalls")]
    public float stallSpawnRate = 1f;
    public int maxActiveStalls = 10;
    public float stallSpawnDistance = 35f;
    public int prewarmStallsCount = 3;
    public GameObject[] stallPrefabs;
    private int stallPrefabIndex = 0;
    private float stallTimer;

    [Header("Passenger Settings")]
    public int passengerSpawnVariable = 10;
    private int totalPedestriansSpawned = 0;

    [Header("Pooling")]
    public float vehicleRecycleDistance = 90f;
    public float pedestrianRecycleDistance = 40f;
    public int maxActiveVehicles = 35;
    private float recycleTimer;

    [Header("Initial Spawn Settings")]
    public Vector3[] stallSpawnPositions;
    public int initialVehicleSpawnDistance = -30;
    public int pedestrianSpawnGaps = 5;
    public int vehicleSpawnGaps = 5;

    private Dictionary<GameObject, Queue<GameObject>> prefabPool = new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<GameObject, Type> activeVehicles = new Dictionary<GameObject, Type>();
    private Dictionary<GameObject, Type> activePedestrians = new Dictionary<GameObject, Type>();
    private Dictionary<GameObject, Type> activeStalls = new Dictionary<GameObject, Type>();
    private List<GameObject> toRecycle = new List<GameObject>();

    [Header("References")]
    public PickUpScipts[] pickUpScript;
    [HideInInspector] public Transform mainCamera;
    [HideInInspector] public GameObject player;
    private PlayerRickshawController playerController;
    [HideInInspector] public UIScript uIScript;
    [HideInInspector] public Animator cameraAnimator;
    private bool gameInitiaded = false;
    private bool gameOverInitiaded = false;
    public static event Action<Transform> OnPlayerChanged;

    //ScoreStats
    int distanceScore = 0;
    int vehicleHit = 0;
    int bikeHit = 0;
    int pedestriansHit = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        GetPlayerReference();
        if (mainCamera == null) mainCamera = GameObject.FindGameObjectWithTag("MainCamera").transform;
        cameraAnimator = mainCamera.GetComponentInParent<Animator>();
        uIScript = GetComponent<UIScript>();
        if (playerController == null || mainCamera == null || uIScript == null)
        {
            Debug.LogError("One or more required components not found!");
        }
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = false;
    }
    void Start()
    {
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
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        PrewarmPool();
        InitialWorldSpawn();
    }

    void Update()
    {
        distanceScore = (int)Mathf.Abs(player.transform.position.z);
        UpdateDifficulty();
        HandleRoadSpawning();
        HandleVehicleSpawning();
        HandlePedestrianAndStallSpawning();
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
            gameOverInitiaded = true;
            Invoke("GameOver", 4f);
        }
        if (uIScript != null)
        {
            uIScript.UpdateScoreUI(distanceScore, highScore);
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

    void HandlePedestrianAndStallSpawning()
    {
        pedestrianTimer += Time.deltaTime;
        stallTimer += Time.deltaTime;
        if (pedestrianTimer >= pedestrianSpawnRate)
        {
            TrySpawnPedestrian();
            pedestrianTimer = 0f;
        }
        if (stallTimer >= stallSpawnRate && gameStarted && !gameOver)
        {
            TrySpawnStalls();
            stallTimer = 0f;
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
        float playerZ = player.transform.position.z;
        if (chosenGroup.type == Type.Barrier || chosenGroup.type == Type.WrongSides)
        {
            if (!gameStarted || gameOver) return;
            int spawnSide = Random.value > 0.5f ? 1 : -1;
            for (int i = 0; i < 2; i++)   // try 2 times max
            {
                spawnPos = new Vector3(5.2f * spawnSide, 1f, playerZ - spawnDistance);
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
                    spawnPos = new Vector3(spawnX, 2f, playerZ - spawnDistance);
                }
                else
                {
                    if (spawnX > 2.5f) spawnX = 2.5f;
                    spawnPos = new Vector3(spawnX, 2f, playerZ + menuSpawnPositionZ);
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

        //For Passenger Spawn Logic
        if (gameStarted && playerController.forHire)
        {
            totalPedestriansSpawned++; 
            if (totalPedestriansSpawned >= passengerSpawnVariable)
            {
                totalPedestriansSpawned = 0;
                NPCCharacterScript npcScript = ped.GetComponent<NPCCharacterScript>();
                if (npcScript != null)
                {
                    npcScript.playerRickshaw = playerController;
                    npcScript.isPassenger = true;
                    if(ped.transform.position.x > 0)
                    {
                        spawnPos.x = 7f;
                        ped.transform.position = spawnPos;
                    }
                    else
                    {
                        spawnPos.x = -7f;
                        ped.transform.position = spawnPos;
                    }
                }
            }
        }
    }

    void TrySpawnStalls()
    {
        if (activeStalls.Count >= maxActiveStalls || stallPrefabs.Length == 0)
            return;

        float sideMultiplier = (Random.value > 0.5f) ? 1f : -1f; // 1. Pick Side
        float spawnX = sideMultiplier * 6.3f;
        Vector3 spawnPos = Vector3.zero;

        spawnPos = new Vector3(spawnX, pedestrianYOffset, player.transform.position.z - padestrianSpawnDistance);

        GameObject prefab = stallPrefabs[stallPrefabIndex];
        stallPrefabIndex++;
        if (stallPrefabIndex >= stallPrefabs.Length)
        {
            stallPrefabIndex = 0;
        }
        GameObject ped = GetFromPool(prefab);
        ped.transform.position = spawnPos;
        ped.SetActive(true);
        activeStalls[ped] = Type.Stall;
    }

    GameObject GetFromPool(GameObject prefab)
    {
        if (!prefabPool.ContainsKey(prefab))
            prefabPool[prefab] = new Queue<GameObject>();

        if (prefabPool[prefab].Count > 0)
            return prefabPool[prefab].Dequeue();

        GameObject obj = Instantiate(prefab);
#if UNITY_EDITOR
        Debug.Log("Instantiating new object for prefab: " + prefab.name);
#endif
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

        if(stallPrefabs != null)  //For Stalls
        {
            foreach (GameObject prefab in stallPrefabs)
            {
                for (int s = 0; s < prewarmStallsCount; s++)
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

    public void RecycleSingleStall(GameObject stallToRecycle)
    {
        if (activeStalls.ContainsKey(stallToRecycle))
        {
            ReturnToPool(stallToRecycle);
            activeStalls.Remove(stallToRecycle);
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
        progress = Mathf.Clamp01(distanceScore / maxDificultyScore);
        progress = Mathf.SmoothStep(0f, 1f, progress);

        playerController.baseSpeed = Mathf.Lerp(playerController.startSpeed, playerController.maxSpeed, progress);
        spawnRate = Mathf.Lerp(startSpawnRate, maxSpawnRate, progress);
    }

    public void InitialWorldSpawn()
    {
        //Initial Pedestrian Spawn
        for (float zPos = -10f; zPos <= 40f; zPos += pedestrianSpawnGaps)
        {
            if (activePedestrians.Count >= maxActivePedestrians || pedestrianPrefabs.Length == 0) break;

            float spawnX = -1f * Random.Range(pedestrianXOffset.x, pedestrianXOffset.y);
            Vector3 spawnPos = new Vector3(spawnX, pedestrianYOffset, zPos);

            GameObject prefab = pedestrianPrefabs[pedestrianPrefabIndex];
            pedestrianPrefabIndex = (pedestrianPrefabIndex + 1) % pedestrianPrefabs.Length;

            GameObject ped = GetFromPool(prefab);
            ped.transform.position = spawnPos;
            ped.SetActive(true);
            activePedestrians[ped] = Type.Pedestrian;
        }

        //Initial Stall Spawn
        for(int i = 0; i < stallSpawnPositions.Length; i++)
        {
            if (activeStalls.Count >= maxActiveStalls || stallPrefabs.Length == 0) break;
            GameObject prefab = stallPrefabs[i];
            stallPrefabIndex = (stallPrefabIndex + 1) % stallPrefabs.Length;
            GameObject stall = GetFromPool(prefab);
            Vector3 stallSpawnPosition = new Vector3(stallSpawnPositions[i].x, 0.3f, stallSpawnPositions[i].z);
            stall.transform.position = stallSpawnPosition;
            stall.SetActive(true);
            activeStalls[stall] = Type.Stall;
        }

        //Initial Vehicle Spawn
        float currentVehicleX = -4f;
        for (float zPos = initialVehicleSpawnDistance; zPos <= 30f; zPos += vehicleSpawnGaps)
        {
            if (activeVehicles.Count >= maxActiveVehicles) break;
            int chosenGroupIndex;
            VehicleGroup chosenGroup;
            do
            {
                chosenGroupIndex = ChooseWeightedTypeIndex();
                chosenGroup = vehicleGroups[chosenGroupIndex];
            }
            while (chosenGroup.type == Type.Barrier || chosenGroup.type == Type.WrongSides);    // Keep picking until we get a type that is NOT a Barrier or WrongSides

            GameObject chosenVehiclePrefab = GetVehiclePrefab(chosenGroupIndex);

            if (chosenVehiclePrefab != null)
            {
                Vector3 spawnPos = new Vector3(currentVehicleX, 2f, zPos);

                GameObject vehicle = GetFromPool(chosenVehiclePrefab);
                
                //vehicle.transform.rotation = Quaternion.identity;
                vehicle.transform.position = spawnPos;
                vehicle.SetActive(true);

                activeVehicles[vehicle] = chosenGroup.type;
                IncreaseBonusForUnselected(chosenGroup);
            }

            currentVehicleX = (currentVehicleX == -4f) ? 0f : -4f;  // Toggle X position
        }
    }

    public void RegisterHit(Type hitType)
    {
        int scoreToApply = 0;
        switch (hitType)
        {
            case Type.Pedestrian:
                scoreToApply = pedestrianHitScore;
                pedestriansHit++;
                break;

            case Type.Bike:
                scoreToApply = bikeHitScore;
                bikeHit++;
                break;

            case Type.Car:
                scoreToApply = vehicleHitScore;
                vehicleHit++;
                break;
            default:
                scoreToApply = vehicleHitScore;
                vehicleHit++;
                break;
        }
        score += scoreToApply;
        uIScript.SpawnFloatingScore(scoreToApply);
    }

    public void SuccessfulPassengerDropOff()
    {
        score += passengerDropOffScore;
        uIScript.SpawnFloatingScore(passengerDropOffScore);
    }

    void StartGame()
    {
        playerController.gameStarted = true;
        gameInitiaded = true;
    }
    void GameOver()
    {
        EconomyManager.Instance.ConvertRunToTaka(score, distanceScore);
        score += distanceScore;
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }
        uIScript.GameOver(distanceScore, vehicleHit, bikeHit, pedestriansHit, score);
    }
    public void GetPlayerReference()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        playerController = player.GetComponent<PlayerRickshawController>();

        foreach (var pickUp in pickUpScript)
        {
            pickUp.GetPlayerReference(player.transform);
        }
        OnPlayerChanged?.Invoke(player.transform);
    }
}
