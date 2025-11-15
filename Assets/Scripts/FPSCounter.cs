using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    private float timer;

    void Update()
    {
        timer += Time.unscaledDeltaTime;

        if (timer >= 0.2f) // update 5 times per second
        {
            float fps = 1f / Time.unscaledDeltaTime;
            fpsText.text = Mathf.RoundToInt(fps) + " FPS";
            timer = 0f;
        }
    }
}