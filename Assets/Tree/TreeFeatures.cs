using System.Collections.Generic;
using UnityEngine;

public static class TreeFeatures
{
    private const float LowBranchCloud = 0.12f;

    public static void AddBranches(TreeSettings settings, TreeVariant variant, System.Random random, TreeSkeleton skeleton)
    {
        if (skeleton.TrunkSteps < 4)
        {
            return;
        }

        if (TreeRandom.Value(random) < settings.ForkChance)
        {
            AddFork(settings, variant, random, skeleton);
        }

        if (TreeRandom.Value(random) < settings.LowBranchChance)
        {
            AddLowBranch(settings, variant, random, skeleton);
        }
    }

    public static void AddHole(TreeSettings settings, System.Random random, TreeSkeleton skeleton)
    {
        if (skeleton.TrunkSteps < 4 || TreeRandom.Value(random) >= settings.HoleChance)
        {
            return;
        }

        skeleton.HasHole = true;
        skeleton.HoleNode = Mathf.RoundToInt(skeleton.TrunkSteps * TreeRandom.Range(random, 0.35f, 0.75f));
        skeleton.HoleAzimuth = Horizontal(random);
    }

    private static void AddFork(TreeSettings settings, TreeVariant variant, System.Random random, TreeSkeleton skeleton)
    {
        List<TreeNode> nodes = skeleton.Nodes;
        int start = Mathf.RoundToInt(skeleton.TrunkSteps * TreeRandom.Range(random, 0.45f, 0.7f));

        Vector3 direction = Vector3.Slerp(Vector3.up, Horizontal(random), TreeRandom.Range(random, 0.3f, 0.5f)).normalized;
        float targetHeight = variant.TrunkHeight * TreeRandom.Range(random, 0.85f, 1.1f);
        Vector3 position = nodes[start].Position;
        int parent = start;
        int step = nodes[start].BirthStep;

        while (position.y < targetHeight && nodes.Count < settings.MaxNodes)
        {
            step++;
            direction = Vector3.Slerp(direction, Vector3.up, settings.TrunkStraighten * 0.5f).normalized;
            position += direction * settings.SegmentLength;
            SpaceColonization.AddNode(nodes, position, parent, step);
            parent = nodes.Count - 1;
        }
    }

    private static void AddLowBranch(TreeSettings settings, TreeVariant variant, System.Random random, TreeSkeleton skeleton)
    {
        List<TreeNode> nodes = skeleton.Nodes;
        int start = Mathf.RoundToInt(skeleton.TrunkSteps * TreeRandom.Range(random, 0.5f, 0.75f));

        Vector3 outward = Horizontal(random);
        Vector3 direction = (outward + Vector3.up * TreeRandom.Range(random, 0.05f, 0.3f)).normalized;
        float length = variant.CrownRadius * TreeRandom.Range(random, 0.35f, 0.6f);
        int segments = Mathf.Max(3, Mathf.RoundToInt(length / settings.SegmentLength));

        Vector3 position = nodes[start].Position;
        int parent = start;
        int step = nodes[start].BirthStep;

        for (int k = 0; k < segments && nodes.Count < settings.MaxNodes; k++)
        {
            step++;
            direction += TreeRandom.InUnitSphere(random) * settings.BranchJitter * 0.5f;
            direction = Vector3.Slerp(direction.normalized, Vector3.up, 0.06f).normalized;
            position += direction * settings.SegmentLength;

            SpaceColonization.AddNode(nodes, position, parent, step);
            parent = nodes.Count - 1;
        }

        float cloudRadius = variant.CrownRadius * 0.35f * TreeRandom.Range(random, 0.8f, 1.2f);
        Vector3 cloudScale = new Vector3(cloudRadius, cloudRadius * 0.7f, cloudRadius);
        Vector3 cloudCenter = position + outward * cloudRadius * 0.3f + Vector3.up * cloudRadius * 0.4f;
        int count = Mathf.RoundToInt(settings.AttractorCount * LowBranchCloud);

        for (int i = 0; i < count; i++)
        {
            skeleton.Attractors.Add(cloudCenter + Vector3.Scale(TreeRandom.InUnitSphere(random), cloudScale));
        }
    }

    private static Vector3 Horizontal(System.Random random)
    {
        Vector3 direction = TreeRandom.OnUnitSphere(random);
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.right;
        }

        return direction.normalized;
    }
}
