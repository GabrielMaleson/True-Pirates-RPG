using UnityEngine;

public class ShopkeeperLoad : MonoBehaviour
{
    public DialogueManager dialogue;
    private GameObject player;
    public bool hasstarted = false;
    public GameObject shop;
    private bool playerInRange = false;

    void Start()
    {
        dialogue = FindFirstObjectByType<DialogueManager>();
    }

    void Update()
    {
        if (!hasstarted && playerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            hasstarted = true;
            dialogue.StartDialogue("shopkeep");
            shop.SetActive(true);
            Destroy(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("Press Space to talk to shopkeeper");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}