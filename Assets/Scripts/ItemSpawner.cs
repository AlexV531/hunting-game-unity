using Unity.Netcode;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject worldItemPrefab;

    // private void Start()
    // {
    //     StartCoroutine(WaitForServerAndSpawn());
    // }

    // private System.Collections.IEnumerator WaitForServerAndSpawn()
    // {
    //     // Wait until the NetworkManager exists and server is running
    //     while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
    //         yield return null;

    //     ItemInstance pelt = new ItemInstance
    //     {
    //         key = 20,
    //         stackSize = 1,
    //         customData = new ItemCustomData
    //         {
    //             quality = 0.85f,
    //             color = Color.brown
    //         }
    //     };

    //     DropItem(pelt, transform.position, Vector3.zero);
    // }

    public void DropItem(ItemInstance instance, Vector3 pos, Vector3 force)
    {
        var obj = Instantiate(worldItemPrefab, pos, Quaternion.identity);
        obj.Spawn();
        obj.GetComponent<WorldItem>().Initialize(instance, pos, Quaternion.identity, force);
    }
}