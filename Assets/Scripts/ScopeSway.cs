using UnityEngine;

[RequireComponent(typeof(Weapon))]
public class ScopeSway : MonoBehaviour
{
    [Header("Sway Settings")]
    public float swayAmount = 0.002f;       // How far it sways
    public float swaySpeed = 1.5f;         // How fast it sways
    public float returnSpeed = 2f;         // How fast it recenters

    private Vector3 initialPos;
    private Weapon weapon;

    void Start()
    {
        initialPos = transform.localPosition;
        weapon = GetComponent<Weapon>();
    }

    void Update()
    {
        if (weapon.IsAiming())
        {
            // Calculate sway with sine waves
            float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
            float swayY = Mathf.Cos(Time.time * swaySpeed * 0.8f) * swayAmount;

            Vector3 targetPos = initialPos + new Vector3(swayX, swayY, 0);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * returnSpeed);
        }
        else
        {
            // Snap back when not aiming
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialPos, Time.deltaTime * returnSpeed);
        }
    }
}