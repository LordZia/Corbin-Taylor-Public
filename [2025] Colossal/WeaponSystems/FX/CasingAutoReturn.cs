using System.Collections;
using UnityEngine;
using VFX;

[DisallowMultipleComponent]
public class CasingAutoReturn : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody rb;

    [Header("Return Rules")]
    [SerializeField] private float lifetime = 3f;          // fallback if manager's def.lifetime not used
    [SerializeField] private float sleepReturnDelay = 0.75f;

    private int _effectID = -1;
    private bool _armed;

    // Call this from your ejector right after PlayEffect(...)
    public void Arm(int effectID, Vector3 linearVel, Vector3 angularVelRad, float overrideLifetime = -1f)
    {
        _effectID = effectID;
        _armed = true;

        if (rb)
        {
            rb.WakeUp();
            rb.velocity = linearVel;
            rb.angularVelocity = angularVelRad; // rad/s
        }

        StopAllCoroutines();
        StartCoroutine(ReturnRoutine(overrideLifetime > 0f ? overrideLifetime : lifetime));
    }

    private IEnumerator ReturnRoutine(float lt)
    {
        float end = Time.time + lt;
        float sleepSince = -1f;

        while (true)
        {
            if (Time.time >= end) break;

            if (rb)
            {
                if (rb.IsSleeping())
                {
                    if (sleepSince < 0f) sleepSince = Time.time;
                    if (Time.time - sleepSince >= sleepReturnDelay) break;
                }
                else sleepSince = -1f;
            }

            yield return null;
        }

        if (_armed && _effectID >= 0 && EffectPoolManager.Instance)
            EffectPoolManager.Instance.DespawnNow(gameObject, _effectID);
    }
}
