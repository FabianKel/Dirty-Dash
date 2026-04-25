#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class IndustrialTilesetGenerator
{
    const string TilesSpriteFolder = "Assets/Sprites/Tiles";
    const int PixelsPerUnit = 85;

    [MenuItem("Tools/Dirty Dash/Configure Tile Sprites")]
    public static void ConfigureTileSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TilesSpriteFolder });
        int changed = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png")) continue;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool dirty = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }

            if (importer.spritePixelsPerUnit != PixelsPerUnit)
            {
                importer.spritePixelsPerUnit = PixelsPerUnit;
                dirty = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                dirty = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }

            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
                changed++;
            }
        }

        Debug.Log($"Configured {changed} tile textures under {TilesSpriteFolder} (PPU={PixelsPerUnit}, Point, Uncompressed).");
    }
}
#endif
