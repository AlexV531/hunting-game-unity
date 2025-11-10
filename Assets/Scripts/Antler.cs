using UnityEngine;
using System.Collections.Generic;
using static RandomUtil;

[RequireComponent(typeof(AntlerMeshGenerator))]
public class Antler : MonoBehaviour
{
    public AntlerMeshGenerator generator;
    public GameObject antlerMirror;

    [Header("Main Beam")]
    public int minMainSegments = 8;
    public int maxMainSegments = 12;
    public float mainLength = 1.2f;
    public float mainLengthVariation = 0.2f; // +/- variation as a fraction
    public float mainRadius = 0.1f;

    [Header("Secondary Beam")]
    [Range(0f, 1f)]
    public float secondaryBeamChance = 0.4f;
    public float secondaryBeamLengthMultiplier = 0.7f;
    public float secondaryBeamRadiusMultiplier = 0.8f;

    [Header("Tines")]
    public int minTines = 2;
    public int maxTines = 5;
    public float tineMaxLength = 0.7f;
    public float tineMaxRadius = 0.04f;
    public float tineMinRadius = 0.02f;

    [Header("Sub-Tines")]
    public int maxDepth = 2;

    [Header("Beam Curvature")]
    public float mainBeamCurvature = 0.22f;
    public float tineCurvature = 0.05f;

    public void Initialize(int seed, float age = 1.0f)
    {
        generator = GetComponent<AntlerMeshGenerator>();
        AntlerBranch main = GenerateMainBranch(seed, age);
        generator.GenerateAntler(main, antlerMirror);
        Debug.Log("Antlers generated with seed " + seed + " and age " + age);
    }

    AntlerBranch GenerateMainBranch(int seed, float age)
    {
        System.Random rng = new System.Random(seed);

        // Clamp age between 0.3 (young) and 1.5 (prime/old)
        age = Mathf.Clamp(age, 0.3f, 1.5f);

        AntlerBranch main = new AntlerBranch();
        main.radius = mainRadius * Mathf.Lerp(0.7f, 1.2f, (age - 0.3f) / 1.2f);

        Quaternion baseRotation = Quaternion.Euler(40f, 142f, -10f);

        // Age affects length - younger deer have shorter antlers
        float ageScaledLength = mainLength * Mathf.Lerp(0.5f, 1.3f, (age - 0.3f) / 1.2f);
        float lengthVariationAmount = RandomRangeFloat(rng, -mainLengthVariation, mainLengthVariation);
        float actualLength = ageScaledLength * (1f + lengthVariationAmount);

        // Vary segment count
        int segments = RandomRangeInt(rng, minMainSegments, maxMainSegments + 1);

        // Apply that rotation to the end direction
        Vector3 start = Vector3.zero;
        Vector3 end = baseRotation * new Vector3(0, actualLength, 0);

        main.pathPoints = GenerateBeam(start, end, segments, mainBeamCurvature, rng);

        // Secondary beam chance increases with age
        float ageAdjustedSecondaryChance = secondaryBeamChance * Mathf.Lerp(0.2f, 1.5f, (age - 0.3f) / 1.2f);
        if (RandomRangeFloat(rng, 0f, 1f) < ageAdjustedSecondaryChance)
        {
            AntlerBranch secondaryBeam = GenerateSecondaryBeam(main, rng, age);
            if (secondaryBeam != null)
                main.children.Add(secondaryBeam);
        }

        // Age affects tine count slightly - focus more on size than quantity
        int minAdjustedTines = Mathf.Max(1, Mathf.RoundToInt(minTines * Mathf.Lerp(0.5f, 0.9f, (age - 0.3f) / 1.2f)));
        int maxAdjustedTines = Mathf.RoundToInt(maxTines * Mathf.Lerp(0.6f, 0.9f, (age - 0.3f) / 1.2f));
        int tineCount = RandomRangeInt(rng, minAdjustedTines, maxAdjustedTines + 1);
        List<int> usedIndices = new List<int>();

        for (int i = 0; i < tineCount; i++)
        {
            AntlerBranch tine = GenerateRandomTine(main, usedIndices, 1, maxDepth, rng, age);
            if (tine != null)
                main.children.Add(tine);
        }

        return main;
    }

    AntlerBranch GenerateSecondaryBeam(AntlerBranch parent, System.Random rng, float age)
    {
        AntlerBranch beam = new AntlerBranch();

        // Attach somewhere in the middle-to-upper portion of the main beam
        int attachIndex = RandomRangeInt(rng, parent.pathPoints.Count / 3, (parent.pathPoints.Count * 2) / 3);
        beam.attachIndex = attachIndex;

        // Secondary beam properties scale with age
        int segments = RandomRangeInt(rng, minMainSegments, maxMainSegments + 1);
        float ageScaledLength = mainLength * secondaryBeamLengthMultiplier * Mathf.Lerp(0.5f, 1.2f, (age - 0.3f) / 1.2f);
        float length = ageScaledLength * RandomRangeFloat(rng, 0.8f, 1.0f);

        float parentRadiusAtAttach = GetParentRadiusAt(parent, attachIndex);
        beam.radius = Mathf.Min(mainRadius * secondaryBeamRadiusMultiplier, parentRadiusAtAttach);

        // Compute tangent at attach point
        int idx = Mathf.Clamp(attachIndex, 1, parent.pathPoints.Count - 2);
        Vector3 tangent = (parent.pathPoints[idx + 1] - parent.pathPoints[idx - 1]).normalized;

        // Create a direction that branches off at an angle
        Vector3 randomUp = RandomInsideUnitSphere(rng).normalized;
        Vector3 perpendicular = Vector3.Cross(randomUp, tangent).normalized;

        if (perpendicular == Vector3.zero)
            perpendicular = Vector3.Cross(tangent, Vector3.up).normalized;

        // Blend tangent direction with perpendicular for natural branching
        float tangentWeight = RandomRangeFloat(rng, 0.6f, 0.9f);
        Vector3 growthDir = (tangent * tangentWeight + perpendicular).normalized;

        Vector3 offset = growthDir * length;

        // Generate the secondary beam path
        beam.pathPoints = GenerateBeam(Vector3.zero, offset, segments, mainBeamCurvature * 0.8f, rng);

        // Add tines to the secondary beam (even fewer on secondary beams)
        List<int> usedIndices = new List<int>();
        int maxSecondaryTines = Mathf.RoundToInt(maxTines * Mathf.Lerp(0.3f, 0.5f, (age - 0.3f) / 1.2f));
        int tineCount = RandomRangeInt(rng, 1, maxSecondaryTines + 1);

        for (int i = 0; i < tineCount; i++)
        {
            AntlerBranch tine = GenerateRandomTine(beam, usedIndices, 1, maxDepth, rng, age);
            if (tine != null)
                beam.children.Add(tine);
        }

        return beam;
    }

