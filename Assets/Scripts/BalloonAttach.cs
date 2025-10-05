using Unity.Netcode;
using UnityEngine;

public class BalloonAttach : NetworkBehaviour
{
    private Transform balloon;
    public Transform objectToAttach;

    public void Attach(Transform balloonTransform)
    {
        balloon = balloonTransform;
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
    }
}