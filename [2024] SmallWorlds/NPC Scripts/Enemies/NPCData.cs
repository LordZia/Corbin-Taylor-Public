using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCData", menuName = "NPCs/NPC Data")]
public class NPCData : ScriptableObject
{
    [SerializeField] private string npcName = "default name";
    [SerializeField] private int npcID = 0;
    public string NpcName => npcName;
    public int NPCID => npcID;

    public void Initialize(string npcName, int npcID)
    {
        this.npcName = npcName;
        this.npcID = npcID;
    }
}
