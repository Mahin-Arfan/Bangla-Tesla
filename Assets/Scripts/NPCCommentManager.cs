using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static NPCCommentManager;

public class NPCCommentManager : MonoBehaviour
{
    public static NPCCommentManager Instance { get; private set; }

    [System.Serializable]
    public struct NPCComment
    {
        public string commentText;
        public AudioClip audioClip;
    }

    [Header("Pool Settings")]
    public GameObject commentUIPrefab;
    public int poolSize = 6;

    [Header("Animation Settings")]
    public Vector3 commentUIMaxSize = Vector3.one;
    public float scaleAnimDuration = 0.25f;
    public Ease easeInType = Ease.OutBack;
    public Ease easeOutType = Ease.InBack;

    private class PooledUI
    {
        public GameObject gameObject;
        public Transform transform;
        public TextMeshPro textMesh;
        public Sequence tweenSequence;
    }

    [Header("Comments")]
    public NPCCommentManager.NPCComment[] crashComments;
    public NPCCommentManager.NPCComment[] passengerCalls;
    public NPCCommentManager.NPCComment[] passengerDrop;
    public NPCCommentManager.NPCComment[] passengerAngry;

    private List<PooledUI> uiPool = new List<PooledUI>();
    private int passengerCommentIndex = 0;
    private int lastCrashCommentIndex = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(commentUIPrefab, transform);
            obj.SetActive(false);
            obj.transform.localScale = Vector3.zero;

            PooledUI pooledItem = new PooledUI
            {
                gameObject = obj,
                transform = obj.transform,
                textMesh = obj.GetComponentInChildren<TextMeshPro>()
            };

            uiPool.Add(pooledItem);
        }
    }

    public void PlayComment(NPCComment comment, float volume, Transform parent, Vector3 localPosition)
    {
        PooledUI ui = GetFreeUI();
        if (ui == null) return;

        //Position & Hierarchy
        ui.transform.SetParent(parent);
        ui.transform.localPosition = localPosition;
        ui.textMesh.text = comment.commentText;
        ui.gameObject.SetActive(true);

        //Audio & Duration Calculation
        float duration = 2.0f;
        if (comment.audioClip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.RequestGameAudioClip(comment.audioClip, parent, volume, 1f, 1f, false);
            duration = comment.audioClip.length;
        }

        //DOTween Sequence
        ui.tweenSequence?.Kill();
        ui.transform.localScale = Vector3.zero;

        ui.tweenSequence = DOTween.Sequence()
            .Append(ui.transform.DOScale(commentUIMaxSize, scaleAnimDuration).SetEase(easeInType))
            .AppendInterval(duration)
            .Append(ui.transform.DOScale(Vector3.zero, scaleAnimDuration).SetEase(easeOutType))
            .OnComplete(() =>
            {
                ui.gameObject.SetActive(false);
                ui.transform.SetParent(transform);
            })
            .SetLink(ui.gameObject);
    }

    public void PlayCrashComment(Transform parentTransform, Vector3 commentOffset)
    {
        int galiIndex = Random.Range(0, crashComments.Length);
        if (lastCrashCommentIndex == galiIndex)
        {
            galiIndex = (galiIndex + 1) % crashComments.Length;
        }
        lastCrashCommentIndex = galiIndex;
        PlayComment(crashComments[galiIndex], 1f, parentTransform, commentOffset);
    }

    public void PlayPassengerCallComment(Transform parentTransform, Vector3 commentOffset)
    {
        passengerCommentIndex = (passengerCommentIndex + 1) % passengerCalls.Length;
        PlayComment(passengerCalls[passengerCommentIndex], 1f, parentTransform, commentOffset);
    }
    public void PlayPassengerDropComment(Transform parentTransform, Vector3 commentOffset)
    {
        PlayComment(passengerDrop[passengerCommentIndex], 1f, parentTransform, commentOffset);
    }
    public void PlayPassengerAngryComment(Transform parentTransform, Vector3 commentOffset)
    {
        PlayComment(passengerAngry[passengerCommentIndex], 1f, parentTransform, commentOffset);
    }

    private PooledUI GetFreeUI()
    {
        for (int i = 0; i < uiPool.Count; i++)
        {
            if (!uiPool[i].gameObject.activeInHierarchy)
                return uiPool[i];
        }
        return null;
    }

    private void OnDestroy()
    {
        for (int i = 0; i < uiPool.Count; i++)
        {
            uiPool[i].tweenSequence?.Kill();
        }
    }
}
