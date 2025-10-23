using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AntlerMeshGenerator))]
public class Antler : MonoBehaviour
{
    public AntlerMeshGenerator generator;

    [Header("Main Beam")]
    public int mainSegments = 10;
    public float mainLength = 1.2f;
    public float mainRadius = 0.06f;

    [Header("Tines")]
    public int minTines = 2;
    public int maxTines = 5;
    public float tineMaxLength = 0.7f;
    public float tineMaxRadius = 0.04f;

    [Header("Sub-Tines")]
    public int maxDepth = 2;

    [Header("Beam Curvature")]
    public float mainBeamCurvature = 0.08f; // default moderate bend
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
        main.pathPoints = GenerateBeam(Vector3.zero, new Vector3(0, mainLength, 0), mainSegments);

        int tineCount = Random.Range(minTines, maxTines + 1);
        for (int i = 0; i < tineCount; i++)
        {
            AntlerBranch tine = GenerateRandomTine(main, 1, maxDepth);
            main.children.Add(tine);
        }

        return main;
    }

    AntlerBranch GenerateRandomTine(AntlerBranch parent, int depth, int maxDepth)
    {
        AntlerBranch tine = new AntlerBranch();

        // Choose attach point along parent
        int attachIndex = Random.Range(1, parent.pathPoints.Count - 2);
        tine.attachIndex = attachIndex;

        // Random length and segments
        int segments = Random.Range(3, 6);
        float length = Random.Range(0.2f, tineMaxLength);

        // Clamp tine radius to the parent’s radius at attach point
        float parentRadiusAtAttach = GetParentRadiusAt(parent, attachIndex);
        float baseRadius = parent.radius * Mathf.Pow(0.6f, depth);
        tine.radius = Mathf.Min(baseRadius, parentRadiusAtAttach);

        // Generate curved, randomized tine
        Vector3 offset = new Vector3(
            Random.Range(-0.2f, 0.2f),
            length,
            Random.Range(-0.2f, 0.2f)
        );
        tine.pathPoints = GenerateBeam(Vector3.zero, offset, segments);

        // Recursively generate sub-tines if depth allows
        if (depth < maxDepth)
        {
            int subTines = Random.Range(0, 1);
            for (int i = 0; i < subTines; i++)
            {
                tine.children.Add(GenerateRandomTine(tine, depth + 1, maxDepth));
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
            float curveX = Mathf.Sin(t * Mathf.PI * 1.5f) * Random.Range(curvature * 0.4f, curvature * 1f);
            float curveZ = Mathf.Sin(t * Mathf.PI * 2f) * Random.Range(curvature * 0.4f, curvature * 1f);

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
}