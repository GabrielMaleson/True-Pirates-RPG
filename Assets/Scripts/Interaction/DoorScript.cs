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

    // Reference to player's movement script to prevent jumping
    private MovimentacaoExploracao playerMovement;

    private void Update()
    {
        // Check for input in Update while player is in range
        if (playerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            // Disable player jumping temporarily
            if (playerMovement != null)
                playerMovement.SetCanJump(false);

            StartCoroutine(TryOpenDoor());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;

            // Get reference to player's movement script
            if (playerMovement == null)
                playerMovement = collision.GetComponent<MovimentacaoExploracao>();

            ShowPopup();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;

            // Clear reference
            playerMovement = null;

            HidePopup();
        }
    }

    private IEnumerator TryOpenDoor()
    {
        spriteRenderer.sprite = opendoor;
        HidePopup();
        yield return new WaitForSeconds(0.3f);

        // Re-enable jumping before scene loads (just in case)
        if (playerMovement != null)
            playerMovement.SetCanJump(true);

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