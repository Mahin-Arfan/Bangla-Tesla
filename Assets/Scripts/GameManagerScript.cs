using System.Runtime.ExceptionServices;
using UnityEngine;

public class GameManagerScript : MonoBehaviour
{
    public Transform spawnRoadPoint;
    public GameObject player;

    public GameObject firstRoad;
    public GameObject secondRoad;
    public GameObject thirdRoad;
    public float firstRoadDistance;
    private Transform[] spawnLocations = new Transform[3];
    private int currentRoadIndex = 2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (firstRoad == null || secondRoad == null || thirdRoad == null)
        {
            Debug.LogError("One or more road segments are not assigned in the GameManagerScript.");

            firstRoad = GameObject.Find("Road 1");
            secondRoad = GameObject.Find("Road 2");
            thirdRoad = GameObject.Find("Road 3");
        }
        spawnLocations[0] = firstRoad.transform.Find("Road_End");
        spawnLocations[1] = secondRoad.transform.Find("Road_End");  
        spawnLocations[2] = thirdRoad.transform.Find("Road_End");
        spawnRoadPoint = spawnLocations[2];
    }

    // Update is called once per frame
    void Update()
    {
        if (firstRoad != null) 
        {
            firstRoadDistance = Vector3.Distance(player.transform.position, firstRoad.transform.position);
        }
        if(firstRoadDistance > 55f)
        {
            SpawnRoad();
        }
    }

    void SpawnRoad()
    {
        firstRoad.transform.position = spawnRoadPoint.position;
        // Update references
        GameObject temp = firstRoad;
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
