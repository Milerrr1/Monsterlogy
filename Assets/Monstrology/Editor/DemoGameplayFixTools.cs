#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Monstrology.Editor
{
    public static class DemoGameplayFixTools
    {
        private const string SpriteDatabasePath =
            "Assets/Monstrology/Art/Resources/SpriteDatabase.asset";
        private const string ForestBootsPath =
            "Assets/Monstrology/Art/Accessories/Forest/ForestBoots.png";

        [MenuItem("Tools/Monstrology/Fix Demo Gameplay Issues")]
        public static void FixDemoGameplayIssues()
        {
            int assetRepairs = 0;
            assetRepairs += ConfigureForestBootsImporter();
            assetRepairs += RegisterForestBootsSprite();

            GameContent content = DemoContentFactory.Create();
            int contentRepairs = DemoGameplayContentRepair.Repair(content);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "PASS: Fix Demo Gameplay Issues completed. " +
                "Asset repairs: " +
                assetRepairs +
                ", runtime content repairs verified: " +
                contentRepairs +
                ".");
            ValidateDemoGameplayFixes();
        }

        [MenuItem("Tools/Monstrology/Validate Demo Gameplay Fixes")]
        public static void ValidateDemoGameplayFixes()
        {
            ValidationReport report = BuildValidationReport();
            report.Print();
        }

        public static void FixAndValidateBatch()
        {
            FixDemoGameplayIssues();
            ValidationReport report = BuildValidationReport();
            report.Print();
            if (report.HasErrors)
            {
                throw new InvalidOperationException(
                    "Demo gameplay fixes validation failed.");
            }
        }

        public static void ValidateBatch()
        {
            ValidationReport report = BuildValidationReport();
            report.Print();
            if (report.HasErrors)
            {
                throw new InvalidOperationException(
                    "Demo gameplay fixes validation failed.");
            }
        }

        private static ValidationReport BuildValidationReport()
        {
            ValidationReport report = new ValidationReport();
            GameContent content = DemoContentFactory.Create();
            DemoGameplayContentRepair.Repair(content);

            CreatureData vacuum = FindCreature(content, "vacuum_rhino");
            report.Check(vacuum != null, "CreatureData vacuum_rhino найден");
            report.Check(
                vacuum != null && vacuum.biome == BiomeType.Desert,
                "Пылесосорог относится к Desert");
            report.Check(
                content.creatures.Count(creature =>
                    creature != null &&
                    creature.id == "vacuum_rhino") == 1,
                "Пылесосорог зарегистрирован без дубликатов");
            report.Check(
                typeof(CreatureCollectionManager).GetMethod("AddPet") != null &&
                typeof(CreatureCollectionManager).GetMethod("RenamePet") != null &&
                typeof(CreatureCollectionManager).GetMethod("SetFavorite") != null,
                "Общий Pet API поддерживает создание, имя и любимчика");
            report.Check(
                HasMethod(
                    typeof(CreatureCollectionManager),
                    "HandleCreatureRegistered") &&
                HasMethod(
                    typeof(CreatureCollectionManager),
                    "RestoreMissingDiscoveredPets"),
                "Открытые виды регистрируются в питомцах и мигрируются");
            report.Check(
                SourceContains(
                    "Assets/Monstrology/Scripts/CreatureCollectionManager.cs",
                    "[PetMigration] Restored missing pet entry for "),
                "Миграция старого сохранения логируется только при восстановлении");
            report.Check(
                typeof(FollowPetController).GetProperty(
                    "HasWorldRarityLabel") != null &&
                !SourceContains(
                    "Assets/Monstrology/Scripts/FollowPetController.cs",
                    "new GameObject(\"Rarity\")"),
                "World follower показывает имя без строки редкости");
            report.Check(
                SourceContains(
                    "Assets/Monstrology/Scripts/PetsPanel.cs",
                    "PetLocalization.Rarity(pet.rarity)"),
                "Редкость сохранена в карточке питомца");

            ItemData fireStone = FindItem(content, "fire_stone");
            CreatureData magma = FindCreature(content, "magma_orb");
            report.Check(
                magma != null && magma.biome == BiomeType.Volcano,
                "Магмошар относится к Volcano");
            report.Check(
                fireStone != null &&
                fireStone.requiredSpeciesId == "magma_orb",
                "Ресурс Магмошара использует itemId fire_stone");
            report.Check(
                fireStone != null &&
                !ExplorationSystem.CanItemDropInBiome(
                    fireStone,
                    BiomeType.Forest),
                "fire_stone отсутствует в Forest");
            report.Check(
                fireStone != null &&
                magma != null &&
                ExplorationSystem.CanItemDropInBiome(
                    fireStone,
                    magma.biome),
                "fire_stone выпадает в правильном биоме");
            report.Check(
                fireStone != null &&
                fireStone.GetVisibleName(false) == ItemData.HiddenName &&
                fireStone.GetVisibleDescription(false) ==
                    ItemData.HiddenDescription,
                "Скрытое название и описание не спойлерят существо");
            report.Check(
                fireStone != null &&
                fireStone.GetVisibleName(true) == fireStone.itemName &&
                fireStone.GetVisibleDescription(true) ==
                    fireStone.description,
                "После открытия возвращаются настоящие данные ресурса");
            report.Check(
                content.items
                    .Where(item =>
                        item != null &&
                        !string.IsNullOrEmpty(item.requiredSpeciesId))
                    .All(item =>
                        FindCreature(
                            content,
                            item.requiredSpeciesId) != null),
                "Все связи ресурсов указывают на существующий CreatureData");

            report.Check(
                typeof(CreatureNestProgress).GetField(
                    "spawnCooldownUntilUtc") != null &&
                typeof(CreatureNestSystem).GetMethod(
                    "IsBiomeSpawnCooldownActive") != null &&
                typeof(CreatureNestSystem).GetMethod(
                    "CanSpawnNest") != null,
                "Cooldown логовищ хранится отдельно от визуального объекта");
            report.Check(
                SourceContains(
                    "Assets/Monstrology/Scripts/WorldExplorationManager.cs",
                    "HasActiveUndiscoveredNest()") &&
                SourceContains(
                    "Assets/Monstrology/Scripts/WorldExplorationManager.cs",
                    "creatureNests.CanSpawnNest"),
                "Генерация допускает только одно временное логово биома");
            report.Check(
                SourceContains(
                    "Assets/Monstrology/Scripts/CreatureNestSystem.cs",
                    "spawnCooldownUntilUtc = GetCooldownUntilUtc(data)"),
                "Cooldown сохраняется после обнаружения и награды");

            AccessoryData boots = FindAccessory(content, "forest_boots");
            SpriteDatabase database =
                AssetDatabase.LoadAssetAtPath<SpriteDatabase>(
                    SpriteDatabasePath);
            Sprite bootsSprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(ForestBootsPath);
            report.Check(boots != null, "forest_boots существует");
            report.Check(
                boots != null && boots.slot == AccessorySlot.Legs,
                "forest_boots использует slot Legs");
            report.Check(
                boots != null &&
                boots.EffectiveSetId == "forest_set",
                "forest_boots входит в forest_set");
            report.Check(
                bootsSprite != null &&
                database != null &&
                database.GetAccessory(boots) == bootsSprite,
                "Sprite лесных сапог подключён");
            report.Check(
                CreatureVisualRig.GetAnchorName(AccessorySlot.Legs) ==
                    CreatureVisualRig.LegAnchorName,
                "Сапоги используют LegAnchor");
            report.Check(
                boots != null &&
                boots.overrideUiVisual &&
                boots.GetUiVisualScale().x > 0f &&
                boots.GetUiVisualScale().y > 0f,
                "Сапоги имеют видимый профиль карточки и preview");
            report.Check(
                boots != null &&
                boots.overrideWorldVisual &&
                boots.GetWorldVisualScale().x > 0f &&
                boots.GetWorldVisualScale().y > 0f,
                "Сапоги имеют видимый профиль follower");
            report.Check(
                ValidateBootsRenderer(database, boots),
                "SpriteRenderer сапог активен и находится поверх существа");
            report.Check(
                typeof(AccessoryInventoryManager).GetMethod(
                    "EquipAccessory") != null &&
                SourceContains(
                    "Assets/Monstrology/Scripts/CreatureCollectionManager.cs",
                    "MigrateEquipmentSlots(pet)") &&
                SourceContains(
                    "Assets/Monstrology/Scripts/CreatureCollectionManager.cs",
                    "game.SavePets(pets)"),
                "Экипировка сохраняется и восстанавливается");

            return report;
        }

        private static bool ValidateBootsRenderer(
            SpriteDatabase database,
            AccessoryData boots)
        {
            if (database == null || boots == null)
            {
                return false;
            }

            SpriteDatabase.Install(database);
            CreatureBaseTemplate template =
                AssetDatabase.LoadAssetAtPath<CreatureBaseTemplate>(
                    "Assets/Monstrology/Art/Resources/" +
                    "CreatureBaseTemplate.asset");
            CreatureBaseTemplate.Install(template);

            GameObject root = new GameObject("BootsValidationRig");
            try
            {
                CreatureVisualRig rig =
                    root.AddComponent<CreatureVisualRig>();
                rig.EnsureStructure();
                rig.ApplyAccessories(new[] { boots }, 20);
                return rig.LegAnchor != null &&
                       rig.HasVisibleAccessory("forest_boots");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static int ConfigureForestBootsImporter()
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(ForestBootsPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError(
                    "ERROR: Forest boots artwork is missing: " +
                    ForestBootsPath);
                return 0;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                return 1;
            }

            return 0;
        }

        private static int RegisterForestBootsSprite()
        {
            SpriteDatabase database =
                AssetDatabase.LoadAssetAtPath<SpriteDatabase>(
                    SpriteDatabasePath);
            Sprite sprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(ForestBootsPath);
            if (database == null || sprite == null)
            {
                Debug.LogError(
                    "ERROR: Cannot register forest_boots sprite. " +
                    SpriteDatabasePath +
                    " / " +
                    ForestBootsPath);
                return 0;
            }

            SerializedObject serialized = new SerializedObject(database);
            SerializedProperty entries =
                serialized.FindProperty("accessories");
            int first = -1;
            int repairs = 0;
            for (int index = 0; index < entries.arraySize; index++)
            {
                SerializedProperty entry =
                    entries.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("id").stringValue ==
                    "forest_boots")
                {
                    first = index;
                    break;
                }
            }

            if (first < 0)
            {
                first = entries.arraySize;
                entries.InsertArrayElementAtIndex(first);
                repairs++;
            }
            else
            {
                for (int index = entries.arraySize - 1;
                     index > first;
                     index--)
                {
                    SerializedProperty entry =
                        entries.GetArrayElementAtIndex(index);
                    if (entry.FindPropertyRelative("id").stringValue ==
                        "forest_boots")
                    {
                        entries.DeleteArrayElementAtIndex(index);
                        repairs++;
                    }
                }
            }

            SerializedProperty target =
                entries.GetArrayElementAtIndex(first);
            target.FindPropertyRelative("id").stringValue = "forest_boots";
            SerializedProperty spriteProperty =
                target.FindPropertyRelative("sprite");
            if (spriteProperty.objectReferenceValue != sprite)
            {
                spriteProperty.objectReferenceValue = sprite;
                repairs++;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            return repairs;
        }

        private static CreatureData FindCreature(
            GameContent content,
            string id)
        {
            return content.creatures.Find(creature =>
                creature != null && creature.id == id);
        }

        private static ItemData FindItem(GameContent content, string id)
        {
            return content.items.Find(item =>
                item != null && item.id == id);
        }

        private static AccessoryData FindAccessory(
            GameContent content,
            string id)
        {
            return content.accessories.Find(accessory =>
                accessory != null && accessory.id == id);
        }

        private static bool HasMethod(Type type, string name)
        {
            return type.GetMethod(
                name,
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic) != null;
        }

        private static bool SourceContains(
            string assetPath,
            string value)
        {
            string absolute = Path.GetFullPath(
                Path.Combine(
                    Directory.GetParent(Application.dataPath).FullName,
                    assetPath));
            return File.Exists(absolute) &&
                   File.ReadAllText(absolute).Contains(value);
        }

        private sealed class ValidationReport
        {
            private readonly List<Entry> entries = new List<Entry>();

            public bool HasErrors
            {
                get { return entries.Any(entry => !entry.passed); }
            }

            public void Check(bool condition, string message)
            {
                entries.Add(new Entry(condition, message));
            }

            public void Print()
            {
                foreach (Entry entry in entries)
                {
                    if (entry.passed)
                    {
                        Debug.Log("PASS: " + entry.message);
                    }
                    else
                    {
                        Debug.LogError("ERROR: " + entry.message);
                    }
                }

                Debug.Log(
                    HasErrors
                        ? "DEMO GAMEPLAY FIXES NOT READY"
                        : "DEMO GAMEPLAY FIXES READY");
            }
        }

        private sealed class Entry
        {
            public readonly bool passed;
            public readonly string message;

            public Entry(bool passed, string message)
            {
                this.passed = passed;
                this.message = message;
            }
        }
    }
}
#endif
