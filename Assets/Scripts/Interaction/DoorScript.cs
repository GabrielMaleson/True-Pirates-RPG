using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class DoorScript : MonoBehaviour
{
    public string destination;
    public SpriteRenderer spriteRenderer;
    public Sprite opendoor;
    private bool playerInRange = false;

    private void Update()
    {
        // Check for input in Update while player is in range and chest is not open
        if (playerInRange && Input.GetKeyDown(KeyCode.T))
        {
            TryOpenDoor();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            {
                playerInRange = true;
                ShowPopup();
            }
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

    private IEnumerator TryOpenDoor()
    {
      
        spriteRenderer.sprite = opendoor;
        HidePopup();
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene(destination);
        
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