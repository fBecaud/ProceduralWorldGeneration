public static class TreeMeshBuilder
{
    public static void Build(MeshData data, TreeSkeleton skeleton, GrowthState state, TreeSettings settings)
    {
        TreeShading shading = new TreeShading(settings, skeleton, state);

        data.Clear();
        TubeMeshBuilder.Build(data, skeleton, state, settings, shading);

        data.ShadowStart = data.Vertices.Count;
        if (settings.CastShadows)
        {
            LeafMeshBuilder.BuildShadow(data, skeleton, state, settings, shading);
        }

        data.LeafStart = data.Vertices.Count;
        LeafMeshBuilder.Build(data, skeleton, state, settings, shading);
    }
}
