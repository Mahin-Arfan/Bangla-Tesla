using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class FloatingText : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public CanvasGroup canvasGroup;
    public RectTransform rectTransform;

    [Header("Animation Settings")]
    public float floatSpeed = 100f; // How fast it moves up
    public float fadeDuration = 1f; // How long before it starts fading
    public float fadeSpeed = 2f;    // How fast it fades out

    private float timer;
    private Vector2 startPosition = new Vector2(0, 100f);

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (textMesh == null) textMesh = GetComponent<TextMeshProUGUI>();
    }

    // This is called by the UIScript to start the animation
    public void SetupAndPlay(string scoreString)
    {
        Debug.Log("Spawning floating text: " + scoreString);
        if(textMesh == null)
        {
            Debug.LogError("TextMeshProUGUI component is missing!");
            return;
        }
        textMesh.text = scoreString;
        rectTransform.anchoredPosition = startPosition;
        canvasGroup.alpha = 1f; // Fully visible
        timer = fadeDuration;

        gameObject.SetActive(true);
    }

    void Update()
    {
        // 1. Move the text up smoothly
        rectTransform.anchoredPosition += Vector2.up * floatSpeed * Time.deltaTime;

        // 2. Handle the fade-out timer
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            canvasGroup.alpha -= fadeSpeed * Time.deltaTime;

            // 3. Turn off when completely invisible (Returns it to the pool!)
            if (canvasGroup.alpha <= 0)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
