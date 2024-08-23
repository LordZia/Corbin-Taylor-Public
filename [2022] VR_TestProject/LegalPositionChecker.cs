using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegalPositionChecker : MonoBehaviour
{
    public GameObject legalPosChecker;
    public bool legalPosition = true;
    public Vector3 positionReal;
    CharacterController positionCheckerController;
    public float moveSpeed = -20;

    // Start is called before the first frame update
    void Start()
    {
        positionCheckerController = legalPosChecker.GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        
        if (legalPosition)
        {
            var offset = legalPosChecker.transform.position - this.transform.position;
            if (offset.magnitude > .05f)
            {
                offset = offset.normalized * moveSpeed;

                positionCheckerController.Move(offset * Time.deltaTime);

            }
        }
    }

    void OnTriggerStay(Collider otherObject)
    {

        if (otherObject.gameObject == legalPosChecker)
        {
            legalPosition = true;
        }
    }

    void OnTriggerExit(Collider otherObject)
    {

        if (otherObject.gameObject == legalPosChecker)
        {
            legalPosition = false;
        }
    }
}
