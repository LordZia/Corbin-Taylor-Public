using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class GhostProjectilePool : MonoBehaviourPunCallbacks
{
    public static GhostProjectilePool Instance { get; private set; }

    [SerializeField] private GhostProjectile ghostPrefab;
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private int projectilesPerPlayer = 15;

    private BaseObjectPool<GhostProjectile> _pool;
    private readonly Dictionary<int, GhostProjectile> _active = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _pool = new BaseObjectPool<GhostProjectile>(ghostPrefab, initialPoolSize, transform);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        PrewarmPoolForPlayers();
    }

    private void PrewarmPoolForPlayers()
    {
        int playerCount = PhotonNetwork.CurrentRoom?.PlayerCount ?? 1;
        int totalToPrewarm = Mathf.Max(initialPoolSize, playerCount * projectilesPerPlayer);

        for (int i = 0; i < totalToPrewarm; i++)
        {
            var g = _pool.Spawn(Vector3.zero, Quaternion.identity);
            _pool.Despawn(g);
        }
    }

    // --- Helper: build a composite id that is unique across players ---
    // Call this from the shooter when you know (ownerViewID, localProjectileId).
    public static int MakeCompositeId(int ownerViewID, int localProjectileId)
    {
        // 0..65535 per shooter is plenty; xor to keep it int
        return ((ownerViewID & 0xFFFF) << 16) ^ (localProjectileId & 0xFFFF);
    }

    /// <summary>
    /// Owner calls: spawns a local ghost immediately and RPCs others to do the same.
    /// This version supports curving toward the real (logic) path.
    /// </summary>
    public void SpawnGhostRPC(
        int id,

        // visual (ghost starts here; muzzle)
        Vector3 visualOrigin,
        Vector3 visualVel,

        // logic (real projectile; camera)
        Vector3 logicOrigin,
        Vector3 logicVel,

        float gravity,
        double startTime,
        float lifetime,

        // steering
        float bendOverMeters,
        float lockDistance
    )
    {
        // 1) Local spawn (owner)
        SpawnLocalGhost(
            id, visualOrigin, visualVel,
            logicOrigin, logicVel,
            gravity, startTime, lifetime,
            bendOverMeters, lockDistance
        );

        // 2) Remote clients spawn theirs (NOT buffered; ghosts are short-lived VFX)
        photonView.RPC(
            nameof(RPC_SpawnGhost),
            RpcTarget.Others,
            id,
            visualOrigin, visualVel,
            logicOrigin, logicVel,
            gravity, startTime, lifetime,
            bendOverMeters, lockDistance
        );
    }

    private void SpawnLocalGhost(
        int id,
        Vector3 visualOrigin,
        Vector3 visualVel,
        Vector3 logicOrigin,
        Vector3 logicVel,
        float gravity,
        double startTime,
        float lifetime,
        float bendOverMeters,
        float lockDistance
    )
    {
        // Reuse if we somehow already have one (shouldn’t, but be robust)
        if (_active.TryGetValue(id, out var existing))
        {
            _pool.Despawn(existing);
            _active.Remove(id);
        }

        var g = _pool.Spawn(visualOrigin, Quaternion.LookRotation(visualVel.normalized, Vector3.up));
        g.Setup(
            id,
            visualOrigin, visualVel,
            logicOrigin, logicVel,
            gravity, startTime, lifetime,
            bendOverMeters, lockDistance
        );
        _active[id] = g;
    }

    [PunRPC]
    private void RPC_SpawnGhost(
        int id,
        Vector3 visualOrigin,
        Vector3 visualVel,
        Vector3 logicOrigin,
        Vector3 logicVel,
        float gravity,
        double startTime,
        float lifetime,
        float bendOverMeters,
        float lockDistance
    )
    {
        SpawnLocalGhost(
            id,
            visualOrigin, visualVel,
            logicOrigin, logicVel,
            gravity, startTime, lifetime,
            bendOverMeters, lockDistance
        );
    }

    /// <summary> Owner calls this to clear the ghost on self + others. </summary>
    public void DestroyGhostRPC(int id)
    {
        // Local first
        if (_active.TryGetValue(id, out var g))
        {
            _pool.Despawn(g);
            _active.Remove(id);
        }

        // Remote
        photonView.RPC(nameof(RPC_DestroyGhost), RpcTarget.Others, id);
    }

    [PunRPC]
    private void RPC_DestroyGhost(int id)
    {
        if (_active.TryGetValue(id, out var g))
        {
            _pool.Despawn(g);
            _active.Remove(id);
        }
    }
}
