using Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClientPlayerMove : NetworkBehaviour
{
    [SerializeField] private PlayerInput m_PlayerInput;
    [SerializeField] private PlayerInputs m_PlayerInputs;
    [SerializeField] private FirstPersonController m_FirstPersonController;
    [SerializeField] private Transform m_CameraTarget;

    void Awake()
    {
        m_PlayerInputs.enabled = false;
        m_PlayerInput.enabled = false;
        m_FirstPersonController.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            Debug.Log("Enabling inputs");
            m_PlayerInputs.enabled = true;
            m_PlayerInput.enabled = true;
            m_FirstPersonController.enabled = true;

            CinemachineVirtualCamera vCam = FindFirstObjectByType<CinemachineVirtualCamera>();
            m_FirstPersonController.vCam = vCam;
            vCam.Follow = m_CameraTarget;
        }
    }
}
