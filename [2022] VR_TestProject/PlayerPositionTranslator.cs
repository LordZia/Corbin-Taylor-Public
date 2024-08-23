using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation.Examples;

public class PlayerPositionTranslator : MonoBehaviour
{
    public GameObject pathStationObj;
    public PathStations pathStations;
    public int currentStation;

    public Transform transformReal;
    public Vector3 positionReal;
    public GameObject rotationPointSub;
    public GameObject rotationPointMain;
    public Vector2 mapSize = new Vector2(5, 5);

    public Vector3 relativePosition;
    public float playerHeight;
    public Vector3 overrideObjectPos;


    public GameObject inGameTilesParent;
    public GameObject inGameObject;
    public Transform[] inGameTiles;

    public GameObject currentRealPanel;
    public GameObject currentGamePanel;
    public GameObject legalPosChecker;

    public Collider legalPosCheckCol;
    public Vector3 playerPositionGame;
    public Vector2 currentObjectPanel;

    public Transform[] gameTransform;
    public Transform transformGame;
    public Transform[] realTransform;

    public GameObject realPanelsParent;
    public Transform[] realGridPanels;
    public GameObject[,] realGridPanel;
   

    public GameObject[,] gameGridPanel;

    public GameObject lastActivePanel;
    public GameObject lastFrameGridPanel;
    public GameObject currentGridPanel;
    
    public bool validLocation;
    public bool legalPosition;

    public SpecialPanel specialPanel;
    public bool useCurveLogic = false;
    bool scaleGameObject = false;

    Vector3 playerScale;
    
    
    public float curveAmount;
    bool orientAlongY;
    bool invertSide;
    public bool useStationLogic;

    public int[][] testArray;
    public int[] intArray;

    public bool useAlternateZone = false;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        AssignPanelArrays();

        playerScale = inGameObject.transform.localScale;

        //assign parent objects to game panels of type 3
        foreach (Transform child in inGameTilesParent.transform)
        {
            if (child.gameObject.hasComponent<SpecialPanel>())
            {
                specialPanel = child.GetComponent<SpecialPanel>();
                if (specialPanel.panelType == 3)
                {
                    if (!specialPanel.hasParent)
                    {
                        child.transform.SetParent(specialPanel.parentObject.transform, true);
                        specialPanel.hasParent = true;
                    }
                }
            }
        }

        if (useStationLogic)
        {
            pathStations = pathStationObj.GetComponent<PathStations>();
        }

