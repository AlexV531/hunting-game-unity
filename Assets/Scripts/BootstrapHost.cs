using Unity.Netcode;
using UnityEngine;

public class BootstrapHost : MonoBehaviour
{
    public bool startHost = true;

    private void Start()
    {
        if (!startHost)
        {
            return;
        }

        if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.StartHost();
            }
            else
            {
                Debug.LogError("NetworkManager not found in the scene!");
            }
    }
}