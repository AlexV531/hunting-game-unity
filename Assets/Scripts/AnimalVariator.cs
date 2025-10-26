using Unity.Netcode;
using UnityEngine;

public class AnimalVariator : NetworkBehaviour
{
    public Renderer rend;
    private AnimalPelt pelt;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Pick a random color on the server
            pelt = GetPeltVariant();
            SetColorClientRpc(pelt);

            Debug.Log($"Spawned deer with {pelt.Description} pelt.");
        }
    }

    [ClientRpc]
    void SetColorClientRpc(AnimalPelt pelt)
    {
        this.pelt = pelt;
        ApplyColor(pelt.Color);
    }

    private void ApplyColor(Color c)
    {
        rend.materials[0].color = c;
    }

    public AnimalPelt GetPeltVariant()
    {
        Color baseColor = rend.materials[0].color;

        // Albino check
        if (Random.value < 0.005f)
        {
            float shade = Random.Range(0.85f, 1f);
            Color albinoColor = new Color(shade, shade * 0.95f, shade * 0.9f);
            return new AnimalPelt(albinoColor, "Albino");
        }

        Color.RGBToHSV(baseColor, out float h, out float s, out float v);
        float hOffset = Random.Range(-0.02f, 0.02f);
        float sOffset = Random.Range(-0.05f, 0.05f);
        float vOffset;

        if (Random.value < 0.8f)
            vOffset = Random.Range(-0.05f, 0.05f);
        else
            vOffset = Random.Range(-0.15f, 0.15f);

        float newH = Mathf.Repeat(h + hOffset, 1f);
        float newS = Mathf.Clamp01(s + sOffset);
        float newV = Mathf.Clamp01(v + vOffset);

        Color newColor = Color.HSVToRGB(newH, newS, newV);

        // Determine descriptive category
        string desc;
        if (newV < 0.35f)
            desc = "Dark Brown";
        else if (newV < 0.55f)
            desc = "Brown";
        else
            desc = "Light Brown";

        return new AnimalPelt(newColor, desc);
    }
}