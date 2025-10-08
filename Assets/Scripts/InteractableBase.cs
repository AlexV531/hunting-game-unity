using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class InteractableBase : NetworkBehaviour
{
    [Header("Collider Sync Settings")]
    [SerializeField]
    private bool enableColliderSync = true; // Toggle to enable/disable collider syncing on start

    public NetworkVariable<bool> interactionEnabled = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool initialInteractionEnabledValue = true;

    private Collider interactCollider;

    protected virtual void Awake()
    {
        interactCollider = GetComponent<Collider>();

        // Only update collider if feature is enabled
        if (enableColliderSync)
        {
            UpdateCollider(interactionEnabled.Value);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            interactionEnabled.Value = initialInteractionEnabledValue;
        }

        if (enableColliderSync)
        {
            interactionEnabled.OnValueChanged += OnInteractionChanged;
            UpdateCollider(interactionEnabled.Value);
        }
    }

    public abstract void Interact(FirstPersonController player);

    public virtual string GetPrompt(FirstPersonController player)
    {
        Debug.Log(interactionEnabled.Value);
        return "Press \"e\" to Interact";
    }

    public virtual bool IsInteractionEnabled() => interactionEnabled.Value;

    public void SetInteractionEnabled(bool enabled)
    {
        if (!IsServer)
            throw new System.InvalidOperationException("SetInteractionEnabled can only be called on the server.");
        
        Debug.Log("Interaction enabled set to " + interactionEnabled.Value);

        interactionEnabled.Value = enabled;
        if (enableColliderSync)
        {
            UpdateCollider(enabled);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetInteractionEnabledServerRpc(bool enabled)
    {
        Debug.Log("Attempting to set interaction enabled to " + interactionEnabled.Value + " from server rpc");
        if (interactionEnabled.Value != enabled)
        {
            interactionEnabled.Value = enabled;
            Debug.Log("Interaction enabled set to " + interactionEnabled.Value + " from server rpc");
            if (enableColliderSync)
            {
                UpdateCollider(enabled);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (enableColliderSync)
        {
            interactionEnabled.OnValueChanged -= OnInteractionChanged;
        }
    }

    private void OnInteractionChanged(bool oldValue, bool newValue)
    {
        UpdateCollider(newValue);
    }

    private void UpdateCollider(bool enabled)
    {
        if (interactCollider != null)
        {
            interactCollider.enabled = enabled;
        }
    }
}
