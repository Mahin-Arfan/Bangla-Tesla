using UnityEngine;
using UnityEngine.UI;

public class SettingsScript : MonoBehaviour
{
    private GameManagerScript gameManager;
    private UIScript uiScript;
    public PlayerRickshawController playerScript;
    public RickshawHealth healthScript;

    [Header("Main Settings (Public)")]
    public GameObject settingsPanel;
    public Toggle tiltToggle;
    public Toggle buttonToggle;
    public Slider steerSensitivity;

    [Header("Developer Options UI")]
    public GameObject developerPanel1;
    public GameObject developerPanel2;
    public Button openDeveloperPanelButton;

    [Header("Developer Sliders: World & Spawning")]
    public Slider vehiceleStartSpawnRateSlider;
    public Slider vehicleMaxSpawnRateSlider;
    public Slider vehicleInactiveDistance;
    public Slider specialRoadSpawnDistanceSlider;
    public Slider maxDifficultyDistance;
    public Slider pedastrianSpawnRate;
    public Slider maxActivePedastrian;

    [Header("Developer Sliders: Player Stats")]
    public Slider startSpeed;
    public Slider maxSpeed;
    public Slider boostSpeed;
    public Slider brakeDeceleration;
    public Slider steerSpeed;
    public Slider maxDistanceBatteryRange;
    public Toggle invincibleToggle;


    void Start()
    {
        gameManager = GameManagerScript.Instance;
        uiScript = gameManager.GetComponent<UIScript>();
        if (developerPanel1 != null && developerPanel2 != null)
        {
            developerPanel1.SetActive(false);
            developerPanel2.SetActive(false);
        }
        tiltToggle.onValueChanged.AddListener((isOn) => { if (isOn) buttonToggle.isOn = false; else { buttonToggle.isOn = true; } });
        buttonToggle.onValueChanged.AddListener((isOn) => { if (isOn) tiltToggle.isOn = false; else { tiltToggle.isOn = true; } });
        LoadSettings();
    }

