using System.Collections.Generic;
using UnityEngine;

public static class TreeSkeletonBuilder
{
    private const float MaxSinkAngle = 60f;
    private const float RootDelay = 0.4f;

    public static TreeSkeleton Build(TreeSettings settings, Vector3 groundNormal, System.Func<Vector3, float> groundHeight, int seed)
    {
        TreeSkeleton skeleton = new TreeSkeleton();
        System.Random random = new System.Random(seed);
        TreeVariant variant = TreeVariant.Create(settings, random);

        skeleton.TrunkSteps = SpaceColonization.GrowTrunk(settings, variant, groundNormal, random, skeleton);
        SpaceColonization.SampleCrown(settings, variant, random, skeleton);
        TreeFeatures.AddBranches(settings, variant, random, skeleton);
        skeleton.RefreshSteps(settings.LeafGrowSteps);

        SpaceColonization.Colonize(settings, variant, random, skeleton);
        BranchSmoother.Smooth(settings, skeleton.Nodes);
        skeleton.RefreshSteps(settings.LeafGrowSteps);
        StoreFinalRadii(settings, skeleton);

        TwigBuilder.Build(settings, variant, random, skeleton.Nodes);
        skeleton.RefreshSteps(settings.LeafGrowSteps);
        StoreFinalRadii(settings, skeleton);

        TreeFeatures.AddHole(settings, random, skeleton);
        SinkIntoGround(groundNormal, skeleton);

        int rootStart = skeleton.TrunkSteps + Mathf.RoundToInt((skeleton.LastStep - skeleton.TrunkSteps) * RootDelay);
        RootBuilder.Build(settings, groundHeight, random, skeleton.Nodes, rootStart, skeleton.LastStep);
        LeafPlacer.Place(settings, variant, random, skeleton);
        skeleton.RefreshSteps(settings.LeafGrowSteps);

        return skeleton;
    }

    private static void StoreFinalRadii(TreeSettings settings, TreeSkeleton skeleton)
    {
        GrowthState state = new GrowthState();
        TreeGrowth.Evaluate(skeleton, settings, 1f, state);

        for (int i = 0; i < skeleton.Nodes.Count; i++)
        {
            skeleton.Nodes[i].Radius = state.Radii[i];
        }
    }

    private static void SinkIntoGround(Vector3 groundNormal, TreeSkeleton skeleton)
    {
        List<TreeNode> nodes = skeleton.Nodes;
        List<Vector3> attractors = skeleton.Attractors;

        float slope = Mathf.Min(Vector3.Angle(Vector3.up, groundNormal), MaxSinkAngle);
        float trunkRadius = nodes[0].Radius;
        Vector3 offset = Vector3.down * (trunkRadius * Mathf.Tan(slope * Mathf.Deg2Rad) + trunkRadius * 0.2f);

        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].Position += offset;
        }

        for (int i = 0; i < attractors.Count; i++)
        {
            attractors[i] += offset;
        }

        skeleton.CrownCenter += offset;
    }
}
