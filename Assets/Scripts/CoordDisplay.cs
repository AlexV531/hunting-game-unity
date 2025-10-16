using UnityEngine;
using TMPro;

public class CoordDisplay : MonoBehaviour
{
    public TextMeshProUGUI textDisplay;

    void Update()
    {
        if (FirstPersonController.LocalPlayer != null)
            textDisplay.text = FirstPersonController.LocalPlayer.transform.position.ToString();
    }
}
