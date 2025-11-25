using UnityEngine;
using Cinemachine;
using Unity.Netcode;
using System.Collections.Generic;

public class Weapon : NetworkBehaviour
{
    protected PlayerInputs _input;
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
    public float scopedFOV = 20f;
    public float zoomSpeed = 10f;

    [Header("Fire Settings")]
    public float fireRate = 1f;
    public float recoilPitch = 50f;
    public float recoilYaw = 1f;
    public float loudness = 80f;
    public bool automaticFire = true;

    public int maxAmmo = 3;
    public AmmoType acceptedAmmoType = AmmoType.Bullet;
    public ItemInstance fauxAmmoInstance;

    private int currentAmmo = 0;
    private float _fireCooldown = 0f;
    private bool aiming = false;

    protected bool initialized = false;
    public static bool recoilEnabled = true;

    [Header("Reload Settings")]
    public Transform reloadPosition;
    public float reloadSpeed = 10f;
    public float reloadDuration = 1.5f;

    private bool isReloading = false;
    private float reloadTimer = 0f;

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

            SelectAmmoFromInventory();

            initialized = true;
        }

        _audioSource = GetComponent<AudioSource>();
        isEquipped.OnValueChanged += OnEquipChange;
        OnEquipChange(true, isEquipped.Value);
    }

    protected virtual void Update()
    {
        if (!initialized)
            Initialize();

        if (!IsOwner)
            return;

        if (!isEquipped.Value)
            return;

        if (_owner.IsPlayerInMenu())
            return;

        if (_fireCooldown > 0f)
            _fireCooldown -= Time.deltaTime;

        HandleReload();
        HandleAim();
        HandleFire();
        HandleZoom();
    }

    protected virtual void LateUpdate()
    {
        if (!IsOwner)
            return;

        HandleFollowTarget();
    }

    public bool IsAiming() => aiming;

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
        if (!model || !aimPosition || !hipPosition)
            return;

        Transform targetPos;

        if (isReloading && reloadPosition != null)
        {
            targetPos = reloadPosition;
        }
        else
        {
            bool wantAim = _input.aim; // allow aiming with zero ammo
            targetPos = wantAim ? aimPosition : hipPosition;

            if (!aiming && wantAim)
                EnterAim();
            else if (aiming && !wantAim)
                ExitAim();
        }

        float speed = isReloading ? reloadSpeed : aimSpeed;

        // Smoothly interpolate both position and rotation
        model.localPosition = Vector3.Lerp(model.localPosition, targetPos.localPosition, speed * Time.deltaTime);
        model.localRotation = Quaternion.Slerp(model.localRotation, targetPos.localRotation, speed * Time.deltaTime);
    }

    void HandleZoom()
    {
        if (_vCam == null)
            return;

        float targetFOV = (_input.aim && !isReloading) ? scopedFOV : GlobalVariables.cameraFOV;
        _vCam.m_Lens.FieldOfView = Mathf.Lerp(_vCam.m_Lens.FieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
    }

    void EnterAim() => aiming = true;
    void ExitAim() => aiming = false;

    protected virtual void HandleFire()
    {
        if (isReloading)
            return;

        if (_input.fire && _fireCooldown <= 0f)
        {
            if (currentAmmo <= 0)
                return;
            
            EmitNoiseServerRpc(transform.position, loudness, "gunshot");
            CreateBulletServerRpc();

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
            _audioSource.Play();
    }

    void HandleReload()
    {
        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0f)
                FinishReload();
            _input.reload = false;
            return;
        }

        if (_input.reload && currentAmmo < maxAmmo)
        {
            StartReload();
            _input.reload = false;
        }
    }

    void StartReload()
    {
        ItemInstance trueAmmoInstance = _owner.GetInventory().GetInstance(fauxAmmoInstance);
        if (trueAmmoInstance.Equals(default))
            return;

        isReloading = true;
        reloadTimer = reloadDuration;
    }

    void FinishReload()
    {
        isReloading = false;

        ItemInstance trueAmmoInstance = _owner.GetInventory().GetInstance(fauxAmmoInstance);
        if (trueAmmoInstance.Equals(default))
            return;

        int needed = maxAmmo - currentAmmo;
        int removed = _owner.GetInventory().RemoveItem(trueAmmoInstance, needed);

        currentAmmo += removed;

        OnAmmoChanged?.Invoke(GetCurrentAmmo(), GetReserveAmmo());
    }

    [ServerRpc(RequireOwnership = false)]
    public void CreateBulletServerRpc(ServerRpcParams rpcParams = default)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        bullet.speed = bulletSpeed;
        bullet.playerClientId = rpcParams.Receive.SenderClientId;

        bulletObj.GetComponent<NetworkObject>().Spawn();
    }

    private void SelectAmmoFromInventory()
    {
        List<ItemInstance> ammoInstances = _owner.GetInventory().GetAmmoInstances();
        foreach (ItemInstance ammoInstance in ammoInstances)
        {
            AmmoDefinition ammoDef = ItemDatabase.Instance.GetItem(ammoInstance.key) as AmmoDefinition;
            if (ammoDef == null)
                continue;
            if (acceptedAmmoType == ammoDef.ammoType)
            {
                fauxAmmoInstance = ammoInstance;
            }
        }
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
        ItemInstance ammo = _owner.GetInventory().GetInstance(fauxAmmoInstance);
        return ammo.Equals(default) ? 0 : ammo.stackSize;
    }
}
