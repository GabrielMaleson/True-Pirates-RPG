using UnityEngine;
using System.Collections;

public class BumperCarsAnimation : MonoBehaviour
{
    [Header("NPCs")]
    public GameObject soldierNPC;
    public GameObject mutatedNPC;

    [Header("Effects")]
    public GameObject clashEffect; // GameObject with SpriteRenderer

    [Header("Settings")]
    public float moveSpeed = 3f;
    public float stopDistance = 0.5f; // Distance from center where they stop before clashing
    public float knockbackDistance = 2f; // How far they get knocked back
    public float clashDuration = 0.3f; // How long the clash effect stays visible

    [Header("Animation Parameters")]
    public string walkAnimationParam = "Andando"; // Parameter for walking animation
    public bool useAnimator = true;

    private Vector3 soldierStartPos;
    private Vector3 mutatedStartPos;
    private Vector3 clashCenter;

    private Animator soldierAnimator;
    private Animator mutatedAnimator;
    private SpriteRenderer clashSpriteRenderer;

    private void Start()
    {
        // Store starting positions
        if (soldierNPC != null)
            soldierStartPos = soldierNPC.transform.position;

        if (mutatedNPC != null)
            mutatedStartPos = mutatedNPC.transform.position;

        // Calculate center point between them
        clashCenter = (soldierStartPos + mutatedStartPos) / 2f;

        // Get components
        if (soldierNPC != null)
            soldierAnimator = soldierNPC.GetComponent<Animator>();

        if (mutatedNPC != null)
            mutatedAnimator = mutatedNPC.GetComponent<Animator>();

        if (clashEffect != null)
        {
            clashSpriteRenderer = clashEffect.GetComponent<SpriteRenderer>();
            // Position clash effect at center
            clashEffect.transform.position = clashCenter;
            // Start hidden
            clashEffect.SetActive(false);
        }

        StartBumperCars();

    }

    // Call this method to start the bumper cars animation
    public void StartBumperCars()
    {
        StartCoroutine(BumperCarsCoroutine());
    }

    private IEnumerator BumperCarsCoroutine()
    {
        // Loop indefinitely
        while (true)
        {
            // 1. Both NPCs walk toward each other
            yield return StartCoroutine(MoveTowardCenter());

            // 2. Clash effect appears between them
            yield return StartCoroutine(ShowClashEffect());

            // 3. Both NPCs get knocked back to starting positions
            yield return StartCoroutine(KnockBackToStart());

            // Optional short pause between cycles to avoid instant repeat (tweak as needed)
            yield return null;
        }
    }

    private IEnumerator MoveTowardCenter()
    {
        // Start walking animations
        if (useAnimator)
        {
            if (soldierAnimator != null)
                soldierAnimator.SetBool(walkAnimationParam, true);

            if (mutatedAnimator != null)
                mutatedAnimator.SetBool(walkAnimationParam, true);
        }

        // Calculate target positions (stop just before center)
        Vector3 soldierDirection = (clashCenter - soldierStartPos).normalized;
        Vector3 mutatedDirection = (clashCenter - mutatedStartPos).normalized;

        Vector3 soldierTarget = clashCenter - (soldierDirection * stopDistance);
        Vector3 mutatedTarget = clashCenter - (mutatedDirection * stopDistance);

        // Move until both reach their targets
        while (Vector3.Distance(soldierNPC.transform.position, soldierTarget) > 0.1f ||
               Vector3.Distance(mutatedNPC.transform.position, mutatedTarget) > 0.1f)
        {
            if (soldierNPC != null)
            {
                soldierNPC.transform.position = Vector3.MoveTowards(
                    soldierNPC.transform.position,
                    soldierTarget,
                    moveSpeed * Time.deltaTime
                );
            }

            if (mutatedNPC != null)
            {
                mutatedNPC.transform.position = Vector3.MoveTowards(
                    mutatedNPC.transform.position,
                    mutatedTarget,
                    moveSpeed * Time.deltaTime
                );
            }

            yield return null;
        }

        // Stop walking animations
        if (useAnimator)
        {
            if (soldierAnimator != null)
                soldierAnimator.SetBool(walkAnimationParam, false);

            if (mutatedAnimator != null)
                mutatedAnimator.SetBool(walkAnimationParam, false);
        }
    }

