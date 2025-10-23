using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AntlerBranch
{
    public List<Vector3> pathPoints = new List<Vector3>(); // path of this branch
    public float radius = 0.05f;
    public List<AntlerBranch> children = new List<AntlerBranch>(); // tines
    public int attachIndex = 0; // where to attach to parent
    public float taperStart = 1f;
    public float taperEnd = 0.2f;
}