using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RealPlayerController : MonoBehaviour
{
    public float mouseSensitivity = 150f;
    private float xRotation = 0;
    public float movementSpeed;

    public GameObject camera1;
    public GameObject camera2;

    GameObject currentGamePanel;
    GameObject rotationPoint;
    public GameObject inGameObject;

    public GameObject LegalPosChecker;
    public LegalPositionChecker LegalPosCheck;


    public bool legalPosition;

    // Start is called before the first frame update
    void Start()
    {
        LegalPosCheck = GetComponent<LegalPositionChecker>();
    }

    // Update is called once per frame
    void Update()
    {
        CameraController();
        legalPosition = LegalPosCheck.legalPosition;


    }

    private void CameraController()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");


        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        camera1.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        camera2.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);


        this.transform.Rotate(Vector3.up * mouseX);

        //inGameObject.transform.Rotate(Vector3.up * mouseX);

        Vector3 move = transform.right * (x * (movementSpeed)) + transform.forward * (z * (movementSpeed));
        transform.Translate(move * movementSpeed * Time.deltaTime, Space.World);

        if (legalPosition)
        {
            camera2.SetActive(true);
            camera1.SetActive(false);

        }
        else if (!legalPosition)
        {
            camera1.SetActive(true);
            camera2.SetActive(false);

        }
    }
}
