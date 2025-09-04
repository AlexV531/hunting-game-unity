using UnityEngine;
using System.Collections.Generic;

public class Animal : MonoBehaviour
{
    public float health = 100f;
    public float maxHealth = 100f;
    public List<HitData> hits = new List<HitData>();
    public float distDamageFactor = 0.5f; // How much the distance the bullet travels through the internal affects the damage
    public Transform markerPrefab; // optional, for PlaceMarker visualization
    public float animalBleedFactor = 0.1f;
    public float animalHealFactor = 0.1f;
    public AnimalStateManager fsm;

    LayerMask layerMask;

    void Awake()
    {
        layerMask = LayerMask.GetMask("Internal");
    }

    void Start()
    {
        fsm = GetComponent<AnimalStateManager>();
    }

    private void Update()
    {
        // Dead check
        if (IsDead())
            return;

        if (GlobalVariables.debugTarget != Vector3.zero)
        {
            fsm.MovingState.ClearTargets();
            fsm.MovingState.AddTarget(GlobalVariables.debugTarget);
            fsm.ChangeState(fsm.MovingState);
            GlobalVariables.debugTarget = Vector3.zero;
        }

        // Bleed section
        foreach (var hit in hits)
        {
            if (health <= 0f)
                break;

            if (hit.bleedRate <= 0f)
            {
                hit.bleedRate = 0f;
                continue;
            }

            // Apply bleed damage
            health = Mathf.Clamp(
                health - hit.bleedRate * animalBleedFactor * Time.deltaTime,
                0f,
                maxHealth
            );

            // Reduce bleed rate by healing
            hit.bleedRate -= hit.healRate * animalHealFactor * Time.deltaTime;
        }

        if (health <= 0f)
            KillAnimal();
    }

