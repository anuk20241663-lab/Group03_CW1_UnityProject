
using UnityEngine;
using UnityEngine.SceneManagement;

public class GateSceneTransition : MonoBehaviour
{
    private bool isTransitioning = false;

    private void OnTriggerEnter(Collider other)
    {
        Transform player = other.transform.root;

        if (player.CompareTag("Player") && !isTransitioning)
        {
            isTransitioning = true;

            Debug.Log("Entering the ancient ruins!");

            SceneManager.LoadScene("Cave");
        }
    }
}
