using UnityEngine;

public class MovimentacaoExploracao : MonoBehaviour
{
    [Header("Movement")]
    public float velocidade = 5f;
    public float forcaPulo = 10f;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public LayerMask oneWayPlatformLayer; // Layer for platforms you can jump through

    [Header("Physics")]
    public float friction = 0f; // Optional: Add some friction

    private Rigidbody2D rb;
    private Vector2 movimento;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private bool isGrounded;
    private bool isOnOneWayPlatform;
    private bool isTouchingWall;

    // For one-way platform jumping down
    private float jumpThroughTimer = 0f;
    private const float JUMP_THROUGH_DURATION = 0.2f;
    private bool ignorePlatforms = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        // Set up physics material to reduce sticking
        if (rb.sharedMaterial == null)
        {
            // Create a physics material with no friction
            PhysicsMaterial2D noFriction = new PhysicsMaterial2D("NoFriction");
            noFriction.friction = 0f;
            rb.sharedMaterial = noFriction;
        }

        // Create ground check if not assigned
        if (groundCheck == null)
        {
            GameObject check = new GameObject("GroundCheck");
            check.transform.parent = transform;
            check.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = check.transform;
        }
    }

    void Update()
    {
        // 1. Captura o input do jogador
        movimento.x = Input.GetAxisRaw("Horizontal");

        // 2. Check if grounded - separate checks for regular ground and one-way platforms
        CheckGroundStatus();

        // 3. Jump input
        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded)
            {
                // Regular jump
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, forcaPulo);
                // anim.SetTrigger("Pulando");
            }
            else if (isOnOneWayPlatform && Input.GetAxisRaw("Vertical") < 0)
            {
                // Jump down through one-way platform
                StartCoroutine(JumpThroughPlatform());
            }
        }

        // 4. Jump down through platform (down + jump)
        if (Input.GetButtonDown("Jump") && isOnOneWayPlatform && Input.GetAxisRaw("Vertical") < 0)
        {
            StartCoroutine(JumpThroughPlatform());
        }

        // 5. Controle da animação
        if (movimento.x != 0 && !isTouchingWall)
        {
            anim.SetFloat("Horizontal", movimento.x);
            anim.SetBool("Andando", true);

            // Flip sprite based on direction
            spriteRenderer.flipX = movimento.x < 0;
        }
        else
        {
            anim.SetBool("Andando", false);
        }
    }

    private void FixedUpdate()
    {
        // Apply horizontal movement with wall check
        float targetVelocityX = movimento.x * velocidade;

        // Don't push into walls
        if (isTouchingWall && Mathf.Abs(movimento.x) > 0)
        {
            // Check if we're trying to move into the wall
            bool movingRight = movimento.x > 0;
            bool wallOnRight = CheckWallOnSide(Vector2.right);
            bool wallOnLeft = CheckWallOnSide(Vector2.left);

            if ((movingRight && wallOnRight) || (!movingRight && wallOnLeft))
            {
                targetVelocityX = 0; // Stop horizontal movement
            }
        }

        // Apply velocity
        rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);

        // Apply friction if needed (optional)
        if (Mathf.Abs(movimento.x) < 0.1f && friction > 0 && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * (1 - friction), rb.linearVelocity.y);
        }
    }

    private void CheckGroundStatus()
    {
        // Check for regular ground
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Check for one-way platforms (only when not ignoring them)
        if (!ignorePlatforms)
        {
            isOnOneWayPlatform = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, oneWayPlatformLayer);
        }
        else
        {
            isOnOneWayPlatform = false;
        }

        // Check for walls on both sides
        isTouchingWall = CheckWallOnSide(Vector2.right) || CheckWallOnSide(Vector2.left);
    }

    private bool CheckWallOnSide(Vector2 direction)
    {
        float checkDistance = 0.6f; // Slightly larger than half the player's width
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, checkDistance, groundLayer | oneWayPlatformLayer);

        // Visualize the ray in editor
        Debug.DrawRay(transform.position, direction * checkDistance, Color.blue);

        return hit.collider != null;
    }

    private System.Collections.IEnumerator JumpThroughPlatform()
    {
        ignorePlatforms = true;

        // Small upward boost to detach from platform
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 2f);

        yield return new WaitForSeconds(JUMP_THROUGH_DURATION);

        ignorePlatforms = false;
    }

    // Optional: Add a method to create a platform grid
    public void CreatePlatformGrid(Vector2 start, Vector2 end, float spacing, GameObject platformPrefab)
    {
        for (float x = start.x; x <= end.x; x += spacing)
        {
            for (float y = start.y; y <= end.y; y += spacing)
            {
                Vector3 position = new Vector3(x, y, 0);
                Instantiate(platformPrefab, position, Quaternion.identity);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Draw wall check rays
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, Vector2.right * 0.6f);
        Gizmos.DrawRay(transform.position, Vector2.left * 0.6f);
    }
}