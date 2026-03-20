using UnityEngine;

public class ShopkeeperLoad : MonoBehaviour
{
    public DialogueManager dialogue;
    private GameObject player;
    public bool hasstarted = false;
    public GameObject shop;
    private bool playerInRange = false;
    public float Ugh = 0f;
    public GameObject Player;
    private MovimentacaoExploracao playerMovement;
    void Start()
    {
        dialogue = FindFirstObjectByType<DialogueManager>();
    }

    void Update()
    {
        if (!hasstarted && playerInRange)
        {
            ShowPopup();
        }
        if (!hasstarted && playerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            hasstarted = true;
            if (Player == null)
                Player = GameObject.FindGameObjectWithTag("Player");

            if (Player != null)
                playerMovement = Player.GetComponent<MovimentacaoExploracao>();
            playerMovement.enabled = false;
            dialogue.StartDialogue("shopkeep");
        }
        if (playerInRange && Input.GetKeyDown(KeyCode.Space)) 
        {
            Ugh++;
        }
        if (Ugh > 6f)
        {
            shop.SetActive(true);
            Destroy(this);
            playerMovement.enabled = true;
        }
    }



    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            HidePopup();
        }
    }

    private void ShowPopup()
    {
        GameObject popup = GameObject.FindWithTag("Notification");
        if (popup != null)
        {
            SpriteRenderer spriteRend = popup.GetComponent<SpriteRenderer>();
            if (spriteRend != null)
            {
                Color color = spriteRend.color;
                color.a = 1f;
                spriteRend.color = color;
            }
        }
    }

    private void HidePopup()
    {
        GameObject popup = GameObject.FindWithTag("Notification");
        if (popup != null)
        {
            SpriteRenderer spriteRend = popup.GetComponent<SpriteRenderer>();
            if (spriteRend != null)
            {
                Color color = spriteRend.color;
                color.a = 0f;
                spriteRend.color = color;
            }
        }
    }



}