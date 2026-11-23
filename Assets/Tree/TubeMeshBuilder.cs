using System.Collections.Generic;
using UnityEngine;

public static class TubeMeshBuilder
{
    private const int HoleSubdivisions = 4;
    private const int HoleRadialSegments = 16;
    private const float HoleDepth = 0.4f;
    private const float HoleDarkness = 0.9f;

    private static readonly List<Vector3> _positions = new List<Vector3>();
    private static readonly List<float> _radii = new List<float>();
    private static readonly List<bool> _roots = new List<bool>();
    private static readonly List<int> _chain = new List<int>();
    private static readonly Stack<int> _chainStarts = new Stack<int>();

    public static void Build(MeshData data, TreeSkeleton skeleton, GrowthState state, TreeSettings settings, TreeShading shading)
    {
        List<TreeNode> nodes = skeleton.Nodes;
        List<int> chain = _chain;
        Stack<int> chainStarts = _chainStarts;
        chainStarts.Clear();

        if (nodes.Count > 0 && state.Progress[0] > 0f)
        {
            chainStarts.Push(0);
        }

        while (chainStarts.Count > 0)
        {
            int start = chainStarts.Pop();
            chain.Clear();

            int parent = nodes[start].Parent;
            if (parent >= 0)
            {
                chain.Add(parent);
            }

            int current = start;
            while (current >= 0)
            {
                chain.Add(current);

                int main = MainChild(nodes, state, current);
                List<int> children = nodes[current].Children;
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i] != main && state.Progress[children[i]] > 0f)
                    {
                        chainStarts.Push(children[i]);
                    }
                }

