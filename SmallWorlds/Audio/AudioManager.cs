using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField]
    private List<AudioEntry> audioEntries = new List<AudioEntry>();

    private Dictionary<AudioType, List<AudioClip>> audioDictionary = new Dictionary<AudioType, List<AudioClip>>();
    private Dictionary<AudioType, Queue<AudioSource>> audioPools = new Dictionary<AudioType, Queue<AudioSource>>();

    [SerializeField]
    [Tooltip("higher values = more random pitch changing")]
    private float pitchVariation = 0.1f; // Adjust this value for more or less pitch variation

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        foreach (var entry in audioEntries)
        {
            audioDictionary[entry.type] = entry.audioClips;
            audioPools[entry.type] = new Queue<AudioSource>();
        }
    }

    public void PlaySound(AudioType type, Vector3 position)
    {
        if (audioDictionary.TryGetValue(type, out var audioClips) && audioClips.Count > 0)
        {
            int randomIndex = Random.Range(0, audioClips.Count);
            AudioClip selectedClip = audioClips[randomIndex];

            AudioSource audioSource = GetAudioInstance(type);
            audioSource.transform.position = position;
            audioSource.clip = selectedClip;
            audioSource.pitch = 1.0f + Random.Range(-pitchVariation, pitchVariation); // Apply pitch variation
            audioSource.gameObject.SetActive(true);
            audioSource.Play();

            StartCoroutine(DisableSoundAfterDuration(audioSource, type, selectedClip.length));
        }
        else
        {
            Debug.LogWarning($"Audio clips of type {type} not found or empty");
        }
    }

    private IEnumerator DisableSoundAfterDuration(AudioSource audioSource, AudioType type, float duration)
    {
        yield return new WaitForSeconds(duration);
        audioSource.Stop();
        audioSource.gameObject.SetActive(false);
        audioPools[type].Enqueue(audioSource);
    }

    private AudioSource GetAudioInstance(AudioType type)
    {
        if (audioPools[type].Count > 0)
        {
            AudioSource audioSource = audioPools[type].Dequeue();
            return audioSource;
        }
        else
        {
            return CreateNewAudioInstance(type);
        }
    }

    private AudioSource CreateNewAudioInstance(AudioType type)
    {
        GameObject audioObject = new GameObject(type + "_AudioInstance");
        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioObject.transform.SetParent(transform);
        audioObject.SetActive(false);
        return audioSource;
    }

    [System.Serializable]
    public struct AudioEntry
    {
        public AudioType type;
        public List<AudioClip> audioClips;
    }
}

public enum AudioType
{
    Explosion,
    Fire,
    Impact,
    Pickup,
    GUIInteract,
    AirHiss,
    RocksBreaking,
    // Add other audio types here
}