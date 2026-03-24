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

    public void Fit(Camera cam = null)
    {
        Camera c = cam ?? targetCamera;
        if (c == null || sr == null || sr.sprite == null) return;

        float camHeight = c.orthographicSize * 2f;
        float camWidth = camHeight * c.aspect;

        Vector2 spriteSize = sr.sprite.bounds.size;

        transform.localScale = new Vector3(
            camWidth / spriteSize.x,
            camHeight / spriteSize.y,
            1f
        );

        transform.position = new Vector3(
            c.transform.position.x,
            c.transform.position.y,
            transform.position.z
        );
    }
}
