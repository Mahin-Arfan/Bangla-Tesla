using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GarageUIManager : MonoBehaviour
{
    [Header("Vehicle Info")]
    [SerializeField] private TMP_Text vehicleNameText;
    [SerializeField] private TMP_Text currencyText;

    [Header("Stats Panel")]
    [SerializeField] private Slider speedBar;
    [SerializeField] private Slider batteryBar;
    [SerializeField] private Slider handlingBar;
    [SerializeField] private TMP_Text speedValueText;
    [SerializeField] private TMP_Text batteryValueText;
    [SerializeField] private TMP_Text handlingValueText;

    [Header("Single Contextual Action Button")]
    [SerializeField] private Button actionButton;
    [SerializeField] private Image actionButtonBackgroundImage;
    [SerializeField] private Sprite buyIcon;
    [SerializeField] private Sprite equipIcon;
    [SerializeField] private Sprite equippedIcon;

    [Header("Navigation (static buttons, dynamic-triggering)")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;

    private void OnEnable()
    {
        GarageManager.OnVehicleChanged += HandleVehicleChanged;
        EconomyManager.OnBalanceChanged += HandleBalanceChanged;

        actionButton.onClick.AddListener(HandleActionButtonClicked);
        nextButton.onClick.AddListener(() => GarageManager.Instance.CycleNext());
        previousButton.onClick.AddListener(() => GarageManager.Instance.CyclePrevious());
    }

    private void OnDisable()
    {
        GarageManager.OnVehicleChanged -= HandleVehicleChanged;
        EconomyManager.OnBalanceChanged -= HandleBalanceChanged;

        actionButton.onClick.RemoveListener(HandleActionButtonClicked);
        nextButton.onClick.RemoveAllListeners();
        previousButton.onClick.RemoveAllListeners();
    }

    private void Start()
    {
        currencyText.text = FormatTaka(EconomyManager.Instance.CurrentBalance);
    }

    private void HandleActionButtonClicked()
    {
        GarageManager.Instance.OnActionButtonPressed();
    }

    private void HandleBalanceChanged(int newBalance)
    {
        currencyText.text = FormatTaka(newBalance);
    }

    private void HandleVehicleChanged(RickshawData data, bool isOwned, bool isEquipped)
    {
        vehicleNameText.text = data.displayName;

        speedBar.value = data.topSpeedKmh;
        batteryBar.value = data.batteryDrainPerSecond;
        handlingBar.value = data.durability;

        speedValueText.text = $"{data.topSpeedKmh:0} km/h";
        batteryValueText.text = $"{data.batteryDrainPerSecond:0} mi";
        handlingValueText.text = $"{data.durability:0}%";

        RefreshActionButton(data, isOwned, isEquipped);
    }

    private void RefreshActionButton(RickshawData data, bool isOwned, bool isEquipped)
    {
        if (isEquipped)
        {
            actionButtonBackgroundImage.sprite = equippedIcon;
            actionButton.interactable = false;
        }
        else if (isOwned)
        {
            actionButtonBackgroundImage.sprite = equipIcon;
            actionButton.interactable = true;
        }
        else
        {
            actionButtonBackgroundImage.sprite = buyIcon;
            actionButton.interactable = true;
        }
    }

    private string FormatTaka(int amount) => $" {amount:N0}"; // ৳ symbol \u09F3
}
