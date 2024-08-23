using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    void Start()
    {
        Utility.SetChildRendererStatus(this.transform, false);
        Utility.SetChildColliderStatus(this.transform, false);
    }

}
