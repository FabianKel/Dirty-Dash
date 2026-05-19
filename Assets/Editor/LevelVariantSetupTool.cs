using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class LevelVariantSetupTool
{
    private const string MainScenePath = "Assets/Scenes/MainGame.unity";
    private const string Level2ScenePath = "Assets/Scenes/MainGame_Level2.unity";
    private const string Level3ScenePath = "Assets/Scenes/MainGame_Level3.unity";
    private const string Level2SpritesFolder = "Assets/Sprites/Tiles/2ndTileset";
    private const string Level3SpritesFolder = "Assets/Sprites/Tiles/3rdTileset";
    private const string GeneratedTilesRoot = "Assets/Sprites/Tiles/Generated";
    private const string GeneratedLevel2Tiles = "Assets/Sprites/Tiles/Generated/Level2";
    private const string GeneratedLevel3Tiles = "Assets/Sprites/Tiles/Generated/Level3";
    private const string CuratedLevel2Tiles = "Assets/Sprites/Tiles/Generated/Level2_Curated";
    private const string OriginalLevel2TilesFolder = "Assets/Sprites/Tiles/2ndTileset";
    private static readonly HashSet<int> NonSolidMorningSheetIndices = new HashSet<int>
    {
        0, 1, 2, 3,
        14, 15, 16, 17,
        24, 25, 26, 27, 28, 29,
        30, 31, 32, 33,
        40, 41, 42
    };

    [MenuItem("Tools/Dirty Dash/Fix Level 2 Tile Colliders")]
    public static void FixLevel2TileColliders()
    {
        NormalizeLevel2TileColliders(OriginalLevel2TilesFolder);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Original 2ndTileset colliders normalized by MorningSheet index rules.");
    }

    [MenuItem("Tools/Dirty Dash/Rebuild Level 2 Layout")]
    public static void RebuildLevel2Layout()
    {
        var scene = EditorSceneManager.OpenScene(Level2ScenePath, OpenSceneMode.Single);
        NormalizeLevel2TileColliders(OriginalLevel2TilesFolder);

        // Use original user tiles directly from 2ndTileset (no generated remap).
        Tile bodyTile = LoadMorningSheetTileAsset(20);
        Tile bodyTileAlt = LoadMorningSheetTileAsset(21) ?? bodyTile;
        Tile topTile = LoadMorningSheetTileAsset(34) ?? bodyTile;
        Tile topTileAlt = LoadMorningSheetTileAsset(35) ?? topTile;
        Tile oneWayTile = LoadMorningSheetTileAsset(36) ?? topTile;
        Tile fakeTile = LoadMorningSheetTileAsset(1) ?? topTile;
        Tile ceilingTile = LoadMorningSheetTileAsset(4) ?? topTile;

        if (bodyTile == null)
            throw new InvalidOperationException("No solid tiles found in Level2 generated tiles folder.");

        var grid = GameObject.Find("Grid");
        if (grid == null)
            throw new InvalidOperationException("Grid object not found in MainGame_Level2.");

        grid.tag = "Ground";
        grid.layer = 3;

        for (int i = grid.transform.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(grid.transform.GetChild(i).gameObject);

        Tilemap ground = CreateTilemap(grid.transform, "Tilemap", "Ground", 3, withCollider: true, oneWay: false);
        Tilemap fake = CreateTilemap(grid.transform, "FakeSpikesEnd", "_FALSE", 3, withCollider: true, oneWay: false);
        Tilemap oneWay = CreateTilemap(grid.transform, "OneWayPlatforms", "Ground", 3, withCollider: true, oneWay: true);

        ground.ClearAllTiles();
        fake.ClearAllTiles();
        oneWay.ClearAllTiles();

        // Ceiling/floor boundaries
        FillRect(ground, ceilingTile, -64, 66, -13, -12);
        FillRect(ground, ceilingTile, -64, 66, 18, 19);

        // Top path
        PaintPlatform(ground, bodyTile, topTile, -58, -44, 9, 10);
        PaintPlatform(ground, bodyTileAlt, topTileAlt, -39, -31, 11, 12);
        PaintPlatform(ground, bodyTile, topTile, -26, -17, 13, 14);
        PaintPlatform(ground, bodyTileAlt, topTileAlt, -11, -2, 10, 11);
        PaintPlatform(ground, bodyTileAlt, topTileAlt, 4, 12, 12, 13);
        PaintPlatform(ground, bodyTile, topTile, 18, 26, 14, 15);
        PaintPlatform(ground, bodyTileAlt, topTileAlt, 32, 40, 12, 13);
        PaintPlatform(ground, bodyTileAlt, topTileAlt, 46, 60, 10, 11);

        PaintPlatform(ground, bodyTile, topTile, -42, -41, 10, 14);
        PaintPlatform(ground, bodyTile, topTile, -14, -13, 10, 15);
        PaintPlatform(ground, bodyTile, topTile, 28, 29, 12, 16);

        FillLine(oneWay, oneWayTile, -33, -29, 13);
        FillLine(oneWay, oneWayTile, -19, -15, 15);
        FillLine(oneWay, oneWayTile, -4, 1, 12);
        FillLine(oneWay, oneWayTile, 12, 16, 14);
        FillLine(oneWay, oneWayTile, 27, 31, 16);
        FillLine(oneWay, oneWayTile, 41, 45, 14);

        FillLine(fake, fakeTile, -30, -27, 10);
        FillLine(fake, fakeTile, -16, -12, 9);
        FillLine(fake, fakeTile, 0, 3, 9);
        FillLine(fake, fakeTile, 16, 17, 11);
        FillLine(fake, fakeTile, 30, 31, 11);
        FillLine(fake, fakeTile, 44, 45, 10);

        // Bottom mirrored path (same challenge for player 2)
        int[,] topRects =
        {
            { -58, -44, 9, 10 }, { -39, -31, 11, 12 }, { -26, -17, 13, 14 },
            { -11, -2, 10, 11 }, { 4, 12, 12, 13 }, { 18, 26, 14, 15 },
            { 32, 40, 12, 13 }, { 46, 60, 10, 11 }
        };

        for (int i = 0; i < topRects.GetLength(0); i++)
        {
            int x0 = topRects[i, 0];
            int x1 = topRects[i, 1];
            int y0 = topRects[i, 2];
            int y1 = topRects[i, 3];
            Tile body = i % 2 == 0 ? bodyTile : bodyTileAlt;
            Tile top = i % 2 == 0 ? topTile : topTileAlt;
            PaintPlatform(ground, body, top, x0, x1, MirrorY(y1), MirrorY(y0));
        }

        PaintPlatform(ground, bodyTile, topTile, -42, -41, MirrorY(14), MirrorY(10));
        PaintPlatform(ground, bodyTile, topTile, -14, -13, MirrorY(15), MirrorY(10));
        PaintPlatform(ground, bodyTile, topTile, 28, 29, MirrorY(16), MirrorY(12));

        FillLine(oneWay, oneWayTile, -33, -29, MirrorY(13));
        FillLine(oneWay, oneWayTile, -19, -15, MirrorY(15));
        FillLine(oneWay, oneWayTile, -4, 1, MirrorY(12));
        FillLine(oneWay, oneWayTile, 12, 16, MirrorY(14));
        FillLine(oneWay, oneWayTile, 27, 31, MirrorY(16));
        FillLine(oneWay, oneWayTile, 41, 45, MirrorY(14));

        FillLine(fake, fakeTile, -30, -27, MirrorY(10));
        FillLine(fake, fakeTile, -16, -12, MirrorY(9));
        FillLine(fake, fakeTile, 0, 3, MirrorY(9));
        FillLine(fake, fakeTile, 16, 17, MirrorY(11));
        FillLine(fake, fakeTile, 30, 31, MirrorY(11));
        FillLine(fake, fakeTile, 44, 45, MirrorY(10));

        // End connector and finish funnel
        PaintPlatform(ground, bodyTileAlt, topTileAlt, 54, 56, -1, 7);
        FillLine(oneWay, oneWayTile, 51, 54, 8);
        FillLine(oneWay, oneWayTile, 51, 54, -2);

        SetTransformPosition("Player1", new Vector3(-54f, 11.2f, 0f));
        SetTransformPosition("Player2", new Vector3(-54f, -2.2f, 0f));
        SetTransformPosition("WinGoal", new Vector3(58f, 3f, 0f));
        SetTransformPosition("RandomPickupSpawner", new Vector3(8f, 12.6f, 0f));
        SetTransformPosition("RandomPickupSpawner (1)", new Vector3(8f, -3.4f, 0f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("MainGame_Level2 rebuilt with new mirrored top/bottom layout.");
    }

    [MenuItem("Tools/Dirty Dash/Setup Level Variants")]
    public static void SetupLevelVariants()
    {
        if (!File.Exists(MainScenePath))
            throw new FileNotFoundException("MainGame scene not found", MainScenePath);

        string sourceSceneText = File.ReadAllText(MainScenePath);
        List<(Tile tile, string guid)> sourceTiles = CollectSourceTilesFromScene(sourceSceneText);

        if (sourceTiles.Count == 0)
            throw new InvalidOperationException("No tile assets were found in MainGame scene data.");

        List<Sprite> spritesLevel2 = LoadSpritesFromFolder(Level2SpritesFolder);
        List<Sprite> spritesLevel3 = LoadSpritesFromFolder(Level3SpritesFolder);

        if (spritesLevel2.Count == 0)
            throw new InvalidOperationException("No sprites found for 2nd tileset.");
        if (spritesLevel3.Count == 0)
            throw new InvalidOperationException("No sprites found for 3rd tileset.");

        EnsureFolder(GeneratedTilesRoot, "Assets/Sprites/Tiles", "Generated");
        EnsureFolder(GeneratedLevel2Tiles, GeneratedTilesRoot, "Level2");
        EnsureFolder(GeneratedLevel3Tiles, GeneratedTilesRoot, "Level3");

        Dictionary<string, string> mapLevel2 = BuildTileVariantMap(sourceTiles, spritesLevel2, GeneratedLevel2Tiles, "L2");
        Dictionary<string, string> mapLevel3 = BuildTileVariantMap(sourceTiles, spritesLevel3, GeneratedLevel3Tiles, "L3");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        WriteVariantScene(Level2ScenePath, sourceSceneText, mapLevel2);
        WriteVariantScene(Level3ScenePath, sourceSceneText, mapLevel3);

        ConfigureBuildSettings();
        ConfigureMainMenuButtons();

        Debug.Log($"Level variants configured. Source tiles: {sourceTiles.Count}, L2 sprites: {spritesLevel2.Count}, L3 sprites: {spritesLevel3.Count}");
    }

    private static List<(Tile tile, string guid)> CollectSourceTilesFromScene(string sceneText)
    {
        var regex = new System.Text.RegularExpressions.Regex(@"guid: ([0-9a-f]{32}), type: 2");
        var matches = regex.Matches(sceneText);
        var guids = new HashSet<string>();
        var result = new List<(Tile tile, string guid)>();

        for (int i = 0; i < matches.Count; i++)
        {
            string guid = matches[i].Groups[1].Value;
            if (!guids.Add(guid))
                continue;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                continue;

            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
                continue;

            result.Add((tile, guid));
        }

        result.Sort((a, b) => string.CompareOrdinal(a.tile.name + "|" + a.guid, b.tile.name + "|" + b.guid));
        return result;
    }

    private static List<Sprite> LoadSpritesFromFolder(string folder)
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        var texturePaths = textureGuids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToList();
        var sprites = new List<Sprite>();

        foreach (string texturePath in texturePaths)
        {
            var fromTexture = AssetDatabase.LoadAllAssetsAtPath(texturePath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name);

            sprites.AddRange(fromTexture);
        }

        return sprites;
    }

    private static Dictionary<string, string> BuildTileVariantMap(
        List<(Tile tile, string guid)> sourceTiles,
        List<Sprite> targetSprites,
        string outputFolder,
        string suffix)
    {
        var map = new Dictionary<string, string>();

        for (int i = 0; i < sourceTiles.Count; i++)
        {
            Tile sourceTile = sourceTiles[i].tile;
            string sourceGuid = sourceTiles[i].guid;
            Sprite sprite = targetSprites[i % targetSprites.Count];

            string safeName = sourceTile.name.Replace('/', '_').Replace('\\', '_').Replace(':', '_');
            string tilePath = $"{outputFolder}/{safeName}_{suffix}.asset";

            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            tile.sprite = sprite;
            tile.color = sourceTile.color;
            tile.transform = sourceTile.transform;
            tile.flags = sourceTile.flags;
            tile.colliderType = sourceTile.colliderType;

            EditorUtility.SetDirty(tile);
            map[sourceGuid] = AssetDatabase.AssetPathToGUID(tilePath);
        }

        return map;
    }

    private static void WriteVariantScene(string scenePath, string sourceSceneText, Dictionary<string, string> guidMap)
    {
        string sceneText = sourceSceneText;
        foreach (var pair in guidMap)
        {
            sceneText = sceneText.Replace($"guid: {pair.Key}, type: 2", $"guid: {pair.Value}, type: 2");
        }

        File.WriteAllText(scenePath, sceneText, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceSynchronousImport);
    }

    private static void ConfigureBuildSettings()
    {
        var desiredScenePaths = new[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/MainGame.unity",
            "Assets/Scenes/MainGame_Level2.unity",
            "Assets/Scenes/MainGame_Level3.unity"
        };

        var current = EditorBuildSettings.scenes;
        var enabledByPath = new Dictionary<string, bool>();
        foreach (var scene in current)
            enabledByPath[scene.path] = scene.enabled;

        var updated = new List<EditorBuildSettingsScene>();
        foreach (string path in desiredScenePaths)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Scene missing for build settings", path);

            bool enabled = enabledByPath.TryGetValue(path, out bool wasEnabled) ? wasEnabled : true;
            updated.Add(new EditorBuildSettingsScene(path, enabled));
        }

        EditorBuildSettings.scenes = updated.ToArray();
    }

    private static void ConfigureMainMenuButtons()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

        var sceneManagerObject = GameObject.Find("SceneManager");
        if (sceneManagerObject == null)
            throw new InvalidOperationException("MainMenu is missing SceneManager GameObject.");

        var navigation = sceneManagerObject.GetComponent<SceneNavigationManager>();
        if (navigation == null)
            throw new InvalidOperationException("SceneManager is missing SceneNavigationManager component.");

        var buttonsContainer = GameObject.Find("UI/Canvas/MainMenuPanel/Buttons")?.transform;
        if (buttonsContainer == null)
            throw new InvalidOperationException("MainMenu buttons container not found.");

        Button level1 = null;
        Button level2 = null;
        Button level3 = null;
        Button settings = null;
        Button quit = null;

        for (int i = 0; i < buttonsContainer.childCount; i++)
        {
            Transform child = buttonsContainer.GetChild(i);
            var button = child.GetComponent<Button>();
            if (button == null)
                continue;

            switch (child.name)
            {
                case "PlayButton":
                case "Level1Button":
                    level1 = button;
                    break;
                case "Level2Button":
                    level2 = button;
                    break;
                case "Level3Button":
                    level3 = button;
                    break;
                case "SettingsButton":
                    settings = button;
                    break;
                case "QuitButton":
                    quit = button;
                    break;
            }
        }

        if (level1 == null)
            throw new InvalidOperationException("PlayButton / Level1Button not found.");

        level1.gameObject.name = "Level1Button";
        ConfigureLevelButton(level1, navigation.SelectLevel1, "Nivel 1");

        if (level2 == null)
        {
            level2 = UnityEngine.Object.Instantiate(level1, buttonsContainer);
            level2.gameObject.name = "Level2Button";
        }
        ConfigureLevelButton(level2, navigation.SelectLevel2, "Nivel 2");

        if (level3 == null)
        {
            level3 = UnityEngine.Object.Instantiate(level1, buttonsContainer);
            level3.gameObject.name = "Level3Button";
        }
        ConfigureLevelButton(level3, navigation.SelectLevel3, "Nivel 3");

        level1.transform.SetSiblingIndex(0);
        level2.transform.SetSiblingIndex(1);
        level3.transform.SetSiblingIndex(2);
        if (settings != null) settings.transform.SetSiblingIndex(3);
        if (quit != null) quit.transform.SetSiblingIndex(4);

        EditorUtility.SetDirty(navigation);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureLevelButton(Button button, UnityEngine.Events.UnityAction callback, string label)
    {
        button.onClick.RemoveAllListeners();
        while (button.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(button.onClick, 0);

        UnityEventTools.AddPersistentListener(button.onClick, callback);

        var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
            text.text = label;

        EditorUtility.SetDirty(button);
        if (text != null)
            EditorUtility.SetDirty(text);
    }

    private static void EnsureFolder(string folderPath, string parentPath, string folderName)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder(parentPath, folderName);
    }

    private static Tile LoadTileByName(string tileName)
    {
        string[] guids = AssetDatabase.FindAssets($"{tileName} t:Tile", new[] { GeneratedLevel2Tiles });
        if (guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<Tile>(path);
    }

    private static Tilemap CreateTilemap(Transform parent, string name, string tag, int layer, bool withCollider, bool oneWay)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.tag = tag;
        go.layer = layer;

        var tilemap = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();

        if (withCollider)
        {
            var collider = go.AddComponent<TilemapCollider2D>();
            collider.extrusionFactor = 0.05f;

            if (oneWay)
            {
                collider.usedByEffector = true;
                var effector = go.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.surfaceArc = 180f;
            }
        }

        return tilemap;
    }

    private static void FillRect(Tilemap tilemap, Tile tile, int x0, int x1, int y0, int y1)
    {
        for (int x = x0; x <= x1; x++)
        {
            for (int y = y0; y <= y1; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }

    private static void FillLine(Tilemap tilemap, Tile tile, int x0, int x1, int y)
    {
        for (int x = x0; x <= x1; x++)
        {
            tilemap.SetTile(new Vector3Int(x, y, 0), tile);
        }
    }

    private static int MirrorY(int y)
    {
        return 6 - y;
    }

    private static void SetTransformPosition(string objectName, Vector3 position)
    {
        var go = GameObject.Find(objectName);
        if (go != null)
            go.transform.position = position;
    }

    private static void PaintPlatform(Tilemap tilemap, Tile body, Tile top, int x0, int x1, int y0, int y1)
    {
        FillRect(tilemap, body, x0, x1, y0, y1);
        FillLine(tilemap, top, x0, x1, y1);
    }

    private static Tile GetOrCreateCuratedTile(string assetName, string spriteName, Tile.ColliderType colliderType)
    {
        string path = $"{CuratedLevel2Tiles}/{assetName}.asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }

        Sprite sprite = LoadSpriteFromMorningSheet(spriteName);
        if (sprite == null)
            return tile.sprite != null ? tile : null;

        tile.sprite = sprite;
        tile.color = Color.white;
        tile.transform = Matrix4x4.identity;
        tile.flags = TileFlags.LockColor;
        tile.colliderType = colliderType;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static Sprite LoadSpriteFromMorningSheet(string spriteName)
    {
        string texturePath = "Assets/Sprites/Tiles/2ndTileset/MorningSheet.png";
        var assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        for (int i = 0; i < assets.Length; i++)
        {
            var sprite = assets[i] as Sprite;
            if (sprite != null && sprite.name == spriteName)
                return sprite;
        }

        return null;
    }

    private static Tile LoadMorningSheetTileAsset(int index)
    {
        string path = $"{OriginalLevel2TilesFolder}/MorningSheet_{index}.asset";
        return AssetDatabase.LoadAssetAtPath<Tile>(path);
    }

    private static void NormalizeLevel2TileColliders(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
            return;

        string[] guids = AssetDatabase.FindAssets("t:Tile", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null || tile.sprite == null)
                continue;

            int index = ParseMorningSheetIndex(tile.sprite.name);
            if (index < 0)
                continue;

            Tile.ColliderType targetCollider = NonSolidMorningSheetIndices.Contains(index)
                ? Tile.ColliderType.None
                : Tile.ColliderType.Sprite;

            if (tile.colliderType != targetCollider)
            {
                tile.colliderType = targetCollider;
                EditorUtility.SetDirty(tile);
            }
        }
    }

    private static int ParseMorningSheetIndex(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
            return -1;

        const string prefix = "MorningSheet_";
        if (!spriteName.StartsWith(prefix, StringComparison.Ordinal))
            return -1;

        string num = spriteName.Substring(prefix.Length);
        return int.TryParse(num, out int index) ? index : -1;
    }
}
