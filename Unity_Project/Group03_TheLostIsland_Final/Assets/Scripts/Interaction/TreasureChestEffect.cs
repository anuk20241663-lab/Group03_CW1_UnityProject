
using UnityEngine;

public class TreasureChestEffect : MonoBehaviour
{
    public ParticleSystem treasureEffect;
    public GameObject victoryMessage;

    private bool treasureFound = false;

    void Start()
    {
        if (treasureEffect != null)
            treasureEffect.Stop();

        if (victoryMessage != null)
            victoryMessage.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (treasureFound)
            return;

        if (other.CompareTag("Player") ||
            other.transform.root.CompareTag("Player"))
        {
            treasureFound = true;

            // Play golden treasure effect
            if (treasureEffect != null)
                treasureEffect.Play();

            // Display victory message
            if (victoryMessage != null)
                victoryMessage.SetActive(true);

            Debug.Log("TREASURE FOUND!");
        }
    }
}
