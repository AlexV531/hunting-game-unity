using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AntlerMeshGenerator))]
public class Antler : MonoBehaviour
{
    public AntlerMeshGenerator generator;

    [Header("Main Beam")]
    public int mainSegments = 10;
    public float mainLength = 1.2f;
    public float mainRadius = 0.1f;

    [Header("Tines")]
    public int minTines = 2;
    public int maxTines = 5;
    public float tineMaxLength = 0.7f;
    public float tineMaxRadius = 0.04f;

    [Header("Sub-Tines")]
    public int maxDepth = 2;

    [Header("Beam Curvature")]
    public float mainBeamCurvature = 0.22f;
    public float tineCurvature = 0.05f;

    public void Initialize(int seed)
    {
        generator = GetComponent<AntlerMeshGenerator>();
        AntlerBranch main = GenerateMainBranch(seed);
        generator.GenerateAntler(main);
        Debug.Log("Antlers generated with seed " + seed);
    }

    AntlerBranch GenerateMainBranch(int seed)
    {
        System.Random rng = new System.Random(seed);

        AntlerBranch main = new AntlerBranch();
        main.radius = mainRadius;
        main.pathPoints = GenerateBeam(Vector3.zero, new Vector3(0, mainLength, 0), mainSegments, mainBeamCurvature, rng);

        int tineCount = RandomRangeInt(rng, minTines, maxTines + 1);
        List<int> usedIndices = new List<int>();

        for (int i = 0; i < tineCount; i++)
        {
            AntlerBranch tine = GenerateRandomTine(main, usedIndices, 1, maxDepth, rng);
            if (tine != null)
                main.children.Add(tine);
        }

        return main;
    }

    AntlerBranch GenerateRandomTine(AntlerBranch parent, List<int> usedIndices, int depth, int maxDepth, System.Random rng)
    {
        AntlerBranch tine = new AntlerBranch();

        // Choose attach point along parent
        int attachIndex = GetUniqueAttachIndex(parent.pathPoints.Count, usedIndices, 2, rng);
        tine.attachIndex = attachIndex;
        usedIndices.Add(attachIndex);

        // Random length and segments
        int segments = RandomRangeInt(rng, 3, 6);
        float length = RandomRangeFloat(rng, 0.2f, tineMaxLength);

        // Clamp tine radius to parent's radius at attach point
        float parentRadiusAtAttach = GetParentRadiusAt(parent, attachIndex);
        float baseRadius = parent.radius * Mathf.Pow(0.6f, depth);
        tine.radius = Mathf.Min(baseRadius, parentRadiusAtAttach);

        // Compute local tangent direction of the parent beam at the attach point
        int idx = Mathf.Clamp(attachIndex, 1, parent.pathPoints.Count - 2);
        Vector3 tangent = (parent.pathPoints[idx + 1] - parent.pathPoints[idx - 1]).normalized;

        // Choose a roughly perpendicular direction
        Vector3 randomUp = RandomInsideUnitSphere(rng).normalized;
        Vector3 perpendicular = Vector3.Cross(randomUp, tangent).normalized;

        if (perpendicular == Vector3.zero)
            perpendicular = Vector3.Cross(tangent, Vector3.up).normalized;

        // Define offset direction slightly angled upward
        float upwardBias = RandomRangeFloat(rng, 0.8f, 1.5f);
        Vector3 growthDir = (perpendicular + tangent * upwardBias).normalized;

        // Scale by tine length
        Vector3 offset = growthDir * length;

        // Generate tine beam
        tine.pathPoints = GenerateBeam(Vector3.zero, offset, segments, tineCurvature, rng);

        // Recursively generate sub-tines if depth allows
        if (depth < maxDepth)
        {
            List<int> subUsed = new List<int>();
            int subTines = RandomRangeInt(rng, 0, 1); // small chance of sub-tines
            for (int i = 0; i < subTines; i++)
            {
                AntlerBranch sub = GenerateRandomTine(tine, subUsed, depth + 1, maxDepth, rng);
                tine.children.Add(sub);
            }
        }

        return tine;
    }

    List<Vector3> GenerateBeam(Vector3 start, Vector3 end, int segments, float curvature, System.Random rng)
    {
        List<Vector3> pts = new List<Vector3>();

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);

            float curveX = Mathf.Sin(t * Mathf.PI * 1f) * RandomRangeFloat(rng, curvature * 0.8f, curvature * 1f);
            float curveZ = Mathf.Sin(t * Mathf.PI * 1.2f) * RandomRangeFloat(rng, curvature * 0.8f, curvature * 1f);

            pos.x += curveX;
            pos.z += curveZ;

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

    // --- Deterministic Random Helpers ---

    int RandomRangeInt(System.Random rng, int min, int max)
    {
        return rng.Next(min, max); // upper bound exclusive
    }

    float RandomRangeFloat(System.Random rng, float min, float max)
    {
        return (float)(rng.NextDouble() * (max - min) + min);
    }

    Vector3 RandomInsideUnitSphere(System.Random rng)
    {
        // Uniformly random vector inside a unit sphere
        float u = (float)rng.NextDouble();
        float v = (float)rng.NextDouble();
        float theta = 2f * Mathf.PI * u;
        float phi = Mathf.Acos(2f * v - 1f);
        float r = Mathf.Pow((float)rng.NextDouble(), 1f / 3f);
        float sinPhi = Mathf.Sin(phi);

        return new Vector3(
            r * sinPhi * Mathf.Cos(theta),
            r * sinPhi * Mathf.Sin(theta),
            r * Mathf.Cos(phi)
        );
    }
}