    public void ToggleDeveloperPanel()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
        developerPanel1.SetActive(!developerPanel1.activeSelf);
        LoadSettings();
    }

    public void MoreSettingsButton()
    {
        developerPanel2.SetActive(!developerPanel2.activeSelf);
        developerPanel1.SetActive(!developerPanel1.activeSelf);
    }

    public void ApplyPublicSettings()
    {
        gameManager.tiltSteeringControl = tiltToggle.isOn;
        playerScript.tiltSensitivity = steerSensitivity.value;
        PlayerPrefs.SetInt("UseTilt", tiltToggle.isOn ? 1 : 0);
        PlayerPrefs.SetFloat("SteerSens", steerSensitivity.value);
        uiScript.BackToMainMenu();
    }

    public void ApplyAllSettings()
    {
        gameManager.tiltSteeringControl = tiltToggle.isOn;
        playerScript.tiltSensitivity = steerSensitivity.value;

        //Playr Settings
        playerScript.startSpeed = startSpeed.value;
        playerScript.maxSpeed = maxSpeed.value;
        playerScript.boostSpeed = boostSpeed.value;
        playerScript.brakeDeceleration = brakeDeceleration.value;
        playerScript.steerSpeed = steerSpeed.value;
        healthScript.initialRangeInMeters = maxDistanceBatteryRange.value;

        //Spawn Settings
        gameManager.startSpawnRate = vehiceleStartSpawnRateSlider.value;
        gameManager.maxSpawnRate = vehicleMaxSpawnRateSlider.value;
        gameManager.vehicleRecycleDistance = vehicleInactiveDistance.value;
        gameManager.specialRoadSpawnDistance = specialRoadSpawnDistanceSlider.value;
        gameManager.maxDificultyScore = maxDifficultyDistance.value;
        gameManager.pedestrianSpawnRate = pedastrianSpawnRate.value;
        gameManager.maxActivePedestrians = Mathf.RoundToInt(maxActivePedastrian.value);
        healthScript.invincible = invincibleToggle.isOn;
        SaveToDevice();
        developerPanel1.SetActive(false);
        developerPanel2.SetActive(false);
        uiScript.BackToMainMenu();
    }

    private void SaveToDevice()
    {
        PlayerPrefs.SetInt("UseTilt", tiltToggle.isOn ? 1 : 0);
        PlayerPrefs.SetFloat("SteerSens", steerSensitivity.value);

        PlayerPrefs.SetFloat("StartSpawnRate", vehiceleStartSpawnRateSlider.value);
        PlayerPrefs.SetFloat("MaxSpawnRate", vehicleMaxSpawnRateSlider.value);
        PlayerPrefs.SetFloat("VehInactiveDist", vehicleInactiveDistance.value);
        PlayerPrefs.SetFloat("SpecialRoadDist", specialRoadSpawnDistanceSlider.value);
        PlayerPrefs.SetFloat("MaxDiffDist", maxDifficultyDistance.value);
        PlayerPrefs.SetFloat("PedSpawnRate", pedastrianSpawnRate.value);
        PlayerPrefs.SetInt("MaxPed", gameManager.maxActivePedestrians);

        PlayerPrefs.SetFloat("StartSpeed", startSpeed.value);
        PlayerPrefs.SetFloat("MaxSpeed", maxSpeed.value);
        PlayerPrefs.SetFloat("BoostSpeed", boostSpeed.value);
        PlayerPrefs.SetFloat("BrakeDecel", brakeDeceleration.value);
        PlayerPrefs.SetFloat("SteerSpeed", steerSpeed.value);
        PlayerPrefs.SetFloat("MaxBattery", maxDistanceBatteryRange.value);

        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        if (!PlayerPrefs.HasKey("UseTilt")) //if no saved settings exist, use default values
        {
            SyncUIWithDefaultVariables();
            return;
        }

        bool savedTilt = PlayerPrefs.GetInt("UseTilt", 1) == 1;
        tiltToggle.isOn = savedTilt;
        buttonToggle.isOn = !savedTilt;

        steerSensitivity.value = PlayerPrefs.GetFloat("SteerSens", 2f);

        startSpeed.value = PlayerPrefs.GetFloat("StartSpeed", 8f);
        maxSpeed.value = PlayerPrefs.GetFloat("MaxSpeed", 20f);
        boostSpeed.value = PlayerPrefs.GetFloat("BoostSpeed", 5f);
        brakeDeceleration.value = PlayerPrefs.GetFloat("BrakeDecel", 5f);
        steerSpeed.value = PlayerPrefs.GetFloat("SteerSpeed", 5f);
        maxDistanceBatteryRange.value = PlayerPrefs.GetFloat("MaxBattery", 600f);

        vehiceleStartSpawnRateSlider.value = PlayerPrefs.GetFloat("StartSpawnRate", 2f);
        vehicleMaxSpawnRateSlider.value = PlayerPrefs.GetFloat("MaxSpawnRate", 5f);
        vehicleInactiveDistance.value = PlayerPrefs.GetFloat("VehInactiveDist", 180f);
        specialRoadSpawnDistanceSlider.value = PlayerPrefs.GetFloat("SpecialRoadDist", 400f);
        maxDifficultyDistance.value = PlayerPrefs.GetFloat("MaxDiffDist", 1500f);
        pedastrianSpawnRate.value = PlayerPrefs.GetFloat("PedSpawnRate", 1f);
        maxActivePedastrian.value = PlayerPrefs.GetInt("MaxPed", 10);
        invincibleToggle.isOn = false;
    }

    private void SyncUIWithDefaultVariables()
    {
        if (gameManager == null || playerScript == null) return;

        tiltToggle.isOn = gameManager.tiltSteeringControl;
        buttonToggle.isOn = !gameManager.tiltSteeringControl;
        steerSensitivity.value = playerScript.tiltSensitivity;

        vehiceleStartSpawnRateSlider.value = gameManager.startSpawnRate;
        vehicleMaxSpawnRateSlider.value = gameManager.maxSpawnRate;
        vehicleInactiveDistance.value = gameManager.vehicleRecycleDistance;
        specialRoadSpawnDistanceSlider.value = gameManager.specialRoadSpawnDistance;
        maxDifficultyDistance.value = gameManager.maxDificultyScore;
        pedastrianSpawnRate.value = gameManager.pedestrianSpawnRate;
        maxActivePedastrian.value = gameManager.maxActivePedestrians;

        startSpeed.value = playerScript.startSpeed;
        maxSpeed.value = playerScript.maxSpeed;
        boostSpeed.value = playerScript.boostSpeed;
        brakeDeceleration.value = playerScript.brakeDeceleration;
        steerSpeed.value = playerScript.steerSpeed;
        maxDistanceBatteryRange.value = healthScript.initialRangeInMeters;
        invincibleToggle.isOn = false;
    }

    public void DefaultSet()
    {
        gameManager.tiltSteeringControl = true;
        playerScript.tiltSensitivity = 2f;
        //Player Settings
        playerScript.startSpeed = 8f;
        playerScript.maxSpeed = 20f;
        playerScript.boostSpeed = 5f;
        playerScript.brakeDeceleration = 5f;
        playerScript.steerSpeed = 5f;
        healthScript.initialRangeInMeters = 600f;
        //Spawn Settings
        gameManager.startSpawnRate = 2f;
        gameManager.maxSpawnRate = 5f;
        gameManager.vehicleRecycleDistance = 180f;
        gameManager.specialRoadSpawnDistance = 400f;
        gameManager.maxDificultyScore = 1500f;
        gameManager.pedestrianSpawnRate = 1f;
        gameManager.maxActivePedestrians = 10;
        SyncUIWithDefaultVariables();
        SaveToDevice();
    }
}
