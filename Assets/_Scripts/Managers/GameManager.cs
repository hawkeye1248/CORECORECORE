using System;
using UnityEngine;

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

    private void Start() {
        //TODO inputa esc koyup oyunu durdurma ekle
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
