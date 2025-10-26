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
    public float mainBeamCurvature = 0.22f; // default moderate bend
    public float tineCurvature = 0.05f; // tines can curve more

    void Start()
    {
        generator = GetComponent<AntlerMeshGenerator>();
        AntlerBranch main = GenerateMainBranch();
        generator.GenerateAntler(main);
    }

    AntlerBranch GenerateMainBranch()
    {
        AntlerBranch main = new AntlerBranch();
        main.radius = mainRadius;
        main.pathPoints = GenerateBeam(Vector3.zero, new Vector3(0, mainLength, 0), mainSegments, mainBeamCurvature);

        int tineCount = Random.Range(minTines, maxTines + 1);
        List<int> usedIndices = new List<int>(); // keep track of previous attach points

        for (int i = 0; i < tineCount; i++)
        {
            AntlerBranch tine = GenerateRandomTine(main, usedIndices, 1, maxDepth);
            if (tine != null)
                main.children.Add(tine);
        }

        return main;
    }

    AntlerBranch GenerateRandomTine(AntlerBranch parent, List<int> usedIndices, int depth, int maxDepth)
    {
        AntlerBranch tine = new AntlerBranch();

        // Choose attach point along parent
        int attachIndex = GetUniqueAttachIndex(parent.pathPoints.Count, usedIndices, 2);
        tine.attachIndex = attachIndex;
        usedIndices.Add(attachIndex);

        // Random length and segments
        int segments = Random.Range(3, 6);
        float length = Random.Range(0.2f, tineMaxLength);

        // Clamp tine radius to parent's radius at attach point
        float parentRadiusAtAttach = GetParentRadiusAt(parent, attachIndex);
        float baseRadius = parent.radius * Mathf.Pow(0.6f, depth);
        tine.radius = Mathf.Min(baseRadius, parentRadiusAtAttach);

        // Compute local tangent direction of the parent beam at the attach point
        int idx = Mathf.Clamp(attachIndex, 1, parent.pathPoints.Count - 2);
        Vector3 tangent = (parent.pathPoints[idx + 1] - parent.pathPoints[idx - 1]).normalized;

        // Choose a roughly perpendicular direction
        Vector3 randomUp = Random.insideUnitSphere.normalized; // gives some variation
        Vector3 perpendicular = Vector3.Cross(randomUp, tangent).normalized;

        // Ensure it's truly perpendicular and consistent
        if (perpendicular == Vector3.zero)
            perpendicular = Vector3.Cross(tangent, Vector3.up).normalized;

        // Now define offset direction slightly angled upward
        float upwardBias = Random.Range(0.8f, 1.5f); // much stronger upward influence
        Vector3 growthDir = (perpendicular + tangent * upwardBias).normalized;

        // Scale by tine length
        Vector3 offset = growthDir * length;

        // Generate tine beam
        tine.pathPoints = GenerateBeam(Vector3.zero, offset, segments, tineCurvature);

        // Recursively generate sub-tines if depth allows
        if (depth < maxDepth)
        {
            List<int> subUsed = new List<int>(); // new list per tine level
            int subTines = Random.Range(0, 1); // small chance of sub-tines
            for (int i = 0; i < subTines; i++)
            {
                AntlerBranch sub = GenerateRandomTine(tine, subUsed, depth + 1, maxDepth);
                tine.children.Add(sub);
            }
        }

        return tine;
    }

    // List<Vector3> GenerateBeam(Vector3 start, Vector3 end, int segments)
    // {
    //     List<Vector3> pts = new List<Vector3>();
    //     for (int i = 0; i < segments; i++)
    //     {
    //         float t = i / (float)(segments - 1);
    //         Vector3 pos = Vector3.Lerp(start, end, t);

    //         pos.x += Mathf.Sin(t * Mathf.PI * 1.5f) * Random.Range(0.02f, 0.05f);
    //         pos.z += Mathf.Sin(t * Mathf.PI * 1.5f) * Random.Range(0.02f, 0.05f);

    //         pts.Add(pos);
    //     }
    //     return pts;
    // }

    List<Vector3> GenerateBeam(Vector3 start, Vector3 end, int segments, float curvature = 0.05f)
    {
        List<Vector3> pts = new List<Vector3>();
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);

            // Apply sinusoidal curves scaled by curvature factor
            // float curveX = Mathf.Sin(t * Mathf.PI * 1.5f) * Random.Range(curvature * 0.8f, curvature * 1f);
            // float curveZ = Mathf.Sin(t * Mathf.PI * 2f) * Random.Range(curvature * 0.8f, curvature * 1f);
            float curveX = Mathf.Sin(t * Mathf.PI * 1f) * Random.Range(curvature * 0.8f, curvature * 1f);
            float curveZ = Mathf.Sin(t * Mathf.PI * 1.2f) * Random.Range(curvature * 0.8f, curvature * 1f);

            pos.x += curveX;
            pos.z += curveZ;

            pts.Add(pos);
        }
        return pts;
    }

    float GetParentRadiusAt(AntlerBranch parent, int attachIndex)
    {
        float t = attachIndex / (float)(parent.pathPoints.Count - 1);
        return parent.radius * Mathf.Lerp(parent.taperStart, parent.taperEnd, t); // same taper formula as ExtrudePath
    }

    int GetUniqueAttachIndex(int totalPoints, List<int> usedIndices, int minSpacing)
    {
        const int maxAttempts = 10;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int candidate = Random.Range(1, totalPoints - 2);
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

        // fallback if too many failed attempts
        return Random.Range(1, totalPoints - 2);
    }
}