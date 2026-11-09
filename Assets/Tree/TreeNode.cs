using System.Collections.Generic;
using UnityEngine;

public class TreeNode
{
    public Vector3 Position;
    public int Parent;
    public int BirthStep;
    public float Radius;
    public bool IsRoot;
    public List<int> Children = new List<int>();

    public TreeNode(Vector3 position, int parent, int birthStep)
    {
        Position = position;
        Parent = parent;
        BirthStep = birthStep;
    }
}
