using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VFX
{
    public class EffectPoolManager : MonoBehaviour
    {
        public List<EffectDefinition> effectDefinitions;

        private Dictionary<int, Queue<GameObject>> poolByID = new();
        private Dictionary<int, EffectDefinition> defByID = new();
        private Dictionary<string, EffectDefinition> defByName = new();

        // Track actives per effect (for caps & safe double-returns)
        private Dictionary<int, LinkedList<GameObject>> activeByID = new();

        private static EffectPoolManager instance;
        public static EffectPoolManager Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();
        }

        private void InitializePools()
        {
            foreach (var def in effectDefinitions)
            {
                if (def == null) continue;

                defByID[def.effectID] = def;
                defByName[def.effectName] = def;

                if (!poolByID.ContainsKey(def.effectID))
                    poolByID[def.effectID] = new Queue<GameObject>();

                if (!activeByID.ContainsKey(def.effectID))
                    activeByID[def.effectID] = new LinkedList<GameObject>();

                for (int i = 0; i < def.preloadCount; i++)
                {
                    var obj = Instantiate(def.prefab, this.transform);
                    PrepareForPooling(obj);
                    obj.SetActive(false);
                    poolByID[def.effectID].Enqueue(obj);
                }
            }
        }

        static void PrepareForPooling(GameObject obj)
        {
            foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                if (main.stopAction == ParticleSystemStopAction.Destroy)
                    main.stopAction = ParticleSystemStopAction.Disable;
            }
        }

        public GameObject PlayEffect(int effectID, Vector3 position, Quaternion rotation)
        {
            if (!defByID.TryGetValue(effectID, out var def))
            {
                Debug.LogWarning($"Effect ID {effectID} not found.");
                return null;
            }

            var obj = GetPooledObject(def);
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.transform.SetParent(transform, true);
            obj.SetActive(true);

            MarkActive(effectID, obj);

            if (def.returnPolicy == ReturnPolicy.ManagerTimed)
                StartCoroutine(ReturnAfterLifetime(obj, def.lifetime, effectID));

            return obj;
        }

        public GameObject PlayEffect(string effectName, Vector3 position, Quaternion rotation)
        {
            if (!defByName.TryGetValue(effectName, out var def))
            {
                Debug.LogWarning($"Effect name {effectName} not found.");
                return null;
            }
            return PlayEffect(def.effectID, position, rotation);
        }

        public GameObject PlayEffectFollow(int effectID, Transform target, Vector3 offset, bool offsetIsLocal = true, bool matchRotation = false)
        {
            if (!defByID.TryGetValue(effectID, out var def))
            {
                Debug.LogWarning($"Effect ID {effectID} not found.");
                return null;
            }

            var obj = GetPooledObject(def);

            // Pre-position before activation
            if (target != null)
            {
                obj.transform.position = offsetIsLocal ? target.TransformPoint(offset) : target.position + offset;
                if (matchRotation) obj.transform.rotation = target.rotation;
            }

            obj.SetActive(true);

            // Ensure follower exists/reset
            var follower = obj.GetComponent<EffectFollower>();
            if (!follower) follower = obj.AddComponent<EffectFollower>();
            follower.ResetFollow();
            follower.Setup(target, offset, offsetIsLocal, matchRotation);

            MarkActive(effectID, obj);

            if (def.returnPolicy == ReturnPolicy.ManagerTimed)
                StartCoroutine(ReturnAfterLifetime(obj, def.lifetime, effectID));

            return obj;
        }

        private GameObject GetPooledObject(EffectDefinition def)
        {
            // Optional cap: return oldest active if over limit
            if (def.maxActive > 0 && activeByID.TryGetValue(def.effectID, out var activeList) && activeList.Count >= def.maxActive)
            {
                var oldest = activeList.First?.Value;
                if (oldest != null) DespawnNow(oldest, def.effectID);
            }

            var pool = poolByID[def.effectID];
            if (pool.Count > 0)
                return pool.Dequeue();

            var obj = Instantiate(def.prefab, transform);
            PrepareForPooling(obj);
            return obj;
        }

        private void MarkActive(int effectID, GameObject obj)
        {
            if (!activeByID.TryGetValue(effectID, out var list))
                activeByID[effectID] = list = new LinkedList<GameObject>();
            list.AddLast(obj);
        }

        public void DespawnNow(GameObject obj, int effectID)
        {
            if (obj == null) return;

            if (obj.TryGetComponent<EffectFollower>(out var f)) f.ResetFollow();
            obj.transform.SetParent(transform, true);
            obj.SetActive(false);

            // Remove from active list if present
            if (activeByID.TryGetValue(effectID, out var list))
            {
                var node = list.Find(obj);
                if (node != null) list.Remove(node);
            }

            if (!poolByID.TryGetValue(effectID, out var q))
                poolByID[effectID] = q = new Queue<GameObject>();
            q.Enqueue(obj);
        }

        private IEnumerator ReturnAfterLifetime(GameObject obj, float delay, int effectID)
        {
            var inst = obj;
            yield return new WaitForSeconds(delay);

            if (inst == null) yield break;
            // Already returned? (e.g., a component called DespawnNow)
            if (!inst.activeSelf) yield break;

            try
            {
                if (inst.transform != null)
                    inst.transform.SetParent(transform, true);

                if (inst.TryGetComponent<EffectFollower>(out var follower))
                    follower.ResetFollow();

                inst.SetActive(false);

                if (activeByID.TryGetValue(effectID, out var list))
                {
                    var node = list.Find(inst);
                    if (node != null) list.Remove(node);
                }

                if (!poolByID.TryGetValue(effectID, out var q))
                    poolByID[effectID] = q = new Queue<GameObject>();

                q.Enqueue(inst);
            }
            catch (MissingReferenceException)
            {
                Debug.LogError("An effect was destroyed during its lifetime.");
            }
        }

        public EffectDefinition GetDefinitionByName(string name)
        {
            defByName.TryGetValue(name, out var def);
            return def;
        }
    }

    public enum NetworkEffectID
    {
        ExplosionSmall,
        ExplosionMedium,
        ExplosionLarge,
        BloodHit,
        ShrapnelBurst,
        Impact_Metal,
        Impact_Stone,
        RadiationPulse,
        ElectricShock
        // Add more as needed
    }

    public interface IAutoReturnEffect
    {
        void Arm(int effectID, float lifetimeSeconds);
    }
}
