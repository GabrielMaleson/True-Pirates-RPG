using UnityEngine;

public class SaveHandler : MonoBehaviour
{
    public void NewGame()
    {
        SaveLoadManager saveLoadManager = SaveLoadManager.Instance;
        SaveLoadManager.Instance.NewGame(); 
    }


    public void LoadGame()
    {
        SaveLoadManager saveLoadManager = SaveLoadManager.Instance;
        saveLoadManager.LoadGame();
    }

    public void ExitGame()
    {
        Application.Quit();
    }

}
