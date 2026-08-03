using UnityEngine;

[CreateAssetMenu(fileName = "RickshawData", menuName = "Garage/Rickshaw Data", order = 0)]
public class RickshawData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique, stable ID used for save files. Never rename after shipping.")]
    public string vehicleId;
    public string displayName;
    [TextArea] public string description;
    public Sprite thumbnail;

    [Header("Economy")]
    [Tooltip("Cost in Taka. Set to 0 for the starter vehicle.")]
    public int unlockPrice;

    [Header("Stats — real gameplay values shown as text / used by VehicleController")]
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
