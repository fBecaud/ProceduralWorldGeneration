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
            int parent = skeleton.Nodes[leaf.Node].Parent;
            Vector3 position = Vector3.Lerp(state.Positions[parent], state.Positions[leaf.Node], leaf.Along);
            float size = leaf.Size * scale;

            Color color = shading.Leaf(position, leaf.Shade);
            Vector3 faceNormal = Vector3.Cross(leaf.Side, leaf.Axis).normalized;

            AddCard(data, settings, shading, position, leaf.Axis, leaf.Side, faceNormal, size, color);

            if (settings.LeafCross)
            {
                Vector3 crossNormal = Vector3.Cross(faceNormal, leaf.Axis).normalized;
                AddCard(data, settings, shading, position, leaf.Axis, faceNormal, crossNormal, size, color);
            }
        }
    }

    private static void AddCard(MeshData data, TreeSettings settings, TreeShading shading, Vector3 position, Vector3 axis, Vector3 side, Vector3 faceNormal, float size, Color color)
    {
        int first = data.Vertices.Count;

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

        data.LeafTriangles.Add(first);
        data.LeafTriangles.Add(first + 1);
        data.LeafTriangles.Add(first + 2);

        data.LeafTriangles.Add(first);
        data.LeafTriangles.Add(first + 2);
        data.LeafTriangles.Add(first + 3);

        data.LeafTriangles.Add(first);
        data.LeafTriangles.Add(first + 2);
        data.LeafTriangles.Add(first + 1);

        data.LeafTriangles.Add(first);
        data.LeafTriangles.Add(first + 3);
        data.LeafTriangles.Add(first + 2);
    }
}
