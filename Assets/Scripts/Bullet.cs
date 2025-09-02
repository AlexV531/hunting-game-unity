using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 50f;           // Bullet speed
    public float lifetime = 5f;         // How long the bullet exists
    public float gravity = 9.81f;       // Gravity strength
    public int damage = 25;             // Damage to deal

    private Vector3 velocity;

    private void Start()
    {
        velocity = transform.forward * speed;
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
        if (Physics.Raycast(transform.position, move.normalized, out RaycastHit hit, move.magnitude))
        {
            // Check for animal
            // Animal animal = hit.collider.GetComponent<Animal>();
            // if (animal != null)
            // {
            //     animal.TakeDamage(damage);
            // }

            // Destroy bullet on impact
            Debug.Log("Bullet hit: " + transform.position);
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
