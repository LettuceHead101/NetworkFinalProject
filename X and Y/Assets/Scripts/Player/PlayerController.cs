using UnityEngine;
using Unity.Netcode; // We must add this to use Networking!
using System.Collections;
using UnityEngine.SceneManagement; // Required for listening to level changes

public enum PlayerRole
{
    None,
    PlayerX,
    PlayerY
}

public class PlayerController : NetworkBehaviour 
{
    [Header("Role Settings")]
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

    [ServerRpc]
    public void AddDeathServerRpc()
    {
        deathCount.Value++;
    }
    
    private PlayerRole lastAssignedRole;
    private bool hasInitialized = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; 
    }

    // --- NEW CAMERA & SCENE LOADING LOGIC STARTS HERE ---

    public override void OnNetworkSpawn()
    {
        // 1. Assign Roles (Server Only)
        if (IsServer)
        {
            if (OwnerClientId == 0) currentRole.Value = PlayerRole.PlayerX;
            else currentRole.Value = PlayerRole.PlayerY;
        }

        // 2. Setup Camera (Owner Only)
        if (IsOwner)
        {
            AttachCamera(); // Grab the camera immediately for the first level
            
            // SUBSCRIBE TO THE EVENT: Listen for any future level changes!
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    // We MUST unsubscribe when the player leaves the game to prevent memory leaks
    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    // This triggers automatically every single time a new level finishes loading
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

        // If we are on the win screen, do absolutely nothing! ---
        if (scene.name == "Completion") return;

        AttachCamera();

        // Teleport to spawn immediately when the scene loads ---
        if (IsOwner)
        {
            // 1. Kill any momentum from the last level so they don't slide
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            // 2. Snap them to their designated spawn points
            if (currentRole.Value == PlayerRole.PlayerX)
            {
                transform.position = new Vector3(0f, -2f, 0f); 
            }
            else if (currentRole.Value == PlayerRole.PlayerY)
            {
                transform.position = new Vector3(-2f, 0f, 0f); 
            }
        }
    }

    // The actual logic to find the camera, safely separated into its own function
    private void AttachCamera()
    {
        Camera mainCam = Camera.main;
        
        if (mainCam != null)
        {
            CameraController cam = mainCam.GetComponent<CameraController>();
            if (cam == null)
            {
                cam = mainCam.gameObject.AddComponent<CameraController>();
            }
            cam.SetTarget(this.transform, currentRole.Value);
        }
        else
        {
            Debug.LogError("CRITICAL: Player loaded into " + SceneManager.GetActiveScene().name + ", but no object is tagged 'MainCamera'!");
        }
    }

    // --- NEW CAMERA & SCENE LOADING LOGIC ENDS HERE ---


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

        // --- NEW: Manual Respawn Key ---
        if (Input.GetKeyDown(KeyCode.R))
        {
            // 1. The player teleports THEMSELVES locally first (bypasses network fights)
            rb.linearVelocity = Vector2.zero;
            
            if (currentRole.Value == PlayerRole.PlayerX)
            {
                transform.position = new Vector3(0f, -2f, 0f); 
            }
            else if (currentRole.Value == PlayerRole.PlayerY)
            {
                transform.position = new Vector3(-2f, 0f, 0f); 
            }

            // 2. Tell the Server to handle the score and the flashing visuals
            RequestRespawnServerRpc();
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        float currentGravity = gravityStrength;
        if (isFastFalling) currentGravity += fastFallBonus;

        if (!isExternalForceActive)
        {
            if (currentRole.Value == PlayerRole.PlayerX)
            {
                rb.linearVelocity = new Vector2(inputDirection * moveSpeed, rb.linearVelocity.y);
                rb.AddForce(new Vector2(0f, currentGravity * gravityMultiplier));
            }
            else if (currentRole.Value == PlayerRole.PlayerY)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, inputDirection * -moveSpeed);
                rb.AddForce(new Vector2(currentGravity * gravityMultiplier, 0f));
            }
        }
        else 
        {
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

    
    public void SetExternalForce(float duration)
    {
        isExternalForceActive = true;
        Invoke(nameof(ReleaseExternalForce), duration);
    }

    private void ReleaseExternalForce()
    {
        isExternalForceActive = false;
    }

    [ServerRpc]
    public void FlipGravityServerRpc()
    {
        FlipGravityClientRpc();
    }

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

    [ServerRpc]
    public void TriggerRespawnEffectServerRpc()
    {
        FlashOnRespawnClientRpc();
    }

    [ClientRpc]
    private void FlashOnRespawnClientRpc()
    {
        StartCoroutine(RespawnFlashRoutine());
    }

    private IEnumerator RespawnFlashRoutine()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        
        Color normalColor = (currentRole.Value == PlayerRole.PlayerX) ? Color.blue : Color.red;

        for (int i = 0; i < 4; i++)
        {
            sr.color = Color.white;
            yield return new WaitForSeconds(0.1f); 
            
            sr.color = normalColor;
            yield return new WaitForSeconds(0.1f); 
        }
    }

    // --- MANUAL RESPAWN LOGIC ---
    
    // --- MANUAL RESPAWN LOGIC ---
    [ServerRpc]
    public void RequestRespawnServerRpc()
    {
        // 1. Add a death to the shared counter
        deathCount.Value++;

        // 2. Trigger the visual flash effect for everyone to see
        FlashOnRespawnClientRpc();
    }
}