using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialPanel : MonoBehaviour
{
    public string[] panelTypes;
    //type 1 = curved panel , type 2, scale game character, 3 = use if parenting panel to another object, 4 = use if assigning a "sister" panel.
    public int panelType;
    public Transform panelTransform;
    public GameObject parentObject;
    public GameObject alternatePanel;
    public bool orientAlongY = false;
    public bool invertSide = false;
    public bool useScale = false;
    public bool hasParent = false;
    public Vector3 overridePosition;
   
}
