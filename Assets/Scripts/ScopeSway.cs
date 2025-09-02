using UnityEngine;

public class ScopeSway : MonoBehaviour
{
    [Header("Sway Settings")]
    public float swayAmount = 0.02f;       // How far it sways
    public float swaySpeed = 1.5f;         // How fast it sways
    public float returnSpeed = 2f;         // How fast it recenters
    public bool aiming = false;            // Toggle this when aiming

    private Vector3 initialPos;

    void Start()
    {
        initialPos = transform.localPosition;
    }

    void Update()
    {
        if (aiming)
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