using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Pool Settings")]
    [Tooltip("Higher costs more cpu usage")]
    public int poolSize = 15;
    private List<AudioSource> audioPool = new List<AudioSource>();

    [Header("Sound Settings")]
    [Tooltip("Prevents multiple loud crashes playing on the exact same frame")]
    [Range(0, 1)]
    public float engineVolume = 0.5f;
    [Range(0, 1)]
    public float crashVolume = 1f;
    [Range(0, 1)]
    public float deadVolume = 1f;
    [Range(0, 1)]
    public float environmentVolume = 0.5f;
    [Range(0, 1)]
    public float musicVolume = 0.75f;
    [Range(0, 1)]
    public float dialogueVolume = 0.35f;
    public float audioTriggerDistance = 20f;
    public float crashCooldown = 0.5f;
    public float sourceMaxDistance = 15f;
    private float lastCrashTime;
    private int deadVoiceIndexMale = 0;
    private int lastChosenGaliIndex = 0;
    private int deadVoiceIndexFemale = 0;
    private int crashClipIndex = 0;

    [Header("Audio Clips")]
    public AudioClip[] crashClips;
    public AudioClip[] maleDeadVoiceClips;
    public AudioClip[] femaleDeadVoiceClips;
    public AudioClip[] galiVoiceClips;
    public AudioClip[] stallClips;
    public AudioClip environmentClip;
    public AudioClip music;

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
            source.minDistance = 2f;
            source.maxDistance = sourceMaxDistance;

            audioPool.Add(source);
        }
        Play2DEnvironment(environmentClip, environmentVolume);
        Play2DEnvironment(music, musicVolume);
    }

    private AudioSource GetFreeSource()
    {
        foreach (AudioSource source in audioPool)
        {
            if (!source.isPlaying) return source;
        }
        return null;
    }

    public void PlayCrash(Vector3 position)
    {
        if (Time.time < lastCrashTime + crashCooldown) return;

        AudioSource source = GetFreeSource();
        if (source != null)
        {
            lastCrashTime = Time.time;
            source.transform.SetParent(transform);
            source.transform.position = position;
            source.loop = false;
            source.volume = crashVolume;
            source.pitch = 1f;
            source.spatialBlend = 1f;
            source.clip = crashClips[crashClipIndex];
            crashClipIndex++;
            if (crashClipIndex >= crashClips.Length)
            {
                crashClipIndex = 0;
            }
            source.Play();
        }
    }

    public AudioSource RequestDeadVoiceClip(Vector3 position, bool male)
    {
        AudioSource source = GetFreeSource();
        if (source != null)
        {
            source.transform.SetParent(transform);
            source.transform.position = position;
            source.loop = false;
            source.volume = deadVolume;
            source.pitch = 1f;
            source.spatialBlend = 1f;
            if (male)
            {
                AudioClip clip = maleDeadVoiceClips[deadVoiceIndexMale];
                source.clip = clip;
                deadVoiceIndexMale++;
                if (deadVoiceIndexMale >= maleDeadVoiceClips.Length)
                {
                    deadVoiceIndexMale = 0;
                }
            }
            else
            {
                AudioClip clip = femaleDeadVoiceClips[deadVoiceIndexFemale];
                source.clip = clip;
                deadVoiceIndexFemale++;
                if (deadVoiceIndexFemale >= femaleDeadVoiceClips.Length)
                {
                    deadVoiceIndexFemale = 0;
                }
            }
            source.Play();
            return source;
        }
        return null;
    }

    public void Play3DVoice(AudioClip clip, Vector3 position) //for stalls & specialRoadAudio
    {
        if (clip == null) return;

        AudioSource source = GetFreeSource();
        if (source != null)
        {
            source.transform.SetParent(transform);
            source.transform.position = position;
            source.loop = false;
            source.clip = clip;
            source.pitch = 1f;
            source.spatialBlend = 1f;
            source.Play();
        }
    }

    public void Play2DEnvironment(AudioClip clip, float volume)
    {
        if (clip == null) return;

        AudioSource source = GetFreeSource();
        if (source != null)
        {
            source.transform.SetParent(transform);
            source.spatialBlend = 0f; // Force to 2D
            source.loop = true;
            source.clip = clip;
            source.volume = volume;
            source.pitch = 1f;
            source.Play();
        }
    }

    public AudioSource RequestEngineAudio(AudioClip clip, Transform parentTransform)
    {
        if (clip == null) return null;

        AudioSource source = GetFreeSource();
        if (source != null)
        {
            source.transform.SetParent(parentTransform);
            source.transform.localPosition = Vector3.zero;
            source.loop = true;
            source.volume = engineVolume;
            source.spatialBlend = 1f;
            source.clip = clip;
            source.Play();

            return source;
        }
        return null;
    }

    public void RequestDialogueVoiceClip(Vector3 position)
    {
        AudioSource source = GetFreeSource();
        if (source != null)
        {
            source.transform.position = position;
            source.loop = false;
            source.volume = dialogueVolume;
            source.pitch = 1f;
            source.spatialBlend = 1f;
            int galiIndex = Random.Range(0, galiVoiceClips.Length);
            if(lastChosenGaliIndex == galiIndex)
            {
                galiIndex = (galiIndex + 1) % galiVoiceClips.Length;
            }
            lastChosenGaliIndex = galiIndex;
            AudioClip clip = galiVoiceClips[galiIndex];
            source.clip = clip;
            source.Play();
        }
    }

    public void ReturnAudioSource(AudioSource source)
    {
        if (source != null)
        {
            source.Stop();
            source.transform.SetParent(transform);
            source.pitch = 1f;
            source.clip = null;
        }
    }
}