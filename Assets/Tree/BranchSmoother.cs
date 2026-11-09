using System.Collections.Generic;
using UnityEngine;

public static class BranchSmoother
{
    private const int Passes = 2;

    public static void Smooth(TreeSettings settings, List<TreeNode> nodes)
    {
        if (settings.Smoothing <= 0f)
        {
            return;
        }

        Vector3[] smoothed = new Vector3[nodes.Count];
        float amount = settings.Smoothing * 0.5f;

        for (int pass = 0; pass < Passes; pass++)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                TreeNode node = nodes[i];
                smoothed[i] = node.Position;

                if (node.Parent < 0 || node.IsRoot || node.Children.Count == 0)
                {
                    continue;
                }

                Vector3 average = Vector3.zero;
                for (int c = 0; c < node.Children.Count; c++)
                {
                    average += nodes[node.Children[c]].Position;
                }

                average /= node.Children.Count;

                Vector3 target = (nodes[node.Parent].Position + average) * 0.5f;
                smoothed[i] = Vector3.Lerp(node.Position, target, amount);
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Position = smoothed[i];
            }
        }
    }
}
