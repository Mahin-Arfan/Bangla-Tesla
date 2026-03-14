using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Pool Settings")]
    [Tooltip("Higher costs more cpu usage")]
    public int poolSize = 15;
    private List<AudioSource> audioPool = new List<AudioSource>();

    [Header("Crash Throttling")]
    [Tooltip("Prevents multiple loud crashes playing on the exact same frame")]
    public float crashCooldown = 0.2f;
    private float lastCrashTime;

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
            GameObject obj = new GameObject("PooledAudioSource_" + i);
            obj.transform.SetParent(transform);

            AudioSource source = obj.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 0.5f;
            source.maxDistance = 10f;

            audioPool.Add(source);
        }
    }

    private AudioSource GetFreeSource()
    {
        foreach (AudioSource source in audioPool)
        {
            if (!source.isPlaying) return source;
        }
        return null;
    }

    public void Play2DCrash(AudioClip clip, Vector3 position)
    {
        if (clip == null || Time.time < lastCrashTime + crashCooldown) return;

        AudioSource source = GetFreeSource();
        if (source != null)
        {
            lastCrashTime = Time.time;
            source.transform.SetParent(transform);
            source.transform.position = position;
            source.spatialBlend = 1f;
            source.loop = false;
            source.clip = clip;
            source.Play();
        }
    }

    public void Play3DVoice(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        AudioSource source = GetFreeSource();
        if (source != null)
        {
            source.transform.SetParent(transform);
            source.transform.position = position;
            source.spatialBlend = 1f;
            source.loop = false;
            source.clip = clip;
            source.Play();
        }
    }

    // 3. Hand an AudioSource to a Vehicle for its engine
    public AudioSource RequestEngineAudio(AudioClip clip, Transform parentTransform)
    {
        if (clip == null) return null;

        AudioSource source = GetFreeSource();
        if (source != null)
        {
            source.transform.SetParent(parentTransform);
            source.transform.localPosition = Vector3.zero;
            source.spatialBlend = 1f;
            source.loop = true;
            source.clip = clip;
            source.Play();

            return source;
        }
        return null;
    }

    public void ReturnAudioSource(AudioSource source)
    {
        if (source != null)
        {
            source.Stop();
            source.transform.SetParent(transform);
            source.clip = null;
        }
    }
}