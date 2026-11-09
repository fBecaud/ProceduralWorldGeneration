using System.IO;
using UnityEditor;
using UnityEngine;

public static class BarkTextureExporter
{
    private const string Path = "Assets/Tree/Bark.png";

    [MenuItem("Tools/Tree/Export Bark Texture")]
    private static void Export()
    {
        Texture2D texture = BarkTexture.Create(512);
        byte[] png = texture.EncodeToPNG();
        Object.DestroyImmediate(texture);

        File.WriteAllBytes(Path, png);
        AssetDatabase.ImportAsset(Path);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(Path);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.SaveAndReimport();

        Debug.Log("Bark texture saved to " + Path);
    }
}
