using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPC : MonoBehaviour
{
    public NavMeshAgent npc;
    public Transform targetTransform;
    public bool foundPath;

    // Start is called before the first frame update
    void Start()
    {
        //npc.GetComponent<NavMeshAgent>();
        
    }

    // Update is called once per frame
    void Update()
    {
        npc.SetDestination(targetTransform.position);
        foundPath = npc.hasPath;
        //npc.Move(new Vector3(5, 0, 0));
    }
}
