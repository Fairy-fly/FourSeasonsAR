using System;
using UnityEditor;

public sealed class GalleryAssetImport : AssetPostprocessor
{
    bool IsGalleryAsset => assetPath.StartsWith("Assets/Resources/Gallery/", StringComparison.Ordinal);

    void OnPreprocessModel()
    {
        if (!IsGalleryAsset) return;
        var model = (ModelImporter)assetImporter;
        model.materialImportMode = ModelImporterMaterialImportMode.None;
        model.importAnimation = false;
        model.importCameras = false;
        model.importLights = false;
        model.importNormals = ModelImporterNormals.Import;
        model.importTangents = ModelImporterTangents.CalculateMikk;
    }

    void OnPreprocessTexture()
    {
        if (!IsGalleryAsset) return;
        var texture = (TextureImporter)assetImporter;
        bool portrait = assetPath.EndsWith("/Portrait.png", StringComparison.Ordinal);
        bool color = portrait || assetPath.EndsWith("/Color.png", StringComparison.Ordinal);
        bool normal = assetPath.EndsWith("/Normal.png", StringComparison.Ordinal);
        texture.textureType = portrait ? TextureImporterType.Sprite : normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (portrait) { texture.npotScale = TextureImporterNPOTScale.None; texture.spriteImportMode = SpriteImportMode.Single; texture.alphaIsTransparency = true; }
        texture.sRGBTexture = color;
        texture.mipmapEnabled = !portrait;
        texture.anisoLevel = 2;
        var android = texture.GetPlatformTextureSettings("Android");
        android.overridden = true;
        android.maxTextureSize = color ? 2048 : 1024;
        // ETC2 is part of the app's OpenGL ES 3 target; normals need both packed channels.
        android.format = normal || portrait ? TextureImporterFormat.ETC2_RGBA8 : TextureImporterFormat.ETC2_RGB4;
        texture.SetPlatformTextureSettings(android);
    }
}
