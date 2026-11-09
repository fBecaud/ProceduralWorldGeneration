using System.Collections.Generic;
using UnityEngine;

public static class LeafPlacer
{
    public static void Place(TreeSettings settings, TreeVariant variant, System.Random random, TreeSkeleton skeleton)
    {
        List<TreeNode> nodes = skeleton.Nodes;
        List<TreeLeaf> leaves = skeleton.Leaves;
        leaves.Clear();

        if (settings.LeavesPerNode == 0)
        {
            return;
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            TreeNode node = nodes[i];
            if (node.IsRoot || node.Parent < 0)
            {
                continue;
            }

            Vector3 branch = (node.Position - nodes[node.Parent].Position).normalized;
            Vector3 outward = node.Position - skeleton.CrownCenter;
            if (outward.sqrMagnitude < 0.0001f)
            {
                outward = Vector3.up;
            }

            outward.Normalize();

            for (int j = 0; j < settings.LeavesPerNode; j++)
            {
                Vector3 axis = branch + Vector3.up * settings.LeafUpward + outward * settings.LeafOutward + TreeRandom.InUnitSphere(random) * settings.LeafSpread;
                if (axis.sqrMagnitude < 0.0001f)
                {
                    axis = Vector3.up;
                }

                axis.Normalize();

                Vector3 side = Vector3.Cross(axis, TreeRandom.OnUnitSphere(random));
                if (side.sqrMagnitude < 0.0001f)
                {
                    side = Vector3.Cross(axis, Vector3.right);
                }

                side.Normalize();

                float along = TreeRandom.Range(random, 0.2f, 1f);
                float size = settings.LeafSize * TreeRandom.Range(random, 0.7f, 1.3f);
                float shade = Mathf.Clamp01(variant.LeafShade * 0.6f + TreeRandom.Value(random) * 0.4f);
                leaves.Add(new TreeLeaf(i, along, axis, side, size, shade));
            }
        }
    }
}
