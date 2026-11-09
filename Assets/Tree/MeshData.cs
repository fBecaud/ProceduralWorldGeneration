using System.Collections.Generic;
using UnityEngine;

public class MeshData
{
    public List<Vector3> Vertices = new List<Vector3>();
    public List<Vector3> Normals = new List<Vector3>();
    public List<Vector2> Uvs = new List<Vector2>();
    public List<Color> Colors = new List<Color>();
    public List<int> BarkTriangles = new List<int>();
    public List<int> LeafTriangles = new List<int>();

    public void Clear()
    {
        Vertices.Clear();
        Normals.Clear();
        Uvs.Clear();
        Colors.Clear();
        BarkTriangles.Clear();
        LeafTriangles.Clear();
    }

    public void Apply(Mesh mesh)
    {
        mesh.Clear();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(Vertices);
        mesh.SetNormals(Normals);
        mesh.SetUVs(0, Uvs);
        mesh.SetColors(Colors);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(BarkTriangles, 0);
        mesh.SetTriangles(LeafTriangles, 1);
        mesh.RecalculateBounds();
    }
}
