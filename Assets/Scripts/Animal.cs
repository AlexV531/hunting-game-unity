using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System;

public class Animal : NetworkBehaviour
{
    [Header("Animal Stats")]
    public float health = 100f;
    public float maxHealth = 100f;
    public float animalBleedFactor = 0.1f;
    public float animalHealFactor = 0.1f;
    public float distDamageFactor = 0.5f;
    public float hitNoiseLoudness = 60f;

    [Header("Blood Splatter")]
    public GameObject bloodSplatterPrefab;
    public float bloodSplatterLifetime = 180f;
    public float bleedDamageUntilSplatter = 2f;

    [Header("Animal Components")]
    public AnimalAI animalAI;
    public Corpse corpseInteractable;
    public Antler antler;
    public GameObject internalContainer;
    public Internal[] internals;
    public Transform bottom;
    public Transform markerPrefab;

    private bool isDead = false;
    private float bleedDamageSinceLastSplatter = 0f;

    private Dictionary<int, Internal> internalLookup;
    private LayerMask layerMask;
    private BalloonAttach balloonAttach;

    public List<HitData> hits = new List<HitData>();

    void Awake()
    {
        layerMask = LayerMask.GetMask("Internal");
        animalAI = GetComponent<AnimalAI>();
        internalLookup = new Dictionary<int, Internal>();
        foreach (var internalOrg in internals)
        {
            internalLookup[internalOrg.internalId] = internalOrg;
        }
        balloonAttach = GetComponent<BalloonAttach>();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (IsDead())
            return;

        foreach (var hit in hits)
        {
            if (health <= 0f)
                break;

            if (hit.bleedRate <= 0f)
            {
                hit.bleedRate = 0f;
                continue;
            }

            float bleedDamage = hit.bleedRate * animalBleedFactor * Time.deltaTime;
            bleedDamageSinceLastSplatter += bleedDamage;
            if (health - bleedDamage < 0)
            {
                hit.bleedDamageDone += health;
            }
            else
            {
                hit.bleedDamageDone += bleedDamage;
            }
            health = Mathf.Clamp(health - bleedDamage, 0f, maxHealth);

            hit.bleedRate -= hit.healRate * animalHealFactor * Time.deltaTime;
        }

        if (bleedDamageSinceLastSplatter >= bleedDamageUntilSplatter)
        {
            SpawnBloodSplatter();
            bleedDamageSinceLastSplatter = 0;
        }

        if (health <= 0f)
            KillAnimal();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyProjectileHitServerRpc(
        Vector3 globalHitPos,
        Vector3 direction,
        int internalId,
        Vector3 animalPos,
        Quaternion animalRot,
        float power = 6f,
        float bulletStrength = 1f,
        float bulletBleed = 1f,
        float bulletHeal = 1f,
        ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        ProjectileHit(globalHitPos, direction, GetInternalById(internalId), senderClientId,
            power, bulletStrength, bulletBleed, bulletHeal);
    }

    public void ApplyProjectileHit(
        Vector3 globalHitPos,
        Vector3 direction,
        int internalId,
        ulong playerClientId,
        float power = 6f,
        float bulletStrength = 1f,
        float bulletBleed = 1f,
        float bulletHeal = 1f)
    {
        ProjectileHit(globalHitPos, direction, GetInternalById(internalId), playerClientId,
            power, bulletStrength, bulletBleed, bulletHeal);
    }

    public void ProjectileHit(
        Vector3 globalHitPos,
        Vector3 direction,
        Internal internalHit,
        ulong playerClientId,
        float power = 6f,
        float bulletStrength = 1f,
        float bulletBleed = 1f,
        float bulletHeal = 1f)
    {
        Debug.Log("Projectile hit!");

        EmitAnimalHitNoise();

        Vector3 scale = transform.localScale;
        if (!Mathf.Approximately(scale.x, scale.y) || !Mathf.Approximately(scale.x, scale.z))
        {
            Debug.LogWarning("Animal scale not uniform, internal distance calculations may be off.");
        }
        Debug.Log("Whose shot hit: " + playerClientId);
        HitData hitData = new HitData
        {
            playerClientId = playerClientId,
            bulletStrength = bulletStrength,
            bulletBleed = bulletBleed,
            healRate = bulletHeal
        };

        Vector3 finalPoint = Vector3.zero;
        List<Internal> internalStack = new List<Internal>();
        List<Internal> internalsHit = new List<Internal>();

        internalStack.Add(internalHit);
        internalsHit.Add(internalHit);
        hitData.AddInternalHitData(new HitData.InternalHitData(internalHit, power));

        Vector3 localHitPos = transform.InverseTransformPoint(globalHitPos);
        // PlaceMarker(localHitPos);
        PlaceMarkerClientRpc(localHitPos);
        hitData.AddIntersectionPoint(localHitPos);

        float rayCastDist = 100f;
        Vector3 globalRayOrigin = globalHitPos;
        Vector3 globalRayDir = direction.normalized;
        Internal newInternal = internalHit;

        while (newInternal != null)
        {
            float epsilon = 0.001f;
            globalRayOrigin += globalRayDir * epsilon;

            if (Physics.Raycast(globalRayOrigin, globalRayDir, out RaycastHit hitInfo, rayCastDist, layerMask))
            {
                newInternal = hitInfo.collider.GetComponent<Internal>();
                if (newInternal != null)
                {
                    Vector3 nextHitPos = transform.InverseTransformPoint(hitInfo.point);
                    float internalDist = Vector3.Distance(transform.InverseTransformPoint(globalRayOrigin), nextHitPos) * scale.x;
                    float strength = internalStack[internalStack.Count - 1].strength;

                    hitData.AddToHitDist(internalStack[internalStack.Count - 1], internalDist);

                    float remainingPower = power - (internalDist * strength);
                    if (remainingPower <= 0)
                    {
                        finalPoint = transform.InverseTransformPoint(globalRayOrigin) + (transform.InverseTransformDirection(globalRayDir) * (power / strength));
                        // PlaceMarker(finalPoint);
                        PlaceMarkerClientRpc(finalPoint);
                        hitData.AddIntersectionPoint(finalPoint);
                        AddHit(hitData);
                        power = 0f;
                        break;
                    }

                    power = remainingPower;
                    // PlaceMarker(nextHitPos);
                    PlaceMarkerClientRpc(nextHitPos);
                    hitData.AddIntersectionPoint(nextHitPos);

                    if (internalStack.Contains(newInternal))
                    {
                        internalStack.RemoveAt(internalStack.Count - 1);
                    }
                    else
                    {
                        internalStack.Add(newInternal);
                        if (!internalsHit.Contains(newInternal))
                        {
                            internalsHit.Add(newInternal);
                            hitData.AddInternalHitData(new HitData.InternalHitData(newInternal, power));
                        }
                    }

                    if (internalStack.Count == 0)
                        break;

                    globalRayOrigin = hitInfo.point;
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        if (finalPoint == Vector3.zero)
        {
            AddHit(hitData);
        }

        animalAI?.OnHit(direction.normalized);
    }

    private void SpawnBloodSplatter()
    {
        int randomSeed = UnityEngine.Random.Range(1, 10000);
        SpawnBloodSplatterClientRpc(bottom.transform.position, randomSeed);
    }

    [ClientRpc]
    private void SpawnBloodSplatterClientRpc(Vector3 position, int seed)
    {
        if (bloodSplatterPrefab == null) return;

        System.Random rng = new System.Random(seed);

        Quaternion baseRotation = Quaternion.Euler(90f, 0f, 0f);

        float randomAngle = RandomUtil.RandomRangeFloat(rng, 0f, 360f);
        Quaternion randomRotation = Quaternion.AngleAxis(randomAngle, Vector3.forward);

        Quaternion finalRotation = baseRotation * randomRotation;

        GameObject decal = Instantiate(bloodSplatterPrefab, position, finalRotation);

        Destroy(decal, bloodSplatterLifetime);
    }

    public bool IsDead()
    {
        return isDead;
    }

    public Internal GetInternalById(int id)
    {
        internalLookup.TryGetValue(id, out var internalOrg);
        return internalOrg;
    }

    [ClientRpc]
    public void DisableInternalCollidersClientRpc()
    {
        foreach (var internalPart in internals)
        {
            Collider col = internalPart.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
        }
    }

    [ClientRpc]
    public void EnableInternalCollidersClientRpc()
    {
        foreach (var internalPart in internals)
        {
            Collider col = internalPart.GetComponent<Collider>();
            if (col != null)
                col.enabled = true;
        }
    }

    public void SetVisualsEnabled(bool enable)
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = enable;

        foreach (var collider in GetComponentsInChildren<Collider>())
            collider.enabled = enable;
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

            if (health - initialDamage < 0)
            {
                hitData.initialDamageDone += health;
            }
            else
            {
                hitData.initialDamageDone += initialDamage;
            }
            health = Mathf.Clamp(health - initialDamage, 0f, maxHealth);

            float bleedInflicted = Mathf.Clamp(
                ((internalHit.hitWithPower * (internalHit.hitDist / distDamageFactor)) * hitData.bulletBleed)
                / internalHit.internalPart.bleedFactor, 0f, 1f) * internalHit.internalPart.lethality;

            hitData.bleedRate = bleedInflicted;

            Debug.Log($"Internal: {internalHit.internalPart.name} Health reduced by: {initialDamage}, Bleed inflicted: {bleedInflicted}");
        }

        SpawnBloodSplatter();

        Debug.Log("Total health remaining: " + health);

        if (health <= 0f)
            KillAnimal();
    }

    [ClientRpc]
    private void PlaceMarkerClientRpc(Vector3 localPosition)
    {
        if (markerPrefab != null)
            Instantiate(markerPrefab, transform.TransformPoint(localPosition), Quaternion.identity, internalContainer.transform);
    }

    private void EmitAnimalHitNoise()
    {
        if (animalAI.IsPanicked())
            return;
        NoiseEvent noiseEvent = new NoiseEvent(transform.position, hitNoiseLoudness, "Animal hit noise");
        NoiseManager.Instance.EmitNoise(noiseEvent);
    }

    private void KillAnimal()
    {
        if (!IsServer || IsDead())
            return;
        isDead = true;
        Debug.Log($"{name} has died.");
        corpseInteractable.SetInteractionEnabledServerRpc(true);
        if (animalAI != null)
        {
            animalAI.animator.SetTrigger("dead");
            animalAI.fsm.ChangeState(animalAI.fsm.DeadState);
            animalAI.agent.enabled = false;
            if (animalAI.herd != null)
                animalAI.herd.UnregisterHerdAnimal(animalAI);
        }

        FirstPersonController killCredit = DetermineKillCredit(hits);
        if (killCredit != null)
        {
            killCredit.AddKillServerRpc();
        }
    }

    private static FirstPersonController DetermineKillCredit(List<HitData> hits)
    {
        if (hits == null || hits.Count == 0)
            return null;

        Dictionary<ulong, float> playerDamage = new Dictionary<ulong, float>();

        foreach (HitData hit in hits)
        {
            if (hit == null)
                continue;

            float totalDamage = 0f;
            foreach (var internalHit in hit.internalsHit)
            {
                totalDamage += internalHit.hitWithPower;
            }

            if (playerDamage.ContainsKey(hit.playerClientId))
                playerDamage[hit.playerClientId] += totalDamage;
            else
                playerDamage[hit.playerClientId] = totalDamage;
        }

        FirstPersonController topPlayer = null;
        float maxDamage = 0f;

        foreach (var kvp in playerDamage)
        {
            if (kvp.Value > maxDamage)
            {
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(kvp.Key, out var client))
                {
                    topPlayer = client.PlayerObject.GetComponent<FirstPersonController>();
                    maxDamage = kvp.Value;
                }
            }
        }

        return topPlayer;
    }

    public BalloonAttach GetBalloonAttach() => balloonAttach;

    public List<HitDataStrings> GetHitData()
    {
        List<HitDataStrings> hitDataStringArrays = new List<HitDataStrings>();
        foreach (HitData hit in hits)
        {
            HitDataStrings hitDataStrings = new HitDataStrings();
            NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[hit.playerClientId].PlayerObject;
            FirstPersonController player = playerObject.GetComponent<FirstPersonController>();
            if (player != null)
                hitDataStrings.string1 = "Shot by " + player.PlayerName.Value;
            else
                hitDataStrings.string1 = "Shot by null";
            hitDataStrings.string2 = Math.Round((hit.initialDamageDone + hit.bleedDamageDone) / maxHealth * 100) + "% - " + Math.Round(hit.initialDamageDone) + " initial - " + Math.Round(hit.bleedDamageDone) + " bleed";
            string internalHitDataString = "Hit:";
            foreach (HitData.InternalHitData internalHit in hit.internalsHit)
            {
                internalHitDataString += " " + internalHit.internalPart.name;
            }
            hitDataStrings.string3 = internalHitDataString;
            hitDataStringArrays.Add(hitDataStrings);
        }
        return hitDataStringArrays;
    }
}