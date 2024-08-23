using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundsDrawTest : MonoBehaviour
{
    Bounds bounds;
    // Start is called before the first frame update
    void Start()
    {
        bounds = Utility.GetBounds(this.gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        Utility.DrawBounds(bounds, Color.red);
    }
}
