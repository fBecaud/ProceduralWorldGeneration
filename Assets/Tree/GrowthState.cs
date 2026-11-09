using UnityEngine;

public class GrowthState
{
    public Vector3[] Positions = new Vector3[0];
    public float[] Radii = new float[0];
    public float[] Progress = new float[0];
    public float[] Carried = new float[0];
    public float[] LeafScales = new float[0];
    public float SizeScale = 1f;

    public void Resize(int nodeCount, int leafCount)
    {
        if (Positions.Length != nodeCount)
        {
            Positions = new Vector3[nodeCount];
            Radii = new float[nodeCount];
            Progress = new float[nodeCount];
            Carried = new float[nodeCount];
        }

        if (LeafScales.Length != leafCount)
        {
            LeafScales = new float[leafCount];
        }
    }
}
