using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MoveBetweenPoints : MonoBehaviour
{
    public float movementSpeed = 1;
    public float movementDelay = 3;
    public Vector3 startingPos;
    public Transform[] targetPos;
    
    public GameObject realPlayer;
    public GameObject panelLock;
    public GameObject realPanelReferance;

    Transform pos2;
    public bool[] lockState1 = { true, true, true, true };
    public bool[] lockState2 = { true, true, true, true };
    public bool[] lockState3 = { true, true, true, true };
    public bool[] lockState4 = { true, true, true, true };
    public bool[] lockState5 = { true, true, true, true };

    public bool[][] currentLockState;
    public float lockStateRotation;

    Vector3 panelLockPos1;

    float currentTimer = 0;
    public int currentPosTarget = 0;

    public int currentPos;

    public bool isWaiting = true;
    public bool incrementUp = true;

    public bool isActive;
    public bool useRotation;
    float interp = 0;
    public float rotSpeed = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        startingPos = transform.position;
        panelLockPos1 = panelLock.transform.position;
        panelLock.transform.position = new Vector3(realPanelReferance.transform.position.x - 1.5f, panelLock.transform.position.y, realPanelReferance.transform.position.z);

        currentLockState = new bool[][] { lockState1, lockState2, lockState3, lockState4, lockState5 };


       

    }

    // Update is called once per frame
    void Update()
    {
        //currentPosTarget = Mathf.Clamp(currentPosTarget, 0, targetPos.Length);

        if (isActive)
        {
            if (currentPosTarget >= targetPos.Length - 1)
            {
                incrementUp = false;
            }
            if (currentPosTarget == 0)
            {
                incrementUp = true;
            }
            if (currentTimer <= movementDelay && isWaiting)
            {
                currentTimer += Time.deltaTime;
            }
            if (currentTimer >= movementDelay)
            {
                currentTimer = 0;
                isWaiting = false;
            }

            CustomLockState(panelLock, currentLockState[LockStateCalculator(currentPos, isWaiting)]);

            if (Vector3.Distance(transform.position, targetPos[currentPosTarget].position) >= 0.01f && !isWaiting)
            {
                // Move our position a step closer to the target.
                var step = movementSpeed * Time.deltaTime; // calculate distance to move
                transform.position = Vector3.MoveTowards(transform.position, targetPos[currentPosTarget].position, step);


            }

            if (useRotation)
            {
                if (interp < 1) interp += Time.deltaTime * rotSpeed;
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetPos[currentPosTarget].rotation, interp);
                }
            }
           

            if (Vector3.Distance(transform.position, targetPos[currentPosTarget].position) < 0.01f)
            {
                if (!isWaiting)
                {
                    currentPos = currentPosTarget;
                }

                isWaiting = true;

                if (targetPos[currentPosTarget].tag == "NoMoveDelay")
                {
                    currentTimer = 2.5f;
                }

                if (incrementUp)
                {
                    currentPosTarget += 1;
                }

                if (!incrementUp)
                {
                    currentPosTarget -= 1;
                }
                //panelLock.transform.position = panelLockPos1;
            }

        }
    }

    static void CustomLockState(GameObject panelLock, bool[] activeWalls)
    {
        Collider[] wallColliders = panelLock.GetComponentsInChildren<Collider>();
        NavMeshObstacle[] obstacles = panelLock.GetComponentsInChildren<NavMeshObstacle>();
        Transform parent = panelLock.transform;

        Transform[] childTransform = new Transform[panelLock.transform.childCount];

        for (int i = 0; i < childTransform.Length; i++)
        {
            if (activeWalls[i])
            {
                wallColliders[i].enabled = true;
                obstacles[i].enabled = true;
            }
            else if (!activeWalls[i])
            {
                wallColliders[i].enabled = false;
                obstacles[i].enabled = false;
            }
        }
        
    }

    static int LockStateCalculator(int _currentLockState, bool _isWaiting)
    {
        int lockState;

        if(_isWaiting)
        {
            lockState = _currentLockState;    
        }

        else
        {
            lockState = 4;
        }

        return lockState;
    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag == "DisableMovement")
        {
            isActive = false;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "DisableMovement")
        {
            isActive = true;
        }
    }

}
