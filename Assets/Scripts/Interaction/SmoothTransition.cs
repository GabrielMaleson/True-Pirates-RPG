using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class RoomTransitionEffect : MonoBehaviour
{
    [Header("Tilemap Reference")]
    [SerializeField] private Tilemap thing; // The Tilemap to fade

    [Header("Fade Settings")]
    [SerializeField][Range(0f, 1f)] private float fadeAlpha = 0.176f; // 45/255 ≈ 0.176
    [SerializeField][Range(0f, 1f)] private float normalAlpha = 1f; // 255/255 = 1
    [SerializeField] private float fadeDuration = 0.5f; // Duration of fade in seconds

    private Color originalColor;
    private bool isPlayerInside = false;
    private Coroutine fadeCoroutine;

    private void Start()
    {
        // Validate that the Tilemap is assigned
        if (thing == null)
        {
            Debug.LogError("Tilemap 'Thing' is not assigned! Please assign a Tilemap in the inspector.");
            return;
        }

        // Store the original color of the Tilemap
        originalColor = thing.color;

        // Ensure the Tilemap starts with normal alpha
        Color startColor = originalColor;
        startColor.a = normalAlpha;
        thing.color = startColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the entering object is the player
        if (other.CompareTag("Player") && thing != null)
        {
            isPlayerInside = true;
            SetTilemapAlphaSmooth(fadeAlpha);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if the exiting object is the player
        if (other.CompareTag("Player") && thing != null)
        {
            isPlayerInside = false;
            SetTilemapAlphaSmooth(normalAlpha);
        }
    }

    // For 3D colliders, also include these methods
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && thing != null)
        {
            isPlayerInside = true;
            SetTilemapAlphaSmooth(fadeAlpha);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && thing != null)
        {
            isPlayerInside = false;
            SetTilemapAlphaSmooth(normalAlpha);
        }
    }

    private void SetTilemapAlphaSmooth(float targetAlpha)
    {
        // Stop any existing fade coroutine
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        // Start new fade coroutine
        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private System.Collections.IEnumerator FadeRoutine(float targetAlpha)
    {
        Color startColor = thing.color;
        float startAlpha = startColor.a;
        float elapsed = 0f;

        // Don't fade if we're already at the target alpha
        if (Mathf.Approximately(startAlpha, targetAlpha))
            yield break;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            Color newColor = startColor;
            newColor.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            thing.color = newColor;

            yield return null;
        }

        // Ensure we end exactly at the target alpha
        Color finalColor = startColor;
        finalColor.a = targetAlpha;
        thing.color = finalColor;

        fadeCoroutine = null;
    }
}