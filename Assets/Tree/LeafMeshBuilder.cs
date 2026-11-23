using System.Collections.Generic;
using UnityEngine;

public static class LeafMeshBuilder
{
    public static void Build(MeshData data, TreeSkeleton skeleton, GrowthState state, TreeSettings settings, TreeShading shading)
    {
        List<TreeLeaf> leaves = skeleton.Leaves;

        for (int l = 0; l < leaves.Count; l++)
        {
            float scale = state.LeafScales[l];
            if (scale <= 0f)
            {
                continue;
            }

            TreeLeaf leaf = leaves[l];
            Vector3 position = Position(skeleton, state, leaf);
            float size = leaf.Size * scale;

            Color color = shading.Leaf(position, leaf.Shade);
            Vector3 faceNormal = Vector3.Cross(leaf.Side, leaf.Axis).normalized;

            AddCard(data, data.LeafTriangles, data.LeafStart, settings, shading, position, leaf.Axis, leaf.Side, faceNormal, size, color);

            if (settings.LeafCross)
            {
                Vector3 crossNormal = Vector3.Cross(faceNormal, leaf.Axis).normalized;
                AddCard(data, data.LeafTriangles, data.LeafStart, settings, shading, position, leaf.Axis, faceNormal, crossNormal, size, color);
            }
        }
    }

    public static void BuildShadow(MeshData data, TreeSkeleton skeleton, GrowthState state, TreeSettings settings, TreeShading shading)
    {
        List<TreeLeaf> leaves = skeleton.Leaves;
        int stride = Mathf.Max(1, settings.ShadowLeafStride);
        float enlarge = Mathf.Sqrt(stride);

        for (int l = 0; l < leaves.Count; l += stride)
        {
            float scale = state.LeafScales[l];
            if (scale <= 0f)
            {
                continue;
            }

            TreeLeaf leaf = leaves[l];
            Vector3 position = Position(skeleton, state, leaf);
            Vector3 faceNormal = Vector3.Cross(leaf.Side, leaf.Axis).normalized;

            AddCard(data, data.ShadowTriangles, 0, settings, shading, position, leaf.Axis, leaf.Side, faceNormal, leaf.Size * scale * enlarge, Color.black);
        }
    }

    private static Vector3 Position(TreeSkeleton skeleton, GrowthState state, TreeLeaf leaf)
    {
        int parent = skeleton.Nodes[leaf.Node].Parent;
        return Vector3.Lerp(state.Positions[parent], state.Positions[leaf.Node], leaf.Along);
    }

    private static void AddCard(MeshData data, List<int> triangles, int indexBase, TreeSettings settings, TreeShading shading, Vector3 position, Vector3 axis, Vector3 side, Vector3 faceNormal, float size, Color color)
    {
        int first = data.Vertices.Count - indexBase;

        Vector3 tip = axis * size;
        Vector3 half = axis * size * 0.5f;
        Vector3 width = side * size * 0.25f;
        Vector3 fold = faceNormal * size * settings.LeafFold;
        Vector3 normal = shading.LeafNormal(position + half, faceNormal);

        data.Vertices.Add(position);
        data.Vertices.Add(position + half + width - fold);
        data.Vertices.Add(position + tip);
        data.Vertices.Add(position + half - width - fold);

        for (int i = 0; i < 4; i++)
        {
            data.Normals.Add(normal);
            data.Colors.Add(color);
        }

        data.Uvs.Add(new Vector2(0.5f, 0f));
        data.Uvs.Add(new Vector2(1f, 0.5f));
        data.Uvs.Add(new Vector2(0.5f, 1f));
        data.Uvs.Add(new Vector2(0f, 0.5f));

        triangles.Add(first);
        triangles.Add(first + 1);
        triangles.Add(first + 2);

        triangles.Add(first);
        triangles.Add(first + 2);
        triangles.Add(first + 3);

        triangles.Add(first);
        triangles.Add(first + 2);
        triangles.Add(first + 1);

        triangles.Add(first);
        triangles.Add(first + 3);
        triangles.Add(first + 2);
    }
}
