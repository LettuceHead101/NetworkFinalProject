using UnityEngine;
using Unity.Netcode; // We must add this to use Networking!

// Change MonoBehaviour to NetworkBehaviour
public class PlayerController : NetworkBehaviour 
{
    public enum PlayerRole { PlayerX, PlayerY }
    
    [Header("Role Settings")]
    // NetworkVariables sync automatically across all clients
    public NetworkVariable<PlayerRole> currentRole = new NetworkVariable<PlayerRole>(
        PlayerRole.PlayerX, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    [Header("Movement Stats")]
    public float moveSpeed = 7f;
    public float jumpForce = 12f;
    public float gravityStrength = 25f;
    public float fastFallBonus = 30f;
    
    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    
    private Rigidbody2D rb;
    private float inputDirection;
    private bool isFastFalling;
    private bool isGrounded;
    private float gravityMultiplier = -1f; 
    
    // Add these two new variables
    private PlayerRole lastAssignedRole;
    private bool hasInitialized = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; 
    }

    // This runs automatically the moment the player connects to the network
    public override void OnNetworkSpawn()
    {
        // ONLY the Server is allowed to assign roles
        if (IsServer)
        {
            // The Host is always assigned Client ID 0. 
            if (OwnerClientId == 0)
            {
                currentRole.Value = PlayerRole.PlayerX;
            }
            // Anyone else who joins gets Player Y
            else
            {
                currentRole.Value = PlayerRole.PlayerY;
            }
        }
    }

    void Update()
    {

        // --- NEW: Smart Initialization ---
        // If this is our first frame, OR if the server just changed our role...
        if (!hasInitialized || currentRole.Value != lastAssignedRole)
        {
            lastAssignedRole = currentRole.Value;
            hasInitialized = true;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();

            if (currentRole.Value == PlayerRole.PlayerX)
            {
                sr.color = Color.blue;
                // Move sensor to the bottom edge
                groundCheck.localPosition = new Vector3(0f, -0.5f, 0f); 
            }
            else if (currentRole.Value == PlayerRole.PlayerY)
            {
                sr.color = Color.red;
                // Move sensor to the left edge
                groundCheck.localPosition = new Vector3(-0.5f, 0f, 0f); 
            }
        }
        // ---------------------------------

        // THE GOLDEN RULE OF NETWORKING: 
        // If this character does not belong to the computer running this code, DO NOTHING.
        if (!IsOwner) return;

        // Spacebar flips gravity. We call a ServerRpc so the server can tell EVERYONE to flip.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            FlipGravityServerRpc();
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Player X Inputs (Bottom Floor)
        if (currentRole.Value == PlayerRole.PlayerX)
        {
            inputDirection = 0f;
            if (Input.GetKey(KeyCode.D)) inputDirection = 1f;
            else if (Input.GetKey(KeyCode.A)) inputDirection = -1f;

            // W jumps UP, S falls DOWN
            if (Input.GetKeyDown(KeyCode.W) && isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * (gravityMultiplier * -1));
            }
            isFastFalling = Input.GetKey(KeyCode.S);
        }
        
        // Player Y Inputs (Left Wall)
        else if (currentRole.Value == PlayerRole.PlayerY)
        {
            inputDirection = 0f;
            // W moves UP the wall, S moves DOWN the wall
            if (Input.GetKey(KeyCode.W)) inputDirection = 1f;
            else if (Input.GetKey(KeyCode.S)) inputDirection = -1f;

            // D jumps RIGHT (into the room), A falls LEFT (back to the wall)
            if (Input.GetKeyDown(KeyCode.D) && isGrounded)
            {
                rb.linearVelocity = new Vector2(jumpForce * (gravityMultiplier * -1), rb.linearVelocity.y);
            }
            isFastFalling = Input.GetKey(KeyCode.A);
        }
    }

    void FixedUpdate()
    {
        // Only the owner processes their own physics to prevent laggy movement
        if (!IsOwner) return;

        float currentGravity = gravityStrength;
        if (isFastFalling) currentGravity += fastFallBonus;

        if (currentRole.Value == PlayerRole.PlayerX)
        {
            rb.linearVelocity = new Vector2(inputDirection * moveSpeed, rb.linearVelocity.y);
            rb.AddForce(new Vector2(0f, currentGravity * gravityMultiplier));
        }
        else if (currentRole.Value == PlayerRole.PlayerY)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, inputDirection * moveSpeed);
            rb.AddForce(new Vector2(currentGravity * gravityMultiplier, 0f));
        }
    }

    // --- NETWORKING METHODS ---

    // A Client calls this to ask the Server to do something
    [ServerRpc]
    public void FlipGravityServerRpc()
    {
        // The Server then shouts back to ALL clients to execute the flip
        FlipGravityClientRpc();
    }

    // The Server calls this to force ALL Clients to run this code
    [ClientRpc]
    public void FlipGravityClientRpc()
    {
        gravityMultiplier *= -1f;
        groundCheck.localPosition = -groundCheck.localPosition;
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}