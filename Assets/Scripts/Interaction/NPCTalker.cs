using UnityEngine;

public class NPCTalker : MonoBehaviour
{
    public DialogueManager dialogue;
    public bool hasstarted = false;
    private bool playerInRange = false;
    public string thething;
    public bool joodiescript = false;
    public bool DestroyThe = false;
    public GameObject Activator;
    public GameObject Activator2;
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
            dialogue.StartDialogue(thething);
            HidePopup();
            if (DestroyThe)
            {
                Activator.SetActive(true);
                Destroy(this);
                if (Activator2 != null)
                {
                    Activator2.SetActive(true);
                }
            }
        }

        if (!hasstarted && playerInRange && joodiescript)
        {
            hasstarted = true;
            dialogue.StartDialogue(thething);
            HidePopup();
            Destroy(this);
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