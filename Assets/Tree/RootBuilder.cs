using System.Collections.Generic;
using UnityEngine;

public static class RootBuilder
{
    private const float MaxWobbleAngle = 45f;

    public static void Build(TreeSettings settings, System.Func<Vector3, float> groundHeight, System.Random random, List<TreeNode> nodes, int firstStep, int lastStep)
    {
        if (settings.RootCount == 0 || nodes.Count < 2)
        {
            return;
        }

        float footRadius = nodes[0].Radius;
        float trunkRadius = footRadius / (1f + settings.Flare);
        Vector3 foot = nodes[0].Position;

        for (int r = 0; r < settings.RootCount; r++)
        {
            float angle = (r + TreeRandom.Value(random) * 0.6f) / settings.RootCount * Mathf.PI * 2f;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            float length = settings.RootLength * TreeRandom.Range(random, 0.6f, 1.3f);
            float baseRadius = settings.RootRadius * trunkRadius * TreeRandom.Range(random, 0.7f, 1.2f);
            float amplitude = baseRadius * settings.RootWaves * TreeRandom.Range(random, 0.5f, 1f);
            float waves = TreeRandom.Range(random, 1f, 2.5f);
            float phase = TreeRandom.Range(random, 0f, Mathf.PI * 2f);
            int steps = Mathf.Max(3, Mathf.RoundToInt(length / settings.SegmentLength));

            Vector3 position = foot + direction * footRadius * 0.8f;
            position.y = groundHeight(position) - baseRadius * 0.2f;

            TreeNode first = new TreeNode(position, 0, firstStep);
            first.IsRoot = true;
            first.Radius = baseRadius;
            nodes.Add(first);
            nodes[0].Children.Add(nodes.Count - 1);
            int parent = nodes.Count - 1;

            for (int k = 1; k <= steps; k++)
            {
                float t = (float)k / steps;
                float yaw = TreeRandom.Range(random, -1f, 1f) * settings.RootWobble * MaxWobbleAngle;
                direction = Quaternion.AngleAxis(yaw, Vector3.up) * direction;

                position += direction * settings.SegmentLength;
                float radius = Mathf.Lerp(baseRadius, settings.TipRadius, Mathf.Pow(t, 0.75f));
                float wave = Mathf.Sin(t * Mathf.PI * waves + phase) * amplitude * (1f - t);
                position.y = groundHeight(position) + wave - radius * 0.4f - settings.RootDepth * t * t * t;

                int birth = firstStep + Mathf.RoundToInt(t * (lastStep - firstStep));
                TreeNode node = new TreeNode(position, parent, birth);
                node.IsRoot = true;
                node.Radius = radius;

                nodes.Add(node);
                nodes[parent].Children.Add(nodes.Count - 1);
                parent = nodes.Count - 1;
            }
        }
    }
}
