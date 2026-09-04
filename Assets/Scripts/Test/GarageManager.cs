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

    [Header("Color Customization")]
    [Tooltip("The renderer whose material gets swapped for color variants. ")]
    [SerializeField] private List<VehicleRendererGroup> displayModelBodyRenderers;
    [Serializable]  private struct VehicleRendererGroup
    {
        [Tooltip("All the MeshRenderers on this vehicle that use the color material")]
        public Renderer[] renderers;
    }

    private int _currentIndex;
    private HashSet<string> _unlockedIds;
    private string _equippedId; 
    private int _currentColorIndex;

    public static event Action<RickshawData, bool, bool> OnVehicleChanged;

    public static event Action<string> OnVehicleUnlocked;

    public static event Action<RickshawData> OnPurchaseFailed;
    public static event Action<int, bool> OnColorChanged;

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

        _currentColorIndex = SaveSystem.LoadColorIndex(data.vehicleId);
        ApplyColorToRenderer(_currentIndex, _currentColorIndex);
        OnColorChanged?.Invoke(_currentColorIndex, isOwned);
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

    public void ShowEquippedVehicle()
    {
        int equippedIndex = allVehicles.FindIndex(v => v.vehicleId == _equippedId);

        if (equippedIndex < 0) equippedIndex = 0;

        _currentIndex = equippedIndex;

        for (int i = 0; i < displayModels.Count; i++)
        {
            displayModels[i].SetActive(i == equippedIndex);
        }
    }

    public void SelectColor(int colorIndex)
    {
        RickshawData data = allVehicles[_currentIndex];
        if (data.colorMaterials == null || colorIndex < 0 || colorIndex >= data.colorMaterials.Length)
        {
            Debug.LogWarning($"[GarageManager] Color index {colorIndex} out of range for {data.displayName}.");
            return;
        }

        _currentColorIndex = colorIndex;
        ApplyColorToRenderer(_currentIndex, colorIndex);
        SaveSystem.SaveColorIndex(data.vehicleId, colorIndex);

        bool isOwned = _unlockedIds.Contains(data.vehicleId);
        OnColorChanged?.Invoke(colorIndex, true);
    }

    private void ApplyColorToRenderer(int vehicleIndex, int colorIndex)
    {
        if (vehicleIndex < 0 || vehicleIndex >= displayModelBodyRenderers.Count) return;

        RickshawData data = allVehicles[vehicleIndex];
        if (data.colorMaterials == null || colorIndex < 0 || colorIndex >= data.colorMaterials.Length)
        {
            return;
        }

        Material colorMaterial = data.colorMaterials[colorIndex];
        Renderer[] renderers = displayModelBodyRenderers[vehicleIndex].renderers;
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].sharedMaterial = colorMaterial;
            }
        }
    }

    public void onHomeButtonPressed()
    {
        garageUICamera.cullingMask = defaultUIMask;
        cameraAnimator.SetTrigger("MainMenu");
        ShowEquippedVehicle();
        GameManagerScript.Instance.GetPlayerReference();
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
