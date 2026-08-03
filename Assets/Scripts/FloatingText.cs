using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
[RequireComponent(typeof(RectTransform))]
public class FloatingText : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private RectTransform rectTransform;

    [Header("Animation Settings")]
    public float floatDistance = 50f;
    public float totalDuration = 1.5f;
    private Vector2 startPosition = new Vector2(0, 100f);

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (textMesh == null) textMesh = GetComponent<TextMeshProUGUI>();
    }

    public void SetupAndPlay(string scoreString)
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshProUGUI>();
            if(textMesh == null)
            {
                Debug.LogError("TextMeshProUGUI component is missing!");
                return;
            }
        }
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogError("RectTransform component is missing!");
                return;
            }
        }

        //Reset state
        textMesh.text = "+" + scoreString;
        rectTransform.anchoredPosition = startPosition;
        textMesh.alpha = 1f;
        gameObject.SetActive(true);

        //Active animation Disable
        rectTransform.DOKill();
        textMesh.DOKill();
        Sequence floatSequence = DOTween.Sequence();

        //Move up smoothly
        floatSequence.Append(rectTransform.DOAnchorPosY(startPosition.y + floatDistance, totalDuration)
            .SetEase(Ease.OutQuad));

        //Fade out smoothly
        floatSequence.Insert(totalDuration / 2f, textMesh.DOFade(0f, totalDuration / 2f));

        //Return to pool
        floatSequence.OnComplete(() => gameObject.SetActive(false));
    }
}
