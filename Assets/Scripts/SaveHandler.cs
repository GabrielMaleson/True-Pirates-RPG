using UnityEngine;

public class SaveHandler : MonoBehaviour
{
    public void NewGame()
    {
        SaveLoadManager.Instance?.NewGame();
    }

    public void LoadGame()
    {
        SaveLoadManager.Instance?.LoadGame();
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
