using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody))]
public class GravityHandler : MonoBehaviour, IGravityEffectable
{
    [SerializeField] private bool Debug_View = false;
    [SerializeField] private LayerMask groundLayer; // Assign the ground layer in the Inspector
    [SerializeField] private GravitySource defaultSpawnSource = null;

    private Vector3 customGravity = Vector3.zero;
    private Vector3 gravityDir = Vector3.zero;
    private Rigidbody rb;

    [SerializeField] private bool conformRotationToGravity = true;
    [SerializeField] private float objectRotationSpeedMax = 500f;
    [SerializeField] private float maxRotRampUpTime = 2;
    private float currentRotRampUpTime = 0;
    private float rotSpeedScale = 1;
    private float currentRot = 0;

    [SerializeField] private float maxParentingTime = 1;
    [SerializeField] private float currentParentingTime = 0;

    private bool isScalingRotationSpeed = false;
    private bool isGrounded = false;
    private bool isMoving = false;
    [SerializeField] private bool isParented = false;

    [SerializeField] private List<GravitySource> regionalGravitySources = new List<GravitySource>();
    [SerializeField] private List<GravitySource> activeGravitySources = new List<GravitySource>();

    private GravitySource globalGravity;
    public Vector3 GravityDir { get { return gravityDir; } }
    public Vector3 CustomGravity { get { return customGravity; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        if (GlobalGravity.instance != null)
        {
            globalGravity = GlobalGravity.instance;
        }
        else
        {
            //Debug.LogError("No global gravity volume found in scene");
        }

        if (defaultSpawnSource != null) 
        {
            AddGravitySource(defaultSpawnSource);
        }
    }

    void FixedUpdate()
    {
        HandleParenting();
        if (activeGravitySources == null || activeGravitySources.Count == 0)
        {
            //ApplyGravity(globalGravity.GetGravity(transform));
            return;
        }

        List<Vector3> gravityDirList = new List<Vector3>();
        List<Quaternion> gravityRotationList = new List<Quaternion>();
        foreach (var gravitySource in activeGravitySources)
        {
            gravityDirList.Add(gravitySource.GetGravity(transform));
            gravityRotationList.Add(gravitySource.GetTargetRotation(transform));
        }
        foreach(var gravityDir in gravityDirList)
        {
            //Debug.DrawLine(this.transform.position, this.transform.position + gravityDir, Color.yellow);
        }

        // Calculate the average gravity and rotation of the active gravity sources
        Vector3 averageGravity = Utility.AverageVector3(gravityDirList);
        Quaternion averageRotation = Utility.AverageRotation(gravityRotationList);

        // Apply the average gravity and rotation
        ApplyGravity(averageGravity);
        SetRotation(averageRotation);

        isGrounded = CheckGrounded();
        isMoving = CheckIsMoving(rb);   
    }

    private void HandleParenting()
    {
     
        if (currentParentingTime <= maxParentingTime)
        {
            currentParentingTime += Time.fixedDeltaTime;
        }
        else if (!isParented)
        {
            currentParentingTime = 0;
            transform.SetParent(activeGravitySources[0].gameObject.transform);

            if (Debug_View)
            {
                Debug.DrawLine(this.transform.position, transform.parent.transform.position, Color.cyan, 5);
            }

            isParented = true;
        }

        if (activeGravitySources.Count == 0)
        {
            currentParentingTime = 0;
            transform.SetParent(null);
            isParented = false;
        }

        if (this.transform.parent != null && Debug_View)
        {
            Debug.DrawLine(this.transform.position, transform.parent.transform.position, Color.green);
        }
        
    } 

    public void AddGravitySource(GravitySource gravitySource)
    {
        // in some nieche cases gravity sources are being added twice, this extra check ensures duplicate sources are handled. 
        // this will be removed when the source of the problem is solved.
        if (activeGravitySources.Contains(gravitySource))
        {
            return;
        }

        regionalGravitySources.Add(gravitySource);
        activeGravitySources = FindHighestPriorityGravitySources();
    }

    public void RemoveGravitySource(GravitySource gravitySource)
    {
        regionalGravitySources.Remove(gravitySource);
        activeGravitySources.Remove(gravitySource);
        activeGravitySources = FindHighestPriorityGravitySources();
    }

    public void ApplyGravity(Vector3 gravityForce)
    {
        customGravity = gravityForce;
        gravityDir = customGravity.normalized;

        // Apply gravity
        rb.AddForce(customGravity, ForceMode.Acceleration);
    }
    public void SetRotation(Quaternion newRotation)
    {
        rotSpeedScale = 1;
        if (isScalingRotationSpeed)
        {
            currentRotRampUpTime += Time.fixedDeltaTime;
            rotSpeedScale = currentRotRampUpTime / maxRotRampUpTime;
            if (currentRotRampUpTime >= maxRotRampUpTime)
            {
                currentRotRampUpTime = 0;
                rotSpeedScale = 1;

                isScalingRotationSpeed = false;
            }
        }

        if (!conformRotationToGravity)
        {
            return;
        }

        currentRot = rotSpeedScale * objectRotationSpeedMax;
        Quaternion targetRotation = Quaternion.Slerp(this.transform.rotation, newRotation, currentRot * Time.fixedDeltaTime);
        transform.rotation = targetRotation;
    }

    private List<GravitySource> FindHighestPriorityGravitySources()
    {
        int highestPriority = int.MinValue;
        List<GravitySource> highestPrioritySources = new List<GravitySource>();

        foreach (GravitySource gravitySource in regionalGravitySources)
        {
            // Check the priority of the current gravity source
            int currentPriority = gravitySource.GravitySourcePriority;

            // If the current priority is higher, update the highest priority and reset the list
            if (currentPriority > highestPriority)
            {
                highestPriority = currentPriority;
                highestPrioritySources.Clear();
                highestPrioritySources.Add(gravitySource);
            }
            // If the current priority is equal to the highest priority, add it to the list
            else if (currentPriority == highestPriority)
            {
                highestPrioritySources.Add(gravitySource);
            }
        }

        OnSourceChange();
        return highestPrioritySources;
    }

    private void OnSourceChange()
    {
        // trigger rotation smoothing
        currentRotRampUpTime = 0;
        isScalingRotationSpeed = true;
    }

    private bool CheckGrounded()
    {
        float groundCheckDistance = 5;
        // Perform a raycast downward to check if the object is close to the ground
        RaycastHit hit;
        if (Physics.Raycast(transform.position, gravityDir, out hit, groundCheckDistance, groundLayer))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool CheckIsMoving(Rigidbody rigidbody)
    {
        float velocityThreshold = 0.01f;

        // Check if the rigidbody's velocity magnitude is greater than the threshold
        return rigidbody.velocity.magnitude > velocityThreshold;
    }
}
