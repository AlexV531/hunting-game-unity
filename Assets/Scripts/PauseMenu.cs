using UnityEngine;
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    private static bool isPaused = false;
    private FirstPersonController _localPlayer;
    private PlayerInputs _inputs;

    private void Awake()
    {
        pauseMenuUI.SetActive(false);
        StartCoroutine(FindLocalPlayerRoutine());
    }

    private IEnumerator FindLocalPlayerRoutine()
    {
        // Keep checking until the local player exists
        while (_localPlayer == null)
        {
            foreach (var player in FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None))
            {
                if (player.IsLocalPlayer)
                {
                    _localPlayer = player;
                    _inputs = player.GetComponent<PlayerInputs>();
                    break;
                }
            }
            yield return null; // Wait for next frame
        }

        Debug.Log("Local player found for pause menu.");
    }

    private void Update()
    {
        if (_localPlayer != null && _inputs.pause)
        {
            if (isPaused)
                Resume();
            else
                Pause();

            _inputs.pause = false;
        }
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
        if (_localPlayer != null)
        {
            _localPlayer.SavePlayer();
            Debug.Log("Game Saved for local player.");
        }
    }

    public void SaveAndQuit()
    {
        if (_localPlayer != null)
        {
            _localPlayer.SavePlayer();
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
}
