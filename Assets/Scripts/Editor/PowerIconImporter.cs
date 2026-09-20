#if UNITY_EDITOR
using UnityEditor;

public sealed class PowerIconImporter : AssetPostprocessor
{
    private const string Folder = "Assets/Resources/UI/Generated/";

    [InitializeOnLoadMethod]
    private static void ConfigureExistingIcons()
    {
        EditorApplication.delayCall += () =>
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer ||
                    importer.textureType == TextureImporterType.Sprite && importer.maxTextureSize == 512) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
        };
    }

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder) || !assetPath.EndsWith(".png")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 512;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
    }
}
#endif
