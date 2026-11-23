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
    public List<int> ShadowTriangles = new List<int>();
    public List<int> LeafTriangles = new List<int>();
    public int ShadowStart;
    public int LeafStart;

    public void Clear()
    {
        Vertices.Clear();
        Normals.Clear();
        Uvs.Clear();
        Colors.Clear();
        BarkTriangles.Clear();
        ShadowTriangles.Clear();
        LeafTriangles.Clear();
        ShadowStart = 0;
        LeafStart = 0;
    }

    public void Apply(Mesh bark, Mesh leaves, Mesh shadow)
    {
        bark.Clear();
        bark.indexFormat = Format(ShadowStart);
        bark.SetVertices(Vertices, 0, ShadowStart);
        bark.SetNormals(Normals, 0, ShadowStart);
        bark.SetUVs(0, Uvs, 0, ShadowStart);
        bark.SetColors(Colors, 0, ShadowStart);
        bark.SetTriangles(BarkTriangles, 0, true);

        int leafCount = Vertices.Count - LeafStart;
        leaves.Clear();
        leaves.indexFormat = Format(leafCount);
        leaves.SetVertices(Vertices, LeafStart, leafCount);
        leaves.SetNormals(Normals, LeafStart, leafCount);
        leaves.SetUVs(0, Uvs, LeafStart, leafCount);
        leaves.SetColors(Colors, LeafStart, leafCount);
        leaves.SetTriangles(LeafTriangles, 0, true);

        if (shadow == null)
        {
            return;
        }

        shadow.Clear();
        shadow.indexFormat = Format(LeafStart);
        shadow.SetVertices(Vertices, 0, LeafStart);
        shadow.SetNormals(Normals, 0, LeafStart);
        shadow.subMeshCount = 2;
        shadow.SetTriangles(BarkTriangles, 0, true);
        shadow.SetTriangles(ShadowTriangles, 1, true);
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
