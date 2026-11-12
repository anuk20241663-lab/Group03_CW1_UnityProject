
using UnityEngine;
using UnityEngine.SceneManagement;

public class OpeningLobbyButtons : MonoBehaviour
{
    [Header("Scene Names")]
    public string gameplaySceneName = "MainIsland";
    public string openingLobbySceneName = "OpeningLobby";

    public void NewGame()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(openingLobbySceneName);
    }

    public void ExitGame()
    {
        Debug.Log("Exit Game clicked");
        Application.Quit();
    }
}