                current = main;
            }

            AddTube(data, nodes, state, settings, shading, chain, parent >= 0);
        }
    }

    private static int MainChild(List<TreeNode> nodes, GrowthState state, int node)
    {
        List<int> children = nodes[node].Children;
        int main = -1;
        float mainRadius = -1f;

        for (int i = 0; i < children.Count; i++)
        {
            int child = children[i];
            if (nodes[child].IsRoot || state.Progress[child] <= 0f)
            {
                continue;
            }

            if (state.Radii[child] > mainRadius)
            {
                mainRadius = state.Radii[child];
                main = child;
            }
        }

        if (main >= 0)
        {
            return main;
        }

        for (int i = 0; i < children.Count; i++)
        {
            int child = children[i];
            if (state.Progress[child] <= 0f)
            {
                continue;
            }

            if (state.Radii[child] > mainRadius)
            {
                mainRadius = state.Radii[child];
                main = child;
            }
        }

        return main;
    }

    private static void AddTube(MeshData data, List<TreeNode> nodes, GrowthState state, TreeSettings settings, TreeShading shading, List<int> chain, bool isSideBranch)
    {
        if (chain.Count < 2)
        {
            return;
        }

        int first = 0;
        if (isSideBranch)
        {
            first = 1;
        }

        int sides = Mathf.RoundToInt(state.Radii[chain[first]] / settings.TipRadius) + 2;
        int radialSegments = Mathf.Clamp(sides, settings.TwigRadialSegments, settings.RadialSegments);

        bool nearHole = Sample(nodes, state, settings, shading, chain, isSideBranch);
        if (nearHole)
        {
            radialSegments = Mathf.Max(radialSegments, HoleRadialSegments);
        }

        int firstVertex = data.Vertices.Count;
        int ringSize = radialSegments + 1;
        Vector3 tangent = Vector3.up;
        Vector3 normal = Vector3.zero;
        float length = 0f;

        for (int i = 0; i < _positions.Count; i++)
        {
            Vector3 position = _positions[i];
            Vector3 previousTangent = tangent;
            tangent = Tangent(i, previousTangent);

            if (i == 0)
            {
                normal = Perpendicular(tangent);
            }
            else
            {
                normal = Quaternion.FromToRotation(previousTangent, tangent) * normal;
                normal = Vector3.ProjectOnPlane(normal, tangent).normalized;
                length += (position - _positions[i - 1]).magnitude;
            }

            Vector3 binormal = Vector3.Cross(tangent, normal);
            float radius = _radii[i];
            float slope = Slope(i);
            Color ringColor = shading.Bark(position, radius, _roots[i]);

            for (int j = 0; j <= radialSegments; j++)
            {
                float angle = (float)j / radialSegments * Mathf.PI * 2f;
                Vector3 offset = normal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle);
                Vector3 vertex = position + offset * radius;
                Vector3 vertexNormal = (offset - tangent * slope).normalized;
                Color color = ringColor;

                if (nearHole)
                {
                    float hole = shading.Hole(vertex);
                    if (hole > 0f)
                    {
                        float depth = shading.HoleRadius * HoleDepth;
                        float step = shading.HoleRadius * 0.1f;
                        Vector3 around = Vector3.Cross(tangent, offset).normalized;
                        float alongSlope = (shading.Hole(vertex + tangent * step) - shading.Hole(vertex - tangent * step)) / (2f * step);
                        float aroundSlope = (shading.Hole(vertex + around * step) - shading.Hole(vertex - around * step)) / (2f * step);

                        vertex -= offset * depth * hole;
                        vertexNormal = (vertexNormal + (tangent * alongSlope + around * aroundSlope) * depth).normalized;
                        color = ringColor * (1f - HoleDarkness * hole);
                        color.a = 1f;
                    }
                }

                data.Vertices.Add(vertex);
                data.Normals.Add(vertexNormal);
                data.Uvs.Add(new Vector2((float)j / radialSegments, length));
                data.Colors.Add(color);
            }

            if (i > 0)
            {
                int previousRing = firstVertex + (i - 1) * ringSize;
                int ring = firstVertex + i * ringSize;

                for (int j = 0; j < radialSegments; j++)
                {
                    data.BarkTriangles.Add(previousRing + j);
                    data.BarkTriangles.Add(previousRing + j + 1);
                    data.BarkTriangles.Add(ring + j);

                    data.BarkTriangles.Add(previousRing + j + 1);
                    data.BarkTriangles.Add(ring + j + 1);
                    data.BarkTriangles.Add(ring + j);
                }
            }
        }

        int last = _positions.Count - 1;
        int lastRing = firstVertex + last * ringSize;
        AddCap(data, shading, _positions[last], _radii[last], _roots[last], tangent, lastRing, radialSegments, length);
    }

    private static bool Sample(List<TreeNode> nodes, GrowthState state, TreeSettings settings, TreeShading shading, List<int> chain, bool isSideBranch)
    {
        _positions.Clear();
        _radii.Clear();
        _roots.Clear();

        bool nearHole = false;

        for (int i = 0; i < chain.Count; i++)
        {
            Vector3 position = state.Positions[chain[i]];
            float radius = ChainRadius(state, settings, chain, i, isSideBranch);
            bool root = nodes[chain[i]].IsRoot;

            _positions.Add(position);
            _radii.Add(radius);
            _roots.Add(root);

            if (i == chain.Count - 1)
            {
                break;
            }

            Vector3 next = state.Positions[chain[i + 1]];
            if (!shading.NearHole(position, next))
            {
                continue;
            }

            nearHole = true;
            float nextRadius = ChainRadius(state, settings, chain, i + 1, isSideBranch);

            for (int s = 1; s < HoleSubdivisions; s++)
            {
                float t = (float)s / HoleSubdivisions;
                _positions.Add(Vector3.Lerp(position, next, t));
                _radii.Add(Mathf.Lerp(radius, nextRadius, t));
                _roots.Add(root);
            }
        }

        return nearHole;
    }

    private static void AddCap(MeshData data, TreeShading shading, Vector3 position, float radius, bool isRoot, Vector3 tangent, int lastRing, int radialSegments, float length)
    {
        int cap = data.Vertices.Count;
        data.Vertices.Add(position + tangent * radius);
        data.Normals.Add(tangent);
        data.Uvs.Add(new Vector2(0.5f, length + radius));
        data.Colors.Add(shading.Bark(position, radius, isRoot));

        for (int j = 0; j < radialSegments; j++)
        {
            data.BarkTriangles.Add(lastRing + j);
            data.BarkTriangles.Add(lastRing + j + 1);
            data.BarkTriangles.Add(cap);
        }
    }

    private static float ChainRadius(GrowthState state, TreeSettings settings, List<int> chain, int i, bool isSideBranch)
    {
        if (!isSideBranch)
        {
            return state.Radii[chain[i]];
        }

        float radius = state.Radii[chain[i]];
        if (i == 0)
        {
            radius = state.Radii[chain[1]];
        }

        float collar = 1f;
        if (i == 0)
        {
            collar = settings.BranchCollar;
        }
        else if (i == 1)
        {
            collar = 1f + (settings.BranchCollar - 1f) * 0.4f;
        }
        else if (i == 2)
        {
            collar = 1f + (settings.BranchCollar - 1f) * 0.12f;
        }

        float parentRadius = state.Radii[chain[0]];
        return Mathf.Min(radius * collar, Mathf.Max(parentRadius * 0.95f, radius));
    }

    private static float Slope(int i)
    {
        int previous = Mathf.Max(i - 1, 0);
        int next = Mathf.Min(i + 1, _positions.Count - 1);
        float distance = (_positions[next] - _positions[previous]).magnitude;

        if (distance < 0.0001f)
        {
            return 0f;
        }

        return (_radii[next] - _radii[previous]) / distance;
    }

    private static Vector3 Tangent(int i, Vector3 fallback)
    {
        int previous = Mathf.Max(i - 1, 0);
        int next = Mathf.Min(i + 1, _positions.Count - 1);
        Vector3 delta = _positions[next] - _positions[previous];

        if (delta.sqrMagnitude < 0.0000001f)
        {
            return fallback;
        }

        return delta.normalized;
    }

    private static Vector3 Perpendicular(Vector3 direction)
    {
        Vector3 reference = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(direction, reference)) > 0.99f)
        {
            reference = Vector3.right;
        }

        return Vector3.Cross(direction, reference).normalized;
    }
}
