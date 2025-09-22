using Unity.Netcode;
using UnityEngine;

public abstract class InteractableBase : NetworkBehaviour
{
    public NetworkVariable<bool> interactionEnabled = new NetworkVariable<bool>(true);

    // Actual behavior when interacted with
    public abstract void Interact(FirstPersonController player);

    // UI prompt
    public virtual string GetPrompt()
    {
        return "Press \"e\" to Interact";
    }

    // Check if interaction is currently allowed
    public virtual bool IsInteractionEnabled() => interactionEnabled.Value;

    // Allow external scripts to enable/disable interaction
    public void SetInteractionEnabled(bool enabled)
    {
        if (IsServer)
            interactionEnabled.Value = enabled;
    }
}