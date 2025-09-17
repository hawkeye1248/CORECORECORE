using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEnder : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            SceneManager.LoadSceneAsync("TestScene1", LoadSceneMode.Additive);
            other.TryGetComponent<Player>(out Player player);
            player.RestartLevel();
            SceneManager.UnloadSceneAsync("TestScene1");
            
        }
    }
}
