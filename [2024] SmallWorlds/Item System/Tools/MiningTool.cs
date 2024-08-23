using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(LineRenderer))]
public class MiningTool : Usable
{
    [Header("Main Settings")]
    [SerializeField] private float raycastDistance = 3f;
    [SerializeField] private float miningInterval = 1f;
    [SerializeField] private float miningDuration = 2f;
    [SerializeField] private float gainModifier = 1f;
    [SerializeField] private float damage = 20f;

    [Header("Visual Settings")]
    [SerializeField] private Vector3 swingRotationTarget = Vector3.zero;

    // Runtime vars
    private float timeSinceLastMining = 0f;

    // References to be assigned during runtime
    private LineRenderer lineRenderer;
    private Camera playerCamera;
    private Inventory inventory;

    void Awake()
    {
        // Initialize the LineRenderer component
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // Set up LineRenderer default properties
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }


    protected override void ConfigureExtendedReferences()
    {
        playerCamera = Utility.GetComponentFromAnyParent<Camera>(this.gameObject);
        inventory = player.GetComponent<Inventory>();
    }

    void Mine()
    {
        // Show the mining line
        lineRenderer.enabled = true;
        timeSinceLastMining = 0f;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, playerCamera.transform.position + playerCamera.transform.forward * raycastDistance);

        // Perform raycast
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, raycastDistance))
        {
            // Check if the hit object has the "Breakable" tag
            if (hit.collider.CompareTag("Breakable"))
            {
                // Perform mining or destruction logic here (you may want to call a separate method)
                Debug.Log("Hit a breakable object!");
                HandleImpactEffects(hit);

                ResourceSource resource = Utility.GetComponentFromAnyParent<ResourceSource>(hit.collider.gameObject);
                if (resource != null)
                {
                    resource.OnHit(inventory, gainModifier, damage);
                }

                StructureData structure = Utility.GetComponentFromAnyParent<StructureData>(hit.collider.gameObject);
                if (structure != null)
                {
                    structure.ApplyDamage(damage);
                }
            }

            IDamageable damageableObject = hit.collider.gameObject.GetComponent<IDamageable>();
            if (damageableObject != null)
            {
                damageableObject.ApplyDamage(damage);
            }
        }
    }

    private void HandleImpactEffects(RaycastHit hit)
    {
        VFXManager.Instance.PlayEffect(VFXType.Impact, hit.point, Quaternion.Euler(hit.normal), 1.5f);
    }

    void ContinueMining()
    {
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position + playerCamera.transform.forward * raycastDistance);

        // Check if enough time has passed since the last mining
        timeSinceLastMining += Time.deltaTime;
        if (timeSinceLastMining >= miningInterval)
        {
            // Perform mining again
            Mine();
        }
    }

    void StopMining()
    {
        // Hide the mining line
        lineRenderer.enabled = false;
    }

    public override void HandleLeftClick()
    {
        //StartMining();
    }

    public override void HandleLeftClickHold()
    {
        ContinueMining();
    }

    public override void HandleLeftClickUp()
    {
        StopMining();
    }

}
