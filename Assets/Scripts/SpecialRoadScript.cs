using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SpecialRoadScript : MonoBehaviour
{
    public enum EnvironmentTypes { CraneWork, MetroRail }

    public EnvironmentTypes environmentType;
    [Header("Settings")]
    public float actionDistance = 50f;
    public float objectDropDistance = 15f;
    public float inactiveDistace = 100f;

    [Header("References")]
    public Rigidbody[] rigidObjects;
    public Transform playerTransform;
    private Animator animator;
    private Collider[] objectColliders;

    [Header("Directional Arrow Settings")]
    public RectTransform arrowRect;
    public Image iconImage;
    public Image arrowImage;
    public TextMeshProUGUI textDistance;

    public float pulseScaleMultiplier = 1.2f;
    public float basePulseDuration = 0.5f;
    private Vector3 iconOriginalScale;

    [Header("Dynamic Pulse Speed")]
    public float maxPulseSpeedMultiplier = 4f;
    public float startSpeedingUpDistance = 50f;
    public float maxSpeedDistance = 20f;
    private Tween pulseTween;

    //internals
    float distanceToPlayer;
    Vector3 dropObjectPosition;
    Vector3 dropObjectRotation;
    int actionObjectIndex = 0;
    bool actionStarted = false;
    bool objectDropped = false;

    void OnDisable()
    {
        arrowRect.DOKill();
    }

    void Start()
    {
        playerTransform = GameManagerScript.Instance.player.transform;
        animator = GetComponent<Animator>();
        objectColliders = new Collider[rigidObjects.Length];
        if (environmentType == EnvironmentTypes.CraneWork)
        {
            objectColliders[0] = rigidObjects[0].GetComponent<Collider>();
            dropObjectPosition = rigidObjects[0].transform.localPosition;
            dropObjectRotation = rigidObjects[0].transform.localEulerAngles;
        }
        else
        {
            for(int i = 0; i < rigidObjects.Length; i++)
            {
                objectColliders[i] = rigidObjects[i].GetComponent<Collider>();
            }
        }
        RectTransform iconRect = iconImage.GetComponent<RectTransform>();
        iconOriginalScale = iconRect.localScale;
        pulseTween = iconRect.DOScale(iconOriginalScale * pulseScaleMultiplier, basePulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    void Update()
    {
        distanceToPlayer = playerTransform.position.z - transform.position.z;
        float distanceZ = Mathf.Abs(distanceToPlayer);
        if (distanceToPlayer > 0f)
        {
            if (distanceToPlayer < actionDistance)
            {
                StartAction();
            }
        }
        else
        {
            if (distanceZ > inactiveDistace)
            {
                rigidObjects[actionObjectIndex].isKinematic = true;
                rigidObjects[actionObjectIndex].transform.localPosition = dropObjectPosition;
                rigidObjects[actionObjectIndex].transform.localEulerAngles = dropObjectRotation;
                objectColliders[actionObjectIndex].enabled = false;
                actionStarted = false;
                objectDropped = false;
                this.gameObject.SetActive(false);
            }
        }
        if (arrowRect != null && distanceZ < 100f)
        {
            UpdateRotation();
            UpdateAlpha(distanceZ);
        }
    }

    void StartAction()
    {
        if (!actionStarted)
        {
            animator.SetTrigger("Action");
            actionStarted = true;
        }
        if (!objectDropped && distanceToPlayer < objectDropDistance)
        {
            if(environmentType == EnvironmentTypes.CraneWork)
            {
                actionObjectIndex = 0;
            }
            else
            {
                actionObjectIndex = Random.Range(0, rigidObjects.Length);
                dropObjectPosition = rigidObjects[actionObjectIndex].transform.localPosition;
                dropObjectRotation = rigidObjects[actionObjectIndex].transform.localEulerAngles;
            }
            objectColliders[actionObjectIndex].enabled = true;
            rigidObjects[actionObjectIndex].isKinematic = false;
            if (environmentType == EnvironmentTypes.MetroRail)
            {
                float randomXForce = rigidObjects[actionObjectIndex].transform.position.x < 0 ? 0.5f : -0.5f;
                Vector3 dropForce = new Vector3(randomXForce, 0f, 0f).normalized;
                float hitForce = Random.Range(5, 15f);
                rigidObjects[actionObjectIndex].AddForce(dropForce * hitForce, ForceMode.Impulse);
            }
            objectDropped = true;
        }
    }

    private void UpdateRotation()
    {
        Vector3 directionToTarget = rigidObjects[actionObjectIndex].transform.position - playerTransform.position;
        directionToTarget.y = 0;
        float angle = Vector3.SignedAngle(Vector3.back, directionToTarget, Vector3.up);
        arrowRect.localEulerAngles = new Vector3(0, 0, -angle);
    }

    private void UpdateAlpha(float distanceZ)
    {
        float playerZ = playerTransform.position.z;
        float targetZ = rigidObjects[actionObjectIndex].transform.position.z;
        float targetAlpha = 0f;

        if (playerZ > targetZ)
        {
            targetAlpha = Mathf.InverseLerp(100f, 50f, distanceZ);
        }
        else
        {
            targetAlpha = Mathf.InverseLerp(10f, 0f, distanceZ);
        }
        textDistance.text = Mathf.RoundToInt(distanceZ).ToString() + "m";
        Color currentColor = arrowImage.color;
        Color currentIconColor = iconImage.color;
        currentColor.a = targetAlpha;
        currentIconColor.a = targetAlpha;
        arrowImage.color = currentColor;
        iconImage.color = currentIconColor;
        textDistance.color = currentColor;
        UpdatePulseSpeed(distanceZ);
    }

    private void UpdatePulseSpeed(float distanceZ)
    {
        float speedUpPercentage = Mathf.InverseLerp(startSpeedingUpDistance, maxSpeedDistance, distanceZ);

        float currentSpeed = Mathf.Lerp(1f, maxPulseSpeedMultiplier, speedUpPercentage);

        if (pulseTween != null && pulseTween.IsActive())
        {
            pulseTween.timeScale = currentSpeed;
        }
    }
}
