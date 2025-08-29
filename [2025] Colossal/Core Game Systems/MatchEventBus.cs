using System;
using UnityEngine;
using Zia.GameModes;
using UnityEngine.SceneManagement;

public class MatchEventBus : MonoBehaviour
{
    public static MatchEventBus Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool logEvents = false;

    // -------------------------
    // Event payloads
    // -------------------------

    public struct PlayerSpawnedEvent { public int PlayerId; public PlayerMain Player; }
    public struct PlayerDespawnedEvent { public int PlayerId; public PlayerMain Player; }

    public struct MatchPhaseChangedEvent
    {
        public MatchPhase Phase;
        public int RoundIndex;
    }
    
    // Positional Events
    public struct PlayerPositionEvent
    {
        public int PlayerId;
        public Vector3 Position;
        public float TimeSeconds;
    }

    public struct TrackedSpawned { public int ViewId; public TrackedObjectType Type; }
    public struct TrackedDespawned { public int ViewId; public TrackedObjectType Type; }
    public struct TrackedPosition { public int ViewId; public TrackedObjectType Type; public Vector3 Position; public float TimeSeconds; }


    // Combat
    public struct PlayerDamagedEvent
    {
        public int AttackerId;
        public int VictimId;
        public int Damage;
        public int WeaponId;      // optional; -1 if unknown
        public float TimeSeconds;
    }

    public struct PlayerKilledEvent
    {
        public int KillerId;
        public int VictimId;
        public int WeaponId;      // optional; -1 if unknown
        public float TimeSeconds;
    }

    public struct PlayerDiedEvent
    {
        public int VictimId;
        public float TimeSeconds;
    }

    public struct PlayerAssistEvent
    {
        public int AssisterId;
        public int VictimId;
        public float TimeSeconds;
    }

    // Scoring / Lives (for UI + replication listeners)
    public struct ScoreDeltaEvent
    {
        public bool IsTeam;
        public Team Team;     // valid if IsTeam==true
        public int PlayerId;  // valid if IsTeam==false
        public int NewScore;
    }

    public struct LivesDeltaEvent
    {
        public int PlayerId;
        public int LivesRemaining; // -1 for infinite
    }

    // Respawn flow (authority decides, spawn system listens)
    public struct PlayerRespawnPermittedEvent
    {
        public int PlayerId;
        public float DelaySeconds;
    }

    // Specific Gamemode payloads
    public struct TargetDestroyedEvent
    {
        public int PlayerId;     // attacker who destroyed the target
        public int TargetId;     // we’ll use PhotonView.ViewID (or a custom ID if you prefer)
        public float TimeSeconds;
    }

    // -------------------------
    // Events
    // -------------------------
    // Match
    public event Action<MatchPhase, int> OnMatchPhaseChanged;

    public event Action<PlayerSpawnedEvent> OnPlayerSpawned;
    public event Action<PlayerDespawnedEvent> OnPlayerDespawned;

    // Position Tracking
    public event Action<PlayerPositionEvent> OnPlayerPositionShout;
    public event Action<TrackedSpawned> OnTrackedSpawned;
    public event Action<TrackedDespawned> OnTrackedDespawned;
    public event Action<TrackedPosition> OnTrackedPosition;

    // Combat
    public event Action<PlayerDamagedEvent> OnPlayerDamaged;
    public event Action<PlayerKilledEvent> OnPlayerKilled;
    public event Action<PlayerDiedEvent> OnPlayerDied;
    public event Action<PlayerAssistEvent> OnPlayerAssist;

    public event Action<ScoreDeltaEvent> OnScoreDelta;
    public event Action<LivesDeltaEvent> OnLivesDelta;

    public event Action<PlayerRespawnPermittedEvent> OnPlayerRespawnPermitted;

    // specific gamemode events
    public event Action<TargetDestroyedEvent> OnTargetDestroyed;

    // -------------------------
    // Lifetime
    // -------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // -------------------------
    // Raisers
    // -------------------------
    public void RaiseMatchPhaseChanged(MatchPhase phase, int roundIndex)
    {
        if (logEvents) Debug.Log($"[MatchEventBus] MatchPhaseChanged phase={phase} round={roundIndex}");
        OnMatchPhaseChanged?.Invoke(phase, roundIndex);

        if (phase == MatchPhase.MatchEnd)
            SceneManager.LoadScene("Main Menu"); // VERY TEMP TESTING!!!
    }
    public void RaisePlayerSpawned(int playerId, PlayerMain player)
    {
        if (logEvents) Debug.Log($"[MatchEventBus] Spawned pid={playerId}");
        OnPlayerSpawned?.Invoke(new PlayerSpawnedEvent { PlayerId = playerId, Player = player });
    }

    public void RaisePlayerDespawned(int playerId, PlayerMain player)
    {
        if (logEvents) Debug.Log($"[MatchEventBus] Despawned pid={playerId}");
        OnPlayerDespawned?.Invoke(new PlayerDespawnedEvent { PlayerId = playerId, Player = player });
    }

