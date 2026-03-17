using UnityEngine;
using System.Collections;
public class FightProgress : MonoBehaviour
{
    public DialogueManager dialogueRunner;
    public SistemaInventario sistemaInventario;
    public GameObject scamps;
    public GameObject foe;
    public GameObject Player;
    public Transform retreat;
    void Start()
    {
        dialogueRunner = FindFirstObjectByType<DialogueManager>();
        sistemaInventario = FindFirstObjectByType<SistemaInventario>();
        Player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        if (sistemaInventario.HasProgress("mutantfought"))
        {
            NextMoment();
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            StartCoroutine(MoveToPoint(Player, retreat.position, true));
            dialogueRunner.StartDialogue("cantprogressyet");
        }
    }

    private void NextMoment()
    {
        Destroy(scamps);
        foe.SetActive(true);
        Destroy(this);
    }

    private IEnumerator MoveToPoint(GameObject character, Vector3 target, bool useAnimator)
    {
        Animator anim = character.GetComponent<Animator>();
        SpriteRenderer sprite = character.GetComponent<SpriteRenderer>();
        float speed = 5f;

        float originalY = character.transform.position.y;
        Vector3 lockedTarget = new Vector3(target.x, originalY, target.z);

        if (useAnimator && anim != null)
        {
            anim.SetBool("Andando", true);
        }

        Vector3 startPos = character.transform.position;
        float distance = Mathf.Abs(lockedTarget.x - startPos.x);
        float duration = distance / speed;
        float elapsed = 0f;

        if (distance < 0.01f)
        {
            if (useAnimator && anim != null)
                anim.SetBool("Andando", false);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float newX = Mathf.Lerp(startPos.x, lockedTarget.x, t);
            character.transform.position = new Vector3(newX, originalY, character.transform.position.z);

            if (sprite != null)
            {
                float direction = lockedTarget.x - character.transform.position.x;
                if (Mathf.Abs(direction) > 0.1f)
                {
                    sprite.flipX = direction < 0;
                }
            }

            yield return null;
        }

        character.transform.position = new Vector3(lockedTarget.x, originalY, character.transform.position.z);

        if (useAnimator && anim != null)
        {
            anim.SetBool("Andando", false);
        }
    }

}
