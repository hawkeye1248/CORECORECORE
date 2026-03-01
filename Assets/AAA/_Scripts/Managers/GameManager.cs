using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        GAMEPLAY,
        LOSE,
        PAUSED
    }

    private GameState currentState;
    public event EventHandler OnStateChanged;

    private void Awake()
    {
        Instance = this;

        //TODO değiştirilcek
        currentState = GameState.GAMEPLAY;
    }

    private void Start()
    {
        //TODO inputa esc koyup oyunu durdurma ekle

        //GameInput.Instance.OnJumpPerformed += on_jump_performed;

        PlayerWeaponController.instance.GetComponent<Health>().OnPlayerDeath += on_player_death;
    }

    private void Update()
    {
        switch (currentState)
        {
            case GameState.GAMEPLAY:
                break;
            case GameState.LOSE:
                break;
            case GameState.PAUSED:
                break;
            default:
                break;
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            if (IsGameLost())
            {
                RestartLevel();
            }
        }
    }

    private void on_jump_performed(object sender, EventArgs e)
    {
        
    }
    private void on_player_death(object sender, EventArgs e)
    {
        currentState = GameState.LOSE;
    }

    public void RestartLevel()
    {
        //Player.Instance.RestartLevel();
        SceneManager.LoadScene(0);
        currentState = GameState.GAMEPLAY;
    }

    public void LoseGame()
    {
        currentState = GameState.LOSE;
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void PauseGame()
    {
        currentState = GameState.PAUSED;
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ResumeGame()
    {
        currentState = GameState.GAMEPLAY;
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool IsGamePlaying()
    {
        return currentState == GameState.GAMEPLAY;
    }

    public bool IsGameLost()
    {
        return currentState == GameState.LOSE;
    }

    public bool IsPaused()
    {
        return currentState == GameState.PAUSED;
    }
}
