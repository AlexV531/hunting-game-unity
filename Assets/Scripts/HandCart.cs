using Unity.Netcode;
using UnityEngine;

public class HandCart : InteractableBase
{
    private Rigidbody cartRb;
    private Joint activeJoint;

    protected override void Awake()
    {
        base.Awake();

        cartRb = GetComponent<Rigidbody>();
    }

    public override void Interact(FirstPersonController player)
    {
        throw new System.NotImplementedException();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Only the server should run physics on this cart
        cartRb.isKinematic = !IsServer;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestGrabServerRpc(ulong playerId)
    {
        NetworkObject playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerId);

        if (playerObj == null)
        {
            Debug.LogWarning($"Player {playerId} not found on server.");
            return;
        }

        Transform grabPoint = playerObj.transform.Find("GrabPoint");
        if (grabPoint == null)
        {
            Debug.LogWarning("GrabPoint not found on player.");
            return;
        }

        AttachToPlayer(grabPoint);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestReleaseServerRpc()
    {
        ReleaseFromPlayer();
    }

    private void AttachToPlayer(Transform grabPoint)
    {
        if (activeJoint != null)
            Destroy(activeJoint);

        SpringJoint joint = gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedBody = grabPoint.GetComponentInParent<Rigidbody>();
        joint.connectedAnchor = Vector3.zero;
        joint.anchor = Vector3.zero;

        joint.spring = 150f;
        joint.damper = 20f;
        joint.maxDistance = 2f;

        activeJoint = joint;

        Debug.Log($"Cart attached to player {grabPoint.root.name}");
    }

    private void ReleaseFromPlayer()
    {
        if (activeJoint != null)
        {
            Destroy(activeJoint);
            activeJoint = null;
            Debug.Log("Cart released.");
        }
    }
}
