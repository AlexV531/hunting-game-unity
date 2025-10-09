using Unity.Netcode;
using UnityEngine;

public class BalloonAttach : NetworkBehaviour
{
    private Transform balloon;
    public Transform objectToAttach;
    private AttachInteractable interactable;

    public void Attach(Transform balloonTransform, AttachInteractable newInteractable = null)
    {
        balloon = balloonTransform;
        if (newInteractable != null)
        {
            interactable = newInteractable;
        }
    }

    public void Release(bool calledFromAttachInteractable = false)
    {
        if (interactable != null && !calledFromAttachInteractable)
        {
            interactable.ReleaseTarget(this);
        }
        else
        {
            balloon = null;
        }
    }

    public bool IsAttached()
    {
        return balloon != null;
    }

    private void Update()
    {
        if (!IsServer || balloon == null)
            return;

        objectToAttach.position = balloon.position;
        objectToAttach.rotation = balloon.rotation;
    }
}