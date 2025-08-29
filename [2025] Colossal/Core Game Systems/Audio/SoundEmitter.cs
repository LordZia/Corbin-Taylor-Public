using UnityEngine;
using System.Collections;

public enum SoundOrigin { Local, Remote }

[RequireComponent(typeof(AudioSource))]
public class SoundEmitter : MonoBehaviour, IPoolable
{
    private AudioSource _source;
    private Coroutine _returnRoutine;

    public SoundDefinition Definition { get; private set; }
    public float SpawnTime { get; private set; }
    public SoundOrigin Origin { get; private set; }
    public int OwnerId { get; private set; }

    private Transform _followTarget;
    private Vector3 _moveDirection;
    private float _moveSpeed;
    private bool _useMoveMode;

    public string PoolKey => "SoundEmitter";

    public void Initialize(SoundPool pool) { /* no-op for pool linking */ }
    public void OnWarm() 
    {
        _source = this.GetComponent<AudioSource>(); 
    }
    public void OnDespawn()
    {
        if (_returnRoutine != null)
            StopCoroutine(_returnRoutine);
        _source.Stop();
        _followTarget = null;
        _useMoveMode = false;

        SoundManager.Instance.OnEmitterFinished(this);
    }
    public void OnSpawn() { }

    public void Play(
        SoundDefinition def,
        float volumeMultiplier  = 1f,
        float pitchOffset   = 0f,
        Transform followTarget  = null,
        Vector3? moveDirection  = null,
        float moveSpeed         = 0f,
        SoundOrigin origin      = SoundOrigin.Local,
        int ownerId             = 0
        )
    {
        Definition    = def;
        SpawnTime     = Time.time;
        Origin        = origin;
        OwnerId       = ownerId;

        _followTarget   = followTarget;
        _useMoveMode    = moveDirection.HasValue;
        _moveDirection  = moveDirection ?? Vector3.zero;
        _moveSpeed      = moveSpeed;

        // initial position
        if (_followTarget != null)
            transform.position = _followTarget.position;

        _source.clip       = def.clip;
        _source.volume     = def.volume * SoundManager.Instance.MasterVolume * volumeMultiplier;

        float rawPitch = 1f
                + Random.Range(-def.pitchVariance, def.pitchVariance)
                + pitchOffset;

        // Clamp pitch to a reasonable audible range, 0.5x and 2.0x
        float clampedPitch = Mathf.Clamp(rawPitch, 0.5f, 2.0f);
        _source.pitch = clampedPitch;


        //Debug.Log($"[SoundEmitter] pitch being applied {_source.pitch} our pitch offset was {pitchOffset}");

        // Choose spatial blend based on local / remote origin
        bool use3D = origin == SoundOrigin.Local
            ? def.is3DLocal
            : def.is3DRemote;

        _source.spatialBlend = use3D ? 1f : 0f;

        _source.Play();
        _returnRoutine = StartCoroutine(ReturnAfter(def.clip.length));
    }

    private IEnumerator ReturnAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        SoundManager.Instance.soundPool.Despawn(this);
    }

    private void Update()
    {
        if (_followTarget != null)
            transform.position = _followTarget.position;
        else if (_useMoveMode)
            transform.position += _moveDirection.normalized * _moveSpeed * Time.deltaTime;
    }

    public void RegisterToPool()
    {
        throw new System.NotImplementedException();
    }
}