        lastFrameGridPanel = gameGridPanel[0,0];
        lastActivePanel = gameGridPanel[0, 0];
    }

    private void AssignPanelArrays()
    {
        inGameTiles = new Transform[(int)mapSize.x * (int)mapSize.y];
        gameGridPanel = new GameObject[(int)mapSize.x, (int)mapSize.y];
        realGridPanels = new Transform[(int)mapSize.x * (int)mapSize.y];
        realGridPanel = new GameObject[(int)mapSize.x, (int)mapSize.y];
        int index = 0;

        for (int i = 0; i < inGameTiles.Length; i++)
        {
            inGameTiles[i] = inGameTilesParent.transform.GetChild(i);
            realGridPanels[i] = realPanelsParent.transform.GetChild(i);
        }


        for (int height = 0; height < mapSize.x; height++)
        {

            for (int row = 0; row < mapSize.y; row++)
            {
                
                gameGridPanel[height, row] = inGameTiles[index].gameObject;

                realGridPanel[height, row] = realGridPanels[index].gameObject;
                index += 1;

            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        transformGame = inGameObject.transform;
        positionReal = this.transform.position;

        FindRelativePanelTransform();
        TrackRealObject();

        currentGridPanel = currentGamePanel;

        if (useStationLogic)
        {
            if (lastActivePanel.hasComponent<SpecialPanel>())
            {
                SpecialPanel lastSpecialPanel = lastActivePanel.GetComponent<SpecialPanel>();

                if (currentGridPanel.hasComponent<SpecialPanel>())
                {
                    SpecialPanel currentSpecialPanel = currentGamePanel.GetComponent<SpecialPanel>();

                    if (lastSpecialPanel.panelType == 3 && currentSpecialPanel.panelType == 4)
                    {
                        if (pathStations.useAlternateZone == true)
                        {
                            useAlternateZone = true;
                        }

                    }

                    if (lastSpecialPanel.panelType == 4 && currentSpecialPanel.panelType == 3)
                    {
                        if (pathStations.useAlternateZone == false)
                        {
                            useAlternateZone = false;
                        }
                    }

                }

            }

        }
        MoveGameObject();

        if (lastFrameGridPanel.name != currentGamePanel.name)
        {
            lastActivePanel = lastFrameGridPanel;
            
        }

        lastFrameGridPanel = currentGamePanel;

        
        
        Color color = new Color(1, 1, 1);
        Debug.DrawLine(inGameObject.transform.position, lastActivePanel.transform.position, color);

        
    }

    public void TrackRealObject()
    {
        float x = positionReal.z / 3;
        float z = positionReal.x / 3;

        Vector3 playerGridPositionReal = new Vector3(x, 0, z);
        FindCurrentPanel(x, z);
        FindRelativePanelTransform();
    }

    private void FindCurrentPanel(float x, float z)
    {
        if (x >= 0 && x <= mapSize.x && z >= 0 && z <= mapSize.y)
        {
            int intX = Mathf.FloorToInt(x);
            int intZ = Mathf.FloorToInt(z);

            currentObjectPanel.y = intX;
            currentObjectPanel.x = intZ;
        }
    }

    private void FindRelativePanelTransform()
    {
        int currentPlayerPanelX = (int)currentObjectPanel.x;
        int currentPlayerPanelY = (int)currentObjectPanel.y;
        currentRealPanel = realGridPanel[currentPlayerPanelY , currentPlayerPanelX];
        currentGamePanel = gameGridPanel[currentPlayerPanelY , currentPlayerPanelX];

        if (currentGamePanel.hasComponent<SpecialPanel>())
        {
            specialPanel = currentGamePanel.GetComponent<SpecialPanel>();
            overrideObjectPos = specialPanel.overridePosition;
            int type = specialPanel.panelType;

            if (type == 1)
            {
                useCurveLogic = true;
            }
            else
            {
                useCurveLogic = false;
            }

            if (type == 2)
            {
                scaleGameObject = true;
            }

            else
            {
                scaleGameObject = false;
            }

            if (specialPanel.orientAlongY)
            {
                orientAlongY = true;
            }
            else
            {
                orientAlongY = false;
            }

            if (specialPanel.invertSide)
            {
                invertSide = true;
            }
            else
            {
                invertSide = false;
            }
        }
        else
        {
            useCurveLogic = false;
            invertSide = false;
            scaleGameObject = false;
            orientAlongY = false;
        }
            
    }

    private void MoveGameObject()
    {
        Transform targetTransform = currentGamePanel.transform;

        if (useAlternateZone)
        {
            targetTransform = currentGamePanel.GetComponent<SpecialPanel>().alternatePanel.transform;
        }

        relativePosition = this.transform.position - currentRealPanel.transform.position;
        Vector3 relativeRotPos = RotateAroundPoint(this.transform, currentRealPanel.transform.position, targetTransform.eulerAngles.z);
        Vector3 rotationValue = targetTransform.rotation.eulerAngles;

        rotationPointMain.transform.position = targetTransform.position;
        rotationPointMain.transform.rotation = targetTransform.rotation;
        rotationPointSub.transform.position = targetTransform.position;

        Vector2 coordinates = new Vector2(relativePosition.x, relativePosition.z);
        Vector2 scaleFactor = new Vector2(targetTransform.localScale.x / 1.5f, targetTransform.localScale.z / 1.5f);
        Vector2 scaledPos = new Vector2(relativePosition.x * scaleFactor.x, relativePosition.z * scaleFactor.y);

        if (scaleGameObject)
        {
            float scale = Mathf.Min(scaleFactor.x, scaleFactor.y);
            inGameObject.transform.localScale = new Vector3(scale, scale * 2, scale);
            playerHeight = scale * 2 + overrideObjectPos.y;
        }
        else
        {
            inGameObject.transform.localScale = playerScale;
            playerHeight = 0;
        }

        if (useCurveLogic)
        {
            
            //nasty hard coded garbage but it works. fix later
            float overRideY = 0;
            float overRideRotZ = 0;
            float posY = -3;
            float invert = 1;
            float overrideRotationAngle = 0;

            float fixDualRotation = 0;
            if (orientAlongY == true)
            {
                coordinates = InvertCoordinates(coordinates);
                overRideY = -90;
            }
            if (invertSide)
            {
                posY = -5.1f;
                overRideRotZ = 180;
                invert *= -1;
                overrideRotationAngle = 90;
            }
            if (orientAlongY && invertSide)
            {
                fixDualRotation = 180;
            }

            curveAmount = (((coordinates.y / 3) * -90f) - 45 );
            Vector3 curvedRelativePosition = new Vector3(coordinates.x, relativePosition.y, 0);
            rotationPointSub.transform.localEulerAngles = new Vector3( (curveAmount * invert) - overrideRotationAngle, 0, 0 );

            inGameObject.transform.localEulerAngles = new Vector3
                (
                this.transform.eulerAngles.x + overRideRotZ,
                (transform.eulerAngles.y * invert) + overRideY + fixDualRotation,
                transform.eulerAngles.z
                );

            inGameObject.transform.localPosition = new Vector3
                (
                curvedRelativePosition.x,
                curvedRelativePosition.y + posY,
                curvedRelativePosition.z
                );
        }
        if (!useCurveLogic)
        {
            rotationPointSub.transform.localEulerAngles = new Vector3(0, 0, 0);
                
            inGameObject.transform.localPosition = new Vector3(scaledPos.x, relativePosition.y + playerHeight, scaledPos.y);
            inGameObject.transform.localRotation = this.transform.rotation;

            rotationPointMain.transform.rotation = targetTransform.rotation;
        }
    }
    
    public static Vector3 RotateAroundPoint(Transform target, Vector3 pivot, float _angle)
    {
        Vector3 relativePos = target.position - pivot;
        float xRaw = relativePos.x;
        float zRaw = relativePos.z;
        float angle = _angle * Mathf.PI;

        float xRot = xRaw * (Mathf.Cos(angle)) - (zRaw * (Mathf.Sin(angle)));
        float zRot = xRaw * (Mathf.Sin(angle)) + (zRaw * (Mathf.Cos(angle)));

        Vector3 relativeRotatedPos = new Vector3(xRot, relativePos.y, zRot);

        return relativeRotatedPos;
    }

    public static Vector2 InvertCoordinates(Vector2 _coordinates)
    {
        Vector2 InvertedCoordinates = new Vector2(_coordinates.y * -1, _coordinates.x);
        return InvertedCoordinates;
    }

}
