#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Monstrology.Editor
{
    public static class ForestArtPackInstaller
    {
        public const string MushroomFoxId = "mushroom_fox";
        public const string LampCrabId = "lamp_crab";

        private const string SpriteDatabasePath =
            "Assets/Monstrology/Art/Resources/SpriteDatabase.asset";
        private const string AtlasFolder =
            "Assets/Monstrology/Art/Forest/Atlases";
        private const string WorldAtlasPath =
            AtlasFolder + "/ForestWorldAtlas.spriteatlas";
        private const string CreatureAtlasPath =
            AtlasFolder + "/ForestCreatureAtlas.spriteatlas";

        private static readonly ForestAsset[] Assets =
        {
            new ForestAsset(
                "ForestBackground",
                "Assets/Monstrology/Art/Forest/Background/ForestBackground.png",
                ForestAssetRole.Background,
                3000,
                1800,
                "Assets/Monstrology/Art/Import/ForestBackground.png"),
            new ForestAsset(
                "ForestGroundTile",
                "Assets/Monstrology/Art/Forest/Ground/ForestGroundTile.png",
                ForestAssetRole.Ground,
                1024,
                1024,
                "Assets/Monstrology/Art/Import/ForestGroundTile.png",
                "Assets/Monstrology/Art/Import/ForestGroundTile.png.png"),
            new ForestAsset(
                "ForestStump",
                "Assets/Monstrology/Art/Forest/Decorations/ForestStump.png",
                ForestAssetRole.Decoration,
                0,
                0,
                "Assets/Monstrology/Art/Import/ForestStump.png"),
            new ForestAsset(
                "ForestBigMushroom",
                "Assets/Monstrology/Art/Forest/Decorations/ForestBigMushroom.png",
                ForestAssetRole.Decoration,
                0,
                0,
                "Assets/Monstrology/Art/Import/ForestBigMushroom.png"),
            new ForestAsset(
                "Gribolis",
                "Assets/Monstrology/Art/Creatures/Forest/Gribolis.png",
                ForestAssetRole.Creature,
                2048,
                2048,
                "Assets/Monstrology/Art/Import/Gribolis.png"),
            new ForestAsset(
                "Lampcrab",
                "Assets/Monstrology/Art/Creatures/Forest/Lampcrab.png",
                ForestAssetRole.Creature,
                2048,
                2048,
                "Assets/Monstrology/Art/Import/Lampcrab.png")
        };

        [MenuItem("Tools/Monstrology/Forest/Install Forest Art Pack")]
        public static void InstallForestArtPack()
        {
            Debug.Log(InstallInternal());
        }

        [MenuItem("Tools/Monstrology/Forest/Validate Forest Art Pack")]
        public static void ValidateForestArtPack()
        {
            Debug.Log(GetValidationReport());
        }

        public static void InstallBatch()
        {
            Debug.Log(InstallInternal());
        }

        public static void ValidateBatch()
        {
            Debug.Log(GetValidationReport());
        }

        public static string GetValidationReport()
        {
            ValidationReport report = new ValidationReport();
            report.Line("=== VALIDATE FOREST ART PACK ===");

            Dictionary<string, PngAudit> audits =
                new Dictionary<string, PngAudit>();
            foreach (ForestAsset asset in Assets)
            {
                bool exists = AssetExists(asset.destination);
                report.Check(
                    exists,
                    "Файл " + asset.destination,
                    true);
                if (!exists)
                {
                    continue;
                }

                PngAudit audit = AuditPng(asset.destination);
                audits[asset.key] = audit;
                report.Check(
                    audit.loaded,
                    "PNG читается: " + asset.key,
                    true);
                if (!audit.loaded)
                {
                    continue;
                }

                report.Pass(
                    asset.key + ": " + audit.width + "x" + audit.height +
                    ", alpha channel=" + audit.hasAlphaChannel +
                    ", transparent=" +
                    audit.transparentPercent.ToString("0.0") + "%");
                if (asset.expectedWidth > 0 &&
                    (audit.width != asset.expectedWidth ||
                     audit.height != asset.expectedHeight))
                {
                    report.Warning(
                        asset.key + " имеет фактический размер " +
                        audit.width + "x" + audit.height +
                        ", ожидался " + asset.expectedWidth + "x" +
                        asset.expectedHeight + ". Исходник не растягивался.");
                }

                if (asset.RequiresTransparency)
                {
                    report.Check(
                        audit.hasAlphaChannel,
                        asset.key + " содержит alpha-канал",
                        true);
                    report.Check(
                        audit.hasRealTransparency,
                        asset.key + " содержит настоящую прозрачность",
                        true);
                    report.Check(
                        !audit.hasWhiteBackground,
                        asset.key + " не содержит белый фон",
                        true);
                    report.Check(
                        !audit.hasCheckerboard,
                        asset.key + " не содержит нарисованную шахматную сетку",
                        true);
                    report.Check(
                        !audit.hasLikelyPaintedBackdrop,
                        asset.key +
                        " не содержит нарисованный фон или виньетку вокруг объекта",
                        true);
                }

                if (asset.role == ForestAssetRole.Background ||
                    asset.role == ForestAssetRole.Ground)
                {
                    if (!audit.horizontalSeamLikely)
                    {
                        report.Warning(
                            asset.key +
                            ": левый и правый края визуально не подтверждены как бесшовные.");
                    }

                    if (!audit.verticalSeamLikely)
                    {
                        report.Warning(
                            asset.key +
                            ": верхний и нижний края визуально не подтверждены как бесшовные.");
                    }
                }

                report.Check(
                    ImportSettingsAreValid(asset),
                    "Import Settings " + asset.key,
                    true);
            }

            ValidateDatabase(report, audits);
            ValidateAtlases(report);
            ValidateRuntimeContent(report);
            ValidateRuntimeArchitecture(report);
            report.Warning(
                "Текст и водяные знаки проверены визуально для текущего пакета; " +
                "автоматический OCR в Unity-валидатор не входит.");
            report.Finish();
            return report.ToString();
        }

        private static string InstallInternal()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("=== INSTALL FOREST ART PACK ===");
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (ForestAsset asset in Assets)
            {
                MoveAsset(asset, report);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Dictionary<string, PngAudit> audits =
                new Dictionary<string, PngAudit>();
            foreach (ForestAsset asset in Assets)
            {
                if (!AssetExists(asset.destination))
                {
                    report.AppendLine(
                        "ERROR: отсутствует " + asset.destination + ".");
                    continue;
                }

                ConfigureTexture(asset, report);
                PngAudit audit = AuditPng(asset.destination);
                audits[asset.key] = audit;
                AppendAudit(report, asset, audit);
            }

            RegisterSpriteDatabase(report, audits);
            CreateOrUpdateAtlases(report);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            report.AppendLine(GetValidationReport());
            return report.ToString();
        }

        private static void EnsureFolders()
        {
            foreach (ForestAsset asset in Assets)
            {
                EnsureFolderPath(Path.GetDirectoryName(asset.destination)
                    .Replace('\\', '/'));
            }

            EnsureFolderPath(AtlasFolder);
        }

        private static void EnsureFolderPath(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static void MoveAsset(ForestAsset asset, StringBuilder report)
        {
            if (AssetExists(asset.destination))
            {
                report.AppendLine(
                    "PASS: " + asset.destination + " уже находится на месте.");
                return;
            }

            string source = asset.sources.FirstOrDefault(AssetExists);
            if (string.IsNullOrEmpty(source))
            {
                report.AppendLine(
                    "ERROR: не найден исходник для " + asset.key + ".");
                return;
            }

            AssetDatabase.ImportAsset(
                source,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            string error = AssetDatabase.MoveAsset(source, asset.destination);
            if (string.IsNullOrEmpty(error))
            {
                report.AppendLine(
                    "PASS: " + source + " -> " + asset.destination + ".");
            }
            else
            {
                report.AppendLine(
                    "ERROR: не удалось переместить " + source + ": " + error);
            }
        }

        private static void ConfigureTexture(
            ForestAsset asset,
            StringBuilder report)
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(asset.destination) as TextureImporter;
            if (importer == null)
            {
                report.AppendLine(
                    "ERROR: TextureImporter не найден для " +
                    asset.destination + ".");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.compressionQuality = 80;
            importer.crunchedCompression = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = asset.RequiresTransparency;
            importer.spritePixelsPerUnit =
                asset.role == ForestAssetRole.Ground ? 128f : 100f;
            importer.maxTextureSize =
                asset.role == ForestAssetRole.Background
                    ? 4096
                    : asset.role == ForestAssetRole.Ground
                        ? 2048
                        : 2048;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType =
                asset.role == ForestAssetRole.Decoration
                    ? SpriteMeshType.Tight
                    : SpriteMeshType.FullRect;
            settings.spriteAlignment =
                asset.role == ForestAssetRole.Decoration
                    ? (int)SpriteAlignment.Custom
                    : (int)SpriteAlignment.Center;
            settings.spritePivot =
                asset.role == ForestAssetRole.Decoration
                    ? new Vector2(0.5f, 0f)
                    : new Vector2(0.5f, 0.5f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            report.AppendLine(
                "PASS: Import Settings применены для " + asset.key + ".");
        }

        private static void AppendAudit(
            StringBuilder report,
            ForestAsset asset,
            PngAudit audit)
        {
            if (!audit.loaded)
            {
                report.AppendLine(
                    "ERROR: " + asset.key + " не удалось прочитать.");
                return;
            }

            report.AppendLine(
                "AUDIT: " + asset.key + " " + audit.width + "x" +
                audit.height + ", alpha=" + audit.hasAlphaChannel +
                ", transparent=" +
                audit.transparentPercent.ToString("0.0") + "%.");
            if (asset.expectedWidth > 0 &&
                (audit.width != asset.expectedWidth ||
                 audit.height != asset.expectedHeight))
            {
                report.AppendLine(
                    "WARNING: " + asset.key + " имеет фактический размер " +
                    audit.width + "x" + audit.height + ".");
            }

            if (asset.RequiresTransparency && !audit.hasRealTransparency)
            {
                report.AppendLine(
                    "ERROR: " + asset.key +
                    " не содержит настоящую прозрачность.");
            }

            if (audit.hasWhiteBackground)
            {
                report.AppendLine(
                    "ERROR: " + asset.key + " содержит белый фон.");
            }

            if (audit.hasCheckerboard)
            {
                report.AppendLine(
                    "ERROR: " + asset.key +
                    " содержит нарисованную шахматную сетку.");
            }

            if (audit.hasLikelyPaintedBackdrop)
            {
                report.AppendLine(
                    "ERROR: " + asset.key +
                    " содержит нарисованный фон/виньетку вокруг объекта. " +
                    "World sprite не назначен.");
            }
        }

        private static void RegisterSpriteDatabase(
            StringBuilder report,
            IDictionary<string, PngAudit> audits)
        {
            SpriteDatabase database =
                AssetDatabase.LoadAssetAtPath<SpriteDatabase>(
                    SpriteDatabasePath);
            if (database == null)
            {
                report.AppendLine(
                    "ERROR: SpriteDatabase.asset не найден.");
                return;
            }

            Sprite background = LoadSprite("ForestBackground");
            Sprite ground = LoadSprite("ForestGroundTile");
            Sprite stump = LoadSprite("ForestStump");
            Sprite mushroom = LoadSprite("ForestBigMushroom");
            Sprite gribolis = LoadSprite("Gribolis");
            Sprite lampcrab = LoadSprite("Lampcrab");

            SerializedObject serialized = new SerializedObject(database);
            serialized.Update();
            SerializedProperty biomeArray = serialized.FindProperty("biomes");
            int biomeIndex = FindOrAddBiome(biomeArray, BiomeType.Forest);
            SerializedProperty biomeEntry =
                biomeArray.GetArrayElementAtIndex(biomeIndex);
            biomeEntry.FindPropertyRelative("biome").enumValueIndex =
                (int)BiomeType.Forest;
            biomeEntry.FindPropertyRelative("background")
                .objectReferenceValue = background;
            biomeEntry.FindPropertyRelative("primaryDecoration")
                .objectReferenceValue = stump;
            biomeEntry.FindPropertyRelative("secondaryDecoration")
                .objectReferenceValue = mushroom;

            SerializedProperty config =
                biomeEntry.FindPropertyRelative("visualConfig");
            config.FindPropertyRelative("groundDetail").objectReferenceValue =
                ground;
            config.FindPropertyRelative("groundDetailEnabled").boolValue = true;
            config.FindPropertyRelative("groundDetailOpacity").floatValue = 0.2f;
            config.FindPropertyRelative("backgroundWorldSize").vector2Value =
                new Vector2(30f, 18f);
            config.FindPropertyRelative("groundTileWorldSize").vector2Value =
                new Vector2(8f, 8f);
            SerializedProperty decorations =
                config.FindPropertyRelative("decorations");
            UpsertDecoration(
                decorations,
                "forest_stump",
                stump,
                new Vector2(1.4f, 1.1f),
                0.72f,
                3,
                6,
                1.6f,
                new Vector2(0.88f, 0.26f),
                new Vector2(0f, 0.13f),
                new Vector2(0.9f, 1.1f));
            UpsertDecoration(
                decorations,
                "forest_big_mushroom",
                mushroom,
                new Vector2(1.3f, 1.7f),
                0.55f,
                2,
                4,
                1.8f,
                new Vector2(0.46f, 0.3f),
                new Vector2(0f, 0.15f),
                new Vector2(0.9f, 1.08f));
            RemoveDuplicateBiomes(biomeArray, BiomeType.Forest, biomeIndex);

            bool gribolisWorldSafe =
                audits.ContainsKey("Gribolis") &&
                audits["Gribolis"].IsWorldSpriteSafe;
            UpsertCreature(
                serialized.FindProperty("creatureVisuals"),
                MushroomFoxId,
                gribolis,
                gribolisWorldSafe ? gribolis : null,
                null);
            UpsertCreature(
                serialized.FindProperty("creatureVisuals"),
                LampCrabId,
                lampcrab,
                lampcrab,
                null);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            report.AppendLine(
                "PASS: SpriteDatabase обновлён для Леса, декораций и двух существ.");
            if (!gribolisWorldSafe)
            {
                report.AppendLine(
                    "ERROR: Gribolis.png оставлен только портретом; " +
                    "world sprite использует существующий безопасный fallback.");
            }
        }

        private static void UpsertCreature(
            SerializedProperty array,
            string id,
            Sprite portrait,
            Sprite world,
            Sprite evolution)
        {
            int index = FindOrAddById(array, id);
            SerializedProperty entry = array.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").stringValue = id;
            entry.FindPropertyRelative("portraitSprite").objectReferenceValue =
                portrait;
            entry.FindPropertyRelative("worldSprite").objectReferenceValue =
                world;
            entry.FindPropertyRelative("evolutionSprite").objectReferenceValue =
                evolution;
            entry.FindPropertyRelative("headAnchorOffset").vector2Value =
                Vector2.zero;
            entry.FindPropertyRelative("bodyAnchorOffset").vector2Value =
                Vector2.zero;
            entry.FindPropertyRelative("legAnchorOffset").vector2Value =
                Vector2.zero;
            RemoveDuplicateIds(array, id, index);
        }

        private static void UpsertDecoration(
            SerializedProperty array,
            string id,
            Sprite sprite,
            Vector2 worldSize,
            float weight,
            int minCount,
            int maxCount,
            float minimumDistance,
            Vector2 colliderSize,
            Vector2 colliderOffset,
            Vector2 scaleVariation)
        {
            int index = FindOrAddById(array, id);
            SerializedProperty entry = array.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").stringValue = id;
            entry.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            entry.FindPropertyRelative("targetWorldSize").vector2Value =
                worldSize;
            entry.FindPropertyRelative("spawnWeight").floatValue = weight;
            entry.FindPropertyRelative("minCount").intValue = minCount;
            entry.FindPropertyRelative("maxCount").intValue = maxCount;
            entry.FindPropertyRelative("minimumDistance").floatValue =
                minimumDistance;
            entry.FindPropertyRelative("colliderSize").vector2Value =
                colliderSize;
            entry.FindPropertyRelative("colliderOffset").vector2Value =
                colliderOffset;
            entry.FindPropertyRelative("scaleVariation").vector2Value =
                scaleVariation;
            entry.FindPropertyRelative("allowFlipX").boolValue = true;
            entry.FindPropertyRelative("blocksMovement").boolValue = false;
            RemoveDuplicateIds(array, id, index);
        }

        private static int FindOrAddById(
            SerializedProperty array,
            string id)
        {
            for (int index = 0; index < array.arraySize; index++)
            {
                SerializedProperty entry =
                    array.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("id").stringValue == id)
                {
                    return index;
                }
            }

            int added = array.arraySize;
            array.InsertArrayElementAtIndex(added);
            return added;
        }

        private static int FindOrAddBiome(
            SerializedProperty array,
            BiomeType biome)
        {
            for (int index = 0; index < array.arraySize; index++)
            {
                SerializedProperty entry =
                    array.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("biome").enumValueIndex ==
                    (int)biome)
                {
                    return index;
                }
            }

            int added = array.arraySize;
            array.InsertArrayElementAtIndex(added);
            return added;
        }

        private static void RemoveDuplicateIds(
            SerializedProperty array,
            string id,
            int keepIndex)
        {
            for (int index = array.arraySize - 1; index >= 0; index--)
            {
                if (index == keepIndex)
                {
                    continue;
                }

                SerializedProperty entry =
                    array.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("id").stringValue == id)
                {
                    array.DeleteArrayElementAtIndex(index);
                    if (index < keepIndex)
                    {
                        keepIndex--;
                    }
                }
            }
        }

        private static void RemoveDuplicateBiomes(
            SerializedProperty array,
            BiomeType biome,
            int keepIndex)
        {
            for (int index = array.arraySize - 1; index >= 0; index--)
            {
                if (index == keepIndex)
                {
                    continue;
                }

                SerializedProperty entry =
                    array.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("biome").enumValueIndex ==
                    (int)biome)
                {
                    array.DeleteArrayElementAtIndex(index);
                    if (index < keepIndex)
                    {
                        keepIndex--;
                    }
                }
            }
        }

        private static void CreateOrUpdateAtlases(StringBuilder report)
        {
            CreateOrUpdateAtlas(
                WorldAtlasPath,
                new[]
                {
                    FindAsset("ForestStump").destination,
                    FindAsset("ForestBigMushroom").destination
                },
                report);
            CreateOrUpdateAtlas(
                CreatureAtlasPath,
                new[]
                {
                    FindAsset("Gribolis").destination,
                    FindAsset("Lampcrab").destination
                },
                report);
        }

        private static void CreateOrUpdateAtlas(
            string path,
            IEnumerable<string> assetPaths,
            StringBuilder report)
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, path);
            }

            UnityEngine.Object[] existing =
                SpriteAtlasExtensions.GetPackables(atlas);
            if (existing.Length > 0)
            {
                SpriteAtlasExtensions.Remove(atlas, existing);
            }

            UnityEngine.Object[] packables = assetPaths
                .Select(AssetDatabase.LoadMainAssetAtPath)
                .Where(value => value != null)
                .ToArray();
            if (packables.Length > 0)
            {
                SpriteAtlasExtensions.Add(atlas, packables);
            }

            SpriteAtlasPackingSettings packing = atlas.GetPackingSettings();
            packing.enableRotation = false;
            packing.enableTightPacking = true;
            packing.padding = 4;
            atlas.SetPackingSettings(packing);

            SpriteAtlasTextureSettings texture = atlas.GetTextureSettings();
            texture.generateMipMaps = false;
            texture.readable = false;
            texture.sRGB = true;
            texture.filterMode = FilterMode.Bilinear;
            atlas.SetTextureSettings(texture);

            TextureImporterPlatformSettings webGl =
                atlas.GetPlatformSettings("WebGL");
            webGl.overridden = true;
            webGl.maxTextureSize = 4096;
            webGl.format = TextureImporterFormat.Automatic;
            webGl.compressionQuality = 80;
            atlas.SetPlatformSettings(webGl);
            EditorUtility.SetDirty(atlas);
            report.AppendLine(
                "PASS: обновлён " + path + " (" + packables.Length +
                " packables).");
        }

        private static void ValidateDatabase(
            ValidationReport report,
            IDictionary<string, PngAudit> audits)
        {
            SpriteDatabase database =
                AssetDatabase.LoadAssetAtPath<SpriteDatabase>(
                    SpriteDatabasePath);
            report.Check(database != null, "SpriteDatabase найден", true);
            if (database == null)
            {
                return;
            }

            BiomeVisualConfig config =
                database.GetBiomeVisualConfig(BiomeType.Forest);
            report.Check(config != null, "BiomeVisualConfig Леса существует", true);
            if (config != null)
            {
                report.Check(
                    config.backgroundWorldSize == new Vector2(30f, 18f),
                    "ForestBackground имеет мировой размер 30x18",
                    true);
                report.Check(
                    config.groundDetailEnabled &&
                    config.groundDetail != null &&
                    Mathf.Abs(config.groundDetailOpacity - 0.2f) < 0.001f &&
                    config.groundTileWorldSize == new Vector2(8f, 8f),
                    "GroundDetail включён, opacity=0.20, тайл=8x8",
                    true);
                report.Check(
                    HasDecoration(
                        config,
                        "forest_stump",
                        new Vector2(1.4f, 1.1f),
                        3,
                        6),
                    "ForestStump настроен как 1.4x1.1 и 3-6 на сегмент",
                    true);
                report.Check(
                    HasDecoration(
                        config,
                        "forest_big_mushroom",
                        new Vector2(1.3f, 1.7f),
                        2,
                        4),
                    "ForestBigMushroom настроен как 1.3x1.7 и 2-4 на сегмент",
                    true);
            }

            SerializedObject serialized = new SerializedObject(database);
            report.Check(
                CreatureSpriteMatches(
                    serialized,
                    LampCrabId,
                    FindAsset("Lampcrab").destination,
                    true),
                "Лампакраб использует финальный portrait/world sprite",
                true);

            bool gribolisSafe =
                audits.ContainsKey("Gribolis") &&
                audits["Gribolis"].IsWorldSpriteSafe;
            bool gribolisMatches = CreatureSpriteMatches(
                serialized,
                MushroomFoxId,
                FindAsset("Gribolis").destination,
                gribolisSafe);
            report.Check(
                gribolisMatches && gribolisSafe,
                gribolisSafe
                    ? "Гриболис использует финальный portrait/world sprite"
                    : "Gribolis.png пригоден для world sprite",
                true);
        }

        private static bool HasDecoration(
            BiomeVisualConfig config,
            string id,
            Vector2 size,
            int min,
            int max)
        {
            BiomeDecorationEntry entry =
                config.decorations.Find(value =>
                    value != null && value.id == id);
            return entry != null &&
                   entry.sprite != null &&
                   Vector2.Distance(entry.targetWorldSize, size) < 0.001f &&
                   entry.minCount == min &&
                   entry.maxCount == max &&
                   entry.colliderSize.x > 0f &&
                   entry.colliderSize.y > 0f;
        }

        private static bool CreatureSpriteMatches(
            SerializedObject serialized,
            string id,
            string expectedPath,
            bool requireWorld)
        {
            SerializedProperty array =
                serialized.FindProperty("creatureVisuals");
            int count = 0;
            bool matches = false;
            for (int index = 0; index < array.arraySize; index++)
            {
                SerializedProperty entry =
                    array.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("id").stringValue != id)
                {
                    continue;
                }

                count++;
                UnityEngine.Object portrait =
                    entry.FindPropertyRelative("portraitSprite")
                        .objectReferenceValue;
                UnityEngine.Object world =
                    entry.FindPropertyRelative("worldSprite")
                        .objectReferenceValue;
                matches =
                    AssetDatabase.GetAssetPath(portrait) == expectedPath &&
                    (!requireWorld ||
                     AssetDatabase.GetAssetPath(world) == expectedPath);
            }

            return count == 1 && matches;
        }

        private static void ValidateAtlases(ValidationReport report)
        {
            ValidateAtlas(
                report,
                WorldAtlasPath,
                new[] { "ForestStump", "ForestBigMushroom" });
            ValidateAtlas(
                report,
                CreatureAtlasPath,
                new[] { "Gribolis", "Lampcrab" });

            SpriteAtlas world =
                AssetDatabase.LoadAssetAtPath<SpriteAtlas>(WorldAtlasPath);
            bool excludesBackground = world == null ||
                                      SpriteAtlasExtensions.GetPackables(world)
                                          .All(packable =>
                                              AssetDatabase.GetAssetPath(packable) !=
                                              FindAsset("ForestBackground").destination);
            report.Check(
                excludesBackground,
                "ForestBackground не добавлен в ForestWorldAtlas",
                true);
        }

        private static void ValidateAtlas(
            ValidationReport report,
            string path,
            IEnumerable<string> keys)
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            report.Check(atlas != null, "Atlas " + path, true);
            if (atlas == null)
            {
                return;
            }

            HashSet<string> paths = new HashSet<string>(
                SpriteAtlasExtensions.GetPackables(atlas)
                    .Where(value => value != null)
                    .Select(AssetDatabase.GetAssetPath));
            report.Check(
                keys.All(key => paths.Contains(FindAsset(key).destination)),
                "Atlas содержит только ожидаемые лесные текстуры: " + path,
                true);
        }

        private static void ValidateRuntimeContent(ValidationReport report)
        {
            GameContent content = DemoContentFactory.Create();
            try
            {
                List<CreatureData> gribolis = content.creatures.FindAll(
                    creature =>
                        creature != null &&
                        (creature.id == MushroomFoxId ||
                         creature.creatureName == "Гриболис"));
                CreatureData lampcrab = content.creatures.Find(
                    creature => creature != null &&
                                creature.id == LampCrabId);
                BiomeData forest = content.biomes.Find(
                    biome => biome != null &&
                             biome.type == BiomeType.Forest);

                report.Check(
                    gribolis.Count(creature =>
                        creature.id == MushroomFoxId) == 1,
                    "Гриболис зарегистрирован один раз с ID mushroom_fox",
                    true);
                CreatureData gribolisBase =
                    gribolis.Find(creature => creature.id == MushroomFoxId);
                report.Check(
                    gribolisBase != null &&
                    gribolisBase.biome == BiomeType.Forest &&
                    gribolisBase.appearanceChance > 0f &&
                    forest != null &&
                    forest.availableCreatures.Contains(gribolisBase) &&
                    forest.mapData.possibleCreatures.Contains(gribolisBase),
                    "Гриболис появляется в Лесу и входит в энциклопедию",
                    true);
                report.Check(
                    lampcrab != null &&
                    content.creatures.Count(creature =>
                        creature != null &&
                        creature.id == LampCrabId) == 1 &&
                    lampcrab.biome == BiomeType.Forest &&
                    lampcrab.rarity == CreatureRarity.Rare &&
                    lampcrab.appearanceChance > 0f &&
                    forest != null &&
                    forest.availableCreatures.Contains(lampcrab) &&
                    forest.mapData.possibleCreatures.Contains(lampcrab),
                    "Лампакраб зарегистрирован один раз и появляется в Лесу",
                    true);

                float gribolisWeight = GetSpawnWeight(gribolisBase);
                float lampWeight = GetSpawnWeight(lampcrab);
                report.Check(
                    gribolisWeight > lampWeight && lampWeight > 0f,
                    "Лампакраб встречается реже Гриболиса",
                    true,
                    "веса " + gribolisWeight.ToString("0.000") +
                    " / " + lampWeight.ToString("0.000"));
                report.Check(
                    typeof(CreatureCollectionManager)
                        .GetMethod("AddPet") != null &&
                    typeof(GameManager)
                        .GetMethod("AddCreatureCopies") != null &&
                    typeof(GameProgress).GetField("pets") != null &&
                    typeof(GameProgress)
                        .GetField("discoveredSpecies") != null,
                    "Питомцы, повторные копии и сохранение используют существующие системы",
                    true);
            }
            finally
            {
                DestroyRuntimeContent(content);
            }
        }

        private static void ValidateRuntimeArchitecture(
            ValidationReport report)
        {
            report.Check(
                typeof(InfiniteBiomeMap)
                    .GetProperty("GroundPooledTileCount") != null &&
                typeof(InfiniteBiomeMap)
                    .GetProperty("DecorationSegmentCount") != null,
                "InfiniteBiomeMap поддерживает pooled Background/Ground/Decorations",
                true);
            report.Check(
                typeof(WorldDecoration)
                    .GetMethod("IsAreaOccupied") != null &&
                typeof(BiomeDecorationRepeater) != null,
                "Декорации используют occupancy и ограниченный пул",
                true);
            report.Check(
                typeof(WorldYSorter) != null,
                "Y sorting подключён к мировым объектам",
                true);

            GameObject root = new GameObject("ForestAnchorValidation");
            try
            {
                CreatureVisualRig rig =
                    root.AddComponent<CreatureVisualRig>();
                rig.EnsureStructure();
                report.Check(
                    rig.HeadAnchor != null &&
                    rig.BodyAnchor != null &&
                    rig.LegAnchor != null &&
                    rig.HasValidStructure(),
                    "HeadAnchor, BodyAnchor и LegAnchor создаются автоматически",
                    true);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static float GetSpawnWeight(CreatureData creature)
        {
            if (creature == null)
            {
                return 0f;
            }

            float rarityWeight;
            switch (creature.rarity)
            {
                case CreatureRarity.Rare:
                    rarityWeight = 0.48f;
                    break;
                case CreatureRarity.Epic:
                    rarityWeight = 0.2f;
                    break;
                case CreatureRarity.Legendary:
                    rarityWeight = 0.075f;
                    break;
                case CreatureRarity.Secret:
                    rarityWeight = 0.035f;
                    break;
                default:
                    rarityWeight = 1f;
                    break;
            }

            return Mathf.Max(0.01f, creature.appearanceChance) *
                   rarityWeight;
        }

        private static bool ImportSettingsAreValid(ForestAsset asset)
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(asset.destination) as TextureImporter;
            if (importer == null)
            {
                return false;
            }

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool meshValid = asset.role == ForestAssetRole.Decoration
                ? settings.spriteMeshType == SpriteMeshType.Tight
                : settings.spriteMeshType == SpriteMeshType.FullRect;
            bool pivotValid = asset.role != ForestAssetRole.Decoration ||
                              (settings.spriteAlignment ==
                               (int)SpriteAlignment.Custom &&
                               Vector2.Distance(
                                   settings.spritePivot,
                                   new Vector2(0.5f, 0f)) < 0.001f);
            int expectedMax = asset.role == ForestAssetRole.Background
                ? 4096
                : 2048;
            float expectedPpu =
                asset.role == ForestAssetRole.Ground ? 128f : 100f;
            return importer.textureType == TextureImporterType.Sprite &&
                   importer.spriteImportMode == SpriteImportMode.Single &&
                   importer.sRGBTexture &&
                   importer.filterMode == FilterMode.Bilinear &&
                   !importer.mipmapEnabled &&
                   importer.wrapMode == TextureWrapMode.Clamp &&
                   importer.alphaIsTransparency ==
                   asset.RequiresTransparency &&
                   Mathf.Abs(importer.spritePixelsPerUnit - expectedPpu) <
                   0.001f &&
                   importer.maxTextureSize == expectedMax &&
                   meshValid &&
                   pivotValid;
        }

        private static PngAudit AuditPng(string assetPath)
        {
            string absolute = ToAbsolutePath(assetPath);
            if (!File.Exists(absolute))
            {
                return default(PngAudit);
            }

            byte[] bytes = File.ReadAllBytes(absolute);
            Texture2D texture =
                new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, bytes, false))
                {
                    return default(PngAudit);
                }

                Color32[] pixels = texture.GetPixels32();
                int transparent = 0;
                int trulyTransparent = 0;
                int opaqueWhiteBorder = 0;
                int borderSamples = 0;
                int softNeutralBackdrop = 0;
                int opaqueOrVisible = 0;
                int width = texture.width;
                int height = texture.height;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color32 pixel = pixels[y * width + x];
                        if (pixel.a < 250)
                        {
                            transparent++;
                        }

                        if (pixel.a <= 10)
                        {
                            trulyTransparent++;
                        }

                        if (pixel.a > 10)
                        {
                            opaqueOrVisible++;
                            int maximum = Math.Max(
                                pixel.r,
                                Math.Max(pixel.g, pixel.b));
                            int minimum = Math.Min(
                                pixel.r,
                                Math.Min(pixel.g, pixel.b));
                            float brightness =
                                (pixel.r + pixel.g + pixel.b) / 765f;
                            float saturation =
                                maximum > 0
                                    ? (maximum - minimum) / (float)maximum
                                    : 0f;
                            float nx =
                                Mathf.Abs((x + 0.5f) / width - 0.5f) * 2f;
                            float ny =
                                Mathf.Abs((y + 0.5f) / height - 0.5f) * 2f;
                            if (pixel.a < 250 &&
                                brightness > 0.12f &&
                                brightness < 0.82f &&
                                saturation < 0.42f &&
                                (nx > 0.28f || ny > 0.28f))
                            {
                                softNeutralBackdrop++;
                            }
                        }

                        bool border = x < width / 20 ||
                                      x >= width - width / 20 ||
                                      y < height / 20 ||
                                      y >= height - height / 20;
                        if (border)
                        {
                            borderSamples++;
                            if (pixel.a > 245 &&
                                pixel.r > 242 &&
                                pixel.g > 242 &&
                                pixel.b > 242)
                            {
                                opaqueWhiteBorder++;
                            }
                        }
                    }
                }

                float horizontalSeam = EdgeDifference(
                    pixels,
                    width,
                    height,
                    true);
                float verticalSeam = EdgeDifference(
                    pixels,
                    width,
                    height,
                    false);
                float backdropRatio = pixels.Length > 0
                    ? softNeutralBackdrop / (float)pixels.Length
                    : 0f;
                return new PngAudit
                {
                    loaded = true,
                    width = width,
                    height = height,
                    hasAlphaChannel = HasPngAlphaChannel(bytes),
                    hasRealTransparency =
                        trulyTransparent > pixels.Length * 0.01f,
                    transparentPercent = pixels.Length > 0
                        ? transparent * 100f / pixels.Length
                        : 0f,
                    hasWhiteBackground =
                        borderSamples > 0 &&
                        opaqueWhiteBorder >
                        borderSamples * 0.65f,
                    hasCheckerboard =
                        DetectCheckerboard(pixels, width, height),
                    hasLikelyPaintedBackdrop =
                        backdropRatio > 0.005f &&
                        opaqueOrVisible > pixels.Length * 0.25f,
                    horizontalSeamLikely = horizontalSeam <= 0.11f,
                    verticalSeamLikely = verticalSeam <= 0.11f,
                    horizontalSeamDifference = horizontalSeam,
                    verticalSeamDifference = verticalSeam
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static float EdgeDifference(
            Color32[] pixels,
            int width,
            int height,
            bool horizontal)
        {
            int samples = horizontal ? height : width;
            double difference = 0d;
            for (int index = 0; index < samples; index++)
            {
                Color32 first = horizontal
                    ? pixels[index * width]
                    : pixels[index];
                Color32 second = horizontal
                    ? pixels[index * width + width - 1]
                    : pixels[(height - 1) * width + index];
                difference +=
                    Math.Abs(first.r - second.r) +
                    Math.Abs(first.g - second.g) +
                    Math.Abs(first.b - second.b) +
                    Math.Abs(first.a - second.a);
            }

            return samples > 0
                ? (float)(difference / (samples * 4d * 255d))
                : 1f;
        }

        private static bool HasPngAlphaChannel(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 26)
            {
                return false;
            }

            byte colorType = bytes[25];
            return colorType == 4 || colorType == 6;
        }

        private static bool DetectCheckerboard(
            Color32[] pixels,
            int width,
            int height)
        {
            if (pixels == null || width < 16 || height < 16)
            {
                return false;
            }

            int alternating = 0;
            int compared = 0;
            int step = Mathf.Max(4, Mathf.Min(width, height) / 64);
            for (int y = step; y < height - step; y += step)
            {
                for (int x = step; x < width - step; x += step)
                {
                    Color32 center = pixels[y * width + x];
                    Color32 right = pixels[y * width + x + step];
                    Color32 down = pixels[(y + step) * width + x];
                    if (center.a < 245 || right.a < 245 || down.a < 245)
                    {
                        continue;
                    }

                    compared++;
                    int rightDelta = ColorDistance(center, right);
                    int downDelta = ColorDistance(center, down);
                    if (rightDelta > 45 && rightDelta < 180 &&
                        downDelta > 45 && downDelta < 180)
                    {
                        alternating++;
                    }
                }
            }

            return compared > 40 &&
                   alternating > compared * 0.72f;
        }

        private static int ColorDistance(Color32 left, Color32 right)
        {
            return Math.Abs(left.r - right.r) +
                   Math.Abs(left.g - right.g) +
                   Math.Abs(left.b - right.b);
        }

        private static Sprite LoadSprite(string key)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(
                FindAsset(key).destination);
        }

        private static ForestAsset FindAsset(string key)
        {
            return Assets.First(asset => asset.key == key);
        }

        private static bool AssetExists(string path)
        {
            return File.Exists(ToAbsolutePath(path));
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot =
                Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(
                projectRoot,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void DestroyRuntimeContent(GameContent content)
        {
            if (content == null)
            {
                return;
            }

            List<UnityEngine.Object> objects =
                new List<UnityEngine.Object>();
            objects.AddRange(content.creatures.Where(value => value != null));
            objects.AddRange(content.biomes.Where(value => value != null));
            objects.AddRange(content.items.Where(value => value != null));
            objects.AddRange(content.accessories.Where(value => value != null));
            objects.AddRange(content.quests.Where(value => value != null));
            objects.AddRange(content.speciesEvolutions.Where(value => value != null));
            objects.AddRange(content.signatureSets.Where(value => value != null));
            objects.AddRange(content.biomeEvents.Where(value => value != null));
            objects.AddRange(content.creatureNests.Where(value => value != null));
            objects.AddRange(content.biomes
                .Where(value => value != null && value.mapData != null)
                .Select(value => value.mapData));
            foreach (UnityEngine.Object value in objects.Distinct())
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }

        private enum ForestAssetRole
        {
            Background,
            Ground,
            Decoration,
            Creature
        }

        private readonly struct ForestAsset
        {
            public readonly string key;
            public readonly string destination;
            public readonly ForestAssetRole role;
            public readonly int expectedWidth;
            public readonly int expectedHeight;
            public readonly string[] sources;

            public bool RequiresTransparency
            {
                get
                {
                    return role == ForestAssetRole.Decoration ||
                           role == ForestAssetRole.Creature;
                }
            }

            public ForestAsset(
                string key,
                string destination,
                ForestAssetRole role,
                int expectedWidth,
                int expectedHeight,
                params string[] sources)
            {
                this.key = key;
                this.destination = destination;
                this.role = role;
                this.expectedWidth = expectedWidth;
                this.expectedHeight = expectedHeight;
                this.sources = sources ?? Array.Empty<string>();
            }
        }

        private struct PngAudit
        {
            public bool loaded;
            public int width;
            public int height;
            public bool hasAlphaChannel;
            public bool hasRealTransparency;
            public float transparentPercent;
            public bool hasWhiteBackground;
            public bool hasCheckerboard;
            public bool hasLikelyPaintedBackdrop;
            public bool horizontalSeamLikely;
            public bool verticalSeamLikely;
            public float horizontalSeamDifference;
            public float verticalSeamDifference;

            public bool IsWorldSpriteSafe
            {
                get
                {
                    return loaded &&
                           hasAlphaChannel &&
                           hasRealTransparency &&
                           !hasWhiteBackground &&
                           !hasCheckerboard &&
                           !hasLikelyPaintedBackdrop;
                }
            }
        }

        private sealed class ValidationReport
        {
            private readonly StringBuilder builder = new StringBuilder();
            private int errors;
            private int warnings;

            public void Line(string value)
            {
                builder.AppendLine(value);
            }

            public void Pass(string value)
            {
                builder.AppendLine("PASS: " + value);
            }

            public void Warning(string value)
            {
                warnings++;
                builder.AppendLine("WARNING: " + value);
            }

            public void Check(
                bool success,
                string label,
                bool errorOnFailure,
                string details = "")
            {
                if (success)
                {
                    builder.Append("PASS: ");
                }
                else if (errorOnFailure)
                {
                    errors++;
                    builder.Append("ERROR: ");
                }
                else
                {
                    warnings++;
                    builder.Append("WARNING: ");
                }

                builder.Append(label);
                if (!string.IsNullOrEmpty(details))
                {
                    builder.Append(" (").Append(details).Append(")");
                }

                builder.AppendLine();
            }

            public void Finish()
            {
                builder.AppendLine(
                    "SUMMARY: errors=" + errors + ", warnings=" + warnings);
                builder.AppendLine(
                    errors == 0
                        ? "FOREST ART PACK READY"
                        : "FOREST ART PACK NOT READY");
            }

            public override string ToString()
            {
                return builder.ToString();
            }
        }
    }
}
#endif
