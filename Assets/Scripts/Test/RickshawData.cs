using UnityEngine;

[CreateAssetMenu(fileName = "RickshawData", menuName = "Garage/Rickshaw Data", order = 0)]
public class RickshawData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique, stable ID used for save files. Never rename after shipping.")]
    public string vehicleId;
    public string displayName;

    [Header("Color Variants")]
    [Tooltip("Material per color (e.g. Red, Green, Blue).")]
    public Material[] colorMaterials;

    [Header("Economy")]
    [Tooltip("Cost in Taka.")]
    public int unlockPrice;

    [Header("Stats — real gameplay values shown as text")]
    public float topSpeedKmh;
    public float batteryDrainPerSecond;
    public float durability;

    [Header("Scene Pooling Reference")]
    [Tooltip("Index into GarageManager.displayModels. Must match the order of the " +
             "allVehicles list exactly, since both lists are indexed together.")]
    public int displayModelIndex;

    [Header("Gameplay Prefab")]
    [Tooltip("Prefab actually spawned/driven in the driving scene once equipped. " +
             "This is separate from the pooled garage-display model.")]
    public GameObject gameplayPrefab;
}
