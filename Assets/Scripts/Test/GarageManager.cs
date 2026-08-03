using System;
using System.Collections.Generic;
using UnityEngine;

public class GarageManager : MonoBehaviour
{
    public static GarageManager Instance { get; private set; }

    [Header("Data — index[i] must correspond to displayModels[i]")]
    [SerializeField] private List<RickshawData> allVehicles;

    [Header("Pre-placed pooled models in the display pivot")]
    [SerializeField] private List<GameObject> displayModels;

    private int _currentIndex;
    private HashSet<string> _unlockedIds;
    private string _equippedId;

    public static event Action<RickshawData, bool, bool> OnVehicleChanged;

    public static event Action<string> OnVehicleUnlocked;

    public static event Action<RickshawData> OnPurchaseFailed;

    public LayerMask defaultUIMask;
    public LayerMask garageRenderUIMask;
    private Camera garageUICamera;
    private Animator cameraAnimator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _unlockedIds = new HashSet<string>(SaveSystem.LoadUnlockedVehicles());
        _equippedId = SaveSystem.LoadEquippedVehicle();
        garageUICamera = GameManagerScript.Instance.mainCamera.GetComponent<Camera>();
        cameraAnimator = GameManagerScript.Instance.mainCamera.GetComponentInParent<Animator>();

        if (allVehicles.Count > 0 && string.IsNullOrEmpty(_equippedId))
        {
            _equippedId = allVehicles[0].vehicleId;
            _unlockedIds.Add(_equippedId);
            SaveSystem.SaveUnlockedVehicle(_equippedId);
            SaveSystem.SaveEquippedVehicle(_equippedId);
        }

        int equippedIndex = allVehicles.FindIndex(v => v.vehicleId == _equippedId);
        _currentIndex = equippedIndex >= 0 ? equippedIndex : 0;
        ShowCurrent();
        GameManagerScript.Instance.GetPlayerReference();
    }

    public void CycleNext()
    {
        _currentIndex = (_currentIndex + 1) % allVehicles.Count;
        ShowCurrent();
    }

    public void CyclePrevious()
    {
        _currentIndex = (_currentIndex - 1 + allVehicles.Count) % allVehicles.Count;
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        for (int i = 0; i < displayModels.Count; i++)
        {
            displayModels[i].SetActive(i == _currentIndex);
        }

        RickshawData data = allVehicles[_currentIndex];
        bool isOwned = _unlockedIds.Contains(data.vehicleId);
        bool isEquipped = data.vehicleId == _equippedId;
        OnVehicleChanged?.Invoke(data, isOwned, isEquipped);
    }

    public void OnActionButtonPressed()
    {
        RickshawData data = allVehicles[_currentIndex];
        bool isOwned = _unlockedIds.Contains(data.vehicleId);
        bool isEquipped = data.vehicleId == _equippedId;

        if (isEquipped) return;

        if (!isOwned)
        {
            if (EconomyManager.Instance.SpendTaka(data.unlockPrice))
            {
                _unlockedIds.Add(data.vehicleId);
                SaveSystem.SaveUnlockedVehicle(data.vehicleId);
                OnVehicleUnlocked?.Invoke(data.vehicleId);
                Equip(data);
            }
            else
            {
                OnPurchaseFailed?.Invoke(data);
            }
            return;
        }

        Equip(data);
    }

    public void onHomeButtonPressed()
    {
        garageUICamera.cullingMask = defaultUIMask;
        cameraAnimator.SetTrigger("MainMenu");
    }
    public void onCustomizeButtonPressed()
    {
        garageUICamera.cullingMask = garageRenderUIMask;
        cameraAnimator.SetTrigger("Garage");
    }

    private void Equip(RickshawData data)
    {
        _equippedId = data.vehicleId;
        SaveSystem.SaveEquippedVehicle(_equippedId);
        GameManagerScript.Instance.GetPlayerReference();
        OnVehicleChanged?.Invoke(data, true, true);
    }

    public RickshawData CurrentData => allVehicles[_currentIndex];
}
