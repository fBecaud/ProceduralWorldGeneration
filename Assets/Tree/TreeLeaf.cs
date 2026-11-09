using UnityEngine;

public class TreeLeaf
{
    public int Node;
    public float Along;
    public Vector3 Axis;
    public Vector3 Side;
    public float Size;
    public float Shade;

    public TreeLeaf(int node, float along, Vector3 axis, Vector3 side, float size, float shade)
    {
        Node = node;
        Along = along;
        Axis = axis;
        Side = side;
        Size = size;
        Shade = shade;
    }
}
