using UnityEngine;
using UnityEngine.Rendering.Universal;
using Cinemachine;

public class Weapon : MonoBehaviour
{
    private PlayerInputs _input; // Reference to central input hub
    private CameraRecoil _recoil;

    [Header("Weapon Settings")]
    public Transform model;            // assign your model in Inspector
    public Transform aimPosition;      // where the weapon moves to when aiming
    public Transform hipPosition;      // default position

    public float aimSpeed = 10f;       // speed to move weapon between positions

    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 60f;

    [Header("Zoom Settings")]
    public CinemachineVirtualCamera vCam;
    public float scopedFOV = 20f;          // FOV when scoped
    public float zoomSpeed = 10f;          // FOV transition speed

    [Header("Shadow & LOD Settings")]
    public UniversalRenderPipelineAsset urpAsset;   // Assigned URP Asset
    public float scopedShadowDistance = 500f;       // Shadow distance when scoped
    public float normalShadowDistance = 100f;       // Default shadow distance
    public float scopedLODBias = 2f;                // LOD bias when scoped
    public float normalLODBias = 1f;      

    [Tooltip("Time in seconds between shots")]
    public float fireRate = 0.2f;

    [Tooltip("If true, holding the fire button will shoot automatically")]
    public bool automaticFire = true;

    public int maxAmmo = 3;
    public int reserveAmmo = 100;

    private int currentAmmo = 3;
    private float _fireCooldown = 0f;
    private bool aiming = false;

    void Awake()
    {
        // Finds PlayerInputs on parent
        _input = GetComponentInParent<PlayerInputs>();
        _recoil = GetComponentInParent<CameraRecoil>();
    }

    void Update()
    {
        // reduce cooldown each frame
        if (_fireCooldown > 0f)
            _fireCooldown -= Time.deltaTime;

        HandleAim();
        HandleFire();
        HandleReload();
        HandleZoom();
    }

    void HandleAim()
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
        if (vCam != null)
        {
            float targetFOV = _input.aim ? scopedFOV : GlobalVariables.cameraFOV;
            vCam.m_Lens.FieldOfView = Mathf.Lerp(vCam.m_Lens.FieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
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

    void HandleFire()
    {
        if (_input.fire && _fireCooldown <= 0f)
        {
            if (currentAmmo <= 0)
            {
                return;
            }
            Shoot();
            NoiseEvent noise = new NoiseEvent(transform.position, 20f, "gunshot");
            NoiseManager.EmitNoise(noise);
            _recoil.AddRecoil(50f, 1f);
            currentAmmo--;
            _fireCooldown = fireRate;

            if (!automaticFire)
                _input.fire = false;
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
}
