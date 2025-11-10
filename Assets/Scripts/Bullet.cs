using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public float speed = 320f; // Bullet speed
    public float lifetime = 5f; // How long the bullet exists
    public float gravity = 9.81f; // Gravity strength
    public float powerFactor = 1;
    public float bleedFactor = 1;
    public float healPrevention = 1;
    public float impactLoudness = 50;
    public ulong playerClientId;
    public GameObject bulletDecalPrefab;
    public float decalLifetime = 100f;

    private Vector3 velocity;
    private int layerMask;
    private TrailRenderer trail;
    private bool hasCollided = false;

    void Start()
    {
        if (!IsServer)
            return;

        trail = GetComponent<TrailRenderer>();
        velocity = transform.forward * speed;
        layerMask = ~LayerMask.GetMask("Interactable", "AnimalPhysics");
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!IsServer || hasCollided)
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
            hasCollided = true;

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
                        bulletStrength: powerFactor,
                        bulletBleed: bleedFactor,
                        bulletHeal: healPrevention
                    );
                }
            }
            // If not an animal hit
            else
            {
                Debug.Log("Hit point: " + hit.point);
                transform.position = hit.point;
                EmitNoise(hit.point, impactLoudness, "Bullet impacting the ground");
                PlayImpactSoundClientRpc();
                SpawnBulletDecal(hit);
            }

            // Destroy bullet on impact
            // Debug.Log("Bullet hit: " + hit.point);
            StartCoroutine(DestroyAfterTrail());
            return;
        }

        // Move bullet
        transform.position += move;

        // Rotate bullet to face movement direction
        if (velocity != Vector3.zero)
            transform.forward = velocity.normalized;
    }

    [ClientRpc]
    private void PlayImpactSoundClientRpc()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null) return;

        Debug.Log("Playing audio " + audioSource.clip.ToString());
        audioSource.Play();
    }

    void EmitNoise(Vector3 position, float loudness, string name)
    {
        NoiseEvent noiseEvent = new NoiseEvent(position, loudness, name);
        NoiseManager.Instance.EmitNoise(noiseEvent);
    }

    public void SpawnBulletDecal(RaycastHit hit)
    {
        if (!IsServer) return;

        int randomSeed = Random.Range(1, 10000);
        ulong parentId = 0;

        var networkObject = hit.collider.GetComponentInParent<NetworkObject>();
        if (networkObject != null)
            parentId = networkObject.NetworkObjectId;

        SpawnBulletDecalClientRpc(hit.point, hit.normal, parentId, randomSeed);
    }

    [ClientRpc]
    private void SpawnBulletDecalClientRpc(Vector3 hitPoint, Vector3 hitNormal, ulong parentId, int seed)
    {
        if (bulletDecalPrefab == null) return;

        System.Random rng = new System.Random(seed);

        GameObject decal = Instantiate(bulletDecalPrefab, hitPoint + hitNormal * 0.01f, Quaternion.identity);

        // Align decal perpendicular to surface
        decal.transform.rotation = Quaternion.LookRotation(-hitNormal);

        // Add variation
        decal.transform.Rotate(hitNormal, RandomUtil.RandomRangeFloat(rng, 0f, 360f), Space.World);

        // Parent if valid
        if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(parentId, out var parentObj))
        {
            decal.transform.SetParent(parentObj.transform);
        }

        Destroy(decal, decalLifetime);
    }

    private IEnumerator DestroyAfterTrail()
    {
        if (trail != null)
        {
            trail.emitting = false;
            yield return new WaitForSeconds(trail.time);
        }

        if (IsServer)
        {
            var netObj = GetComponent<NetworkObject>();
            if (netObj != null)
                netObj.Despawn();
            else
                Destroy(gameObject); // fallback
        }
    }
}
