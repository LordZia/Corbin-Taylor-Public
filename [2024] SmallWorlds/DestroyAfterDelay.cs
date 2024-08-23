using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfterDelay : MonoBehaviour
{
    private float delay;

    public void Initialize(float delay)
    {
        this.delay = delay;
        Destroy(gameObject, delay);
    }
}