    public void ProjectileHit(Vector3 globalHitPos, Vector3 direction, Internal internalHit,
        float power = 6f, float bulletStrength = 1f, float bulletBleed = 1f, float bulletHeal = 1f)
    {
        Debug.Log("Projectile hit!");

        // Check if scale is uniform
        Vector3 scale = transform.localScale;
        if (!Mathf.Approximately(scale.x, scale.y) || !Mathf.Approximately(scale.x, scale.z))
        {
            Debug.LogWarning("Animal scale not uniform, internal distance calculations may be off.");
        }

        // Create new HitData
        HitData hitData = new HitData
        {
            bulletStrength = bulletStrength,
            bulletBleed = bulletBleed,
            healRate = bulletHeal
        };

        Vector3 finalPoint = Vector3.zero;
        List<Internal> internalStack = new List<Internal>();
        List<Internal> internalsHit = new List<Internal>();

        Debug.Log("Adding " + internalHit.name + " from internal stack");
        internalStack.Add(internalHit);
        internalsHit.Add(internalHit);

        HitData.InternalHitData internalHitData = new HitData.InternalHitData(internalHit, power);
        hitData.AddInternalHitData(internalHitData);

        // Place marker at hit
        PlaceMarker(globalHitPos);
        hitData.AddIntersectionPoint(transform.InverseTransformPoint(globalHitPos));

        // Start raycast simulation
        float rayCastDist = 100f; // adjust as needed
        Vector3 globalRayOrigin = globalHitPos;
        Vector3 globalRayDir = direction.normalized;

        Internal newInternal = internalHit;

        while (newInternal != null)
        {
            // Offset the next ray origin slightly along the ray direction to avoid hitting the same collider
            float epsilon = 0.001f;
            globalRayOrigin += globalRayDir * epsilon;
            // Debug.DrawRay(globalRayOrigin, globalRayDir * rayCastDist, Color.red, 0.1f);
            // Debug.Log(globalRayOrigin + " | " + globalRayDir);
            if (Physics.Raycast(globalRayOrigin, globalRayDir, out RaycastHit hitInfo, rayCastDist, layerMask))
            {
                // Debug.Log("Next hit: " + hitInfo.collider);
                newInternal = hitInfo.collider.GetComponent<Internal>();
                if (newInternal != null)
                {
                    // Debug.Log("Internal stack: " + internalStack);
                    Vector3 nextHitPos = transform.InverseTransformPoint(hitInfo.point);
                    float internalDist = Vector3.Distance(transform.InverseTransformPoint(globalRayOrigin), nextHitPos) * scale.x;
                    float strength = internalStack[internalStack.Count - 1].strength;

                    hitData.AddToHitDist(internalStack[internalStack.Count - 1], internalDist);

                    float remainingPower = power - (internalDist * strength);
                    if (remainingPower <= 0)
                    {
                        finalPoint = transform.InverseTransformPoint(globalRayOrigin) + (transform.InverseTransformDirection(globalRayDir) * (power / strength));
                        PlaceMarker(transform.TransformPoint(finalPoint));
                        hitData.AddIntersectionPoint(finalPoint);
                        AddHit(hitData);
                        power = 0f;
                        Debug.Log("Projectile stopped at point: " + finalPoint);
                        break;
                    }

                    power = remainingPower;
                    PlaceMarker(transform.TransformPoint(nextHitPos));
                    hitData.AddIntersectionPoint(nextHitPos);

                    if (internalStack.Contains(newInternal))
                    {
                        Debug.Log("Removing " + newInternal.name + " from internal stack");
                        internalStack.RemoveAt(internalStack.Count - 1);
                    }
                    else
                    {
                        Debug.Log("Adding " + newInternal.name + " from internal stack");
                        internalStack.Add(newInternal);
                        if (!internalsHit.Contains(newInternal))
                        {
                            internalsHit.Add(newInternal);
                            HitData.InternalHitData newInternalHitData = new HitData.InternalHitData(newInternal, power);
                            hitData.AddInternalHitData(newInternalHitData);
                        }
                    }

                    if (internalStack.Count == 0)
                        break;

                    globalRayOrigin = transform.TransformPoint(nextHitPos);
                    // rayDir = rayDir; // unchanged; handle ricochet logic if needed
                }
                else
                {
                    break; // exit if raycast did not hit an Internal
                }
            }
            else
            {
                break; // exit if raycast misses
            }
        }

        if (finalPoint == Vector3.zero)
        {
            Debug.Log("Projectile exited internals with power: " + power);
            AddHit(hitData);
        }
    }

    private void AddHit(HitData hitData)
    {
        hits.Add(hitData);
        DoDamage(hitData);
    }

    private void DoDamage(HitData hitData)
    {
        foreach (var internalHit in hitData.internalsHit)
        {
            float initialDamage = Mathf.Clamp(
                ((internalHit.hitWithPower * (internalHit.hitDist / distDamageFactor)) * hitData.bulletStrength)
                / internalHit.internalPart.internalStrength, 0f, 1f) * internalHit.internalPart.lethality;

            health = Mathf.Clamp(health - initialDamage, 0f, maxHealth);

            float bleedInflicted = Mathf.Clamp(
                ((internalHit.hitWithPower * (internalHit.hitDist / distDamageFactor)) * hitData.bulletBleed)
                / internalHit.internalPart.bleedFactor, 0f, 1f) * internalHit.internalPart.lethality;

            hitData.bleedRate = bleedInflicted;

            Debug.Log($"Internal: {internalHit.internalPart.name} Health reduced by: {initialDamage}, Bleed inflicted: {bleedInflicted}");
        }

        Debug.Log("Total health remaining: " + health);

        if (health <= 0f)
            KillAnimal();
    }

    private void PlaceMarker(Vector3 position)
    {
        if (markerPrefab != null)
            Instantiate(markerPrefab, position, Quaternion.identity, transform);
    }

    private void KillAnimal()
    {
        Debug.Log($"{name} has died.");
        // Add death logic here
    }
    
    private bool IsDead()
    {
        return health <= 0f;
    }
}
