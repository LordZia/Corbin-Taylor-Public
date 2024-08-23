using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialResource : ResourceSource
{
    [SerializeField] GameObject toggleActiveOnBreak = null;
    [SerializeField]
    [Tooltip("duration is measured in seconds")]
    int objectActiveDuration = 0;
    public override void Break()
    {
        ResourceManager.Instance.AddBrokenRespawnableToSpawnList(this as IRespawnable);
        this.gameObject.SetActive(false);

        toggleActiveOnBreak.SetActive(true);
    }

    protected override void OnRespawn()
    {
        toggleActiveOnBreak.SetActive(false);
    }

}
