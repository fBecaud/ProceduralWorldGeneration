using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshData
{
    public List<Vector3> Vertices = new List<Vector3>();
    public List<Vector3> Normals = new List<Vector3>();
    public List<Vector2> Uvs = new List<Vector2>();
    public List<Color> Colors = new List<Color>();
    public List<int> BarkTriangles = new List<int>();
    public List<int> LeafTriangles = new List<int>();
    public int LeafStart;

    public void Clear()
    {
        Vertices.Clear();
        Normals.Clear();
        Uvs.Clear();
        Colors.Clear();
        BarkTriangles.Clear();
        LeafTriangles.Clear();
        LeafStart = 0;
    }

    public void Apply(Mesh mesh)
    {
        mesh.Clear();
        mesh.indexFormat = Format(Vertices.Count);
        mesh.SetVertices(Vertices);
        mesh.SetNormals(Normals);
        mesh.SetUVs(0, Uvs);
        mesh.SetColors(Colors);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(BarkTriangles, 0, true);
        mesh.SetTriangles(LeafTriangles, 1, true, LeafStart);
    }

    public void ApplySplit(Mesh bark, Mesh leaves)
    {
        bark.Clear();
        bark.indexFormat = Format(LeafStart);
        bark.SetVertices(Vertices, 0, LeafStart);
        bark.SetNormals(Normals, 0, LeafStart);
        bark.SetUVs(0, Uvs, 0, LeafStart);
        bark.SetColors(Colors, 0, LeafStart);
        bark.SetTriangles(BarkTriangles, 0, true);

        int leafCount = Vertices.Count - LeafStart;
        leaves.Clear();
        leaves.indexFormat = Format(leafCount);
        leaves.SetVertices(Vertices, LeafStart, leafCount);
        leaves.SetNormals(Normals, LeafStart, leafCount);
        leaves.SetUVs(0, Uvs, LeafStart, leafCount);
        leaves.SetColors(Colors, LeafStart, leafCount);
        leaves.SetTriangles(LeafTriangles, 0, true);
    }

    private static IndexFormat Format(int vertexCount)
    {
        if (vertexCount > 65535)
        {
            return IndexFormat.UInt32;
        }

        return IndexFormat.UInt16;
    }
}
