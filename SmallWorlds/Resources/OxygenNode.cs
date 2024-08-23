using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OxygenNode : ResourceSource
{
    [SerializeField] private GameObject SpawnObjectOnBreak;
    [SerializeField] private float oxygenSourceLifetimeMin = 1.0f;
    [SerializeField] private float oxygenSourceLifetimeMax = 1.5f;

    // Start is called before the first frame update
    void Awake()
    {
        if (SpawnObjectOnBreak == null)
        {
            Debug.LogError("OxygenNode requires a spawn object on break reference, assign one in the inspector", this);
        }
    }

    protected override void OnBreak()
    {
        GameObject spawnedObject = Instantiate(SpawnObjectOnBreak, this.transform.position, this.transform.rotation);
        AudioManager.Instance.PlaySound(AudioType.AirHiss, this.transform.position);

        float oxygenSourceLifetime = Random.Range(oxygenSourceLifetimeMin, oxygenSourceLifetimeMax);
        spawnedObject.AddComponent<DestroyAfterDelay>().Initialize(oxygenSourceLifetime);
        spawnedObject.SetActive(true);
    }
}
