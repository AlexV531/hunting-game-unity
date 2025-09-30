using UnityEngine;
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    private static bool isPaused = false;
    private PlayerInputs _inputs;

    private void Awake()
    {
        pauseMenuUI.SetActive(false);
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        isPaused = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        isPaused = true;
    }

    public void SaveGame()
    {
        if (FirstPersonController.LocalPlayer != null)
        {
            FirstPersonController.LocalPlayer.SavePlayer();
            Debug.Log("Game Saved for local player.");
        }
    }

    public void SaveAndQuit()
    {
        if (FirstPersonController.LocalPlayer != null)
        {
            FirstPersonController.LocalPlayer.SavePlayer();
            Debug.Log("Game saved. Quitting...");
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public static bool IsPaused()
    {
        return isPaused;
    }

    private void OnApplicationQuit()
    {
        SaveGame(); // Auto-save when player closes the game
    }
}
