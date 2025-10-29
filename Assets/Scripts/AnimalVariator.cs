using Unity.Netcode;
using UnityEngine;

public class AnimalVariator : NetworkBehaviour
{
    public Renderer rend;
    public AnimalPelt pelt;
    public Antler antler;
    private int seed;
    private bool male = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            seed = Random.Range(0, 100000);
            Debug.Log("Animal generated with seed " + seed);

            // Pick a random color on the server
            pelt = GetPeltVariant(seed);
            SetColorClientRpc(pelt);

            if (Random.value < 0.5)
                male = true;

            if (IsServer && antler != null && male)
            {
                GenerateAntlersClientRpc(seed);
            }

            Debug.Log($"Spawned deer with {pelt.Description} pelt.");
        }
    }

    [ClientRpc]
    void SetColorClientRpc(AnimalPelt pelt)
    {
        this.pelt = pelt;
        ApplyColor(pelt.Color);
    }

    [ClientRpc]
    public void GenerateAntlersClientRpc(int antlerSeed)
    {
        if (antler != null)
        {
            antler.Initialize(antlerSeed);
        }
    }

    private void ApplyColor(Color c)
    {
        rend.materials[0].color = c;
    }

    public AnimalPelt GetPeltVariant(int seed)
    {
        System.Random rng = new System.Random(seed);

        Color baseColor = rend.materials[0].color;

        // Albino check
        if (RandomRangeFloat(rng, 0f, 1f) < 0.005f)
        {
            float shade = RandomRangeFloat(rng, 0.85f, 1f);
            Color albinoColor = new Color(shade, shade * 0.95f, shade * 0.9f);
            return new AnimalPelt(albinoColor, "Albino");
        }

        Color.RGBToHSV(baseColor, out float h, out float s, out float v);

        float hOffset = RandomRangeFloat(rng, -0.02f, 0.02f);
        float sOffset = RandomRangeFloat(rng, -0.05f, 0.05f);

        float vOffset;
        if (RandomRangeFloat(rng, 0f, 1f) < 0.8f)
            vOffset = RandomRangeFloat(rng, -0.05f, 0.05f);
        else
            vOffset = RandomRangeFloat(rng, -0.15f, 0.15f);

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

    int RandomRangeInt(System.Random rng, int min, int max)
    {
        return rng.Next(min, max); // upper bound exclusive
    }

    float RandomRangeFloat(System.Random rng, float min, float max)
    {
        return (float)(rng.NextDouble() * (max - min) + min);
    }
}
