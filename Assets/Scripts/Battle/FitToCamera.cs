using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FitToCamera : MonoBehaviour
{
    public Camera targetCamera;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Start()
    {
        Fit();
    }

    public void Fit()
    {
        if (targetCamera == null || sr == null || sr.sprite == null) return;

        float camHeight = targetCamera.orthographicSize * 2f;
        float camWidth = camHeight * targetCamera.aspect;

        Vector2 spriteSize = sr.sprite.bounds.size;

        transform.localScale = new Vector3(
            camWidth / spriteSize.x,
            camHeight / spriteSize.y,
            1f
        );

        transform.position = new Vector3(
            targetCamera.transform.position.x,
            targetCamera.transform.position.y,
            transform.position.z
        );
    }
}
