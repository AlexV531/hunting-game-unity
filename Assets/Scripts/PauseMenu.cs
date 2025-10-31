using UnityEngine;

public class PauseMenu : UIMenu
{
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

    private void OnApplicationQuit()
    {
        SaveGame(); // Auto-save when player closes the game
    }
}
