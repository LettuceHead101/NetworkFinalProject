using UnityEngine;
using Unity.Netcode; // We must add this to use Networking!

public enum PlayerRole
{
    None,
    PlayerX,
    PlayerY
}

// Change MonoBehaviour to NetworkBehaviour
public class PlayerController : NetworkBehaviour 
{
    
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
        if (IsOwner)
        {
            // When I spawn, attach a CameraController to the Main Camera and tell it to follow ME
            CameraController cam = Camera.main.gameObject.AddComponent<CameraController>();
            cam.SetTarget(this.transform, currentRole.Value);
        }

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
        // --- Smart Initialization ---
        if (!hasInitialized || currentRole.Value != lastAssignedRole)
        {
            lastAssignedRole = currentRole.Value;
            hasInitialized = true;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();

            if (currentRole.Value == PlayerRole.PlayerX)
            {
                sr.color = Color.blue;
                groundCheck.localPosition = new Vector3(0f, -0.5f, 0f); 
            }
            else if (currentRole.Value == PlayerRole.PlayerY)
            {
                sr.color = Color.red;
                groundCheck.localPosition = new Vector3(-0.5f, 0f, 0f); 
            }
        }
        // ---------------------------------

        if (!IsOwner) return;

        // 1. Check if we are touching the floor/wall/ceiling
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // 2. Store the A/D input
        inputDirection = Input.GetAxisRaw("Horizontal");

        // --- NEW: DYNAMIC JUMP LOGIC ---
        // If gravity is normal (-1), use W. If flipped (1), use S.
        KeyCode currentJumpKey = (gravityMultiplier < 0) ? KeyCode.W : KeyCode.S;

        if (currentRole.Value == PlayerRole.PlayerX)
        {
            // Normal = Jump Up. Flipped = Jump Down.
            Vector2 jumpDirection = (gravityMultiplier < 0) ? Vector2.up : Vector2.down;
            
            if (Input.GetKeyDown(currentJumpKey) && isGrounded)
            {
                rb.AddForce(jumpDirection * jumpForce, ForceMode2D.Impulse);
            }
        }
        else if (currentRole.Value == PlayerRole.PlayerY)
        {
            // Normal = Jump Right (away from left wall). Flipped = Jump Left (away from right wall).
            Vector2 jumpDirection = (gravityMultiplier < 0) ? Vector2.right : Vector2.left;

            if (Input.GetKeyDown(currentJumpKey) && isGrounded)
            {
                rb.AddForce(jumpDirection * jumpForce, ForceMode2D.Impulse);
            }
        }

        // 4. GRAVITY FLIP LOGIC
        if (Input.GetKeyDown(KeyCode.Space))
        {
            FlipGravityServerRpc();
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        float currentGravity = gravityStrength;
        if (isFastFalling) currentGravity += fastFallBonus;

        // 4. PHYSICS MOVEMENT LOGIC
        if (currentRole.Value == PlayerRole.PlayerX)
        {
            // Player X moves normal (Left/Right on X axis)
            rb.linearVelocity = new Vector2(inputDirection * moveSpeed, rb.linearVelocity.y);
            rb.AddForce(new Vector2(0f, currentGravity * gravityMultiplier));
        }
        else if (currentRole.Value == PlayerRole.PlayerY)
        {
            // Player Y moves visually Left/Right, which is technically Up/Down on the Y axis.
            // We multiply by -moveSpeed because their camera is rotated -90 degrees!
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, inputDirection * -moveSpeed);
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