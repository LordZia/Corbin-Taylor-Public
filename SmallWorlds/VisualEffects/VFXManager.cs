using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;


public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [SerializeField]
    private List<VFXEntry> vfxEntries = new List<VFXEntry>();

    private Dictionary<VFXType, VisualEffectAsset> vfxDictionary = new Dictionary<VFXType, VisualEffectAsset>();
    private Dictionary<VFXType, Queue<VisualEffect>> vfxPools = new Dictionary<VFXType, Queue<VisualEffect>>();

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

        foreach (var entry in vfxEntries)
        {
            vfxDictionary[entry.type] = entry.vfxAsset;
            vfxPools[entry.type] = new Queue<VisualEffect>();
        }
    }

    public void PlayEffect(VFXType type, Vector3 position, Quaternion rotation, float duration)
    {
        if (vfxDictionary.TryGetValue(type, out var vfxAsset))
        {
            VisualEffect vfx = GetVFXInstance(type, vfxAsset);
            vfx.transform.position = position;
            vfx.transform.rotation = rotation;
            vfx.visualEffectAsset = vfxAsset;
            vfx.gameObject.SetActive(true);
            //vfx.Play();

            vfx.SendEvent("Start");

            StartCoroutine(DisableEffectAfterDuration(vfx, type, duration));
        }
        else
        {
            Debug.LogWarning($"VFX of type {type} not found");
        }
    }

    private IEnumerator DisableEffectAfterDuration(VisualEffect vfx, VFXType type, float duration)
    {
        yield return new WaitForSeconds(duration);
        vfx.Stop();
        vfx.gameObject.SetActive(false);
        vfxPools[type].Enqueue(vfx);
    }

    private VisualEffect GetVFXInstance(VFXType type, VisualEffectAsset vfxAsset)
    {
        if (vfxPools[type].Count > 0)
        {
            VisualEffect vfx = vfxPools[type].Dequeue();
            return vfx;
        }
        else
        {
            return CreateNewVFXInstance(type, vfxAsset);
        }
    }

    private VisualEffect CreateNewVFXInstance(VFXType type, VisualEffectAsset vfxAsset)
    {
        GameObject vfxObject = new GameObject(type + "_VFXInstance");
        VisualEffect vfx = vfxObject.AddComponent<VisualEffect>();
        vfx.visualEffectAsset = vfxAsset;
        vfxObject.transform.SetParent(transform);
        vfxObject.SetActive(false);
        return vfx;
    }

    [System.Serializable]
    public struct VFXEntry
    {
        public VFXType type;
        public VisualEffectAsset vfxAsset;
    }
}

public enum VFXType
{
    Explosion,
    Fire,
    Smoke,
    Impact,
    Sparks,
    Dust
}

public static class VisualEffectExtensions
{
    public static bool HasAnyActiveEffects(this VisualEffect vfx)
    {
        // Check if the visual effect is still playing any effects
        return vfx.aliveParticleCount > 0;
    }
}
