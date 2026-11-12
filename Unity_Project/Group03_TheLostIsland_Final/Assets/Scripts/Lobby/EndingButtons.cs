
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingLobbyButtons : MonoBehaviour
{
    public void PlayAgain()
    {
        SceneManager.LoadScene("THE LOST ISLAND");
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void ExitGame()
    {
        Debug.Log("Game Closed");
        Application.Quit();
    }
}
