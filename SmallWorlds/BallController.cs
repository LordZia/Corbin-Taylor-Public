using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    public float lifetime = 10.0f; // Delay in seconds before destroying the ball

    void Awake()
    {
        // Start the coroutine to destroy the ball after a delay
        this.GetComponent<Collider>().enabled = false;
        StartCoroutine(DestroyAfterDelay());
        Invoke("EnableCollision", 0.05f);
    }

    private void EnableCollision()
    {
        this.GetComponent<Collider>().enabled = true;
    }

    IEnumerator DestroyAfterDelay()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(lifetime);

        // Destroy the ball GameObject
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the collision involves the throwing player
        if (collision.gameObject.CompareTag("Player"))
        {
            // Get the collider of the player
            Collider playerCollider = collision.collider;

            // Check if the collider belongs to the throwing player

            // Get the collider of the ball
            Collider ballCollider = GetComponent<Collider>();

            // Ignore collision between the ball and the throwing player
            Physics.IgnoreCollision(playerCollider, ballCollider);
            Debug.Log("Collision with throwing player ignored");
            return;

            /* if multiplayer is ever added have this check:
              
            if (playerCollider.gameObject.GetComponent<PlayerController>().PlayerID == throwingPlayerID)
            {
                // Get the collider of the ball
                Collider ballCollider = GetComponent<Collider>();

                // Ignore collision between the ball and the throwing player
                Physics.IgnoreCollision(playerCollider, ballCollider);
                Debug.Log("Collision with throwing player ignored");
                return;
            }
            */
        }
    }
}