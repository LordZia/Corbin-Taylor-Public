using Photon.Pun;
using UnityEngine;

namespace VFX
{
    public class NetworkEffectManager : MonoBehaviourPun
    {
        public static NetworkEffectManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Called by any client to only play an fx locally.
        /// </summary>
        public void PlayLocalEffect(int effectID, Vector3 position, Quaternion rotation)
        {
            EffectPoolManager.Instance.PlayEffect(effectID, position, rotation);
        }

        /// <summary>
        /// Called by any client to request a networked effect be played across all clients.
        /// </summary>
        public void PlayNetworkedEffect(int effectID, Vector3 position, Quaternion rotation)
        {
            // Play locally immediately
            PlayLocalEffect(effectID, position, rotation);

            // Send to other players
            photonView.RPC(nameof(RPC_PlayEffect), RpcTarget.Others, effectID, position, rotation);
        }

        public void PlayEffectOnOtherClientsOnly(int effectID, Vector3 position, Quaternion rotation)
        {
            // Send to other players
            photonView.RPC(nameof(RPC_PlayEffect), RpcTarget.Others, effectID, position, rotation);
        }

        [PunRPC]
        private void RPC_PlayEffect(int effectID, Vector3 pos, Quaternion rot)
        {
            EffectPoolManager.Instance.PlayEffect(effectID, pos, rot);
        }

        /// <summary>
        /// Optionally play an effect by string name
        /// </summary>
        public void PlayNetworkedEffect(string effectName, Vector3 position, Quaternion rotation)
        {
            if (EffectPoolManager.Instance == null) return;

            var def = EffectPoolManager.Instance.GetDefinitionByName(effectName);
            if (def == null)
            {
                Debug.LogWarning($"Effect '{effectName}' not found.");
                return;
            }

            PlayNetworkedEffect(def.effectID, position, rotation);
        }

        // ---------- Local follow ----------
        // ---------- Local follow helpers ----------
        public void PlayLocalEffectFollow(int effectID, Transform target, Vector3 offset, bool offsetIsLocal = true, bool matchRotation = false)
        {
            EffectPoolManager.Instance.PlayEffectFollow(effectID, target, offset, offsetIsLocal, matchRotation);
        }

        public void PlayLocalEffectFollow(string effectName, Transform target, Vector3 offset, bool offsetIsLocal = true, bool matchRotation = false)
        {
            int effID = EffectPoolManager.Instance.GetDefinitionByName(effectName).effectID;

            EffectPoolManager.Instance.PlayEffectFollow(effID, target, offset, offsetIsLocal, matchRotation);
        }

        // ---------- Networked follow (by target PhotonView + optional child path) ----------
        public void PlayNetworkedEffectFollow(int effectID, PhotonView targetPV, Vector3 offset, bool offsetIsLocal = true, bool matchRotation = false, string childPath = null)
        {
            if (targetPV == null) { Debug.LogWarning("[NetworkEffectManager] targetPV is null."); return; }

            var t = ResolveChild(targetPV.transform, childPath);
            PlayLocalEffectFollow(effectID, t, offset, offsetIsLocal, matchRotation);

            photonView.RPC(nameof(RPC_PlayEffectFollow),
                RpcTarget.Others,
                effectID,
                targetPV.ViewID,
                offset,
                offsetIsLocal,
                matchRotation,
                childPath ?? string.Empty);
        }

        public void PlayNetworkedEffectFollow(string effectName, PhotonView targetPV, Vector3 offset, bool offsetIsLocal = true, bool matchRotation = false, string childPath = null)
        {
            var def = EffectPoolManager.Instance.GetDefinitionByName(effectName);
            if (def == null) { Debug.LogWarning($"Effect '{effectName}' not found."); return; }
            PlayNetworkedEffectFollow(def.effectID, targetPV, offset, offsetIsLocal, matchRotation, childPath);
        }

        private Transform ResolveChild(Transform root, string childPath)
        {
            if (root == null) return null;
            if (string.IsNullOrEmpty(childPath)) return root;
            var child = root.Find(childPath);
            return child != null ? child : root;
        }

        [PunRPC]
        private void RPC_PlayEffectFollow(int effectID, int targetViewId, Vector3 offset, bool offsetIsLocal, bool matchRotation, string childPath)
        {
            var pv = PhotonView.Find(targetViewId);
            if (pv != null)
            {
                var t = ResolveChild(pv.transform, childPath);
                EffectPoolManager.Instance.PlayEffectFollow(effectID, t, offset, offsetIsLocal, matchRotation);
            }
            else
            {
                StartCoroutine(WaitForTargetAndPlay(effectID, targetViewId, offset, offsetIsLocal, matchRotation, childPath));
            }
        }

        private System.Collections.IEnumerator WaitForTargetAndPlay(int effectID, int targetViewId, Vector3 offset, bool offsetIsLocal, bool matchRotation, string childPath)
        {
            float timeout = 0.5f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                var pv = PhotonView.Find(targetViewId);
                if (pv != null)
                {
                    var t = ResolveChild(pv.transform, childPath);
                    EffectPoolManager.Instance.PlayEffectFollow(effectID, t, offset, offsetIsLocal, matchRotation);
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            Debug.LogWarning($"[NetworkEffectManager] Could not resolve target PV {targetViewId} for follow effect {effectID} in time.");
        }

    }
}
