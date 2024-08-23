using UnityEngine;
using static PlayerStateMachine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(GravityHandler))]
[RequireComponent(typeof(PlayerStateMachine))]
public class RBCharacterController : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform characterBody;
    [SerializeField] private PlayerStateMachine playerState;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float playerRotationSpeed = 300f;

    [SerializeField] private float groundFriction = 1.0f;
    [SerializeField] private float maxForce = 1.0f;
    [SerializeField] private float jumpForce = 5f;

    [SerializeField] private Transform gravitySource; // Reference to the gravity source transform
    [SerializeField] private float baseGravityStrength = -9.8f;


    [SerializeField] private float sensitivity = 700f;

    private GravityHandler gravityHandler;
    private Vector3 customGravity = Vector3.zero;
    private Vector3 gravityDir = Vector3.zero;
    private float currentGravityStrength = 0.0f;

    private Rigidbody rb;
    private float xRotation = 0;

    private bool hasUpdatedGravity = false;
    private bool isJumping = false;
    private bool isGrounded = false;


    private Vector3 cameraOffsetPos;

    bool isInInventory = false;

    public bool lockPlayerTurnInput = false;
    public bool lockPlayerMoveInput = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        gravityHandler = GetComponent<GravityHandler>();

        if (playerCamera == null)
        {
            Debug.LogError("Player camera not assigned in inspector defaulting to Camera.main");
            playerCamera = Camera.main;
        }

        if (playerState == null)
        {
            playerState = this.GetComponent<PlayerStateMachine>();
        }


        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Disable the built-in gravity
        rb.useGravity = false;

        currentGravityStrength = baseGravityStrength;

        cameraOffsetPos = playerCamera.transform.position - this.transform.position;


        playerState.OnStateChange += HandleStateChange;
    }

    private void OnDestroy()
    {
        playerState.OnStateChange -= HandleStateChange;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isJumping = true;
        }
    }

    void FixedUpdate()
    {
        isGrounded = GroundCheck();

        gravityDir = gravityHandler.GravityDir;

        if (isInInventory)
        {
            return;
        }

        HandleJumping();

        if (!lockPlayerMoveInput)
        {
            Move();
        }    
        
        // Apply Ground Frction
        //if (isGrounded)
        //{
        //    rb.velocity *= Mathf.Pow(1.0f - groundFriction * Time.fixedDeltaTime, 2.0f);
        //    if (rb.velocity.magnitude <= 0.1f) rb.velocity = Vector3.zero;
        //}

    }

    private void LateUpdate()
    {
        if (!isInInventory && !lockPlayerTurnInput)
        {
            PlayerLook();
        }    
    }

    public void HandleStateChange(playerMainState mainState, playerSubState subState)
    {
        if (mainState == PlayerStateMachine.playerMainState.Alive)
        {
            if (subState == PlayerStateMachine.playerSubState.InventoryControl)
            {
                isInInventory = true;
            }
            else
            {
                isInInventory = false;
            }
        }
        else if (mainState == PlayerStateMachine.playerMainState.Paused)
        {
            isInInventory = true;
        }
    }
    private void HandleJumping()
    {
        if (!isGrounded)
        {
            return;
        }

        if (isJumping)
        {
            rb.AddForce(-gravityDir * jumpForce, ForceMode.Impulse);
            Debug.DrawLine(this.transform.position, this.transform.position + (-gravityDir * jumpForce), Color.blue);
            Debug.Log("Player attempted jump");
            isJumping= false;
        }
    }
    private void Move()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 currentVelocity = rb.velocity;

        Vector3 moveDirection = ((horizontal * this.transform.right) + (vertical * this.transform.forward)).normalized;
        Vector3 moveVelocity = moveDirection * moveSpeed;


        Debug.DrawLine(this.transform.position, this.transform.position + moveVelocity, Color.magenta);


        if (rb.velocity.magnitude >= 5) return;
        rb.AddForce(moveVelocity, ForceMode.Force);
    }

    private void PlayerLook()
    {
        // Rotate the camera based on the player direction
        float horizontalLook = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime;
        Vector3 playerRotation = new Vector3(0, horizontalLook, 0);

        transform.rotation = (transform.rotation * Quaternion.Euler(playerRotation));

        // Rotate the camera based on the player direction
        float verticalLook = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime;

        xRotation -= verticalLook;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }

    bool GroundCheck()
    {
        float sphereRadius = 0.75f; // Adjust the radius of the sphere cast
        float raycastDistance = 0.75f; // Adjust the distance of the sphere cast
        LayerMask layerMask = ~(LayerMask.GetMask("Player") | LayerMask.GetMask("Entity")); // Ignore layers "Player" and "Entity"

        // Perform a sphere cast downward
        if (Physics.SphereCast(transform.position, sphereRadius, -transform.up, out RaycastHit hit, raycastDistance, layerMask))
        {
            Utility.DebugDrawSphere(transform.position - transform.up, sphereRadius, Color.blue);
            return true; // Player is grounded
        }
        else
        {
            Utility.DebugDrawSphere(transform.position - transform.up, sphereRadius, Color.red);
            return false; // Player is not grounded
        }
    }
    private Vector3 LocalVelocity()
    {
        // Get the velocity in world space
        Vector3 worldVelocity = rb.velocity;

        // Transform the velocity to local space
        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        return localVelocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("IgnorePlayerCollision"))
        {
            Collider otherObject = collision.collider;

            // Ignore collision between the player and the ball
            Physics.IgnoreCollision(GetComponent<Collider>(), otherObject);
        }
    }
}

