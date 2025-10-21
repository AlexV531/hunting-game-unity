using UnityEngine;

public class ShaderSwitcher : MonoBehaviour
{
    public Shader newShader;
    public Renderer rend;

    private Shader[] originalShaders;
    private Material[] materials;
    private bool isSwitched = false;

    void Start()
    {
        if (rend == null)
        {
            Debug.LogWarning("ShaderSwitcher: No renderer assigned.", this);
            return;
        }

        materials = rend.materials;
        originalShaders = new Shader[materials.Length];

        for (int i = 0; i < materials.Length; i++)
        {
            originalShaders[i] = materials[i].shader;
        }
    }

    public void ToggleShader()
    {
        if (rend == null || newShader == null)
        {
            Debug.LogWarning("ShaderSwitcher: Missing renderer or newShader.", this);
            return;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].shader = isSwitched ? originalShaders[i] : newShader;
        }

        isSwitched = !isSwitched;
    }
}