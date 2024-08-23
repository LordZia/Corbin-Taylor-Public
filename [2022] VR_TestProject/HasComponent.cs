using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HasComponent
{
    public static bool hasComponent<T>(this GameObject flag) where T : Component
    {
        return flag.GetComponent<T>() != null;
    }
}
