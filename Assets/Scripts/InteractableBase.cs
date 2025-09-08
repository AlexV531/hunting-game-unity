using UnityEngine;

public abstract class InteractableBase : MonoBehaviour
{
    [SerializeField] private bool interactionEnabled = true; // controls if this object can be interacted with

    // Actual behavior when interacted with
    public abstract void Interact(FirstPersonController player);

    // UI prompt
    public virtual string GetPrompt()
    {
        return "Press \"e\" to Interact";
    }

    // Check if interaction is currently allowed
    public virtual bool IsInteractionEnabled()
    {
        return interactionEnabled;
    }

    // Allow external scripts to enable/disable interaction
    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
    }
}