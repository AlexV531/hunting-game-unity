using Unity.Netcode;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject worldItemPrefab;

    void Start()
    {
        ItemInstance pelt = new ItemInstance
        {
            key = 20,
            customData = new ItemCustomData
            {
                quality = 0.85f,
                color = Color.brown
            }
        };
        DropItem(pelt, transform.position, Vector3.zero);
    }

    public void DropItem(ItemInstance instance, Vector3 pos, Vector3 force)
    {
        var obj = Instantiate(worldItemPrefab, pos, Quaternion.identity);
        obj.Spawn();
        obj.GetComponent<WorldItem>().Initialize(instance, pos, Quaternion.identity, force);
    }
}