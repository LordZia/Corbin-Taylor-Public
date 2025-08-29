using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    public SoundPool soundPool;

    [SerializeField, Range(0f, 1f)]
    private float masterVolume = 1f;
    public float MasterVolume { get => masterVolume; set => masterVolume = Mathf.Clamp01(value); }

    [SerializeField]
    private List<SoundDefinition> definitions;
    [SerializeField]
    private int globalMaxActiveSounds = 64;
    [SerializeField]
    private bool debugMode = false;

    private Dictionary<string, SoundDefinition> _defLookup;
    private readonly List<SoundEmitter> _localEmitters = new List<SoundEmitter>();
    private readonly Dictionary<int, List<SoundEmitter>> _remoteEmittersByPlayer =
        new Dictionary<int, List<SoundEmitter>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;   
        }

        _defLookup = definitions
            .Where(d => d != null)
            .ToDictionary(d => d.soundId, d => d);

        foreach (var def in _defLookup)
        {
            if (def.Value.soundId == null)
            {
                Debug.LogError($"[SoundManager] sound entry {def.Value.name} was removed from the _defLookup table : is missing a soundID ");
                _defLookup.Remove(def.Key);
            }
        }
    }

    /// <summary>
    /// Allows the passthrough of any owning component or game object.
    /// Then we check if it has a photon view to allow for networked audio.
    /// If no photon view is detected the sound is only played locally.
    /// </summary>
    public void Play(
        string soundId,
        Vector3 pos,
        Component owner,             // any MonoBehaviour or GameObject
        Transform followTarget = null,
        Vector3? moveDirection = null,
        float moveSpeed = 0f)
    {
        // try to pull a PhotonView off the owner
        int viewID = 0;
        bool isLocal = false;
        if (owner != null)
        {
            var pv = owner.GetComponent<PhotonView>();
            if (pv != null)
            {
                viewID = pv.ViewID;
                isLocal = pv.IsMine;
            }
        }

        Play(soundId, pos, isLocal, viewID, followTarget, moveDirection, moveSpeed);
    }

    /// <summary>
    /// Play a sound identified by soundId at position pos.
    /// ownerId is the Photon actor number of the source (0 for local).
    /// </summary>
    public void Play(
        string soundId,
        Vector3 pos,
        bool isLocal,
        int ownerId = 0,
        Transform followTarget = null,
        Vector3? moveDirection = null,
        float moveSpeed = 0f,
        float pitchAdjustment = 0f
        )
    {
        // Auto-assign local owner if none provided
        if (ownerId == 0)
            ownerId = PhotonNetwork.LocalPlayer.ActorNumber;

        // Determine origin
        SoundOrigin origin = isLocal
            ? SoundOrigin.Local
            : SoundOrigin.Remote;

        if (pitchAdjustment !=0 )
            pitchAdjustment = Mathf.Clamp01(pitchAdjustment);

        if (debugMode)
            Debug.Log("[SoundManager] Play('" + soundId + "') at " + pos
                      + ", origin=" + origin + ", owner=" + ownerId);

        if (debugMode && soundId == null)
            Debug.LogError("soundID invald");

        // 1) Lookup definition
        if (!_defLookup.TryGetValue(soundId, out var def))
        {
            Debug.LogError("[SoundManager] No SoundDefinition found for id '"
                           + soundId + "'. Registered IDs: "
                           + string.Join(", ", _defLookup.Keys));
            return;
        }
        if (debugMode)
            Debug.Log("[SoundManager] Definition found: soundId='"
                      + def.soundId + "', assetName='" + def.name + "'");

        // 2) Clip null check
        if (def.clip == null)
        {
            Debug.LogError("[SoundManager] SoundDefinition '"
                           + def.soundId
                           + "' has no AudioClip assigned in its 'clip' field.");
            return;
        }
        if (debugMode)
            Debug.Log("[SoundManager] AudioClip '" + def.clip.name + "' is assigned");

        // Per-origin cap
        if (origin == SoundOrigin.Local)
        {
            var locals = _localEmitters.Where(e => e.Definition == def).ToList();
            if (locals.Count >= def.maxLocalInstances)
            {
                if (debugMode)
                    Debug.Log($"[SoundManager] Local cap for '{soundId}' reached. Evicting oldest via pool.");
                soundPool.Despawn(locals.OrderBy(e => e.SpawnTime).First());
            }
        }
        else
        {
            if (!_remoteEmittersByPlayer.TryGetValue(ownerId, out var remotes))
            {
                _remoteEmittersByPlayer[ownerId] = remotes = new List<SoundEmitter>();
            }
            int count = remotes.Count(e => e.Definition == def);
            // AFTER (correct):
            if (count >= def.maxRemoteInstances)
            {
                if (debugMode)
                    Debug.Log($"[SoundManager] Remote cap for '{soundId}' from player {ownerId} reached. Evicting via pool.");
                soundPool.Despawn(remotes.OrderBy(e => e.SpawnTime).First());
            }
        }

        // Global cap
        int total = _localEmitters.Count + _remoteEmittersByPlayer.Values.Sum(l => l.Count);
        if (total >= globalMaxActiveSounds)
        {
            var candidate = _localEmitters
                .Concat(_remoteEmittersByPlayer.SelectMany(kv => kv.Value))
                .OrderBy(e => e.Definition.priority)
                .ThenBy(e => e.SpawnTime)
                .FirstOrDefault();
            if (candidate != null)
            {
                if (debugMode)
                {
                    Debug.Log("[SoundManager] Global cap reached. Evicting '"
                              + candidate.Definition.soundId + "'.");
                }
                soundPool.Despawn(candidate);
            }
        }

        // Spawn & play
        var emitter = soundPool.Spawn(pos);
        emitter.Play(
            def,
            masterVolume,
            pitchAdjustment,
            followTarget,
            moveDirection,
            moveSpeed,
            origin,
            ownerId
            );

        // Track
        if (origin == SoundOrigin.Local)
        {
            _localEmitters.Add(emitter);
        }
        else
        {
            _remoteEmittersByPlayer[ownerId].Add(emitter);
        }
    }

    /// <summary>
    /// Called by SoundPool.Despawn to remove an emitter from tracking.
    /// </summary>
    public void OnEmitterFinished(SoundEmitter emitter)
    {
        if (emitter.Origin == SoundOrigin.Local)
        {
            _localEmitters.Remove(emitter);
        }
        else if (_remoteEmittersByPlayer.TryGetValue(emitter.OwnerId, out var list))
        {
            list.Remove(emitter);
        }
    }

    /// <summary>
    /// Show active sound IDs and counts in the inspector.
    /// </summary>
    public IEnumerable<string> GetActiveSoundIds()
    {
        var localGroups = _localEmitters
            .GroupBy(e => e.Definition.soundId)
            .Select(g => g.Key + " (L:" + g.Count() + ")");
        var remoteGroups = _remoteEmittersByPlayer
            .SelectMany(kv => kv.Value)
            .GroupBy(e => e.Definition.soundId)
            .Select(g => g.Key + " (R:" + g.Count() + ")");
        return localGroups.Concat(remoteGroups);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SoundManager))]
    private class SoundManagerEditor : Editor
    {
        private SoundManager _mgr;
        private void OnEnable()
        {
            _mgr = (SoundManager)target;
            EditorApplication.update += RepaintIfPlaying;
        }
        private void OnDisable()
        {
            EditorApplication.update -= RepaintIfPlaying;
        }
        private void RepaintIfPlaying()
        {
            if (Application.isPlaying)
                Repaint();
        }
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Active Sounds", EditorStyles.boldLabel);
            foreach (var entry in _mgr.GetActiveSoundIds())
            {
                EditorGUILayout.LabelField("  - " + entry);
            }
        }
    }
#endif
}
