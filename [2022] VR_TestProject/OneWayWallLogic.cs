using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneWayWallLogic : MonoBehaviour
{

    Collider[] wallColliders;
    MeshRenderer[] childMesh;

    bool allowPass;

    // Start is called before the first frame update
    void Start()
    {
        wallColliders = GetComponentsInChildren<Collider>();
        childMesh = GetComponentsInChildren<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (allowPass)
        {
            wallColliders[1].enabled = false;
            childMesh[1].enabled = false;
        }
        else
        {
            wallColliders[1].enabled = true;
            childMesh[1].enabled = true;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            allowPass = true;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            allowPass = false;
        }
    }

}
