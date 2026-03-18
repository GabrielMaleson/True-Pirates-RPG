using UnityEngine;

public class ShopkeeperLoad : MonoBehaviour
{
    public DialogueManager dialogue;
    private GameObject player;
    public bool hasstarted = false;
    public GameObject shop;
    void Start()
    {
        dialogue = FindFirstObjectByType<DialogueManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasstarted && Input.GetKeyDown(KeyCode.Space) && collision.CompareTag("Player"))
        {
            hasstarted = true;
            dialogue.StartDialogue("shopkeep");
            shop.SetActive(true);
            Destroy(this);
        }
    }
}
