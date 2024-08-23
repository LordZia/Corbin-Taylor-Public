using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectRotater : MonoBehaviour
{
    private Quaternion originalRotation;
    private void Awake()
    {
        originalRotation = transform.localRotation;
    }

    // Rotate to the target rotation over a specified duration
    public void RotateOverTimeTo(Quaternion targetRotation, float duration)
    {
        StartCoroutine(RotateRoutine(targetRotation, duration));
    }

    // Rotate to the target rotation and then back to the original rotation over a specified duration
    public void RotateOverTimePingPong(Quaternion targetRotation, float duration)
    {
        StartCoroutine(RotatePingPongRoutine(targetRotation, duration));
    }

    private System.Collections.IEnumerator RotateRoutine(Quaternion targetRotation, float duration)
    {
        float timeElapsed = 0f;
        Quaternion startRotation = transform.localRotation;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, timeElapsed / duration);
            yield return null;
        }

        transform.localRotation = targetRotation;
    }

    private System.Collections.IEnumerator RotatePingPongRoutine(Quaternion targetRotation, float duration)
    {
        yield return RotateRoutine(targetRotation, duration);

        yield return new WaitForSeconds(0.5f); // Adjust delay before ping pong

        yield return RotateRoutine(originalRotation, duration);
    }
}
