using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GrassPreviewBuffer
{
    private static GraphicsBuffer _buffer;

    static GrassPreviewBuffer()
    {
        _buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, GrassInstance.Stride);
        _buffer.SetData(new GrassInstance[1]);
        Shader.SetGlobalBuffer("_Instances", _buffer);

        AssemblyReloadEvents.beforeAssemblyReload += Release;
        EditorApplication.quitting += Release;
    }

    private static void Release()
    {
        if (_buffer != null)
        {
            _buffer.Release();
            _buffer = null;
        }
    }
}
