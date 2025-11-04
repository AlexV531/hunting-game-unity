using Unity.Netcode;
using UnityEngine;

public class BalloonAttach : NetworkBehaviour
{
    private Transform balloon;
    public Transform objectToAttach;
    private AttachHandler handler;

    public void Attach(Transform balloonTransform, AttachHandler handler = null)
    {
        balloon = balloonTransform;
        if (handler != null)
        {
            this.handler = handler;
        }
    }

    public void Release(bool calledFromAttachHandler = false)
    {
        if (handler != null && !calledFromAttachHandler)
        {
            handler.ReleaseTarget(this);
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

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Release();
    }

    private void LateUpdate()
    {
        if (!IsServer || balloon == null)
            return;

        objectToAttach.position = balloon.position;
        objectToAttach.rotation = balloon.rotation;
    }
}