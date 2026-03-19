using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class FollowPlayer : MonoBehaviour
{
    public Transform target; // The leader's transform
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public float speed = 3.0f; // Movement speed
    public float stoppingDistance = 2.0f; // Distance to stop at
    public float smoothTime = 0.3f; // Smoothing time for movement

    [Header("Height Following Settings")]
    public float maxFollowHeight = 5f; // Maximum height difference to follow upwards
    public float heightIgnoreThreshold = 3f; // Height above which we stop following upwards

    private Vector3 velocity = Vector3.zero;
    private float currentMoveDirectionX = 0f;

    void Update()
    {
        if (target == null) return;

        // Calculate target position with height limitations
        Vector3 targetPosition = GetLimitedTargetPosition();

        // Calculate distance (using only horizontal distance for movement decision)
        float horizontalDistance = Vector2.Distance(
            new Vector2(transform.position.x, 0),
            new Vector2(targetPosition.x, 0)
        );

        // Only move if the follower is outside the stopping distance
        if (horizontalDistance >= stoppingDistance)
        {
            // Smoothly move towards the target position
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime,
                speed
            );
        }
        else
        {
            // Gradually stop when within stopping distance
            velocity = Vector3.Lerp(velocity, Vector3.zero, Time.deltaTime * 5f);
        }

        // Calculate movement direction for animation
        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        currentMoveDirectionX = Mathf.Lerp(currentMoveDirectionX, moveDirection.x, Time.deltaTime * 10f);

        // Update animator
        if (animator != null)
        {
            bool isMoving = horizontalDistance > stoppingDistance;
            animator.SetBool("Andando", isMoving);

            if (isMoving)
            {
                animator.SetFloat("Horizontal", currentMoveDirectionX);
            }
        }

        // Flip sprite based on movement direction
        if (spriteRenderer != null && Mathf.Abs(currentMoveDirectionX) > 0.1f)
        {
            spriteRenderer.flipX = currentMoveDirectionX < 0;
        }
    }

    private Vector3 GetLimitedTargetPosition()
    {
        Vector3 targetPos = target.position;

        // Calculate height difference
        float heightDifference = targetPos.y - transform.position.y;

        // If the player is above the ignore threshold, don't follow upwards
        if (heightDifference > heightIgnoreThreshold)
        {
            // Keep the follower's current Y position
            targetPos.y = transform.position.y;
        }
        // If within follow range, allow following upwards but with smoothing
        else if (heightDifference > 0 && heightDifference <= heightIgnoreThreshold)
        {
            // Partially follow upwards based on how close we are to the threshold
            float followFactor = 1f - (heightDifference / heightIgnoreThreshold);
            targetPos.y = Mathf.Lerp(transform.position.y, targetPos.y, followFactor);
        }
        // Always follow downwards (gravity will handle this naturally)
        // else if (heightDifference < 0) - allow following downwards

        return targetPos;
    }

    // Optional: Visual debugging in the editor
    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);

            // Draw height threshold
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Vector3 thresholdPos = transform.position + Vector3.up * heightIgnoreThreshold;
            Gizmos.DrawWireSphere(thresholdPos, 0.5f);

            // Draw max follow height
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Vector3 maxFollowPos = transform.position + Vector3.up * maxFollowHeight;
            Gizmos.DrawWireSphere(maxFollowPos, 0.5f);
        }
    }
}