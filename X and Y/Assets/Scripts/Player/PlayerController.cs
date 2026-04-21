using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum PlayerRole { PlayerX, PlayerY }
    
    [Header("Role Settings")]
    public PlayerRole currentRole;

    [Header("Movement Stats")]
    public float moveSpeed = 7f;
    public float jumpForce = 12f;
    public float gravityStrength = 25f;
    public float fastFallBonus = 30f; // Extra gravity when holding 'S' or 'Down'
    
    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    
    private Rigidbody2D rb;
    private float inputDirection;
    private bool isFastFalling;
    private bool isGrounded;
    private float gravityMultiplier = -1f; // -1 = Down/Left. 1 = Up/Right.

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; 
    }

    void Update()
    {
        // Check for the Gravity Flip (Spacebar)
        // Because both scripts run this, pressing Space flips BOTH players simultaneously!
        if (Input.GetKeyDown(KeyCode.Space))
        {
            FlipGravity();
        }

        // 1. Check if the player is touching the ground/wall
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // 2. Player X Inputs (WASD Perspective)
        if (currentRole == PlayerRole.PlayerX)
        {
            inputDirection = 0f;
            if (Input.GetKey(KeyCode.D)) inputDirection = 1f;
            else if (Input.GetKey(KeyCode.A)) inputDirection = -1f;

            // Jump (W Key)
            if (Input.GetKeyDown(KeyCode.W) && isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * (gravityMultiplier * -1));
            }

            // Fast Fall (S Key)
            isFastFalling = Input.GetKey(KeyCode.S);
        }
        
        // 3. Player Y Inputs (Arrow Keys Perspective)
        else if (currentRole == PlayerRole.PlayerY)
        {
            inputDirection = 0f;
            if (Input.GetKey(KeyCode.UpArrow)) inputDirection = 1f;
            else if (Input.GetKey(KeyCode.DownArrow)) inputDirection = -1f;

            // Jump (Right Arrow Key) 
            if (Input.GetKeyDown(KeyCode.RightArrow) && isGrounded)
            {
                rb.linearVelocity = new Vector2(jumpForce * (gravityMultiplier * -1), rb.linearVelocity.y);
            }

            // Fast Fall (Left Arrow Key)
            isFastFalling = Input.GetKey(KeyCode.LeftArrow);
        }
    }

    void FixedUpdate()
    {
        float currentGravity = gravityStrength;
        if (isFastFalling) currentGravity += fastFallBonus;

        if (currentRole == PlayerRole.PlayerX)
        {
            rb.linearVelocity = new Vector2(inputDirection * moveSpeed, rb.linearVelocity.y);
            rb.AddForce(new Vector2(0f, currentGravity * gravityMultiplier));
        }
        else if (currentRole == PlayerRole.PlayerY)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, inputDirection * moveSpeed);
            rb.AddForce(new Vector2(currentGravity * gravityMultiplier, 0f));
        }
    }

    public void FlipGravity()
    {
        gravityMultiplier *= -1f;
        
        // This instantly teleports the Sensor to the opposite side of the circle!
        // If it was at (0, -0.5), it becomes (0, 0.5). If it was (-0.5, 0), it becomes (0.5, 0).
        groundCheck.localPosition = -groundCheck.localPosition;
    }

    // This draws a red circle in your Scene view exactly where your sensor is
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}