using UnityEngine;

public class ScopeSway : MonoBehaviour
{
    [Header("Sway Settings")]
    public float swayAmount = 0.5f;      // Max sway in degrees
    public float swaySpeed = 1.5f;       // Sway cycle speed
    public float returnSpeed = 2f;       // How fast it recenters

    [Header("Steady Aim")]
    [Range(0f, 1f)] public float steadyMultiplier = 0.2f;  // Sway scale when steady
    public float steadyTransitionSpeed = 5f;               // Smooth blend speed

    [SerializeField] private FirstPersonController _player;
    [SerializeField] private PlayerInputs _inputs;

    private Quaternion initialRotation;
    private float currentMultiplier = 1f; // Smoothly transitions between normal and steady

    void Start()
    {
        initialRotation = transform.localRotation;
    }

    void Update()
    {
        if (_player.equippedWeapon.IsAiming())
        {
            // Check if player is holding steady aim
            float targetMultiplier = _inputs.steadyAim ? steadyMultiplier : 1f;

            // Smooth transition between multipliers
            currentMultiplier = Mathf.Lerp(
                currentMultiplier,
                targetMultiplier,
                Time.deltaTime * steadyTransitionSpeed
            );

            // Breathing sway
            float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmount * currentMultiplier;
            float swayY = Mathf.Cos(Time.time * swaySpeed * 0.8f) * swayAmount * currentMultiplier;

            Quaternion targetRot = initialRotation * Quaternion.Euler(swayY, swayX, 0);
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetRot,
                Time.deltaTime * returnSpeed
            );
        }
        else
        {
            // Reset to neutral
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                initialRotation,
                Time.deltaTime * returnSpeed
            );

            // Reset multiplier when not aiming
            currentMultiplier = 1f;
        }
    }
}