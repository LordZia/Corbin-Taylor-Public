using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GrapplingHook : Usable
{

    public float maxGrappleDistance = 50f;
    public float grappleForce = 5f;
    public LineRenderer lineRenderer;
    public LayerMask grappleLayerMask;

    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private Camera playerCamera;
    private GameObject grapplePullPoint;
    private bool isGrappling = false; // pull user to grapple point
    private bool isPulling = false; // pull grapple point object towards user

    private PullableObject pullableObject;

    void Awake()
    {
        // Ensure the LineRenderer component is set up in the Inspector
        if (lineRenderer == null)
        {
            this.gameObject.AddComponent<LineRenderer>();
            lineRenderer.enabled = false;
        }

        grapplePullPoint = new GameObject("GrapplePullPoint");
    }

    private void OnDestroy()
    {
        Destroy(grapplePullPoint);
    }

    protected override void ConfigureExtendedReferences()
    {
        playerCamera = Utility.GetComponentFromAnyParent<Camera>(this.gameObject);
        playerRigidbody = player.GetComponent<Rigidbody>();
    }


    void Update()
    {
        // If grappling, update the line renderer and apply force
        if (isGrappling || isPulling)
        {
            UpdateGrapple();

            float distance = Vector3.Distance(this.transform.position, grapplePullPoint.transform.position);
            if (distance <= 2)
            {
                StopGrapple();
            }

            Utility.DebugDrawSphere(grapplePullPoint.transform.position, 1, Color.yellow);
        }
    }

    void StartGrapple()
    {
        Vector3 direction = playerCamera.transform.forward;
        Ray ray = new Ray(this.transform.position, direction);
        RaycastHit hit;

        lineRenderer.enabled = true;

        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position + (ray.direction * maxGrappleDistance));

        // Check for a hit within the max grapple distance
        if (Physics.Raycast(ray, out hit, maxGrappleDistance, grappleLayerMask))
        {
            Debug.Log(hit.collider.name);
            
            pullableObject = hit.collider.gameObject.GetComponent<PullableObject>();
            if (pullableObject != null) // hit a pullable object, trigger pull logic
            {
                pullableObject.OnPull();
                isPulling = true;
            }
            else // hit a solid grapplable object, enable grapple logic
            {
                isGrappling = true;
            }

            grapplePullPoint.gameObject.SetActive(true);
            grapplePullPoint.transform.position = hit.point;
            grapplePullPoint.transform.parent = hit.transform;

            // Set up the LineRenderer
            lineRenderer.SetPosition(1, hit.point);
        }

        if (!isGrappling)
        {
            Invoke("StopGrapple", 0.5f);
        }
    }

    void StopGrapple()
    {
        isGrappling = false;
        isPulling = false;
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, Vector3.zero);
        lineRenderer.enabled = false;
        grapplePullPoint.gameObject.SetActive(false);
    }

    void UpdateGrapple()
    {
        // Update the LineRenderer position
        lineRenderer.SetPosition(0, this.transform.position);
        lineRenderer.SetPosition(1, grapplePullPoint.transform.position);

        // Apply force towards the grapple point
        Vector3 grappleDirection = (grapplePullPoint.transform.position - transform.position).normalized;

        if (isGrappling)
        {
            playerRigidbody.AddForce(grappleDirection * grappleForce * Time.deltaTime, ForceMode.Force);
        }
        
        if (isPulling)
        {
            grappleDirection *= -1; // invert direction to apply force towards the user
            pullableObject.AddPullForce(grappleDirection * grappleForce * Time.deltaTime);
        }
    }

    public override void HandleLeftClick()
    {

    }


    public override void HandleRightClick()
    {
        StartGrapple();
    }

    public override void HandleRightClickUp()
    {
        StopGrapple();
    }
}
