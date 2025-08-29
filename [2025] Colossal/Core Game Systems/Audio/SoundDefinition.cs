using UnityEngine;


[CreateAssetMenu(menuName = "Audio/Sound Definition")]
public class SoundDefinition : ScriptableObject
{
    [HideInInspector] public string soundId;

    public AudioClip clip;
    public float volume = 1f;
    public float pitchVariance = 0.05f;

    [Header("Spatial Settings")]
    [Tooltip("Should this sound use 3D spatialization when played locally?")]
    public bool is3DLocal = true;

    [Tooltip("Should this sound use 3D spatialization when played remotely?")]
    public bool is3DRemote = true;

    [Header("Caps & Priority")]
    public int maxLocalInstances = 4;
    public int maxRemoteInstances = 4;
    public int priority = 0;

    private void OnValidate()
    {
        soundId = name;
    }
}
