using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Items
{
    [RequireComponent(typeof(WeaponController))]
    public class EquippedWeaponDisplayManager : MonoBehaviour
    {
        [Header("ADS Settings")]
        [Tooltip("Hip-fire mount")]
        [SerializeField] private Transform hipAnchor;
        [Tooltip("Aim-down-sights mount")]
        [SerializeField] private Transform adsAnchor;
        [SerializeField] private float aimSpeed = 10f;

        [SerializeField] private Camera playerCam;
        [SerializeField] private float hipFOV = 90f;
        [SerializeField] private float adsFOV = 70f;

        [SerializeField] private CasingEjector_Hybrid casingEjector;

        [Tooltip("Where the pooled view-models live when inactive")]
        [SerializeField] private Transform poolParent;

        [Tooltip("Where to parent the active view-model (hand bone)")]
        [SerializeField] private Transform holdPoint;

        [Tooltip("How many of each model to prewarm")]
        [SerializeField] private int initialPoolSize = 1;

        // One pool per weapon definition
        private Dictionary<WeaponDefinition, BaseObjectPool<PoolableViewModel>> _pools
            = new Dictionary<WeaponDefinition, BaseObjectPool<PoolableViewModel>>();

        private PoolableViewModel _activeModel;
        private WeaponDefinition _activeDef;

        public void Initialize()
        {
            // default parents
            if (poolParent == null) poolParent = WeaponPrefabPool.Instance.transform;
            if (holdPoint == null) Debug.LogError("holdPoint not assigned", this);

            if (playerCam == null) playerCam = this.GetComponent<PlayerMain>().CameraTransform.GetComponent<Camera>(); // this is really stupid will change later, - zia

            // Build one pool per definition
            foreach (var def in WeaponDatabase.Instance.AllDefinitions)
            {
                if (def.viewPrefab == null)
                    continue;

                // grab the poolable component from the prefab
                var prefabComp = def.viewPrefab.GetComponent<PoolableViewModel>();
                if (prefabComp == null)
                {
                    Debug.LogError($"ViewPrefab for '{def.weaponName}' is missing PoolableViewModel!");
                    continue;
                }

                // instantiate a BaseObjectPool under poolParent
                var pool = new BaseObjectPool<PoolableViewModel>(
                    prefabComp,
                    initialPoolSize,
                    poolParent
                );
                _pools[def] = pool;
            }
        }

        /// <summary>
        /// Shows (and returns) the view-model for this definition, aligned so that
        /// prefab.holdPoint lands exactly on controller.holdPoint.
        /// </summary>
        public GameObject ShowWeapon(WeaponDefinition def)
        {
            // 1) Despawn old
            if (_activeModel != null && _activeDef != null)
            {
                _pools[_activeDef].Despawn(_activeModel);
                _activeModel = null;
                _activeDef = null;
            }

            if (def == null || !_pools.TryGetValue(def, out var pool))
                return null;

            // 2) Spawn a new instance at the hand’s world position (approx)
            var inst = pool.Spawn(holdPoint.position, holdPoint.rotation);
            var go = inst.gameObject;

            // 3) Align using WeaponView holdPoint
            var wv = go.GetComponent<PoolableViewModel>();
            if (wv != null && wv.holdPoint != null)
            {
                // Compute how the root must move so wv.holdPoint lands on holdPoint
                Vector3 deltaPos = go.transform.position - wv.holdPoint.position;
                Quaternion deltaRot = go.transform.rotation * Quaternion.Inverse(wv.holdPoint.rotation);

                go.transform.SetPositionAndRotation(
                    holdPoint.position + deltaPos,
                    holdPoint.rotation * deltaRot
                );

                // Now parent under the hand (keep world transform)
                go.transform.SetParent(holdPoint, true);
            }
            else
            {
                // Fallback: just snap to zero under holdPoint
                go.transform.SetParent(holdPoint, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
            }

            // 4) Wire in Casing Spawner
            ConfigureCasingEjector(def, wv.EjectPoint);

            // 5) Activate & store
            inst.OnSpawn();
            _activeModel = inst;
            _activeDef = def;


            return go;
        }

        /// <summary>
        /// Hide whatever’s currently in hand.
        /// </summary>
        public void HideCurrent()
        {
            if (_activeModel != null && _activeDef != null)
            {
                _pools[_activeDef].Despawn(_activeModel);
                _activeModel = null;
                _activeDef = null;
            }
        }

        // Aiming
        private Coroutine _aimRoutine;

        /// <summary>
        /// Call this with true when the player starts aiming, false when they stop.
        /// </summary>
        public void Aim(bool isAiming)
        {
            // stop any in-progress move
            if (_aimRoutine != null)
                StopCoroutine(_aimRoutine);

            // choose the target anchor
            var target = isAiming ? adsAnchor : hipAnchor;
            if (_activeModel == null || target == null)
                return;

            _aimRoutine = StartCoroutine(MoveWeaponToAnchor(_activeModel.gameObject, target));
        }

        void ConfigureCasingEjector(WeaponDefinition def, Transform ejectPoint)
        {

            if (def.casings.enabled)
            {
                casingEjector.ConfigureFromWeaponDef(def, ejectPoint);
            }
            else
            {
                casingEjector.Disable();
            }
        }

        private IEnumerator MoveWeaponToAnchor(GameObject weaponGO, Transform anchor)
        {
            // We'll lerp both position and rotation
            var t = weaponGO.transform;
            while (true)
            {
                // smooth step
                t.position = Vector3.Lerp(t.position, anchor.position, Time.deltaTime * aimSpeed);
                t.rotation = Quaternion.Slerp(t.rotation, anchor.rotation, Time.deltaTime * aimSpeed);

                // done when close enough
                if (Vector3.Distance(t.position, anchor.position) < 0.001f &&
                    Quaternion.Angle(t.rotation, anchor.rotation) < 0.1f)
                {
                    t.position = anchor.position;
                    t.rotation = anchor.rotation;
                    break;
                }

                yield return null;
            }
            _aimRoutine = null;
        }

        private IEnumerator AnimateFOV(float targetFOV)
        {
            while (Mathf.Abs(playerCam.fieldOfView - targetFOV) > 0.1f)
            {
                playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, targetFOV, Time.deltaTime * aimSpeed);
                yield return null;
            }
            playerCam.fieldOfView = targetFOV;
        }
    }
}
