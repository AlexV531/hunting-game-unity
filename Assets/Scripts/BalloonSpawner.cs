using Unity.Netcode;
using UnityEngine;

public class BalloonSpawner : Weapon
{
    public GameObject balloonPrefab;
    public float spawnDistance = 2f;
    public Vector3 targetOffset = new Vector3(0, 0, 10f);

    protected override void HandleFire()
    {
        if (_input.fire)
        {
            _input.fire = false;

            if (IsOwner)
            {
                // Example: get the object to attach to
                // (this could be something like your player, or an object you raycast)
                NetworkObject targetObject = GetTargetAttachObject();
                if (targetObject == null)
                    return;

                Vector3 spawnPos = transform.position + transform.forward * spawnDistance;
                Vector3 targetPos = GlobalVariables.balloonTargetPosition;

                _owner.GetInventory().RemoveItem(weaponInstance.Value, 1);

                // Pass its NetworkObjectId instead of a GameObject
                SpawnBalloonServerRpc(spawnPos, targetPos, targetObject.NetworkObjectId);
            }
        }
    }

    // Replace this with your actual logic to decide what to attach to
    private NetworkObject GetTargetAttachObject()
    {
        InteractableBase currentInteractable = _owner.GetCurrentInteractable();
        if (currentInteractable.NetworkObject.GetComponent<BalloonAttach>() != null)
        {
            return currentInteractable.NetworkObject;
        }
        Debug.Log("Hey what are we doing here");
        return null;
    }
    
    [ServerRpc(RequireOwnership = false)]
    protected void SpawnBalloonServerRpc(Vector3 spawnPos, Vector3 targetPos, ulong attachTargetId)
    {
        if (balloonPrefab == null)
        {
            Debug.LogWarning("Balloon prefab not assigned.");
            return;
        }

        GameObject balloonInstance = Instantiate(balloonPrefab, spawnPos, Quaternion.identity);
        var balloon = balloonInstance.GetComponent<Balloon>();

        BalloonAttach attach = null;

        // Look up the target object on the server by ID
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(attachTargetId, out NetworkObject targetNetObj))
        {
            attach = targetNetObj.GetComponent<BalloonAttach>();
            if (attach == null)
                Debug.LogWarning($"Could not find attach target on target");
            // else
            //     attach.Attach(balloon.GetTetherPoint());
        }
        else
        {
            Debug.LogWarning($"Could not find attach target with ID {attachTargetId}");
        }
        
        balloon.Initialize(targetPos, attach);
        balloon.GetComponent<NetworkObject>().Spawn();
    }
}