    private IEnumerator ShowClashEffect()
    {
        if (clashEffect != null)
        {
            // Show clash effect
            clashEffect.SetActive(true);

            // Optional: Add a simple flash/pulse effect
            if (clashSpriteRenderer != null)
            {
                Color originalColor = clashSpriteRenderer.color;

                // Flash white quickly
                clashSpriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.05f);
                clashSpriteRenderer.color = originalColor;
            }

            // Wait for clash duration
            yield return new WaitForSeconds(clashDuration);

            // Hide clash effect
            clashEffect.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(clashDuration);
        }
    }

    private IEnumerator KnockBackToStart()
    {
        // Start walking animations backward
        if (useAnimator)
        {
            if (soldierAnimator != null)
                soldierAnimator.SetBool(walkAnimationParam, true);

            if (mutatedAnimator != null)
                mutatedAnimator.SetBool(walkAnimationParam, true);
        }

        // Move both back to starting positions
        while (Vector3.Distance(soldierNPC.transform.position, soldierStartPos) > 0.1f ||
               Vector3.Distance(mutatedNPC.transform.position, mutatedStartPos) > 0.1f)
        {
            if (soldierNPC != null)
            {
                soldierNPC.transform.position = Vector3.MoveTowards(
                    soldierNPC.transform.position,
                    soldierStartPos,
                    moveSpeed * Time.deltaTime
                );

                // Flip sprite if needed (moving backward)
                SpriteRenderer sprite = soldierNPC.GetComponent<SpriteRenderer>();
                if (sprite != null)
                {
                    // Determine direction they should face when knocked back
                    float direction = soldierStartPos.x - soldierNPC.transform.position.x;
                    if (Mathf.Abs(direction) > 0.1f)
                        sprite.flipX = direction < 0;
                }
            }

            if (mutatedNPC != null)
            {
                mutatedNPC.transform.position = Vector3.MoveTowards(
                    mutatedNPC.transform.position,
                    mutatedStartPos,
                    moveSpeed * Time.deltaTime
                );

                // Flip sprite if needed
                SpriteRenderer sprite = mutatedNPC.GetComponent<SpriteRenderer>();
                if (sprite != null)
                {
                    float direction = mutatedStartPos.x - mutatedNPC.transform.position.x;
                    if (Mathf.Abs(direction) > 0.1f)
                        sprite.flipX = direction < 0;
                }
            }

            yield return null;
        }

        // Stop animations
        if (useAnimator)
        {
            if (soldierAnimator != null)
                soldierAnimator.SetBool(walkAnimationParam, false);

            if (mutatedAnimator != null)
                mutatedAnimator.SetBool(walkAnimationParam, false);
        }

        // Ensure they're exactly at start positions
        if (soldierNPC != null)
            soldierNPC.transform.position = soldierStartPos;

        if (mutatedNPC != null)
            mutatedNPC.transform.position = mutatedStartPos;
    }

    // Optional: Add a method to reset positions manually
    public void ResetPositions()
    {
        if (soldierNPC != null)
            soldierNPC.transform.position = soldierStartPos;

        if (mutatedNPC != null)
            mutatedNPC.transform.position = mutatedStartPos;

        if (clashEffect != null)
            clashEffect.SetActive(false);
    }

    // Draw gizmos to visualize the setup in editor
    private void OnDrawGizmosSelected()
    {
        if (soldierNPC != null && mutatedNPC != null)
        {
            // Draw start positions
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(soldierNPC.transform.position, 0.3f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(mutatedNPC.transform.position, 0.3f);

            // Draw center point
            Vector3 center = (soldierNPC.transform.position + mutatedNPC.transform.position) / 2f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, 0.3f);

            // Draw path lines
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(soldierNPC.transform.position, center);
            Gizmos.DrawLine(mutatedNPC.transform.position, center);
        }
    }
}