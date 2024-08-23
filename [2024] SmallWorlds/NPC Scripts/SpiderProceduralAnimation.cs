using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using Color = UnityEngine.Color;

public class SpiderProceduralAnimation : MonoBehaviour
{
    [SerializeField] public Transform bodyTransform;
    [SerializeField] public Transform[] legTargets;
    [SerializeField] public float stepSize = 1f;
    [SerializeField] public int smoothness = 1;
    [SerializeField] public float stepHeight = 0.1f;
    [SerializeField] public bool bodyOrientation = true;
    [SerializeField] private int legsInMotionLimit = 2;

    [SerializeField] private float raycastRange = 1f;
    private Vector3[] defaultLegPositions;
    private Vector3[] lastLegPositions;
    private Vector3 lastBodyUp;
    private bool[] legMoving;
    private int nbLegs;

    [SerializeField] Vector3 startingVelocityDebug = Vector3.zero;
    private Vector3 velocity;
    private Vector3 lastVelocity;
    private Vector3 lastBodyPos;

    [SerializeField] private Vector3 downDirection = Vector3.zero;

    private float velocityMultiplier = 15f;

    [SerializeField] private List<int> legGroup1 = new List<int>();
    [SerializeField] private List<int> legGroup2 = new List<int>();

    private float chestHeight;

