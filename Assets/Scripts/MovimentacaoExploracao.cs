using UnityEngine;

public class MovimentacaoExploracao : MonoBehaviour
{
    public float velocidade = 5f;
    public float forcaPulo = 10f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Vector2 movimento;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    public bool isGrounded;
    private bool canJump = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }
    public void SetCanJump(bool can)
    {
        canJump = can;
    }

    void Update()
    {
        // 1. Captura o input do jogador
        movimento.x = Input.GetAxisRaw("Horizontal");

        // 2. Check if grounded
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // 3. Jump input
        if (Input.GetButtonDown("Jump") && isGrounded && canJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, forcaPulo);
            // anim.SetTrigger("Pulando");
        }

        // 4. Controle da animação
        if (movimento.x != 0)
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

        // Set animator parameters
        // anim.SetBool("NoChao", isGrounded);
        // anim.SetFloat("VelocidadeVertical", rb.linearVelocity.y);
    }

    private void FixedUpdate()
    {
        // Only apply horizontal input; let physics handle Y entirely
        rb.linearVelocity = new Vector2(movimento.x * velocidade, rb.linearVelocity.y);
    }
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}