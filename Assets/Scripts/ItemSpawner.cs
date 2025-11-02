using Unity.Netcode;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject worldItemPrefab;

    public void DropItem(ItemInstance instance, Vector3 pos, Vector3 force)
    {
        var obj = Instantiate(worldItemPrefab, pos, transform.rotation);
        obj.Spawn();
        obj.GetComponent<WorldItem>().Initialize(instance, pos, Quaternion.identity, force);
    }
}