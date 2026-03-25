using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveHandler : MonoBehaviour
{
    public void NewGame()
    {
        SceneManager.LoadScene("Beginning");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
