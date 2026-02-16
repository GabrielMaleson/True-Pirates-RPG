using UnityEngine;

public class ControladorInimigo : MonoBehaviour
{
    //FSM [Finite State Machine]
    //Estados: Patrulha, Perseguir, Parado
    //1. Definindo os estados possiveis (Não termina com ";" e não precisa de aspas)
    public enum EstadoInimigo { Parado, Patrulha, Perseguicao}

    [Header("IA do inimigo")]
    public EstadoInimigo estadoAtual;

    [Header("Movimentação")]
    public float velocidade = 2.0f;
    public Transform[] pontosDePatrulha; //Armazena os pontos de parada/patrulha
    public int indicePontoAtual = 0;     //Ponto a ser visitado

    [Header("Espera")]
    public float tempoDeEspera = 2.0f;
    public float cronometroEspera = 0f;

    [Header("Sensores")]
    public float raioVisao = 5f; //Distancia para começar a perseguir
    public float raioPerseguicao = 8f; //Distancia para desistir da perseguição
    public float distanciaAtaque = 1f; //Distancia para iniciar o combate

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private GameObject jogador; //Armazena os dados do Jogador

    private void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        //Ao iniciar a partida, começa parado (idle)
        estadoAtual = EstadoInimigo.Patrulha;

        //Procura pelo GameObject com a tag Player
        if(jogador == null)
        {
            jogador = GameObject.FindGameObjectWithTag("Player");
        }
    }

    private void Update()
    {
        //0. Segurança. Se o jogador morrer ou não for encontrado, não faz nada.
        if(jogador == null)
        {
            return;
        }

        //1. Calculando a distância entre os gameobjects
        float distancia = Vector2.Distance(transform.position, jogador.transform.position);
        //Debug.Log($"Distancia entre INIMIGO e JOGADOR: {distancia}");


        switch (estadoAtual) 
        { 
            case EstadoInimigo.Parado:
                //Toca a animação em velocidade normal
                animator.speed = 1f;

                animator.SetBool("Andando", false);
                Debug.Log($"{gameObject.name} mudando para o estado {estadoAtual}.");
                break;
            case EstadoInimigo.Patrulha:
                //Regra 01: Se o jogador entro no raio de visão
                //Toca a animação em velocidade normal
                animator.speed = 1f;

                if (distancia < raioVisao)
                {
                    estadoAtual = EstadoInimigo.Perseguicao;
                }
                animator.SetBool("Andando", true);
                Debug.Log($"{gameObject.name} mudando para o estado {estadoAtual}.");
                Patrulhar();
                break;
            case EstadoInimigo.Perseguicao:
                //Regra 02: O jogador saiu do raio de perseguição

                //Toca a animação em velocidade 50% mais rapida
                animator.speed = 1.5f;
                if(distancia > raioPerseguicao)
                {
                    estadoAtual = EstadoInimigo.Patrulha;
                }

                //Regra 03: Inimigo alcançou o jogador. Inicio de combate.
                if(distancia < distanciaAtaque)
                {
                    //Inicio do combate
                    IniciarCombate();
                }
                else
                {
                    //Continua a perseguição
                    Perseguir();
                }
                animator.SetBool("Andando", true);
                Debug.Log($"{gameObject.name} mudando para o estado {estadoAtual}.");
                break;
        }
    }

    private void IniciarCombate()
    {
        Debug.Log("Inicio do combate!");
        //Apenas para o jogo (por enquanto)
        Time.timeScale = 0f;
    }

    private void Perseguir()
    {
        //1. Calcular a direção (Do inimigo para o Jogador)
        Vector3 direcao = (jogador.transform.position - transform.position).normalized;

        //2. Atualiza o animator
        animator.SetBool("Andando", true);
        animator.SetFloat("Horizontal", direcao.x);
        animator.SetFloat("Vertical", direcao.y);

        //3. Flip
        Flip(direcao);

        //Mover o inimigo

        /*- Ajustar a movimentação do inimigo para o RIGIBODY.*/
        transform.position = Vector2.MoveTowards(transform.position,
                                                 jogador.transform.position,
                                                 velocidade * Time.deltaTime * 1.3f);
    }

    private void Flip(Vector3 direcao)
    {
        if (direcao.x < -0.1f)
        {
            spriteRenderer.flipX = true;
        }
        else if (direcao.x > 0.1f)
        {
            spriteRenderer.flipX = false;
        }
    }

    private void Patrulhar()
    {
        //Defino o ponto(alvo) atual da patrulha
        Transform alvo = pontosDePatrulha[indicePontoAtual];

        //1. Verifica a distancia (Chegou ao alvo?)
        if (Vector2.Distance(transform.position, alvo.position) < 0.1f) 
        {
            //Chegou ao ponto (ESPERA)
            animator.SetBool("Andando", false);

            //Conta o tempo de espera
            cronometroEspera += Time.deltaTime;

            //Se o tempo passou do limite
            if(cronometroEspera >= tempoDeEspera)
            {
                //Reseta o cronometro
                cronometroEspera = 0;
                indicePontoAtual++;

                //Caso a posicao atual do alvo, seja maior que o total de posicoes, volta para o começo.

                if(indicePontoAtual>= pontosDePatrulha.Length)
                {
                    indicePontoAtual = 0;
                }
            }
        }
        else
        {
            //Se não chegou ao destino, continua andando

            //2. Calcular a direção
            Vector2 direcao = (alvo.position - transform.position).normalized;

            //3. Atualizar o animator
            animator.SetBool("Andando", true);
            animator.SetFloat("Horizontal", direcao.x);
            animator.SetFloat("Vertical", direcao.y);

            //4. Flip
            Flip(direcao);

            //5. Mover
            /*- Ajustar a movimentação do inimigo para o RIGIBODY.*/
            transform.position = Vector2.MoveTowards(transform.position,
                                                            alvo.position,
                                                            velocidade * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        //Desenha o raio de visão (vermelho)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, raioVisao);

        //Desenha o raio da perseguição (amarelo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, raioPerseguicao);

        //Desenha a distancia entre o inimigo e o jogador (azul)
        Gizmos.color = Color.blue;
        if (jogador != null) 
        {
            Gizmos.DrawLine(transform.position, jogador.transform.position);
        }        
    }
}