    AntlerBranch GenerateRandomTine(AntlerBranch parent, List<int> usedIndices, int depth, int maxDepth, System.Random rng, float age)
    {
        AntlerBranch tine = new AntlerBranch();

        // Choose attach point along parent
        int attachIndex = GetUniqueAttachIndex(parent.pathPoints.Count, usedIndices, 2, rng);
        tine.attachIndex = attachIndex;
        usedIndices.Add(attachIndex);

        // Random length and segments - scaled by age
        int segments = RandomRangeInt(rng, 3, 6);
        float ageScaledMaxLength = tineMaxLength * Mathf.Lerp(0.6f, 1.2f, (age - 0.3f) / 1.2f);
        float length = RandomRangeFloat(rng, 0.2f, ageScaledMaxLength);

        // Clamp tine radius to parent's radius at attach point
        float parentRadiusAtAttach = GetParentRadiusAt(parent, attachIndex);
        float baseRadius = parent.radius * Mathf.Pow(0.6f, depth);
        tine.radius = Mathf.Max(tineMinRadius, Mathf.Min(baseRadius, parentRadiusAtAttach));

        // Compute local tangent direction of the parent beam at the attach point
        int idx = Mathf.Clamp(attachIndex, 1, parent.pathPoints.Count - 2);
        Vector3 tangent = (parent.pathPoints[idx + 1] - parent.pathPoints[idx - 1]).normalized;

        // Choose a roughly perpendicular direction
        Vector3 randomUp = RandomInsideUnitSphere(rng).normalized;
        Vector3 perpendicular = Vector3.Cross(randomUp, tangent).normalized;

        if (perpendicular == Vector3.zero)
            perpendicular = Vector3.Cross(tangent, Vector3.up).normalized;

        // Bias heavily toward upward direction
        float upwardBias = RandomRangeFloat(rng, 1.2f, 2.0f);
        float outwardAmount = RandomRangeFloat(rng, 0.3f, 0.6f);
        Vector3 growthDir = (perpendicular * outwardAmount + Vector3.up * upwardBias + tangent * 0.5f).normalized;

        // Scale by tine length
        Vector3 offset = growthDir * length;

        // Generate tine beam
        tine.pathPoints = GenerateBeam(Vector3.zero, offset, segments, tineCurvature, rng);

        // Recursively generate sub-tines if depth allows - slightly more likely on older deer
        if (depth < maxDepth)
        {
            List<int> subUsed = new List<int>();
            int maxSubTines = age > 1.0f ? 2 : 1; // Only very mature deer get 2 sub-tines
            int subTines = RandomRangeInt(rng, 0, maxSubTines);
            for (int i = 0; i < subTines; i++)
            {
                AntlerBranch sub = GenerateRandomTine(tine, subUsed, depth + 1, maxDepth, rng, age);
                tine.children.Add(sub);
            }
        }

        return tine;
    }

    List<Vector3> GenerateBeam(Vector3 start, Vector3 end, int segments, float curvature, System.Random rng, Quaternion rotation = default)
    {
        if (rotation == default)
            rotation = Quaternion.identity;

        List<Vector3> pts = new List<Vector3>();

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);

            float curveX = Mathf.Sin(t * Mathf.PI * 1f + Mathf.PI) * RandomRangeFloat(rng, curvature * 0.8f, curvature * 1f);
            float curveZ = Mathf.Sin(t * Mathf.PI * 1.2f + Mathf.PI) * RandomRangeFloat(rng, curvature * 0.8f, curvature * 1f);

            pos.x += curveX;
            pos.z += curveZ;

            pos = rotation * pos;

            pts.Add(pos);
        }

        return pts;
    }

    float GetParentRadiusAt(AntlerBranch parent, int attachIndex)
    {
        float t = attachIndex / (float)(parent.pathPoints.Count - 1);
        return parent.radius * Mathf.Lerp(parent.taperStart, parent.taperEnd, t);
    }

    int GetUniqueAttachIndex(int totalPoints, List<int> usedIndices, int minSpacing, System.Random rng)
    {
        const int maxAttempts = 10;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int candidate = RandomRangeInt(rng, 1, totalPoints - 2);
            bool tooClose = false;

            foreach (int used in usedIndices)
            {
                if (Mathf.Abs(candidate - used) < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
                return candidate;
        }

        return RandomRangeInt(rng, 1, totalPoints - 2);
    }
}
