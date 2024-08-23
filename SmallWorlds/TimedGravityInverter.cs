using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(GravitySource))]
public class TimedGravityInverter : MonoBehaviour
{
    [SerializeField] private float interval = 5f;
    [SerializeField] private bool active = false;

    private float lastExecutedTime;
    private GravitySource gravitySource;

    void Start()
    {
        gravitySource = this.GetComponent<GravitySource>();

        SetActive();
    }

    IEnumerator ExecuteTargetMethod()
    {
        while (true)
        {
            if (active && Time.time - lastExecutedTime >= interval)
            {
                gravitySource.InvertGravityDir();
                lastExecutedTime = Time.time;
            }
            yield return null;
        }
    }

    public void SetActive()
    {
        active = true;
        lastExecutedTime = Time.time;
        StartCoroutine(ExecuteTargetMethod());
    }

    public void SetNotActive()
    {
        active = false;
        StopCoroutine(ExecuteTargetMethod());
    }

    public void ForceGravityInversionState(bool boolean)
    {
        gravitySource.SetGravityInversionState(boolean);
    }
}
