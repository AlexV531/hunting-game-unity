using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AntlerMeshGenerator : MonoBehaviour
{
    public Material material;
    public int radialSegments = 8;

    public void GenerateAntler(AntlerBranch root, GameObject antlerMirror)
    {
        List<CombineInstance> combineInstances = new List<CombineInstance>();
        BuildBranchMesh(root, combineInstances, Vector3.zero);
        
        Mesh finalMesh = new Mesh();
        finalMesh.CombineMeshes(combineInstances.ToArray(), true, true);

        GetComponent<MeshFilter>().sharedMesh = finalMesh;
        GetComponent<MeshRenderer>().sharedMaterial = material;
        if (antlerMirror != null)
        {
            antlerMirror.GetComponent<MeshFilter>().sharedMesh = finalMesh;
            antlerMirror.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }

    void BuildBranchMesh(AntlerBranch branch, List<CombineInstance> combineInstances, Vector3 parentAttachPos)
    {
        // Translate branch path to parent attach position
        List<Vector3> translatedPath = new List<Vector3>();
        Vector3 offset = parentAttachPos;
        foreach (var pt in branch.pathPoints)
            translatedPath.Add(pt + offset);

        // Generate mesh
        Mesh branchMesh = ExtrudePath(translatedPath, branch.radius, branch.taperStart, branch.taperEnd);

        CombineInstance ci = new CombineInstance();
        ci.mesh = branchMesh;
        ci.transform = Matrix4x4.identity;
        combineInstances.Add(ci);

        // Recurse into children
        foreach (var child in branch.children)
        {
            int attachIdx = Mathf.Clamp(child.attachIndex, 0, translatedPath.Count - 1);
            Vector3 attachPos = translatedPath[attachIdx];
            BuildBranchMesh(child, combineInstances, attachPos);
        }
    }

    Mesh ExtrudePath(List<Vector3> pathPoints, float baseRadius, float taperStart, float taperEnd)
    {
        Mesh mesh = new Mesh();
        if (pathPoints.Count < 2)
            return mesh;

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        // Initial frame
        Vector3 prevForward = (pathPoints[1] - pathPoints[0]).normalized;
        Vector3 prevUp = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(prevUp, prevForward)) > 0.99f)
            prevUp = Vector3.right;
        Vector3 prevRight = Vector3.Cross(prevForward, prevUp).normalized;
        prevUp = Vector3.Cross(prevRight, prevForward).normalized;

        for (int i = 0; i < pathPoints.Count; i++)
        {
            Vector3 forward = (i < pathPoints.Count - 1) ?
                (pathPoints[i + 1] - pathPoints[i]).normalized : prevForward;

            // Parallel transport frame
            Vector3 v = Vector3.Cross(prevForward, forward);
            float sinAngle = v.magnitude;
            if (sinAngle > 0.0001f)
            {
                float cosAngle = Vector3.Dot(prevForward, forward);
                float angle = Mathf.Atan2(sinAngle, cosAngle) * Mathf.Rad2Deg;
                Quaternion q = Quaternion.AngleAxis(angle, v.normalized);
                prevRight = q * prevRight;
                prevUp = q * prevUp;
            }

            float t = i / (float)(pathPoints.Count - 1);
            float taper = Mathf.Lerp(taperStart, taperEnd, t);

            // Optional twist
            float twistAngle = t * 15f; // degrees
            Quaternion twist = Quaternion.AngleAxis(twistAngle, forward);

            for (int j = 0; j < radialSegments; j++)
            {
                float theta = (j / (float)radialSegments) * Mathf.PI * 2;
                Vector3 offset = (Mathf.Cos(theta) * prevRight + Mathf.Sin(theta) * prevUp) * baseRadius * taper;
                offset = twist * offset;
                verts.Add(pathPoints[i] + offset);
            }

            prevForward = forward;
        }

        // Triangles for the tube surface
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            int start = i * radialSegments;
            int next = (i + 1) * radialSegments;
            for (int j = 0; j < radialSegments; j++)
            {
                int a = start + j;
                int b = start + (j + 1) % radialSegments;
                int c = next + j;
                int d = next + (j + 1) % radialSegments;

                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        }

        // Add end cap at the tip (last ring)
        int tipCenterIdx = verts.Count;
        verts.Add(pathPoints[pathPoints.Count - 1]); // Center vertex at tip
        
        int lastRingStart = (pathPoints.Count - 1) * radialSegments;
        for (int j = 0; j < radialSegments; j++)
        {
            int a = lastRingStart + j;
            int b = lastRingStart + (j + 1) % radialSegments;
            // Triangle pointing inward (normal facing out from tip)
            tris.Add(tipCenterIdx); tris.Add(b); tris.Add(a);
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}