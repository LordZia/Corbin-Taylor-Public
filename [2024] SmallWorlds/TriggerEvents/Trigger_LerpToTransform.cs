using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger_LerpToTransform : MonoBehaviour
{
    [SerializeField] private Transform transformToLerp;
    [SerializeField] private Transform targetTransform;

    public bool debugTrigger = false;

    private void Update()
    {
        if (debugTrigger)
        {
            LerpTransform();
        }
    }

    void Start()
    {
        if (targetTransform == null)
        {
            Debug.LogError("Trigger Telport requires a target position reference, please assign one in he inspector. disabling the script to prevent unexpected behavior.");
            this.gameObject.GetComponent<Collider>().enabled = false;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.gameObject.CompareTag("Player"))
        {
            this.gameObject.GetComponent<Collider>().enabled = false;
            LerpTransform();
        }
    }

    private void LerpTransform()
    {
        transformToLerp.position = targetTransform.position;
        transformToLerp.rotation = targetTransform.rotation;
    }

    public IEnumerator LerpTransform(float lerpTime)
    {
        float elapsedTime = 0.0f;
        Vector3 startingPos = transformToLerp.position;
        Quaternion startingRot = transformToLerp.rotation;

        while (elapsedTime < lerpTime)
        {
            float t = elapsedTime / lerpTime;
            transformToLerp.position = Vector3.Lerp(startingPos, targetTransform.position, t);
            transformToLerp.rotation = Quaternion.Slerp(startingRot, targetTransform.rotation, t);

            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        transformToLerp.position = targetTransform.position;
        transformToLerp.rotation = targetTransform.rotation;

        this.gameObject.GetComponent<Collider>().enabled = false;
    }
}
