using UnityEngine;

public class MovimentacaoExploracao : MonoBehaviour
{
    public float velocidade = 5f;
    private Rigidbody2D rb;
    private Vector2 movimento;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = rb.GetComponent<SpriteRenderer>();
        anim = rb.GetComponent<Animator>();
    }

    void Update()
    {
        //1. Captura o input do jogador (SEMPRE NO UPDATE)
        movimento.x = Input.GetAxisRaw("Horizontal");
        movimento.y = Input.GetAxisRaw("Vertical");

        //2. Controle da animação
        if(movimento != Vector2.zero)
        {
            anim.SetFloat("Horizontal", movimento.x);
            anim.SetFloat("Vertical", movimento.y);
            anim.SetBool("Andando", true);

            if (movimento.x > 0)
            {
                spriteRenderer.flipX = false;
            }
            else if (movimento.x < 0)
            {
                spriteRenderer.flipX = true;
            }
        }
        else
        {
            anim.SetBool("Andando", false);
        }
    }

    private void FixedUpdate()
    {
        //Aplica a física ao play
        //.normalized evita que andar na diagonal seja mais rapido
        rb.MovePosition(rb.position + movimento.normalized * velocidade * Time.fixedDeltaTime);
    }
}
