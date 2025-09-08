using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class AnimalAI : MonoBehaviour, INoiseListener
{
    public Animal animal;
    public AnimalStateManager fsm;
    public Animator animator;
    public NavMeshAgent agent;
    public float HearingThreshold => 0.5f;
    public float fleeAngleSpread = 45f; // degrees
    public float fleeDistance = 50f;
    public Terrain terrain;
    public Herd herd;
    public Vector3 MostRecentSoundPosition;
    public float soundHeard = 0f;
    public float alertTolerance = 3f;
    public float panicTolerance = 10f;
    public float soundForgetRate = 0.05f;
    public float sightRange = 20f;
    public float sightAngle = 160f; // degrees
    public float panicCooldown = 3f; // seconds
    private float lastPanicTime = -Mathf.Infinity;

    void Awake()
    {
        animal = GetComponent<Animal>();
        fsm = GetComponent<AnimalStateManager>();
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        fsm = GetComponent<AnimalStateManager>();
        NoiseManager.RegisterListener(this);
        if (herd != null)
        {
            herd.RegisterHerdAnimal(this);
        }
    }

    private void Update()
    {
        // Dead check
        if (animal.IsDead())
            return;

        animator.SetFloat("speed", GetCurrentVelocity());

        if (soundHeard > 0f)
        {
            soundHeard = Mathf.Clamp(soundHeard - soundForgetRate * Time.deltaTime, 0f, 100f);
        }

        List<GameObject> visiblePlayers = GetVisiblePlayers();
        if (visiblePlayers.Count > 0)
        {
            BecomePanicked(visiblePlayers[0].transform.position);
        }
    }

    public void OnNoiseHeard(NoiseEvent noiseEvent, float perceivedVolume)
    {
        if (animal.IsDead())
            return;

        MostRecentSoundPosition = noiseEvent.position;
        soundHeard += perceivedVolume;
        Debug.Log("Heard noise at " + noiseEvent.position + " with volume " + perceivedVolume + " making sounds heard total " + soundHeard);

        if (soundHeard >= panicTolerance)
        {
            BecomePanicked(noiseEvent.position);
        }
        else if (soundHeard >= alertTolerance)
        {
            BecomeAlert();
        }
    }

    public void BecomeAlert()
    {
        fsm.ChangeState(fsm.ListeningState);
    }

    public void BecomePanicked(Vector3 panicSource)
    {
        // Check panic cooldown
        if (Time.time < lastPanicTime + panicCooldown)
            return;

        lastPanicTime = Time.time;
        if (herd != null)
        {
            herd.HerdFleeTo(ChooseEscapePositions(panicSource));
        }
        else
        {
            SetFleeing(ChooseEscapePositions(panicSource));
        }
    }

    public void SetFleeing(List<Vector3> target_list)
    {
        if (animal.IsDead())
            return;

        fsm.FleeingState.ClearTargets();
        for (int i = 0; i < target_list.Count; i++)
        {
            fsm.FleeingState.AddTarget(target_list[i]);
        }
        fsm.ChangeState(fsm.FleeingState);
    }

    public List<Vector3> ChooseEscapePositions(Vector3 panicSource, int numTargets = 3, int maxAttempts = 10)
    {
        Vector3 deerPos = transform.position;
        List<Vector3> targetQueue = new List<Vector3>();

        TerrainData tData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        for (int j = 0; j < numTargets; j++)
        {
            Vector3 escapeDir = (deerPos - panicSource).normalized;
            bool targetFound = false;

            // First try within angle spread
            for (int i = 0; i < maxAttempts; i++)
            {
                float angleOffset = Random.Range(-fleeAngleSpread, fleeAngleSpread);
                Vector3 rotatedDir = Quaternion.AngleAxis(angleOffset, Vector3.up) * escapeDir;
                Vector3 target = deerPos + rotatedDir * fleeDistance;

                if (target.x >= GlobalVariables.mapMin.x && target.x <= GlobalVariables.mapMax.x &&
                    target.z >= GlobalVariables.mapMin.z && target.z <= GlobalVariables.mapMax.z)
                {
                    target.y = GlobalVariables.GetTerrainHeightAtWorldPos(target, tData, terrainPos);
                    // Debug.Log("within spread");
                    targetQueue.Add(target);

                    panicSource = deerPos;
                    deerPos = target;
                    targetFound = true;
                    break;
                }
            }

            if (targetFound)
                continue;

            // If no valid target found, expand to full 360
            for (int i = 0; i < maxAttempts; i++)
            {
                float angleOffset = Random.Range(-180f, 180f); // full circle
                Vector3 rotatedDir = Quaternion.AngleAxis(angleOffset, Vector3.up) * escapeDir;
                Vector3 target = deerPos + rotatedDir * fleeDistance;

                if (target.x >= GlobalVariables.mapMin.x && target.x <= GlobalVariables.mapMax.x &&
                    target.z >= GlobalVariables.mapMin.z && target.z <= GlobalVariables.mapMax.z)
                {
                    target.y = GlobalVariables.GetTerrainHeightAtWorldPos(target, tData, terrainPos);
                    // Debug.Log("outside spread");
                    targetQueue.Add(target);

                    panicSource = deerPos;
                    deerPos = target;
                    targetFound = true;
                    break;
                }
            }

            if (targetFound)
                continue;

            // Fallback: clamp deer position inside map
            Vector3 clampedTarget = new Vector3(
                Mathf.Clamp(deerPos.x, GlobalVariables.mapMin.x, GlobalVariables.mapMax.x),
                GlobalVariables.GetTerrainHeightAtWorldPos(deerPos, tData, terrainPos),
                Mathf.Clamp(deerPos.z, GlobalVariables.mapMin.z, GlobalVariables.mapMax.z)
            );
            // Debug.Log("nowhere");
            targetQueue.Add(clampedTarget);
            targetFound = true;
            break; // Loop breaks if fallback is reached.
        }

        return targetQueue;
    }

    public List<GameObject> GetVisiblePlayers()
    {
        List<GameObject> visiblePlayers = new List<GameObject>();

        // Find all players (using tag instead of group)
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            Vector3 playerPos = player.transform.position;
            Vector3 toPlayer = (playerPos - transform.position).normalized;

            // Distance check
            float distance = Vector3.Distance(transform.position, playerPos);
            if (distance > sightRange)
                continue;

            // Angle check relative to forward
            float angleToPlayer = Vector3.Angle(transform.forward, toPlayer);
            if (angleToPlayer > sightAngle * 0.5f)
                continue;

            // Line-of-sight check
            int layerMask = ~LayerMask.GetMask("Internal");

            if (Physics.Raycast(transform.position, toPlayer, out RaycastHit hit, sightRange, layerMask))
{
                // Walk up parents until we find something tagged "Player"
                Transform hitTransform = hit.collider.transform;
                bool isPlayer = false;

                while (hitTransform != null)
                {
                    if (hitTransform.CompareTag("Player"))
                    {
                        isPlayer = true;
                        break;
                    }
                    hitTransform = hitTransform.parent;
                }

                if (!isPlayer)
                    continue; // blocked by something else
            }

            visiblePlayers.Add(player);
        }

        return visiblePlayers;
    }

    public float GetCurrentVelocity()
    {
        return agent.velocity.magnitude;
    }

    private void OnDestroy()
    {
        NoiseManager.UnregisterListener(this);
    }
}
