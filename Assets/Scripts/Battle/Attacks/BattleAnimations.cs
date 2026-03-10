using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Battle Animation", menuName = "RPG/Battle Animation")]
public class BattleAnimationData : ScriptableObject
{
    [Header("Animation Clips")]
    public AnimationClip userAnimation;
    public AnimationClip targetHitAnimation;

    [Header("Visual Effects")]
    public GameObject impactVFX;
    public GameObject chargeVFX;

    [Header("Sound Effects")]
    public AudioClip chargeSound;
    public AudioClip impactSound;

    [Header("Camera Effects")]
    public bool useCameraShake = true;
    public float cameraShakeIntensity = 0.3f;
    public float cameraShakeDuration = 0.2f;

    [Header("Timing")]
    public float preDelay = 0.2f;
    public float hitDelay = 0.5f;
    public float postDelay = 0.3f;

    public IEnumerator PlayAnimation(PartyMemberState user, List<PartyMemberState> targets, System.Action onComplete = null)
    {
        // Pre-delay
        yield return new WaitForSeconds(preDelay);

        // Play charge VFX/sound at user's position
        if (chargeVFX != null && user != null && user.transform != null)
        {
            GameObject charge = Object.Instantiate(chargeVFX, user.transform.position, Quaternion.identity);
            Object.Destroy(charge, 1f);
        }

        if (chargeSound != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(chargeSound, Camera.main.transform.position);
        }

        // Play user animation if available
        if (userAnimation != null && user != null && user.transform != null)
        {
            Animator animator = user.transform.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Play(userAnimation.name);
            }
        }

        yield return new WaitForSeconds(hitDelay);

        // Hit effects for each target
        foreach (var target in targets)
        {
            if (target == null) continue;

            // Camera shake
            if (useCameraShake && CameraShake.Instance != null)
            {
                CameraShake.Instance.Shake(cameraShakeIntensity, cameraShakeDuration);
            }

            // Hit VFX at target's position
            if (impactVFX != null && target.transform != null)
            {
                GameObject hit = Object.Instantiate(impactVFX, target.transform.position, Quaternion.identity);
                Object.Destroy(hit, 1f);
            }

            // Hit sound at target's position
            if (impactSound != null && target.transform != null)
            {
                AudioSource.PlayClipAtPoint(impactSound, target.transform.position);
            }

            // Target hit animation
            if (targetHitAnimation != null && target.transform != null)
            {
                Animator animator = target.transform.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.Play(targetHitAnimation.name);
                }
            }
        }

        yield return new WaitForSeconds(postDelay);

        onComplete?.Invoke();
    }
}

// Camera shake component (keep this as is)
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;
    private Vector3 originalPosition;
    private bool isShaking = false;

    private void Awake()
    {
        Instance = this;
        originalPosition = transform.position;
    }

    public void Shake(float intensity, float duration)
    {
        if (!isShaking)
            StartCoroutine(ShakeCoroutine(intensity, duration));
    }

    private IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            transform.position = originalPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
        isShaking = false;
    }
}