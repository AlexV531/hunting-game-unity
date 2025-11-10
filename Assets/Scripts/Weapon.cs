using UnityEngine;
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
    protected AudioSource _audioSource;

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
    public NetworkVariable<ItemInstance> weaponInstance = new NetworkVariable<ItemInstance>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
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
    public float fireRate = 1f;
    public float recoilPitch = 50f;
    public float recoilYaw = 1f;
    public float loudness = 80f;

    [Tooltip("If true, holding the fire button will shoot automatically")]
    public bool automaticFire = true;

    public int maxAmmo = 3;
    public int reserveAmmoKey = 24; // TEMPORARY Eventually ammo will be set in loadout

    private int currentAmmo = 0;
    private float _fireCooldown = 0f;
    private bool aiming = false;

    protected bool initialized = false;

    public static bool recoilEnabled = true;

    public event System.Action<int, int> OnAmmoChanged;

    public virtual void Initialize()
    {
        if (IsOwner)
        {
            GameObject playerObj = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
            _owner = playerObj.GetComponent<FirstPersonController>();
            if (WeaponDatabase.GetWeapon(weaponInstance.Value.key).contextual)
                _owner.GetComponent<WeaponManager>().RegisterSpawnedContextualWeapon(weaponInstance.Value.key, this);
            else
                _owner.GetComponent<WeaponManager>().RegisterSpawnedWeapon(weaponInstance.Value, this);
            _input = _owner.GetComponent<PlayerInputs>();
            _input.fire = false;
            _recoil = _owner.GetComponent<CameraRecoil>();
            _followTarget = _owner.weaponContainer;
            _vCam = _owner.vCam;

            Debug.Log("Weapon initialized for local owner: " + _owner.name);
            initialized = true;
        }

        _audioSource = GetComponent<AudioSource>();
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
            CreateBulletServerRpc();
            EmitNoiseServerRpc(transform.position, loudness, "gunshot");
            if (recoilEnabled)
                _recoil.AddRecoil(recoilPitch, recoilYaw);
            currentAmmo--;
            OnAmmoChanged?.Invoke(GetCurrentAmmo(), GetReserveAmmo());
            _fireCooldown = fireRate;

            if (!automaticFire)
                _input.fire = false;
        }
    }

    [ServerRpc]
    void EmitNoiseServerRpc(Vector3 position, float loudness, string name)
    {
        NoiseEvent noiseEvent = new NoiseEvent(position, loudness, name);
        NoiseManager.Instance.EmitNoise(noiseEvent);
        PlayShootAudioClientRpc();
    }

    [ClientRpc]
    void PlayShootAudioClientRpc()
    {
        if (_audioSource != null)
        {
            _audioSource.Play();
        }
    }

    void HandleReload()
    {
        if (_input.reload)
        {
            Reload();
            _input.reload = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CreateBulletServerRpc(ServerRpcParams rpcParams = default)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        bullet.speed = bulletSpeed;
        bullet.playerClientId = rpcParams.Receive.SenderClientId;

        // Spawn over network
        bulletObj.GetComponent<NetworkObject>().Spawn();
        Debug.Log("Bullet spawned with speed " + bullet.speed);
    }

    void Reload()
    {
        if (currentAmmo >= maxAmmo)
            return;

        // TEMPORARY AMMO SELECTION SECTION Eventually ammo will be set in loadout screen
        ItemInstance ammoInstance = _owner.GetInventory().GetInstance(reserveAmmoKey);

        if (ammoInstance.Equals(default))
            return;

        int needed = maxAmmo - currentAmmo;

        int removed = _owner.GetInventory().RemoveItem(ammoInstance, needed);

        currentAmmo += removed;

        OnAmmoChanged?.Invoke(GetCurrentAmmo(), GetReserveAmmo());

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
        _owner.GetAmmoUI().SetWeapon(this);
    }

    public virtual void OnUnequip()
    {
        if (!IsOwner)
            return;
        isEquipped.Value = false;
        _owner.GetAmmoUI().SetWeapon(null);
    }

    public virtual int GetCurrentAmmo() => currentAmmo;

    public int GetReserveAmmo()
    {
        ItemInstance ammo = _owner.GetInventory().GetInstance(reserveAmmoKey);
        return ammo.Equals(default) ? 0 : ammo.stackSize;
    }
}
