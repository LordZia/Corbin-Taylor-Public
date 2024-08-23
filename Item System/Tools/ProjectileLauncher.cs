using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileLauncher : Usable
{
    [Space]
    [Header("Launcher Settings")]
    public GameObject projectilePrefab; // Prefab for the projectile
    public Transform originPoint; // Point where the projectile will be thrown from
    private Camera playerCamera;
    public float launchForce = 10f; // Force applied to the thrown projectile
    [Space]

    [Header("Rotation Settings")]
    [SerializeField]
    private Vector3 startingAngularMomentum = Vector3.zero;

    [SerializeField]
    [Tooltip("randomness is used as a plus and minus value from the starting angular momentum values to use as a min and max range of a random value")]
    private Vector3 startingAngularMomentumRandomness = Vector3.zero;

    protected override void ConfigureExtendedReferences()
    {
        playerCamera = Utility.GetComponentFromAnyParent<Camera>(this.gameObject);
    }

    // Method to throw the projectile
    void LaunchProjectile()
    {
        // Create a new projectile instance
        GameObject projectile = Instantiate(projectilePrefab, originPoint.position, Quaternion.identity);

        projectile.SetActive(true);

        // Get the rigidbody component of the projectile
        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        // Check if the rigidbody component exists
        if (rb != null)
        {
            // Apply force to the projectile in the forward direction of the throw point
            rb.AddForce(playerCamera.transform.forward * launchForce, ForceMode.Impulse);

            if (startingAngularMomentum != Vector3.zero || startingAngularMomentumRandomness != Vector3.zero)
            {
                Vector3 minRandomRange = startingAngularMomentum - startingAngularMomentumRandomness;
                Vector3 maxRandomRange = startingAngularMomentum + startingAngularMomentumRandomness;

                Vector3 angularMomentum = Vector3.Lerp(minRandomRange, maxRandomRange, Random.Range(0, 1));

                rb.AddTorque(angularMomentum, ForceMode.Impulse);
            }

        }
        else
        {
            Debug.LogWarning("Rigidbody component not found on the projectile prefab.");
        }
    }

    public override void HandleRightClick()
    {
        LaunchProjectile();
    }

}

