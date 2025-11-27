using System.Runtime.ExceptionServices;
using UnityEngine;

public class GameManagerScript : MonoBehaviour
{
    [System.Serializable]
    public enum Type { Truck, Bus, Car, Cng, Bike, Rickshaw, Barrier }
    [System.Serializable]
    public struct Road
    {
        public GameObject roadSegment;
        public Transform endPoint;
    }
    [System.Serializable]
    public struct Vehicle
    {
        public GameObject vehicle;
        public Vector2 spawnLocationX;
        [Range(0f, 100f)]
        public int spawnChance;
        public Type vehicleType;

    }

    public float roadSpawnDistance = 110f;

    public GameObject player;

    private Transform firstRoad;
    private Transform secondRoad;
    private Transform thirdRoad;
    private Transform spawnRoadPoint;
    private float firstRoadDistance;
    private Transform[] spawnLocations = new Transform[3];
    private int currentRoadIndex = 2;

    public Vehicle[] vehicles;
    public Road[] roads;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (roads.Length < 3)
        {
            Debug.LogError("All road segments are not assigned in the GameManagerScript.");
            return;
        }
        firstRoad = roads[0].roadSegment.transform;
        secondRoad = roads[1].roadSegment.transform;
        thirdRoad = roads[2].roadSegment.transform;
        spawnLocations[0] = roads[0].endPoint;
        spawnLocations[1] = roads[1].endPoint;
        spawnLocations[2] = roads[2].endPoint;
        spawnRoadPoint = spawnLocations[2];
    }

    // Update is called once per frame
    void Update()
    {
        if (firstRoad != null) 
        {
            firstRoadDistance = Vector3.Distance(player.transform.position, firstRoad.transform.position);
        }
        if(firstRoadDistance > roadSpawnDistance)
        {
            SpawnRoad();
        }
    }

    void SpawnRoad()
    {
        firstRoad.transform.position = spawnRoadPoint.position;
        // Update references
        Transform temp = firstRoad;
        firstRoad = secondRoad;
        secondRoad = thirdRoad;
        thirdRoad = temp;

        currentRoadIndex++;
        if(currentRoadIndex > 2)
        {
            currentRoadIndex = 0;
        }
        spawnRoadPoint = spawnLocations[currentRoadIndex];
    }
}
