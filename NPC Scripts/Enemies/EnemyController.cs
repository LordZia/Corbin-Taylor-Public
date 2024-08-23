using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour , IStateChangeNotifier<EnemyState>
{
    [SerializeField] private Trigger_OnPlayerEnter playerTrigger;

    [SerializeField]
    private EnemyState currentState = EnemyState.Patrol;

    [SerializeField]
    [Range(0, 100)]
    [Tooltip("Percentage of remaining health that will trigger the enemy to flee")]
    private int fleeHealthThreshold = 0;

    [SerializeField]
    private List<HealthEventTrigger> healthEventTriggers = new List<HealthEventTrigger>();

    [SerializeField]
    private Transform fleeLocation;

    [SerializeField]
    private float lookAngle = 20f;
    [SerializeField]
    private float lookRotationSpeed = 5f; // Speed at which the object rotates

    [SerializeField]
    private float chaseSpeed = 5f;
    [SerializeField]
    private float fleeSpeed = 7f;
    [SerializeField]
    private float patrolSpeed = 2f;
    [SerializeField]
    private float patrolChangeInterval = 5f; // Time in seconds to change direction

    private Enemy enemyStats;
    private bool isActive = true;

    private Vector3 patrolDirection;
    private float patrolTimer;

    private Rigidbody rb;

    [SerializeField]
    private List<Transform> playersInRange = new List<Transform>();
    private Transform currentChaseTarget;


    #region Events

    public event Action<EnemyState> OnStateChange;
    public event Action OnAttack;
    public event Action OnJump;

    #endregion

    public EnemyState GetCurrentState()
    {
        return this.currentState;
    }
    void Awake()
    {
        enemyStats = this.GetComponent<Enemy>();
        if (enemyStats != null)
        {
            enemyStats.OnDamage += OnDamageReceived;
        }

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component missing from the GameObject.");
        }

        if (playerTrigger != null)
        {
            playerTrigger.OnPlayerEnter += OnPlayerEnterRange;
            playerTrigger.OnPlayerExit += OnPlayerExitRange;

        }

        // Initialize patrol direction
        OnStateChange?.Invoke(currentState);
        ChangePatrolDirection();
    }

    void OnDestroy()
    {
        if (enemyStats != null)
        {
            enemyStats.OnDamage -= OnDamageReceived;
        }


        if (playerTrigger != null)
        {
            playerTrigger.OnPlayerEnter -= OnPlayerEnterRange;
            playerTrigger.OnPlayerExit -= OnPlayerExitRange;
        }
    }

    void Update()
    {
        if (!isActive)
            return;

        if (playersInRange.Count > 0)
        {
            foreach (var player in playersInRange)
            {
                bool isLookingAtPlayer = IsTargetInForwardDirection(player.position, lookAngle);
                if (isLookingAtPlayer)
                {
                    currentChaseTarget = player;
                }
            }
        }

        switch (currentState)
        {
            case EnemyState.Stay:
                HandleStay();
                break;
            case EnemyState.Patrol:
                HandlePatrol();
                break;
            case EnemyState.Chase:
                HandleChase();
                break;
            case EnemyState.Flee:
                HandleFlee();
                break;
            default:
                break;
        }
    }

    private void OnPlayerEnterRange(PlayerStats player)
    {
        if (!playersInRange.Contains(player.transform))
        {
            playersInRange.Add(player.transform);
        }
    }
    private void OnPlayerExitRange(PlayerStats player)
    {
        if (playersInRange.Contains(player.transform))
        {
            playersInRange.Remove(player.transform);
        }
    }

    private void HandleStay()
    {
        // Stay idle, can add idle animations or behaviors here
    }

    private void HandlePatrol()
    {
        patrolTimer -= Time.deltaTime;
        if (patrolTimer <= 0f)
        {
            ChangePatrolDirection();
            patrolTimer = patrolChangeInterval;
        }

        MoveTowards(transform.position + patrolDirection, patrolSpeed);
    }

    private void ChangePatrolDirection()
    {
        // Generate a random angle in radians
        float angle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);

        // Get a vector perpendicular to transform.up
        Vector3 perpendicularDirection = Vector3.Cross(transform.up, Vector3.forward).normalized;

        // If the perpendicular direction is zero, use another reference vector
        if (perpendicularDirection == Vector3.zero)
        {
            perpendicularDirection = Vector3.Cross(transform.up, Vector3.right).normalized;
        }

        // Rotate the perpendicular direction around transform.up by the random angle
        Quaternion rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, transform.up);
        patrolDirection = rotation * perpendicularDirection;
    }

    private void HandleChase()
    {
        if (currentChaseTarget == null)
        {
            if (playersInRange.Count > 0)
                currentChaseTarget = Utility.GetClosestTransform(this.transform.position, playersInRange);

            Debug.Log(currentChaseTarget.gameObject.name + " chase target");

            if (currentChaseTarget == null)
                return;
        }

        MoveTowards(currentChaseTarget.position, chaseSpeed);
    }

    private void HandleFlee()
    {
        if (currentChaseTarget == null) return;
        Vector3 fleeDirection = this.transform.position - currentChaseTarget.position;

        MoveTowards(fleeDirection, fleeSpeed);
    }

    private void MoveTowards(Vector3 targetPosition, float forceAmount)
    {
        if (rb == null) return;

        // Flatten the target position to sit on the same plane as transform.right and transform.forward
        //Vector3 flattenedTargetPosition = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        Vector3 direction = (targetPosition - transform.position).normalized;
        rb.AddForce(direction * forceAmount, ForceMode.Force);

        // Draw debug lines
        Debug.DrawLine(transform.position, transform.position + direction * 2, Color.red); // Movement direction
        Debug.DrawRay(transform.position, transform.right * 2, Color.green); // Current look direction

        // Calculate the target rotation based on the direction of movement
        if (direction != Vector3.zero)
        {
            // Calculate the angle between the current forward direction and the target direction
            float angle = Vector3.SignedAngle(transform.right, direction, Vector3.up);

            // Draw target look direction
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Vector3 targetLookDirection = targetRotation * Vector3.forward;
            Debug.DrawLine(transform.position, transform.position + targetLookDirection * 2, Color.blue);

            // Gradually rotate towards the target rotation using RotateAround
            float step = lookRotationSpeed * Time.deltaTime;
            if (Mathf.Abs(angle) > step)
            {
                float rotationStep = Mathf.Sign(angle) * step;
                transform.RotateAround(transform.position, Vector3.up, rotationStep);
            }
            else
            {
                transform.RotateAround(transform.position, Vector3.up, angle);
            }
        }
    }

    // Method to rotate around a given axis by a specified angle
    public void RotateAroundAxis(Vector3 axis, float angle)
    {
        transform.RotateAround(transform.position, axis, angle);
    }

    void OnDamageReceived(int remainingHealth)
    {
        Debug.Log("recieved damage call, remaining health = " + remainingHealth);
        EnemyState triggeredState = CheckHealthEventTriggers(remainingHealth);
        Debug.Log(triggeredState + " triggered state");
        if (triggeredState != EnemyState.None)
        {
            currentState = triggeredState;
            OnStateChange?.Invoke(currentState);
        }
            
    }

    private EnemyState CheckHealthEventTriggers(int remainingHealth)
    {
        var trigger = healthEventTriggers
            .Where(h => h.HealthThreshold >= remainingHealth)
            .OrderBy(h => h.HealthThreshold)
            .FirstOrDefault();


        return trigger.Equals(default(HealthEventTrigger)) ? currentState : trigger.State;
    }

    [System.Serializable]
    private struct HealthEventTrigger
    {
        [SerializeField]
        [Tooltip("Percentage of health required to apply state change")]
        [Range(0, 100)]
        public int HealthThreshold;
        public EnemyState State;
    }

    // Method to check if the target position is within the forward direction
    public bool IsTargetInForwardDirection(Vector3 targetPosition, float angleThreshold)
    {
        // Calculate the direction to the target
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;

        // Get the forward direction of the object
        Vector3 forwardDirection = transform.right;

        // Calculate the dot product between the forward direction and the direction to the target
        float dotProduct = Vector3.Dot(forwardDirection, directionToTarget);

        // Calculate the angle threshold in radians
        float cosThreshold = Mathf.Cos(angleThreshold * Mathf.Deg2Rad);

        DrawDebugLines(targetPosition, angleThreshold);

        // If the dot product is greater than the cosine of the angle threshold, the target is within the forward direction
        return dotProduct >= cosThreshold;
    }

    private void DrawDebugLines(Vector3 targetPosition, float angleThreshold)
    {
        // Get the forward direction of the object
        Vector3 forwardDirection = transform.right;

        // Calculate the direction to the target
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;

        // Calculate the edges of the look area
        float halfAngle = angleThreshold / 2;

        // Use the object's right vector to rotate around the forward direction
        Vector3 leftEdge = Quaternion.AngleAxis(-halfAngle, transform.up) * forwardDirection;
        Vector3 rightEdge = Quaternion.AngleAxis(halfAngle, transform.up) * forwardDirection;

        // Draw the forward direction line
        Debug.DrawLine(transform.position, transform.position + forwardDirection * 5, Color.blue);

        // Draw the direction to the target
        Debug.DrawLine(transform.position, targetPosition, Color.cyan);

        // Draw the edges of the look area
        Debug.DrawLine(transform.position, transform.position + leftEdge * 5, Color.red);
        Debug.DrawLine(transform.position, transform.position + rightEdge * 5, Color.red);
    }

}

public enum EnemyState
{
    None,
    Stay,
    Patrol,
    Chase,
    Flee
}

public interface IStateChangeNotifier<TState>
{
    event Action<TState> OnStateChange;
    TState GetCurrentState();
}