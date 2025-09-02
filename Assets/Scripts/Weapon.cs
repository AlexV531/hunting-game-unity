using UnityEngine;

public class Weapon : MonoBehaviour
{
    private PlayerInputs playerInputs; // Reference to central input hub

    [Header("Weapon Settings")]
    public Transform model;            // assign your model in Inspector
    public Transform aimPosition;      // where the weapon moves to when aiming
    public Transform hipPosition;      // default position

    public float aimSpeed = 10f;       // speed to move weapon between positions

    void Awake()
    {
        // Finds PlayerInputs on parent
        playerInputs = GetComponentInParent<PlayerInputs>();
    }

    void Update()
    {
        HandleAim();
        HandleFire();
    }

    void HandleAim()
    {
        if (!model || !aimPosition || !hipPosition) return;

        Transform targetPos = playerInputs.aim ? aimPosition : hipPosition;
        model.localPosition = Vector3.Lerp(model.localPosition, targetPos.localPosition, aimSpeed * Time.deltaTime);
        model.localRotation = Quaternion.Slerp(model.localRotation, targetPos.localRotation, aimSpeed * Time.deltaTime);
    }

    void HandleFire()
    {
        if (playerInputs.fire)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        // Implement firing logic here: raycast, projectile, sound, recoil, etc.
        Debug.Log("Bang!");
    }
}
