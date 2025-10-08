using Unity.Netcode;
using UnityEngine;

public class HandCart : InteractableBase
{
    [SerializeField] private Transform frontAnchor;
    [SerializeField] private float maxDistanceFromPlayer = 5f; // maximum allowed distance before release
    private Rigidbody cartRb;
    private Joint activeJoint;
    private static readonly ulong UnclaimedId = ulong.MaxValue;

    // Holds the player’s ClientId who currently owns the cart
    private NetworkVariable<ulong> owningPlayerId = new NetworkVariable<ulong>(
        UnclaimedId, // ulong.MaxValue = unassigned
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    protected override void Awake()
    {
        base.Awake();

        cartRb = GetComponent<Rigidbody>();
    }

    public override void Interact(FirstPersonController player)
    {
        // Only the local owner of this player can initiate interaction
        if (!player.IsOwner)
            return;

        ulong playerId = player.OwnerClientId;
        Debug.Log(playerId);

        // If cart is currently unclaimed, request a grab
        if (owningPlayerId.Value == UnclaimedId)
        {
            GrabCartServerRpc(playerId);
        }
        else if (owningPlayerId.Value == playerId)
        {
            ReleaseCartServerRpc(playerId);
        }
        else
        {
            Debug.Log($"Player {playerId} attempted to interact with a cart owned by {owningPlayerId.Value}.");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Only the server should run physics on this cart
        cartRb.isKinematic = !IsServer;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        // Only do this if the cart is currently grabbed
        if (owningPlayerId.Value == UnclaimedId)
            return;

        // Get the player's NetworkObject
        NetworkObject playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(owningPlayerId.Value);
        if (playerObj == null)
            return;

        if (frontAnchor == null)
            frontAnchor = transform; // fallback to cart center

        // Calculate distance
        float distance = Vector3.Distance(frontAnchor.position, playerObj.transform.position);

        // If too far, release the cart
        if (distance > maxDistanceFromPlayer)
        {
            Debug.Log($"Cart too far from player {owningPlayerId.Value}, releasing.");
            ReleaseCartServerRpc(owningPlayerId.Value);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void GrabCartServerRpc(ulong playerId)
    {
        if (owningPlayerId.Value != UnclaimedId)
            return; // Already owned

        owningPlayerId.Value = playerId;

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
    public void ReleaseCartServerRpc(ulong playerId)
    {
        if (playerId != owningPlayerId.Value)
            return; // Not their cart

        owningPlayerId.Value = UnclaimedId;

        DetachFromPlayer();
    }

    private void AttachToPlayer(Transform grabPoint)
    {
        if (activeJoint != null)
            Destroy(activeJoint);

        SpringJoint joint = gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;

        // Connect to player's Rigidbody
        joint.connectedBody = grabPoint.GetComponentInParent<Rigidbody>();
        joint.connectedAnchor = grabPoint.localPosition;

        // Anchor at the front of the cart
        joint.anchor = frontAnchor != null
            ? transform.InverseTransformPoint(frontAnchor.position)
            : Vector3.zero;

        joint.spring = 1500f; // Increase for stiffness
        joint.damper = 100f; // Higher damper reduces oscillation
        joint.maxDistance = 0.5f; // Keep the cart close to the grab point

        activeJoint = joint;
    }

    private void DetachFromPlayer()
    {
        if (activeJoint != null)
        {
            Destroy(activeJoint);
            activeJoint = null;
            Debug.Log("Cart released.");
        }
    }
}
