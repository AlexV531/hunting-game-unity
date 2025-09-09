using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 320f;           // Bullet speed
    public float lifetime = 5f;         // How long the bullet exists
    public float gravity = 9.81f;       // Gravity strength
    public float power_factor = 1.0f;
    public float bleed_factor = 1.0f;
    public float heal_prevention = 1.0f;

    private Vector3 velocity;
    private int layerMask;

    private void Start()
    {
        velocity = transform.forward * speed;
        layerMask = ~LayerMask.GetMask("Interactable");
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        float delta = Time.deltaTime;

        // Apply gravity
        velocity += Vector3.down * gravity * delta;

        // Calculate distance to move this frame
        Vector3 move = velocity * delta;

        // Raycast to detect collision
        if (Physics.Raycast(transform.position, move.normalized, out RaycastHit hit, move.magnitude, layerMask))
        {
            // Check if we hit an organ
            Internal internalHit = hit.collider.GetComponent<Internal>();
            if (internalHit != null)
            {
                Animal animal = internalHit.animal;
                if (animal != null)
                {
                    // Call the ProjectileHit method
                    animal.ProjectileHit(
                        globalHitPos: hit.point,
                        direction: velocity.normalized,
                        internalHit: internalHit,
                        power: 6.0f,
                        bulletStrength: power_factor,
                        bulletBleed: bleed_factor,
                        bulletHeal: heal_prevention
                    );
                }
            }
            // If not an animal hit
            else
            {
                // Debug.Log("Setting debug target to " + hit.point);
                GlobalVariables.debugTarget = hit.point;
            }

            // Destroy bullet on impact
            // Debug.Log("Bullet hit: " + hit.point);
            Destroy(gameObject);
            return;
        }

        // Move bullet
        transform.position += move;

        // Rotate bullet to face movement direction
        if (velocity != Vector3.zero)
            transform.forward = velocity.normalized;
    }
}
