using System.Collections.Generic;
using UnityEngine;

public class TreeSkeleton
{
    public List<TreeNode> Nodes = new List<TreeNode>();
    public List<Vector3> Attractors = new List<Vector3>();
    public List<TreeLeaf> Leaves = new List<TreeLeaf>();
    public Vector3 CrownCenter;
    public float CrownRadius;
    public bool HasHole;
    public int HoleNode;
    public Vector3 HoleAzimuth;
    public int TrunkSteps;
    public int LastStep;
    public int TotalSteps;

    public void RefreshSteps(int leafGrowSteps)
    {
        LastStep = 0;
        for (int i = 0; i < Nodes.Count; i++)
        {
            if (Nodes[i].BirthStep > LastStep)
            {
                LastStep = Nodes[i].BirthStep;
            }
        }

        TotalSteps = LastStep + leafGrowSteps + 1;
    }
}
