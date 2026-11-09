using System.Collections.Generic;
using UnityEngine;

public static class TreeGrowth
{
    public static void Evaluate(TreeSkeleton skeleton, TreeSettings settings, float growth, GrowthState state)
    {
        List<TreeNode> nodes = skeleton.Nodes;
        List<TreeLeaf> leaves = skeleton.Leaves;

        state.Resize(nodes.Count, leaves.Count);

        growth = Mathf.Clamp01(growth);
        float currentStep = growth * skeleton.TotalSteps;
        float sizeScale = Mathf.Lerp(settings.SaplingScale, 1f, growth);
        float radiusScale = Mathf.Sqrt(sizeScale);
        state.SizeScale = sizeScale;

        for (int i = 0; i < nodes.Count; i++)
        {
            TreeNode node = nodes[i];
            float progress = Mathf.Clamp01(currentStep - node.BirthStep);
            state.Progress[i] = progress;

            if (node.Parent < 0)
            {
                state.Positions[i] = node.Position * sizeScale;
            }
            else if (node.IsRoot)
            {
                state.Positions[i] = Vector3.Lerp(nodes[node.Parent].Position, node.Position, progress);
            }
            else
            {
                state.Positions[i] = Vector3.Lerp(nodes[node.Parent].Position, node.Position, progress) * sizeScale;
            }
        }

        System.Array.Clear(state.Carried, 0, state.Carried.Length);
        float footHeight = 0f;
        if (nodes.Count > 0)
        {
            footHeight = nodes[0].Position.y;
        }

        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            TreeNode node = nodes[i];
            if (node.IsRoot)
            {
                continue;
            }

            float progress = state.Progress[i];
            if (progress <= 0f)
            {
                state.Radii[i] = 0f;
                continue;
            }

            float radius = settings.TipRadius * progress;
            if (state.Carried[i] > 0f)
            {
                radius = Mathf.Max(radius, Mathf.Pow(state.Carried[i], 1f / settings.PipeExponent));
            }

            if (node.Parent >= 0)
            {
                state.Carried[node.Parent] += Mathf.Pow(radius, settings.PipeExponent);
            }

            state.Radii[i] = radius * Flare(settings, node.Position.y - footHeight) * radiusScale;
        }

        float trunkRatio = 1f;
        if (nodes.Count > 1 && nodes[1].Radius > 0f)
        {
            trunkRatio = state.Radii[1] / nodes[1].Radius;
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            TreeNode node = nodes[i];
            if (!node.IsRoot)
            {
                continue;
            }

            state.Radii[i] = node.Radius * trunkRatio * state.Progress[i];
        }

        float leafSizeScale = Mathf.Lerp(0.5f, 1f, sizeScale);
        float shedStart = settings.LeafMaxRadius;
        float shedEnd = settings.LeafMaxRadius * 1.5f;

        for (int l = 0; l < leaves.Count; l++)
        {
            int n = leaves[l].Node;
            float age = currentStep - nodes[n].BirthStep - 1f;
            float grow = Mathf.Clamp01(age / settings.LeafGrowSteps);
            float shed = Mathf.Clamp01((shedEnd - state.Radii[n]) / (shedEnd - shedStart));
            state.LeafScales[l] = grow * shed * leafSizeScale;
        }
    }

    private static float Flare(TreeSettings settings, float height)
    {
        if (settings.FlareHeight <= 0f || height >= settings.FlareHeight)
        {
            return 1f;
        }

        float t = 1f - height / settings.FlareHeight;
        return 1f + settings.Flare * t * t;
    }
}
