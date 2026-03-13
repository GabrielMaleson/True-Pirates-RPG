using UnityEngine;

public class PartyFollower : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target; // The player to follow
    public float followDistance = 1.5f; // How far behind to stay
    public float followSpeed = 5f; // How fast to move
    public float stoppingDistance = 0.1f; // How close to get to target position

    [Header("Position History")]
    public int historyLength = 50; // How many positions to remember
    private Vector3[] positionHistory;
    private int historyIndex = 0;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Rigidbody2D rb;

    private void Start()
    {
        // Initialize position history
        positionHistory = new Vector3[historyLength];
        for (int i = 0; i < historyLength; i++)
        {
            positionHistory[i] = transform.position;
        }

        // Get components
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // If no target assigned, try to find player
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
    }

    private void Update()
    {
        if (target == null) return;

        // Store target's position in history
        if (positionHistory != null && positionHistory.Length > 0)
        {
            positionHistory[historyIndex] = target.position;
            historyIndex = (historyIndex + 1) % historyLength;
        }

        // Get position from history based on follow distance
        int index = (historyIndex - Mathf.RoundToInt(followDistance * 10) + historyLength) % historyLength;
        Vector3 targetPosition = positionHistory[index];

        // Calculate movement direction
        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);

        // Update animator
        if (animator != null)
        {
            bool isMoving = distance > stoppingDistance;
            animator.SetBool("Andando", isMoving);

            if (isMoving)
            {
                animator.SetFloat("Horizontal", moveDirection.x);
            }
        }

        // Flip sprite based on movement direction
        if (spriteRenderer != null && moveDirection.x != 0)
        {
            spriteRenderer.flipX = moveDirection.x < 0;
        }
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        // Get target position from history
        int index = (historyIndex - Mathf.RoundToInt(followDistance * 10) + historyLength) % historyLength;
        Vector3 targetPosition = positionHistory[index];

        float distance = Vector3.Distance(transform.position, targetPosition);

        // Move towards target position
        if (distance > stoppingDistance)
        {
            Vector3 moveDirection = (targetPosition - transform.position).normalized;

            if (rb != null)
            {
                // Using Rigidbody2D
                rb.linearVelocity = moveDirection * followSpeed;
            }
            else
            {
                // Using Transform
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, followSpeed * Time.fixedDeltaTime);
            }
        }
        else
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    // Visualize follow path in editor
    private void OnDrawGizmosSelected()
    {
        if (positionHistory == null) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < positionHistory.Length - 1; i++)
        {
            if (positionHistory[i] != Vector3.zero && positionHistory[i + 1] != Vector3.zero)
            {
                Gizmos.DrawLine(positionHistory[i], positionHistory[i + 1]);
            }
        }
    }
}