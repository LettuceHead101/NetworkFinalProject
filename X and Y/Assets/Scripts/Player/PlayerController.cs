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
    public float moveSpeed = 20f;
    public float jumpForce = 12f;
    public float gravityStrength = 20f;
    public float fastFallBonus = 30f;
    public float acceleration = 20f;
    public float deceleration = 4f;
    private bool isExternalForceActive = false;
    
    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    
    private Rigidbody2D rb;
    private float inputDirection;
    private bool isFastFalling;
    private bool isGrounded;
    private float gravityMultiplier = -1f; 

    // The synced death counter
    public NetworkVariable<int> deathCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // A method the KillZone can call to tell the Server we died
    [ServerRpc]
    public void AddDeathServerRpc()
    {
        deathCount.Value++;
    }
    
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

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // If an external force is pushing us (like a bounce pad), disable WASD input!
        if (!isExternalForceActive)
        {
            inputDirection = Input.GetAxisRaw("Horizontal");

            KeyCode currentJumpKey = (gravityMultiplier < 0) ? KeyCode.W : KeyCode.S;

            if (currentRole.Value == PlayerRole.PlayerX)
            {
                Vector2 jumpDirection = (gravityMultiplier < 0) ? Vector2.up : Vector2.down;
                if (Input.GetKeyDown(currentJumpKey) && isGrounded)
                {
                    rb.AddForce(jumpDirection * jumpForce, ForceMode2D.Impulse);
                }
            }
            else if (currentRole.Value == PlayerRole.PlayerY)
            {
                Vector2 jumpDirection = (gravityMultiplier < 0) ? Vector2.right : Vector2.left;
                if (Input.GetKeyDown(currentJumpKey) && isGrounded)
                {
                    rb.AddForce(jumpDirection * jumpForce, ForceMode2D.Impulse);
                }
            }
        }

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

        // --- STOLEN LOGIC: Only apply crisp WASD movement if no external force is active ---
        if (!isExternalForceActive)
        {
            if (currentRole.Value == PlayerRole.PlayerX)
            {
                // Crisp, instant direct assignment (No Lerp!)
                rb.linearVelocity = new Vector2(inputDirection * moveSpeed, rb.linearVelocity.y);
                rb.AddForce(new Vector2(0f, currentGravity * gravityMultiplier));
            }
            else if (currentRole.Value == PlayerRole.PlayerY)
            {
                // Crisp, instant direct assignment (No Lerp!)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, inputDirection * -moveSpeed);
                rb.AddForce(new Vector2(currentGravity * gravityMultiplier, 0f));
            }
        }
        else 
        {
            // If external force IS active, still apply our custom gravity so we don't float away!
            if (currentRole.Value == PlayerRole.PlayerX)
            {
                 rb.AddForce(new Vector2(0f, currentGravity * gravityMultiplier));
            }
            else 
            {
                 rb.AddForce(new Vector2(currentGravity * gravityMultiplier, 0f));
            }
        }
    }

    // --- STOLEN LOGIC: Public Methods for Bounce Pads / Hazards ---
    public void SetExternalForce(float duration)
    {
        isExternalForceActive = true;
        Invoke(nameof(ReleaseExternalForce), duration);
    }

    private void ReleaseExternalForce()
    {
        isExternalForceActive = false;
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