using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public float speed = 320f; // Bullet speed
    public float lifetime = 5f; // How long the bullet exists
    public float gravity = 9.81f; // Gravity strength
    public float power_factor = 1.0f;
    public float bleed_factor = 1.0f;
    public float heal_prevention = 1.0f;
    public ulong playerClientId;

    private Vector3 velocity;
    private int layerMask;

    void Start()
    {
        if (!IsServer)
            return;

        velocity = transform.forward * speed;
        layerMask = ~LayerMask.GetMask("Interactable", "AnimalPhysics");
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!IsServer)
            return;

        float delta = Time.deltaTime;

        // Apply gravity
        velocity += Vector3.down * gravity * delta;

        // Calculate distance to move this frame
        Vector3 move = velocity * delta;

        // VERY IMPORTANT LINE DO NOT REMOVE
        Physics.SyncTransforms();

        // Raycast to detect collision
        if (Physics.Raycast(transform.position, move.normalized, out RaycastHit hit, move.magnitude, layerMask))
        {
            // Check if we hit an organ
            Internal internalHit = hit.collider.GetComponent<Internal>();
            if (internalHit != null)
            {
                Debug.Log($"[Bullet] Hit at time: {Time.time}, frame: {Time.frameCount}");
                Debug.Log($"[Bullet] Calling ApplyProjectileHit immediately");

                Animal animal = internalHit.animal;
                if (animal != null)
                {
                    animal.ApplyProjectileHit(
                        globalHitPos: hit.point,
                        direction: velocity.normalized,
                        internalId: internalHit.internalId,
                        playerClientId: playerClientId,
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
                Debug.Log("Hit point: " + hit.point);
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
