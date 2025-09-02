using UnityEngine;

public class Weapon : MonoBehaviour
{
    private PlayerInputs _input; // Reference to central input hub

    [Header("Weapon Settings")]
    public Transform model;            // assign your model in Inspector
    public Transform aimPosition;      // where the weapon moves to when aiming
    public Transform hipPosition;      // default position

    public float aimSpeed = 10f;       // speed to move weapon between positions

    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 60f;

    void Awake()
    {
        // Finds PlayerInputs on parent
        _input = GetComponentInParent<PlayerInputs>();
    }

    void Update()
    {
        HandleAim();
        HandleFire();
    }

    void HandleAim()
    {
        if (!model || !aimPosition || !hipPosition) return;

        Transform targetPos = _input.aim ? aimPosition : hipPosition;
        model.localPosition = Vector3.Lerp(model.localPosition, targetPos.localPosition, aimSpeed * Time.deltaTime);
        model.localRotation = Quaternion.Slerp(model.localRotation, targetPos.localRotation, aimSpeed * Time.deltaTime);
    }

    void HandleFire()
    {
        if (_input.fire)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        _input.fire = false;
        GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        bullet.speed = bulletSpeed;
    }
}
