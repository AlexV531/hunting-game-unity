using Unity.Netcode;
using UnityEngine;

public class AnimalVariator : NetworkBehaviour
{
    public Renderer rend;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Pick a random color on the server
            Color color = GetColorVariant();
            SetColorClientRpc(color);
        }
    }

    [ClientRpc]
    void SetColorClientRpc(Color color)
    {
        ApplyColor(color);
    }

    private void ApplyColor(Color c)
    {
        rend.materials[0].color = c;
    }

    public Color GetColorVariant()
    {
        // Base color: #4A2A0B
        Color baseColor = new Color32(0x4A, 0x2A, 0x0B, 255);

        // 0.5% chance for an albino-like variant
        if (Random.value < 0.005f)
        {
            // Very light tan to near white (but not pure white)
            float shade = Random.Range(0.85f, 1f);
            return new Color(shade, shade * 0.95f, shade * 0.9f); // warm tone
        }

        // Normal brown variation
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);

        float hOffset = Random.Range(-0.02f, 0.02f);
        float sOffset = Random.Range(-0.05f, 0.05f);
        float vOffset;

        // 80% of the time: subtle variation
        // 20%: more distinct light/dark
        if (Random.value < 0.8f)
            vOffset = Random.Range(-0.05f, 0.05f);
        else
            vOffset = Random.Range(-0.15f, 0.15f);

        float newH = Mathf.Repeat(h + hOffset, 1f);
        float newS = Mathf.Clamp01(s + sOffset);
        float newV = Mathf.Clamp01(v + vOffset);

        return Color.HSVToRGB(newH, newS, newV);
    }
}