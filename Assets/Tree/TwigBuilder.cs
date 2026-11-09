using System.Collections.Generic;
using UnityEngine;

public static class TwigBuilder
{
    public static void Build(TreeSettings settings, TreeVariant variant, System.Random random, List<TreeNode> nodes)
    {
        if (settings.TwigChance <= 0f)
        {
            return;
        }

        int count = nodes.Count;
        float length = settings.SegmentLength * settings.TwigLength;

        for (int i = 1; i < count; i++)
        {
            TreeNode node = nodes[i];
            if (node.IsRoot || node.Parent < 0 || node.Radius > settings.TwigMaxRadius)
            {
                continue;
            }

            if (TreeRandom.Value(random) > settings.TwigChance)
            {
                continue;
            }

            int twigs = 1;
            if (TreeRandom.Value(random) < 0.3f)
            {
                twigs = 2;
            }

            Vector3 branch = (node.Position - nodes[node.Parent].Position).normalized;

            for (int t = 0; t < twigs; t++)
            {
                if (nodes.Count >= settings.MaxNodes)
                {
                    return;
                }

                AddTwig(settings, variant, random, nodes, i, branch, length);
            }
        }
    }

    private static void AddTwig(TreeSettings settings, TreeVariant variant, System.Random random, List<TreeNode> nodes, int start, Vector3 branch, float length)
    {
        Vector3 direction = branch + TreeRandom.InUnitSphere(random) * 0.8f + Vector3.up * variant.Tropism;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.up;
        }

        direction.Normalize();

        Vector3 position = nodes[start].Position;
        int parent = start;

        for (int k = 1; k <= settings.TwigSegments; k++)
        {
            direction = (direction + TreeRandom.InUnitSphere(random) * 0.4f).normalized;
            position += direction * length;

            TreeNode node = new TreeNode(position, parent, nodes[start].BirthStep + k);
            nodes.Add(node);
            nodes[parent].Children.Add(nodes.Count - 1);
            parent = nodes.Count - 1;
        }
    }
}
