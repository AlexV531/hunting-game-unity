using UnityEngine;
using UnityEngine.Rendering.Universal;
using Cinemachine;
using Unity.Netcode;

public enum WeaponClass
{
    Tool,
    Small,
    Large
}

public class Weapon : NetworkBehaviour
{
    protected PlayerInputs _input; // Reference to central input hub
    protected CameraRecoil _recoil;
    protected FirstPersonController _owner;
    protected Transform _followTarget;
    protected CinemachineVirtualCamera _vCam;


    [Header("Weapon Settings")]
    public Transform model;
    public Transform aimPosition;
    public Transform hipPosition;

    public float aimSpeed = 10f;

    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 300f;

    public WeaponClass weaponClass = WeaponClass.Large; // This is for loadouts

    public int weaponKey;
    private NetworkVariable<bool> isEquipped = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    [Header("Zoom Settings")]
    // public CinemachineVirtualCamera vCam;
    public float scopedFOV = 20f; // FOV when scoped
    public float zoomSpeed = 10f; // FOV transition speed

    // [Header("Shadow & LOD Settings")]
    // public UniversalRenderPipelineAsset urpAsset;   // Assigned URP Asset
    // public float scopedShadowDistance = 500f;       // Shadow distance when scoped
    // public float normalShadowDistance = 100f;       // Default shadow distance
    // public float scopedLODBias = 2f;                // LOD bias when scoped
    // public float normalLODBias = 1f;      

    [Tooltip("Time in seconds between shots")]
    public float fireRate = 0.2f;

    [Tooltip("If true, holding the fire button will shoot automatically")]
    public bool automaticFire = true;

    public int maxAmmo = 3;
    public int reserveAmmo = 100;

    private int currentAmmo = 3;
    private float _fireCooldown = 0f;
    private bool aiming = false;

    protected bool initialized = false;

    public static bool recoilEnabled = false;

    public virtual void Initialize()
    {
        if (IsOwner)
        {
            GameObject playerObj = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
            _owner = playerObj.GetComponent<FirstPersonController>();
            _owner.GetComponent<WeaponManager>().RegisterSpawnedWeapon(weaponKey, this);
            _input = _owner.GetComponent<PlayerInputs>();
            _recoil = _owner.GetComponent<CameraRecoil>();
            _followTarget = _owner.weaponContainer;
            _vCam = _owner.vCam;

            Debug.Log("Weapon initialized for local owner: " + _owner.name);
            initialized = true;
        }

        isEquipped.OnValueChanged += OnEquipChange;
        OnEquipChange(true, isEquipped.Value);
    }

    protected virtual void Update()
    {
        if (!initialized)
        {
            Initialize();
        }

        if (!IsOwner)
            return;

        if (!isEquipped.Value)
            return;

        if (_owner.IsPlayerInMenu())
            return;

        // reduce cooldown each frame
        if (_fireCooldown > 0f)
            _fireCooldown -= Time.deltaTime;

        HandleAim();
        HandleFire();
        HandleReload();
        HandleZoom();
    }

    protected virtual void LateUpdate()
    {
        if (!IsOwner)
            return;

        HandleFollowTarget();
    }

    public bool IsAiming()
    {
        return aiming;
    }

    protected virtual void HandleFollowTarget()
    {
        if (_followTarget != null)
        {
            transform.position = _followTarget.position;
            transform.rotation = _followTarget.rotation;
        }
    }

    protected virtual void HandleAim()
    {
        if (!model || !aimPosition || !hipPosition) return;

        if (!aiming && _input.aim)
        {
            EnterAim();
        }
        else if (aiming && !_input.aim)
        {
            ExitAim();
        }

        Transform targetPos = _input.aim ? aimPosition : hipPosition;
        model.localPosition = Vector3.Lerp(model.localPosition, targetPos.localPosition, aimSpeed * Time.deltaTime);
        model.localRotation = Quaternion.Slerp(model.localRotation, targetPos.localRotation, aimSpeed * Time.deltaTime);
    }

    void HandleZoom()
    {
        if (_vCam != null)
        {
            float targetFOV = _input.aim ? scopedFOV : GlobalVariables.cameraFOV;
            _vCam.m_Lens.FieldOfView = Mathf.Lerp(_vCam.m_Lens.FieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }
    }

    void EnterAim()
    {
        aiming = true;

        // // Adjust URP shadow distance
        // if (urpAsset != null)
        // {
        //     urpAsset.shadowDistance = scopedShadowDistance;
        // }

        // // Adjust global LOD bias
        // QualitySettings.lodBias = scopedLODBias;
    }

    void ExitAim()
    {
        aiming = false;

        // // Restore shadow distance
        // if (urpAsset != null)
        // {
        //     urpAsset.shadowDistance = normalShadowDistance;
        // }

        // // Restore LOD bias
        // QualitySettings.lodBias = normalLODBias;
    }

    protected virtual void HandleFire()
    {
        if (_input.fire && _fireCooldown <= 0f)
        {
            if (currentAmmo <= 0)
            {
                return;
            }
            Shoot();
            EmitNoiseServerRpc(transform.position, 20f, "gunshot");
            if (recoilEnabled)
                _recoil.AddRecoil(50f, 1f);
            currentAmmo--;
            _fireCooldown = fireRate;

            if (!automaticFire)
                _input.fire = false;
        }
    }

    [ServerRpc]
    void EmitNoiseServerRpc(Vector3 position, float loudness, string name)
    {
        var noiseEvent = new NoiseEvent(position, loudness, name);
        NoiseManager.Instance.EmitNoise(noiseEvent);
    }

    void HandleReload()
    {
        if (_input.reload)
        {
            Reload();
            _input.reload = false;
        }
    }

    void Shoot()
    {
        GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        bullet.speed = bulletSpeed;
    }

    void Reload()
    {
        if (currentAmmo == maxAmmo)
            return;

        reserveAmmo -= maxAmmo - currentAmmo;
        currentAmmo = maxAmmo;

        // Play reload animation
    }

    private void OnEquipChange(bool previousValue, bool newValue)
    {
        gameObject.SetActive(newValue);
    }

    public virtual void OnEquip()
    {
        if (!IsOwner)
            return;
        isEquipped.Value = true;
    }

    public virtual void OnUnequip()
    {
        if (!IsOwner)
            return;
        isEquipped.Value = false;
    }
}
