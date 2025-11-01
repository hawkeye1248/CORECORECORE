using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    
    void Start()
    {
        GameInput.Instance.OnJumpPerformed += on_space_pressed;
    }

    void OnDestroy()
    {
        GameInput.Instance.OnJumpPerformed -= on_space_pressed;
    }

    void OnDisable()
    {
        GameInput.Instance.OnJumpPerformed -= on_space_pressed;
    }

    private void on_space_pressed(object sender, EventArgs e)
    {
        SceneManager.LoadSceneAsync("PersistentGameplay");
        SceneManager.LoadSceneAsync("TestScene1", LoadSceneMode.Additive);
    }

}
