using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIScript : MonoBehaviour
{
    [Header("Score UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;

    [Header("Rickshaw UI")]
    public TextMeshProUGUI speedText;
    public Slider healthSlider;
    public Slider batterySlider;
    private Image batteryBarColor;

    [Header("UI GameObjects")]
    public GameObject mainMenuUI;
    public GameObject endMenuUI;
    public GameObject inputUIButton;
    public GameObject inputUITilt;
    public GameObject screenMeterUI;
    public GameObject stillCanvas;
    public GameObject settingsCanvas;
    public GameObject SettingOptionsPublic;
    public GameObject updateCanvas;
    public GameObject updateCanvas1;
    public GameObject garageStillCanvas;
    public GameObject garageUpdateCanvas;
    public FloatingText[] floatingScorePool;

    [Header("Score Texts")]
    public TextMeshProUGUI distanceTravelledValue;
    public TextMeshProUGUI bikeHitValue;
    public TextMeshProUGUI vehicleHitValue;
    public TextMeshProUGUI pedestrianHitValue;
    public TextMeshProUGUI takaEarnedValue;

    [Header("Score Animation Settings")]
    public float slideDuration = 0.5f;
    public float countDuration = 1.0f;
    public float delayBetweenRows = 0.2f;

    [Header("Brake UIs")]
    public Image brakingUI;
    public Image brakeBrokenUI;
    public Image brakeUnavailableUI;
    public Slider brakeSlider;
    Vector2 brakeUIOriginalPosition;
    float brakeUIShakeIntensity = 100f;
    RectTransform brakeImageRect;
    Image brakeSliderFillImage;

    private GameManagerScript gameManagerScript;

    //Score UI Texts
    GameObject distanceTravelledText;
    GameObject bikeHitText;
    GameObject vehicleHitText;
    GameObject pedestrianHitText;
    GameObject takaEarnedText;
    private float currentBatteryTier = -1f;
    void Start()
    {
        gameManagerScript = GetComponent<GameManagerScript>();
        inputUIButton.SetActive(false);
        inputUITilt.SetActive(false);
        screenMeterUI.SetActive(false);
        updateCanvas.SetActive(false);
        updateCanvas1.SetActive(false);
        batteryBarColor = batterySlider.GetComponentInChildren<Image>();
         
        brakeImageRect = brakingUI.rectTransform;
        brakeSliderFillImage = brakeSlider.fillRect.GetComponent<Image>();
        if (brakeImageRect != null)
        {
            brakeUIOriginalPosition = brakeImageRect.anchoredPosition;
        }
        brakingUI.enabled = false;
        brakeBrokenUI.enabled = false;

        //Grab Score UI Texts
        distanceTravelledText = distanceTravelledValue.transform.parent.gameObject;
        bikeHitText = bikeHitValue.transform.parent.gameObject;
        vehicleHitText = vehicleHitValue.transform.parent.gameObject;
        pedestrianHitText = pedestrianHitValue.transform.parent.gameObject;
        takaEarnedText = takaEarnedValue.transform.parent.gameObject;
    }

    public void PlayGame()
    {
        if(GameManagerScript.Instance.cameraAnimator != null && GameManagerScript.Instance.player != null) 
        {
            GameManagerScript.Instance.cameraAnimator.SetTrigger("Start");
            GameManagerScript.Instance.player.GetComponent<PlayerRickshawController>().rickshawManAnimator.SetTrigger("Start");
        }
        else
        {
            Debug.LogWarning("Camera Animator or Player is not assigned in GameManagerScript.");
        }
        mainMenuUI.SetActive(false);

        Invoke("StartGame", 1.5f);
    }

    void StartGame()
    {
        GameManagerScript.Instance.mainCamera.SetParent(null);
        screenMeterUI.SetActive(true);
        updateCanvas.SetActive(true);
        updateCanvas1.SetActive(true);
        if (gameManagerScript.tiltSteeringControl == true)
        {
            inputUITilt.SetActive(true);
        }
        else
        {
            inputUIButton.SetActive(true);
        }
        gameManagerScript.gameStarted = true;
    }

    public void UpdateScoreUI(int distanceScore, int currentHighScore)
    {
        if (scoreText != null)
        {
            //scoreText.text = distanceScore.ToString("D5");
            scoreText.text = $"<mspace=0.54em>{distanceScore}</mspace>";
        }

        if (highScoreText != null)
        {
            highScoreText.text = "HIGH SCORE: " + currentHighScore.ToString("D5");
        }
    }
    public void UpdateSpeedUI(int speed)
    {
        if(speedText != null)
        {
            speedText.text = speed.ToString();
        }
    }

    public void SpawnFloatingScore(int pointsAdded)
    {
        foreach (FloatingText ft in floatingScorePool)
        {
            if (!ft.gameObject.activeInHierarchy)
            {
                ft.SetupAndPlay("+" + pointsAdded.ToString());
                return;
            }
        }
    }

    public void HealthUIUpdate(float health)
    {
        if (healthSlider.value != health)
        {
            healthSlider.value = health;
        }
    }
    public void BatteryUIUpdate(float battery)
    {
        float targetValue;
        if (battery > 80f) targetValue = 100f;
        else if (battery > 60f) targetValue = 80f;
        else if (battery > 40f) targetValue = 60f;
        else if (battery > 20f) targetValue = 40f;
        else if (battery > 0f) targetValue = 20f;
        else targetValue = 0f;

        if (targetValue == currentBatteryTier) return;

        currentBatteryTier = targetValue;
        batterySlider.value = targetValue;
        if (targetValue >= 80f)
        {
            batteryBarColor.color = Color.green;
        }
        else if (targetValue == 60f)
        {
            batteryBarColor.color = Color.yellow;
        }
        else if (targetValue == 40f)
        {
            batteryBarColor.color = new Color32(255, 165, 0, 255); // Orange
        }
        else if(targetValue == 20f)
        {
            batteryBarColor.color = Color.red;
            AudioManager.Instance.RequestGameAudioClip(AudioManager.Instance.lowBatteryWarningClip, transform, 0.15f, 1f, 0f, false);
        }
    }

    public void BrakeMeterUIUpdate(float meter, bool isBraking, bool brakeFail)
    {
        if (brakeFail)
        {
            if(!brakeBrokenUI.enabled)
            {
                brakingUI.enabled = false;
                brakeBrokenUI.enabled = true;
                brakeUnavailableUI.enabled = true;
            }

            brakeImageRect.anchoredPosition = brakeUIOriginalPosition;
            return;
        }
        if (brakeUnavailableUI.enabled) brakeUnavailableUI.enabled = false;
        float meterPercentage = Mathf.Clamp01(meter / 80f);
        if (brakeSlider.value != meter)
        {
            brakeSlider.value = meter;
            brakeSliderFillImage.color = Color.Lerp(Color.yellow, Color.red, meterPercentage);
        }

        if (meter > 0)
        {
            if (!brakingUI.enabled)
            {
                brakingUI.enabled = true;
                brakeBrokenUI.enabled = false;
            }

            float currentShake = brakeUIShakeIntensity * meterPercentage;

            Vector2 randomShake = Random.insideUnitCircle * currentShake;
            brakeImageRect.anchoredPosition = brakeUIOriginalPosition + randomShake;
        }
        else
        {
            if (brakingUI.enabled) brakingUI.enabled = false;
            if (brakeBrokenUI.enabled) brakeBrokenUI.enabled = false;
            brakeImageRect.anchoredPosition = brakeUIOriginalPosition;
        }
    }

    public void ExitGame()
    {
        Application.Quit();

        // For testing in Editor temp
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void GameOver(int distanceTravelled, int vehicleHit, int bikeHit, int pedestrianHit, int totalScore)
    {
        inputUIButton.SetActive(false);
        inputUITilt.SetActive(false);
        screenMeterUI.SetActive(false);
        updateCanvas1.SetActive(false);
        endMenuUI.SetActive(true);
        AnimateScoreShowcase(distanceTravelled, bikeHit, vehicleHit, pedestrianHit, totalScore);
    }

    public void Settings()
    {
        stillCanvas.SetActive(false);
        updateCanvas.SetActive(false);
        updateCanvas1.SetActive(false);
        settingsCanvas.SetActive(true);
        SettingOptionsPublic.SetActive(true);
    }

    public void BackToMainMenu()
    {
        stillCanvas.SetActive(true);
        settingsCanvas.SetActive(false);
        updateCanvas.SetActive(false);
        updateCanvas1.SetActive(false);
    }

    public void CustomizeButton()
    {
        mainMenuUI.SetActive(false);
        garageStillCanvas.SetActive(true);
        garageUpdateCanvas.SetActive(true);
        GarageManager.Instance.onCustomizeButtonPressed();
    }

    public void CustomizeToMainMenu()
    {
        mainMenuUI.SetActive(true);

        garageStillCanvas.SetActive(false);
        garageUpdateCanvas.SetActive(false);
        GarageManager.Instance.onHomeButtonPressed();
    }

    public void HomeButton()
    {
        DOTween.KillAll();
        SceneManager.LoadScene("SampleScene");
    }


    //For score showcase animation
    public void AnimateScoreShowcase(int dist, int bikes, int vehicles, int peds, int taka)
    {
        distanceTravelledText.SetActive(false);
        bikeHitText.SetActive(false);
        vehicleHitText.SetActive(false);
        pedestrianHitText.SetActive(false);
        takaEarnedText.SetActive(false);

        Sequence scoreSequence = DOTween.Sequence();

        AnimateRow(scoreSequence, distanceTravelledText, distanceTravelledValue, dist, -24f, -34f);
        AnimateRow(scoreSequence, bikeHitText, bikeHitValue, bikes, -6f, 6f);
        AnimateRow(scoreSequence, vehicleHitText, vehicleHitValue, vehicles, 36f, 46f);
        AnimateRow(scoreSequence, pedestrianHitText, pedestrianHitValue, peds, 76f, 86f);
        AnimateRow(scoreSequence, takaEarnedText, takaEarnedValue, taka, 116f, 126f);
    }

    private void AnimateRow(Sequence seq, GameObject container, TextMeshProUGUI textMesh, int finalValue, float startX, float endX)
    {
        RectTransform rect = container.GetComponent<RectTransform>();

        textMesh.text = "0";

        
        seq.AppendCallback(() =>
        {
            container.SetActive(true);
            Vector2 startPos = rect.anchoredPosition;
            startPos.x = startX;
            rect.anchoredPosition = startPos;
        });

        seq.Append(rect.DOAnchorPosX(endX, slideDuration).SetEase(Ease.OutQuad));

        int currentValue = 0;
        seq.Join(DOTween.To(() => currentValue, x =>
        {
            currentValue = x;
            textMesh.text = currentValue.ToString();
        }, finalValue, countDuration).SetEase(Ease.Linear));

        seq.AppendInterval(delayBetweenRows);
    }
}
