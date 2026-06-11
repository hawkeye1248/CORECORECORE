using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using MovementRework;

public class DeathScreenController : MonoBehaviour
{
    [SerializeField] private GameObject deathScreenPanel;
    [SerializeField] private float deathDelay = 0.5f;

    void Start()
    {
        deathScreenPanel.SetActive(false);
        Player player = Player.Instance;
        if (player != null && player.Health != null)
            player.Health.OnDeath += OnPlayerDeath;
    }

    void OnDestroy()
    {
        if (Player.Instance != null && Player.Instance.Health != null)
            Player.Instance.Health.OnDeath -= OnPlayerDeath;
    }

    void OnPlayerDeath(object sender, System.EventArgs e)
    {
        StartCoroutine(ShowDeathScreen());
    }

    IEnumerator ShowDeathScreen()
    {
        yield return new WaitForSecondsRealtime(deathDelay);
        deathScreenPanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