    void Start()
    {
        lastBodyUp = -bodyTransform.forward;

        nbLegs = legTargets.Length;
        defaultLegPositions = new Vector3[nbLegs];
        lastLegPositions = new Vector3[nbLegs];
        legMoving = new bool[nbLegs];
        for (int i = 0; i < nbLegs; ++i)
        {
            defaultLegPositions[i] = legTargets[i].localPosition;
            lastLegPositions[i] = legTargets[i].position;
            legMoving[i] = false;
        }
        lastBodyPos = transform.position;

        this.GetComponent<Rigidbody>().velocity = startingVelocityDebug;

        Vector3[] raycastResults = new Vector3[2];
        RaycastHit hit;
        Ray ray = new Ray(this.transform.position, 5 * downDirection);

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider != null)
            {
                float distance = (this.transform.position - hit.point).magnitude;
                chestHeight = distance;
            }
        }
    }

    IEnumerator PerformStep(int index, Vector3 targetPoint)
    {
        Vector3 startPos = lastLegPositions[index];
        for (int i = 1; i <= smoothness; ++i)
        {
            legTargets[index].position = Vector3.Lerp(startPos, targetPoint, i / (float)(smoothness + 1f));
            legTargets[index].position += downDirection * Mathf.Sin(i / (float)(smoothness + 1f) * Mathf.PI) * stepHeight;
            yield return new WaitForFixedUpdate();
        }
        legTargets[index].position = targetPoint;
        lastLegPositions[index] = legTargets[index].position;
        legMoving[index] = false;
    }


    void FixedUpdate()
    {
        velocity = transform.position - lastBodyPos;
        velocity = (velocity + smoothness * lastVelocity) / (smoothness + 1f);

        if (velocity.magnitude < 0.000025f)
            velocity = lastVelocity;
        else
            lastVelocity = velocity;


        Vector3[] desiredPositions = new Vector3[nbLegs];
        int indexToMove = -1;

        bool[] legsToMove = new bool[nbLegs];

        float maxDistance = stepSize;
        for (int i = 0; i < nbLegs; ++i)
        {
            desiredPositions[i] = transform.TransformPoint(defaultLegPositions[i]);

            float distance = Vector3.ProjectOnPlane(desiredPositions[i] + velocity * velocityMultiplier - lastLegPositions[i], downDirection).magnitude;
            if (distance > maxDistance)
            {
                maxDistance = distance;
                indexToMove = i;
                legsToMove[i] = true;
            }
            else
            {
                legsToMove[i] = false;
            }
        }
        for (int i = 0; i < nbLegs; ++i)
            if (i != indexToMove)
                legTargets[i].position = lastLegPositions[i];

        
        int totalLegsInMotion = 0;
        for (int i = 0; i < legsToMove.Length; i++)
        {
            if (totalLegsInMotion > legsInMotionLimit) break;
            if (legsToMove[i] == true)
            {
                Vector3 targetPoint = desiredPositions[i] + Mathf.Clamp(velocity.magnitude * velocityMultiplier, 0.0f, 1.5f) * (desiredPositions[i] - legTargets[i].position) + velocity * velocityMultiplier;
                Vector3[] positionAndNormal = MatchToSurfaceFromAbove(targetPoint, raycastRange, downDirection);
                legMoving[i] = true;
                StartCoroutine(PerformStep(i, positionAndNormal[0]));
                totalLegsInMotion++;
            }
        }

        Vector3 targetPos = new Vector3(this.transform.position.x, chestHeight, this.transform.position.z);
        Vector3 targetChestHeight = Vector3.Lerp(this.transform.position, targetPos, Time.fixedDeltaTime);
        //this.transform.position = targetPos;

        /*
        List<int> legsInMotion = new List<int>();

        if (legGroup1.Contains(indexToMove))
        {
            foreach (var item in legGroup1)
            {
                legsInMotion.Add(item);
            }
        }
        else if (legGroup2.Contains(indexToMove))
        {
            foreach (var item in legGroup2)
            {
                legsInMotion.Add(item);
            }
        }
        else
        {
            legsInMotion.Add((int)indexToMove);
        }

        for (int i = 0; i < legsInMotion.Count; i++)
        {
            if (legsToMove[i] == true)
            {
                Vector3 targetPoint = desiredPositions[i] + Mathf.Clamp(velocity.magnitude * velocityMultiplier, 0.0f, 1.5f) * (desiredPositions[i] - legTargets[i].position) + velocity * velocityMultiplier;
                Vector3[] positionAndNormal = MatchToSurfaceFromAbove(targetPoint, raycastRange, downDirection);
                legMoving[i] = true;
                StartCoroutine(PerformStep(i, positionAndNormal[0]));
            }
        }
        */
        /*
        if (indexToMove != -1 && !legMoving[0])
        {
            Vector3 targetPoint = desiredPositions[indexToMove] + Mathf.Clamp(velocity.magnitude * velocityMultiplier, 0.0f, 1.5f) * (desiredPositions[indexToMove] - legTargets[indexToMove].position) + velocity * velocityMultiplier;
            Vector3[] positionAndNormal = MatchToSurfaceFromAbove(targetPoint, raycastRange, downDirection);
            legMoving[0] = true;
            StartCoroutine(PerformStep(indexToMove, positionAndNormal[0]));
        }
        */
        lastBodyPos = transform.position;
        if (nbLegs > 3 && bodyOrientation)
        {
            Vector3 averageLegPosition = Vector3.zero;
            for (int i = 0; i < nbLegs; i++)
            {
                averageLegPosition += legTargets[i].position;
            }
            averageLegPosition /= nbLegs;

            Vector3 v1 = (legTargets[0].position - legTargets[7].position);
            Vector3 v2 = (legTargets[4].position - legTargets[3].position);
            Vector3 normal = Vector3.Cross(v1, v2).normalized;

            Debug.DrawLine(legTargets[0].position, legTargets[7].position, Color.blue);
            Debug.DrawLine(legTargets[4].position, legTargets[3].position, Color.blue);
            Debug.DrawLine(bodyTransform.position, bodyTransform.position + normal * 5, Color.blue, 0.25f);

            Vector3 up = Vector3.Lerp(lastBodyUp, normal, 1f / (float)(smoothness + 1) * Time.fixedDeltaTime);
            bodyTransform.forward = up * -1;
            //bodyTransform.position = averageLegPosition + (-up * 3);
            //Vector3 adjustedUp = 

            Debug.DrawLine(bodyTransform.position, bodyTransform.position + bodyTransform.forward * 5, Color.red, 0.25f);
            Debug.DrawLine(bodyTransform.position, bodyTransform.position + up * 5, Color.magenta, 0.25f);

            //bodyTransform.localEulerAngles = up * -1;
            lastBodyUp = up;

            // old version
            /* 
            Vector3 v1 = legTargets[0].position - legTargets[7].position;
            Vector3 v2 = legTargets[4].position - legTargets[3].position;
            Vector3 normal = Vector3.Cross(v1, v2).normalized;
            Vector3 up = Vector3.Lerp(lastBodyUp, normal, 1f / (float)(smoothness + 1));
            //up.y += 180;
            bodyTransform.forward = -up;
            lastBodyUp = up;
            Debug.DrawLine(bodyTransform.position, bodyTransform.position + bodyTransform.forward * 5, Color.blue, 0.25f);
            */
        }
    }

    static Vector3[] MatchToSurfaceFromAbove(Vector3 point, float halfRange, Vector3 direction)
    {
        Vector3[] raycastResults = new Vector3[2];
        RaycastHit hit;
        Ray ray = new Ray(point + halfRange * direction, -direction);

        if (Physics.Raycast(ray, out hit, 2f * halfRange))
        {
            raycastResults[0] = hit.point;
            raycastResults[1] = hit.normal;
        }
        else
        {
            raycastResults[0] = point;
        }

        if (raycastResults[0] != null)
        {
            Debug.DrawLine(point, raycastResults[0], Color.red, 1f);
        }

        return raycastResults;
    }

    private void OnDrawGizmosSelected()
    {
        for (int i = 0; i < nbLegs; ++i)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(legTargets[i].position, 0.3f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.TransformPoint(defaultLegPositions[i]), stepSize);
        }
    }
}
