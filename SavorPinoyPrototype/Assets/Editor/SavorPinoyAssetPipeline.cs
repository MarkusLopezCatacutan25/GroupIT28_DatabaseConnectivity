using System;
using UnityEditor;
using UnityEngine;

// Applies only to first import: authored sprite borders, slicing and existing GUIDs are preserved.
public sealed class SavorPinoyAssetPipeline : AssetPostprocessor
{
    public override uint GetVersion() => 1;

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Sprites/", StringComparison.Ordinal) || !assetImporter.importSettingsMissing) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
