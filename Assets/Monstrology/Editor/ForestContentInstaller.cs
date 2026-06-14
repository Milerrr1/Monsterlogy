#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Monstrology.Editor
{
    public static class ForestContentInstaller
    {
        private const string AutoInstallSessionKey =
            "Monstrology.ForestContent.AutoInstall.v1";
        private const string SpriteDatabasePath =
            "Assets/Monstrology/Art/Resources/SpriteDatabase.asset";
        private const string BreadcatPath =
            "Assets/Monstrology/Art/Creatures/Breadcat.png";
        private const string VacuumRhinoPath =
            "Assets/Monstrology/Art/Creatures/VacuumRhino.png";
        private const string ForestHatPath =
            "Assets/Monstrology/Art/Accessories/Forest/ForestHat.png";
        private const string ForestScarfPath =
            "Assets/Monstrology/Art/Accessories/Forest/ForestScarf.png";
        private const string ForestBootsPath =
            "Assets/Monstrology/Art/Accessories/Forest/ForestBoots.png";

        private static readonly string[] CreatureArtworkIds =
        {
            "bread_cat",
            "bread_cat_ii",
            "bread_cat_iii",
            "bread_cat_king"
        };

        private static readonly string[] VacuumArtworkIds =
        {
            "vacuum_rhino",
            "vacuum_rhino_ii",
            "vacuum_rhino_iii",
            "turbo_vacuum_rhino"
        };

        private static readonly AssetMove[] ExpectedAssets =
        {
            new AssetMove(
                BreadcatPath,
                "Assets/Monstrology/Art/Import/Breadcat.png"),
            new AssetMove(
                VacuumRhinoPath,
                "Assets/Monstrology/Art/Import/VacuumRhino.png",
                "Assets/Monstrology/Art/Import/vacuumphino.png"),
            new AssetMove(
                ForestHatPath,
                "Assets/Monstrology/Art/Import/ForestHat.png",
                "Assets/Monstrology/Art/Import/Foresthat.png"),
            new AssetMove(
                ForestScarfPath,
                "Assets/Monstrology/Art/Import/ForestScarf.png",
                "Assets/Monstrology/Art/Import/Forestscarf.png"),
            new AssetMove(
                ForestBootsPath,
                "Assets/Monstrology/Art/Import/ForestBoots.png",
                "Assets/Monstrology/Art/Import/forestboots.png")
        };

        private static readonly string[] ForestValidationAssetPaths =
        {
            BreadcatPath,
            ForestHatPath,
            ForestScarfPath,
            ForestBootsPath
        };

        private static readonly string[] DesertValidationAssetPaths =
        {
            VacuumRhinoPath
        };

        [InitializeOnLoadMethod]
        private static void ScheduleAutomaticInstall()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(AutoInstallSessionKey, false) ||
                    EditorApplication.isCompiling ||
                    !ExpectedAssets.Any(asset => AssetExists(asset.destination) ||
                                                 asset.sources.Any(AssetExists)))
                {
                    return;
                }

                SessionState.SetBool(AutoInstallSessionKey, true);
                InstallForestContentInternal(true);
            };
        }

        [MenuItem("Tools/Monstrology/Install Forest Content")]
        public static void InstallForestContent()
        {
            InstallForestContentInternal(true);
        }

        [MenuItem("Tools/Monstrology/Validate Forest Content")]
        public static void ValidateForestContent()
        {
            Debug.Log(GetValidationReport());
        }

        public static string GetValidationReport()
        {
            return BuildValidationReport();
        }

        [MenuItem("Tools/Monstrology/Validate Desert Content")]
        public static void ValidateDesertContent()
        {
            Debug.Log(GetDesertValidationReport());
        }

        public static string GetDesertValidationReport()
        {
            return BuildDesertValidationReport();
        }

        private static void InstallForestContentInternal(bool logReport)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("=== INSTALL FOREST CONTENT ===");
            EnsureFolders();
            MoveAssets(report);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (AssetMove asset in ExpectedAssets)
            {
                ConfigureTexture(asset.destination, report);
            }

            SpriteDatabase database =
                AssetDatabase.LoadAssetAtPath<SpriteDatabase>(SpriteDatabasePath);
            if (database == null)
            {
                report.AppendLine(
                    "WARNING: SpriteDatabase.asset не найден, записи спрайтов не обновлены.");
            }
            else
            {
                RegisterSprites(database, report);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            report.AppendLine(BuildValidationReport());
            report.AppendLine(BuildDesertValidationReport());
            report.AppendLine("FOREST_CONTENT_INSTALL_COMPLETE");
            if (logReport)
            {
                Debug.Log(report.ToString());
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Monstrology/Art", "Creatures");
            EnsureFolder("Assets/Monstrology/Art", "Accessories");
            EnsureFolder("Assets/Monstrology/Art/Accessories", "Forest");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void MoveAssets(StringBuilder report)
        {
            foreach (AssetMove asset in ExpectedAssets)
            {
                if (AssetExists(asset.destination))
                {
                    report.AppendLine("PASS: " + asset.destination + " уже на месте.");
                    continue;
                }

                string source = asset.sources.FirstOrDefault(AssetExists);
                if (string.IsNullOrEmpty(source))
                {
                    report.AppendLine(
                        "WARNING: не найден исходный файл для " + asset.destination + ".");
                    continue;
                }

                string error = AssetDatabase.MoveAsset(source, asset.destination);
                if (string.IsNullOrEmpty(error))
                {
                    report.AppendLine(
                        "PASS: перемещён " + source + " -> " + asset.destination + ".");
                }
                else
                {
                    report.AppendLine(
                        "WARNING: не удалось переместить " + source + ": " + error);
                }
            }
        }

        private static void ConfigureTexture(string path, StringBuilder report)
        {
            if (!AssetExists(path))
            {
                return;
            }

            AssetDatabase.ImportAsset(
                path,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                report.AppendLine("WARNING: TextureImporter не найден для " + path + ".");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            AlphaReport alpha = InspectAlpha(path);
            if (!alpha.loaded)
            {
                report.AppendLine("WARNING: не удалось проверить alpha: " + path + ".");
            }
            else if (!alpha.hasTransparentPixels)
            {
                Debug.LogWarning(
                    "Forest content: фон является частью изображения или alpha отсутствует: " +
                    Path.GetFileName(path));
                report.AppendLine(
                    "WARNING: " + Path.GetFileName(path) +
                    " не содержит прозрачных пикселей; фон считается непрозрачным.");
            }
            else
            {
                report.AppendLine(
                    "PASS: " + Path.GetFileName(path) + " импортирован, alpha " +
                    alpha.transparentPercent.ToString("0.0") + "%.");
            }
        }

        private static void RegisterSprites(
            SpriteDatabase database,
            StringBuilder report)
        {
            Sprite breadcat = AssetDatabase.LoadAssetAtPath<Sprite>(BreadcatPath);
            Sprite vacuumRhino =
                AssetDatabase.LoadAssetAtPath<Sprite>(VacuumRhinoPath);
            Sprite forestHat = AssetDatabase.LoadAssetAtPath<Sprite>(ForestHatPath);
            Sprite forestScarf =
                AssetDatabase.LoadAssetAtPath<Sprite>(ForestScarfPath);
            Sprite forestBoots =
                AssetDatabase.LoadAssetAtPath<Sprite>(ForestBootsPath);

            SerializedObject serialized = new SerializedObject(database);
            serialized.Update();
            foreach (string id in CreatureArtworkIds)
            {
                UpsertCreatureSprite(serialized, id, breadcat);
            }

            foreach (string id in VacuumArtworkIds)
            {
                UpsertCreatureSprite(serialized, id, vacuumRhino);
            }

            UpsertIdSprite(serialized, "accessories", "forest_hat", forestHat);
            UpsertIdSprite(serialized, "accessories", "forest_scarf", forestScarf);
            UpsertIdSprite(serialized, "accessories", "forest_boots", forestBoots);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);

            report.AppendLine(
                "PASS: SpriteDatabase обновлён без дубликатов для 8 форм существ и 3 предметов.");
        }

        private static void UpsertCreatureSprite(
            SerializedObject serialized,
            string id,
            Sprite sprite)
        {
            SerializedProperty array = serialized.FindProperty("creatureVisuals");
            int index = FindOrAdd(array, id);
            SerializedProperty entry = array.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").stringValue = id;
            entry.FindPropertyRelative("portraitSprite").objectReferenceValue = sprite;
            entry.FindPropertyRelative("worldSprite").objectReferenceValue = sprite;
            entry.FindPropertyRelative("evolutionSprite").objectReferenceValue = sprite;
            RemoveDuplicateIds(array, id, index);
        }

        private static void UpsertIdSprite(
            SerializedObject serialized,
            string propertyName,
            string id,
            Sprite sprite)
        {
            SerializedProperty array = serialized.FindProperty(propertyName);
            int index = FindOrAdd(array, id);
            SerializedProperty entry = array.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").stringValue = id;
            entry.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            RemoveDuplicateIds(array, id, index);
        }

        private static int FindOrAdd(SerializedProperty array, string id)
        {
            for (int index = 0; index < array.arraySize; index++)
            {
                SerializedProperty entry = array.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("id").stringValue == id)
                {
                    return index;
                }
            }

            int newIndex = array.arraySize;
            array.InsertArrayElementAtIndex(newIndex);
            return newIndex;
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

                SerializedProperty entry = array.GetArrayElementAtIndex(index);
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

        private static string BuildValidationReport()
        {
            ValidationReport report =
                new ValidationReport("FOREST_CONTENT_VALIDATION");
            report.Header("VALIDATE FOREST CONTENT");

            ValidateAssets(ForestValidationAssetPaths, report);

            SpriteDatabase database =
                AssetDatabase.LoadAssetAtPath<SpriteDatabase>(SpriteDatabasePath);
            report.Check(database != null, "SpriteDatabase найден");
            if (database != null)
            {
                SerializedObject serialized = new SerializedObject(database);
                report.Check(
                    CreatureArtworkIds.All(id =>
                        HasCreatureSprite(serialized, id, BreadcatPath)),
                    "Спрайты всех форм Хлебокота подключены");
                report.Check(
                    HasIdSprite(serialized, "accessories", "forest_hat", ForestHatPath) &&
                    HasIdSprite(serialized, "accessories", "forest_scarf", ForestScarfPath) &&
                    HasIdSprite(serialized, "accessories", "forest_boots", ForestBootsPath),
                    "Спрайты лесного комплекта подключены");
            }

            GameContent content = DemoContentFactory.Create();
            try
            {
                ValidateForestRuntimeContent(content, report);
            }
            finally
            {
                DestroyRuntimeContent(content);
            }

            report.Footer();
            return report.ToString();
        }

        private static string BuildDesertValidationReport()
        {
            ValidationReport report =
                new ValidationReport("DESERT_CONTENT_VALIDATION");
            report.Header("VALIDATE DESERT CONTENT");
            ValidateAssets(DesertValidationAssetPaths, report);

            SpriteDatabase database =
                AssetDatabase.LoadAssetAtPath<SpriteDatabase>(SpriteDatabasePath);
            report.Check(database != null, "SpriteDatabase найден");
            if (database != null)
            {
                SerializedObject serialized = new SerializedObject(database);
                report.Check(
                    VacuumArtworkIds.All(id =>
                        HasCreatureSprite(serialized, id, VacuumRhinoPath)),
                    "Спрайты всех форм Пылесосорога подключены");
            }

            GameContent content = DemoContentFactory.Create();
            try
            {
                ValidateDesertRuntimeContent(content, report);
            }
            finally
            {
                DestroyRuntimeContent(content);
            }

            report.Footer();
            return report.ToString();
        }

        private static void ValidateAssets(
            IEnumerable<string> paths,
            ValidationReport report)
        {
            foreach (string path in paths)
            {
                bool exists = AssetExists(path);
                report.Check(exists, "Файл " + Path.GetFileName(path));
                if (!exists)
                {
                    continue;
                }

                report.Check(
                    ImportSettingsAreValid(path),
                    "Import Settings " + Path.GetFileName(path));
                AlphaReport alpha = InspectAlpha(path);
                report.Check(
                    alpha.loaded && alpha.hasTransparentPixels,
                    "Alpha " + Path.GetFileName(path),
                    alpha.loaded
                        ? alpha.transparentPercent.ToString("0.0") +
                          "% прозрачных пикселей"
                        : "файл не прочитан");
            }
        }

        private static void ValidateForestRuntimeContent(
            GameContent content,
            ValidationReport report)
        {
            CreatureData breadcat = FindCreature(content, "bread_cat");
            CreatureData vacuum = FindCreature(content, "vacuum_rhino");
            report.Check(
                breadcat != null && CountCreature(content, "bread_cat") == 1,
                "ID bread_cat зарегистрирован один раз");

            BiomeData forest = content.biomes.Find(biome =>
                biome != null && biome.type == BiomeType.Forest);
            report.Check(
                IsWildCreature(forest, breadcat, BiomeType.Forest),
                "Хлебокот доступен в Лесу и энциклопедии");
            report.Check(
                vacuum != null &&
                vacuum.biome != BiomeType.Forest &&
                !ContainsCreature(forest, "vacuum_rhino") &&
                !ContainsMapCreature(forest, "vacuum_rhino") &&
                !HasNest(content, "vacuum_rhino", BiomeType.Forest),
                "Пылесосорог отсутствует в лесном спавне, энциклопедии и логовищах");

            report.Check(
                HasEvolution(content, "bread_cat", 100, "bread_cat_ii") &&
                HasEvolution(content, "bread_cat_ii", 250, "bread_cat_iii") &&
                HasEvolution(content, "bread_cat_iii", 500, "bread_cat_king"),
                "Цепочка эволюции Хлебокота 100/250/500");
            report.Check(
                CreatureArtworkIds.All(id => FindCreature(content, id) != null),
                "Все эволюционные формы Хлебокота существуют в данных");
            report.Check(
                CreatureArtworkIds.Skip(1).All(id =>
                    FindCreature(content, id).appearanceChance <= 0f),
                "Эволюционные формы Хлебокота не выпадают как дикие существа");

            report.Check(
                HasUpgradeResource(
                    content,
                    "bread_cat",
                    "bread_crumbs",
                    BiomeType.Forest),
                "Хлебокот использует выпадающие Золотые крошки");

            AccessoryData hat = FindAccessory(content, "forest_hat");
            AccessoryData scarf = FindAccessory(content, "forest_scarf");
            AccessoryData boots = FindAccessory(content, "forest_boots");
            report.Check(
                hat != null && scarf != null && boots != null &&
                CountAccessory(content, "forest_hat") == 1 &&
                CountAccessory(content, "forest_scarf") == 1 &&
                CountAccessory(content, "forest_boots") == 1,
                "Лесной комплект зарегистрирован без дубликатов");
            report.Check(
                IsForestDrop(hat) && IsForestDrop(scarf) && IsForestDrop(boots),
                "Три предмета выпадают только в Лесу");
            report.Check(
                scarf != null && hat != null && boots != null &&
                scarf.dropChance > hat.dropChance &&
                hat.dropChance > boots.dropChance &&
                boots.dropChance > 0f,
                "Редкость комплекта: шарф > шляпа > сапоги");
            report.Check(
                hat != null && hat.slot == AccessorySlot.Head &&
                scarf != null && scarf.slot == AccessorySlot.Body &&
                boots != null && boots.slot == AccessorySlot.Legs &&
                CreatureVisualRig.GetAnchorName(hat.slot) ==
                    CreatureVisualRig.HeadAnchorName &&
                CreatureVisualRig.GetAnchorName(scarf.slot) ==
                    CreatureVisualRig.BodyAnchorName &&
                CreatureVisualRig.GetAnchorName(boots.slot) ==
                    CreatureVisualRig.LegAnchorName,
                "Гардероб подключён к Head/Body/Leg anchors");

            SignatureSetData set = content.signatureSets.Find(entry =>
                entry != null && entry.id == "forest_set");
            report.Check(
                set != null &&
                set.accessoryIds.SequenceEqual(
                    new[] { "forest_hat", "forest_scarf", "forest_boots" }) &&
                set.IsSignatureSpecies("bread_cat"),
                "forest_set содержит нужные ID и signatureSpecies bread_cat");
            report.Check(
                set != null &&
                Mathf.Approximately(set.twoPieceResourceBonus, 0.05f) &&
                Mathf.Approximately(set.fullSetResourceBonus, 0.1f) &&
                Mathf.Approximately(set.fullSetRareCreatureBonus, 0.03f),
                "Бонусы forest_set равны 5% / 10% / 3%");

            report.Check(
                typeof(GameManager).GetMethod("AddCreature") != null &&
                typeof(GameManager).GetMethod("AddCreatureCopies") != null &&
                typeof(CreatureCollectionManager).GetMethod("AddPet") != null,
                "Повторные находки используют копии, питомец вида создаётся один раз");
            report.Check(
                typeof(AccessoryInventoryManager).GetMethod("EquipAccessory") != null &&
                Enum.GetValues(typeof(AccessorySlot)).Length == 3,
                "Один предмет на слот и замена экипировки поддерживаются");
            report.Check(
                typeof(GameProgress).GetField("creatures") != null &&
                typeof(GameProgress).GetField("pets") != null &&
                typeof(GameProgress).GetField("accessories") != null,
                "Копии, питомцы и гардероб входят в существующее сохранение");
        }

        private static void ValidateDesertRuntimeContent(
            GameContent content,
            ValidationReport report)
        {
            const string expectedDescription =
                "Пустынный чистюля, который засасывает песок, пыль и потерянные " +
                "путешественниками предметы металлическим хоботом.";

            CreatureData vacuum = FindCreature(content, "vacuum_rhino");
            BiomeData desert = content.biomes.Find(biome =>
                biome != null && biome.type == BiomeType.Desert);
            BiomeData forest = content.biomes.Find(biome =>
                biome != null && biome.type == BiomeType.Forest);

            report.Check(
                vacuum != null && CountCreature(content, "vacuum_rhino") == 1,
                "ID vacuum_rhino существует и зарегистрирован один раз");
            report.Check(
                vacuum != null &&
                vacuum.creatureName == "Пылесосорог" &&
                vacuum.description == expectedDescription &&
                vacuum.biome == BiomeType.Desert &&
                vacuum.rarity == CreatureRarity.Rare &&
                vacuum.element == CreatureElement.Sand,
                "Пылесосорог имеет пустынные данные, редкость Rare и стихию Sand");
            report.Check(
                IsWildCreature(desert, vacuum, BiomeType.Desert) &&
                ContainsMapCreature(desert, "vacuum_rhino"),
                "Пылесосорог находится в пустынном спавне и энциклопедии");
            report.Check(
                vacuum != null && vacuum.appearanceChance > 0f,
                "Шанс появления Пылесосорога больше нуля",
                vacuum != null
                    ? "вес " + vacuum.appearanceChance.ToString("0.00")
                    : string.Empty);
            report.Check(
                vacuum != null &&
                !ContainsCreature(forest, "vacuum_rhino") &&
                !ContainsMapCreature(forest, "vacuum_rhino") &&
                !HasNest(content, "vacuum_rhino", BiomeType.Forest),
                "Пылесосорог полностью исключён из лесного контента");

            float vacuumWeight = GetSpawnWeight(vacuum);
            float strongestOtherDesertWeight = desert == null
                ? 0f
                : desert.availableCreatures
                    .Where(creature =>
                        creature != null &&
                        creature.id != "vacuum_rhino" &&
                        creature.appearanceChance > 0f)
                    .Select(GetSpawnWeight)
                    .DefaultIfEmpty(0f)
                    .Max();
            float rarityRatio = vacuumWeight > 0f
                ? strongestOtherDesertWeight / vacuumWeight
                : 0f;
            report.Check(
                rarityRatio >= 1.5f,
                "Пылесосорог заметно реже других доступных существ Пустыни",
                "отношение весов " + rarityRatio.ToString("0.00") + ":1");

            report.Check(
                HasNest(content, "vacuum_rhino", BiomeType.Desert),
                "Логово Пылесосорога относится к Пустыне");
            report.Check(
                HasUpgradeResource(
                    content,
                    "vacuum_rhino",
                    "forest_battery",
                    BiomeType.Desert) &&
                content.items.Exists(item =>
                    item != null &&
                    item.id == "forest_battery" &&
                    item.itemName == "Песчаный фильтр"),
                "Песчаный фильтр доступен как пустынный ресурс прокачки");

            report.Check(
                HasEvolution(content, "vacuum_rhino", 100, "vacuum_rhino_ii") &&
                HasEvolution(content, "vacuum_rhino_ii", 250, "vacuum_rhino_iii") &&
                HasEvolution(
                    content,
                    "vacuum_rhino_iii",
                    500,
                    "turbo_vacuum_rhino"),
                "Цепочка эволюции Пылесосорога 100/250/500 работает");
            report.Check(
                VacuumArtworkIds.All(id =>
                {
                    CreatureData form = FindCreature(content, id);
                    return form != null &&
                           form.biome == BiomeType.Desert &&
                           form.element == CreatureElement.Sand;
                }),
                "Все формы Пылесосорога относятся к Пустыне и стихии Sand");
            report.Check(
                VacuumArtworkIds.Skip(1).All(id =>
                {
                    CreatureData form = FindCreature(content, id);
                    return form != null && form.appearanceChance <= 0f;
                }),
                "Эволюционные формы Пылесосорога не появляются в дикой природе");

            report.Check(
                typeof(GameManager).GetMethod("AddCreature") != null &&
                typeof(GameProgress).GetField("discoveredSpecies") != null,
                "Пылесосорог открывается в энциклопедии без смены ID");
            report.Check(
                typeof(CreatureCollectionManager).GetMethod("AddPet") != null &&
                typeof(GameProgress).GetField("pets") != null,
                "Пылесосорог может быть добавлен в питомцы");
            report.Check(
                typeof(GameManager).GetMethod("AddCreatureCopies") != null &&
                typeof(GameProgress).GetField("creatures") != null,
                "Повторная встреча увеличивает количество копий вида");
            report.Check(
                typeof(GameProgress).GetField("items") != null &&
                typeof(GameProgress).GetField("creatureNests") != null,
                "Старые питомцы, открытия, ресурс и логово сохраняют совместимость");
        }

        private static bool ImportSettingsAreValid(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            TextureImporterSettings settings = new TextureImporterSettings();
            if (importer != null)
            {
                importer.ReadTextureSettings(settings);
            }

            return importer != null &&
                   importer.textureType == TextureImporterType.Sprite &&
                   importer.spriteImportMode == SpriteImportMode.Single &&
                   importer.alphaIsTransparency &&
                   settings.spriteMeshType == SpriteMeshType.FullRect &&
                   importer.filterMode == FilterMode.Bilinear &&
                   importer.textureCompression ==
                       TextureImporterCompression.Uncompressed &&
                   importer.maxTextureSize == 2048 &&
                   !importer.mipmapEnabled &&
                   importer.wrapMode == TextureWrapMode.Clamp;
        }

        private static bool HasCreatureSprite(
            SerializedObject serialized,
            string id,
            string expectedPath)
        {
            SerializedProperty array = serialized.FindProperty("creatureVisuals");
            for (int index = 0; index < array.arraySize; index++)
            {
                SerializedProperty entry = array.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("id").stringValue != id)
                {
                    continue;
                }

                UnityEngine.Object portrait =
                    entry.FindPropertyRelative("portraitSprite").objectReferenceValue;
                UnityEngine.Object world =
                    entry.FindPropertyRelative("worldSprite").objectReferenceValue;
                UnityEngine.Object evolution =
                    entry.FindPropertyRelative("evolutionSprite").objectReferenceValue;
                return AssetDatabase.GetAssetPath(portrait) == expectedPath &&
                       AssetDatabase.GetAssetPath(world) == expectedPath &&
                       AssetDatabase.GetAssetPath(evolution) == expectedPath;
            }

            return false;
        }

        private static bool HasIdSprite(
            SerializedObject serialized,
            string propertyName,
            string id,
            string expectedPath)
        {
            SerializedProperty array = serialized.FindProperty(propertyName);
            for (int index = 0; index < array.arraySize; index++)
            {
                SerializedProperty entry = array.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("id").stringValue == id)
                {
                    return AssetDatabase.GetAssetPath(
                               entry.FindPropertyRelative("sprite")
                                   .objectReferenceValue) == expectedPath;
                }
            }

            return false;
        }

        private static CreatureData FindCreature(GameContent content, string id)
        {
            return content.creatures.Find(creature =>
                creature != null && creature.id == id);
        }

        private static int CountCreature(GameContent content, string id)
        {
            return content.creatures.Count(creature =>
                creature != null && creature.id == id);
        }

        private static AccessoryData FindAccessory(GameContent content, string id)
        {
            return content.accessories.Find(accessory =>
                accessory != null && accessory.id == id);
        }

        private static int CountAccessory(GameContent content, string id)
        {
            return content.accessories.Count(accessory =>
                accessory != null && accessory.id == id);
        }

        private static bool IsWildCreature(
            BiomeData biome,
            CreatureData creature,
            BiomeType expectedBiome)
        {
            return creature != null &&
                   creature.biome == expectedBiome &&
                   creature.appearanceChance > 0f &&
                   biome != null &&
                   biome.availableCreatures.Any(entry =>
                        entry != null && entry.id == creature.id);
        }

        private static bool ContainsCreature(BiomeData biome, string speciesId)
        {
            return biome != null &&
                   biome.availableCreatures.Any(creature =>
                       creature != null && creature.id == speciesId);
        }

        private static bool ContainsMapCreature(BiomeData biome, string speciesId)
        {
            return biome != null &&
                   biome.mapData != null &&
                   biome.mapData.possibleCreatures.Any(creature =>
                       creature != null && creature.id == speciesId);
        }

        private static bool HasNest(
            GameContent content,
            string speciesId,
            BiomeType biome)
        {
            return content.creatureNests.Exists(nest =>
                nest != null &&
                nest.speciesId == speciesId &&
                nest.biome == biome);
        }

        private static bool HasEvolution(
            GameContent content,
            string baseId,
            int copies,
            string resultId)
        {
            return content.speciesEvolutions.Exists(evolution =>
                evolution != null &&
                evolution.baseSpeciesId == baseId &&
                evolution.requiredCopies == copies &&
                evolution.resultSpeciesId == resultId);
        }

        private static bool HasUpgradeResource(
            GameContent content,
            string speciesId,
            string itemId,
            BiomeType biome)
        {
            return content.items.Exists(item =>
                item != null &&
                item.id == itemId &&
                item.kind == ItemKind.UpgradeResource &&
                item.requiredSpeciesId == speciesId &&
                item.preferredBiome == biome);
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

            return Mathf.Max(0.01f, creature.appearanceChance) * rarityWeight;
        }

        private static bool IsForestDrop(AccessoryData accessory)
        {
            return accessory != null &&
                   accessory.dropChance > 0f &&
                   accessory.IsSignature &&
                   ExplorationSystem.CanAccessoryDropInBiome(
                       accessory,
                       BiomeType.Forest) &&
                   Enum.GetValues(typeof(BiomeType))
                       .Cast<BiomeType>()
                       .Where(biome => biome != BiomeType.Forest)
                       .All(biome =>
                           !ExplorationSystem.CanAccessoryDropInBiome(
                               accessory,
                               biome));
        }

        private static AlphaReport InspectAlpha(string assetPath)
        {
            string absolutePath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                assetPath);
            if (!File.Exists(absolutePath))
            {
                return default(AlphaReport);
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(
                        texture,
                        File.ReadAllBytes(absolutePath),
                        false))
                {
                    return default(AlphaReport);
                }

                Color32[] pixels = texture.GetPixels32();
                int transparent = pixels.Count(pixel => pixel.a < 250);
                return new AlphaReport
                {
                    loaded = true,
                    hasTransparentPixels = transparent > 0,
                    transparentPercent = pixels.Length > 0
                        ? transparent * 100f / pixels.Length
                        : 0f
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void DestroyRuntimeContent(GameContent content)
        {
            if (content == null)
            {
                return;
            }

            List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
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

        private static bool AssetExists(string path)
        {
            return File.Exists(Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                path));
        }

        private readonly struct AssetMove
        {
            public readonly string destination;
            public readonly string[] sources;

            public AssetMove(string destination, params string[] sources)
            {
                this.destination = destination;
                this.sources = sources ?? Array.Empty<string>();
            }
        }

        private struct AlphaReport
        {
            public bool loaded;
            public bool hasTransparentPixels;
            public float transparentPercent;
        }

        private sealed class ValidationReport
        {
            private readonly StringBuilder builder = new StringBuilder();
            private readonly string marker;
            private int warnings;

            public ValidationReport(string marker)
            {
                this.marker = marker;
            }

            public void Header(string title)
            {
                builder.AppendLine("=== " + title + " ===");
            }

            public void Check(bool success, string label, string details = "")
            {
                if (!success)
                {
                    warnings++;
                }

                builder.Append(success ? "PASS: " : "WARNING: ");
                builder.Append(label);
                if (!string.IsNullOrEmpty(details))
                {
                    builder.Append(" (").Append(details).Append(")");
                }

                builder.AppendLine();
            }

            public void Footer()
            {
                builder.Append(
                    warnings == 0
                        ? marker + "_PASS"
                        : marker + "_WARNINGS=" + warnings);
            }

            public override string ToString()
            {
                return builder.ToString();
            }
        }
    }
}
#endif