    // Position Tracking
    public void RaisePlayerPosition(int playerId, Vector3 position)
    {
        OnPlayerPositionShout?.Invoke(new PlayerPositionEvent
        {
            PlayerId = playerId,
            Position = position,
            TimeSeconds = Time.time
        });
    }
    public void RaiseTrackedSpawned(int viewId, TrackedObjectType t)
    {
        if (logEvents) Debug.Log($"[Bus] TrackedSpawned {t} view={viewId}");
        OnTrackedSpawned?.Invoke(new TrackedSpawned { ViewId = viewId, Type = t });
    }
    public void RaiseTrackedDespawned(int viewId, TrackedObjectType t)
    {
        if (logEvents) Debug.Log($"[Bus] TrackedDespawned {t} view={viewId}");
        OnTrackedDespawned?.Invoke(new TrackedDespawned { ViewId = viewId, Type = t });
    }
    public void RaiseTrackedPosition(int viewId, TrackedObjectType t, Vector3 pos)
    {
        OnTrackedPosition?.Invoke(new TrackedPosition
        { ViewId = viewId, Type = t, Position = pos, TimeSeconds = Time.time });
    }

    // Combat
    public void RaisePlayerDamaged(int attackerId, int victimId, int damage, int weaponId = -1)
    {
        if (logEvents) Debug.Log($"[MatchEventBus] Damage atk={attackerId} vic={victimId} dmg={damage}");
        OnPlayerDamaged?.Invoke(new PlayerDamagedEvent
        {
            AttackerId = attackerId,
            VictimId = victimId,
            Damage = Mathf.Max(0, damage),
            WeaponId = weaponId,
            TimeSeconds = Time.time
        });
    }

    public void RaisePlayerKilled(int killerId, int victimId, int weaponId = -1)
    {
        if (logEvents) Debug.Log($"[MatchEventBus] Kill killer={killerId} victim={victimId}");
        var t = Time.time;

        OnPlayerKilled?.Invoke(new PlayerKilledEvent
        {
            KillerId = killerId,
            VictimId = victimId,
            WeaponId = weaponId,
            TimeSeconds = t
        });

        // Many systems only care about "a death happened"
        OnPlayerDied?.Invoke(new PlayerDiedEvent { VictimId = victimId, TimeSeconds = t });
    }

    // --- Raisers ---
    public void RaisePlayerDied(int victimId, float timeSeconds)
    {
        if (logEvents) Debug.Log($"[MatchEventBus] Died victim={victimId}");
        OnPlayerDied?.Invoke(new PlayerDiedEvent
        {
            VictimId = victimId,
            TimeSeconds = timeSeconds
        });
    }

    // Convenience overload (uses a consistent time source)
    public void RaisePlayerDied(int victimId)
    {
#if PHOTON_UNITY_NETWORKING
        float t = (float)Photon.Pun.PhotonNetwork.Time;
#else
    float t = Time.time;
#endif
        RaisePlayerDied(victimId, t);
    }

    public void RaisePlayerAssist(int assisterId, int victimId)
    {
        if (logEvents) Debug.Log($"[MatchEventBus] Assist assister={assisterId} victim={victimId}");
        OnPlayerAssist?.Invoke(new PlayerAssistEvent
        {
            AssisterId = assisterId,
            VictimId = victimId,
            TimeSeconds = Time.time
        });
    }

    // Scoring / Lives
    public void RaiseScoreForPlayer(int playerId, int newScore)
    {
        OnScoreDelta?.Invoke(new ScoreDeltaEvent
        {
            IsTeam = false,
            PlayerId = playerId,
            Team = Team.FFA,
            NewScore = newScore
        });
    }

    public void RaiseScoreForTeam(Team team, int newScore)
    {
        OnScoreDelta?.Invoke(new ScoreDeltaEvent
        {
            IsTeam = true,
            Team = team,
            PlayerId = -1,
            NewScore = newScore
        });
    }

    public void RaiseLivesForPlayer(int playerId, int livesRemaining)
    {
        OnLivesDelta?.Invoke(new LivesDeltaEvent
        {
            PlayerId = playerId,
            LivesRemaining = livesRemaining
        });
    }

    // Respawn flow
    public void RaisePlayerRespawnPermitted(int playerId, float delaySeconds)
    {
        if (logEvents) Debug.Log($"[MatchEventBus] RespawnPermitted pid={playerId} in {delaySeconds:0.00}s");
        OnPlayerRespawnPermitted?.Invoke(new PlayerRespawnPermittedEvent
        {
            PlayerId = playerId,
            DelaySeconds = Mathf.Max(0f, delaySeconds)
        });
    }

    // Specific Gamemode Raisers
    public void RaiseTargetDestroyed(int playerId, int targetId, float t)
    {
        if (logEvents) Debug.Log($"[MatchEventBus] TargetDestroyed player={playerId} target={targetId}");
        OnTargetDestroyed?.Invoke(new TargetDestroyedEvent { PlayerId = playerId, TargetId = targetId, TimeSeconds = t });
    }
    public void RaiseTargetDestroyed(int playerId, int targetId)
    {
#if PHOTON_UNITY_NETWORKING
        float t = (float)Photon.Pun.PhotonNetwork.Time;
#else
    float t = Time.time;
#endif
        RaiseTargetDestroyed(playerId, targetId, t);
    }
}

/// Data Types
public enum TrackedObjectType
{
    PilotCharacter,
    MechCharacter,
    BTT_Target,
    Objective_Flag,
    Objective_Bomb,
    Custom = 999
}
