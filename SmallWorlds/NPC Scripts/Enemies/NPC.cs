using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour
{
    [SerializeField] protected NPCData npcData;
    [SerializeField] protected string npcName;
    [SerializeField] protected int npcID;

    [SerializeField] protected NPCData NPCInstance;

    void Awake()
    {
        if (npcData == null)
        {
            Debug.LogError($"{this.gameObject.name} does not hold a reference to an NPCData scriptable object, assign one in the inspector");
            return;
        }

        OnAwake();
        UnpackData(npcData);
    }

    private void UnpackData(NPCData _npcData)
    {
        this.npcName = _npcData.NpcName;
        this.npcID = _npcData.NPCID;
        this.NPCInstance = ScriptableObject.CreateInstance<NPCData>();

        OnUnpackData(_npcData);
    }
    protected virtual void OnUnpackData(NPCData _npcData)
    {
    }

    // Alert inheriting classes of successful awake
    protected virtual void OnAwake()
    {
    }
}
