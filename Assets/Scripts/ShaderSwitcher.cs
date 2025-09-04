using UnityEngine;

public class ShaderSwitcher : MonoBehaviour
{
    public PlayerInputs playerInputs; // reference to your PlayerInputs
    public Shader newShader;

    private Shader[] originalShaders;

    void Start()
    {
        if (playerInputs == null)
        {
            playerInputs = GameObject.FindWithTag("Player").GetComponent<PlayerInputs>();
        }

        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            Material[] mats = rend.materials;
            originalShaders = new Shader[mats.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                originalShaders[i] = mats[i].shader;
            }
        }
    }

    void Update()
    {
        if (playerInputs != null && playerInputs.toggleShader)
        {
            ToggleShader();
        }
    }

    private bool isSwitched = false;

    void ToggleShader()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;

        Material[] mats = rend.materials;
        for (int i = 0; i < mats.Length; i++)
        {
            mats[i].shader = isSwitched ? originalShaders[i] : newShader;
        }
        rend.materials = mats;

        isSwitched = !isSwitched;
        playerInputs.toggleShader = false; // reset input to avoid multiple triggers
    }
}