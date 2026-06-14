#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace Monstrology.Editor
{
    public static class RussianFontTools
    {
        private const string FontPath =
            "Assets/Monstrology/Fonts/NotoSans-Regular.ttf";
        private const string TmpFontPath =
            "Assets/Monstrology/Fonts/MonstrologyRussian SDF.asset";
        private const string RuntimeSettingsPath =
            "Assets/Monstrology/Resources/MonstrologyFontSettings.asset";
        private const string TmpSettingsPath =
            "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string TestPrefabPath =
            "Assets/Monstrology/Editor/Tests/RussianFontSample.prefab";
        private const string SampleText =
            "Проверка кириллицы: Привет! Ёжик, № 7, 150 ₽, " +
            "«Монстрология», длинное тире —, многоточие…";

        private static readonly int[] RequiredCharacters =
        {
            'А', 'Я', 'а', 'я', 'Ё', 'ё', '№', '₽',
            '«', '»', '—', '…', '+', '-'
        };

        [MenuItem("Tools/Monstrology/UI/Fix Russian Fonts")]
        public static void FixRussianFonts()
        {
            Font sourceFont;
            TMP_FontAsset tmpFont;
            if (!EnsureFontAssets(out sourceFont, out tmpFont))
            {
                Debug.LogError("ERROR: Russian font assets could not be prepared.");
                return;
            }

            EnsureRuntimeSettings(sourceFont, tmpFont);
            EnsureTmpSettings(tmpFont);
            CreateTestPrefab(sourceFont, tmpFont);

            FixStats stats = new FixStats();
            FixProjectPrefabs(sourceFont, tmpFont, stats);
            FixProjectScenes(sourceFont, tmpFont, stats);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "PASS: Russian fonts fixed. Legacy Text: " +
                stats.legacyTexts +
                ", TextMesh: " +
                stats.textMeshes +
                ", TMP: " +
                stats.tmpTexts +
                ", prefabs: " +
                stats.prefabs +
                ", scenes: " +
                stats.scenes +
                ".");
        }

        [MenuItem("Tools/Monstrology/UI/Validate Russian Fonts")]
        public static void ValidateRussianFonts()
        {
            ValidateRussianFontsInternal();
        }

        public static void FixAndValidateBatch()
        {
            FixRussianFonts();
            if (!ValidateRussianFontsInternal())
            {
                throw new InvalidOperationException(
                    "Russian font validation failed.");
            }
        }

        public static void ValidateRussianFontsBatch()
        {
            if (!ValidateRussianFontsInternal())
            {
                throw new InvalidOperationException(
                    "Russian font validation failed.");
            }
        }

        private static bool EnsureFontAssets(
            out Font sourceFont,
            out TMP_FontAsset tmpFont)
        {
            sourceFont = null;
            tmpFont = null;
            if (!File.Exists(ToAbsolutePath(FontPath)))
            {
                Debug.LogError("ERROR: Embedded font is missing: " + FontPath);
                return false;
            }

            AssetDatabase.ImportAsset(
                FontPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);

            TrueTypeFontImporter importer =
                AssetImporter.GetAtPath(FontPath) as TrueTypeFontImporter;
            if (importer != null && !importer.includeFontData)
            {
                importer.includeFontData = true;
                importer.SaveAndReimport();
            }

            sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (sourceFont == null)
            {
                Debug.LogError("ERROR: Could not import font: " + FontPath);
                return false;
            }

            if (!EnsureTmpEssentialResources())
            {
                return false;
            }

            tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                TmpFontPath);
            if (tmpFont == null)
            {
                tmpFont = CreateTmpFontAsset(sourceFont);
            }

            if (tmpFont == null)
            {
                return false;
            }

            Font importedFont = sourceFont;
            TMP_FontAsset generatedFont = tmpFont;
            uint[] charactersToAdd = BuildCharacterSet()
                .Where(value =>
                    importedFont.HasCharacter((char)value) &&
                    !generatedFont.HasCharacter((int)value))
                .ToArray();
            uint[] missing = null;
            bool added = true;
            if (charactersToAdd.Length > 0)
            {
                tmpFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                added = tmpFont.TryAddCharacters(
                    charactersToAdd,
                    out missing,
                    true);
                tmpFont.atlasPopulationMode = AtlasPopulationMode.Static;
                EditorUtility.SetDirty(tmpFont);
                AssetDatabase.SaveAssets();
            }

            int[] requiredMissing = RequiredCharacters
                .Where(value => !generatedFont.HasCharacter(value))
                .ToArray();
            if (!added ||
                (missing != null && missing.Length > 0) ||
                requiredMissing.Length > 0)
            {
                IEnumerable<string> missingCodePoints =
                    (missing ?? new uint[0])
                    .Select(value => "U+" + value.ToString("X4"))
                    .Concat(requiredMissing.Select(
                        value => "U+" + value.ToString("X4")))
                    .Distinct();
                Debug.LogError(
                    "ERROR: TMP atlas is missing code points: " +
                    string.Join(", ", missingCodePoints));
                return false;
            }

            return true;
        }

        private static bool EnsureTmpEssentialResources()
        {
            const string shaderName = "TextMeshPro/Mobile/Distance Field";
            if (Shader.Find(shaderName) != null)
            {
                return true;
            }

            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(TMP_FontAsset).Assembly);
            string packagePath = package != null
                ? Path.Combine(
                    package.resolvedPath,
                    "Package Resources",
                    "TMP Essential Resources.unitypackage")
                : string.Empty;
            if (string.IsNullOrEmpty(packagePath) ||
                !File.Exists(packagePath))
            {
                Debug.LogError(
                    "ERROR: TMP Essential Resources package was not found.");
                return false;
            }

            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (Shader.Find(shaderName) == null)
            {
                Debug.LogError(
                    "ERROR: TMP Essential Resources could not be imported.");
                return false;
            }

            Debug.Log("PASS: TMP Essential Resources imported.");
            return true;
        }

        private static TMP_FontAsset CreateTmpFontAsset(Font sourceFont)
        {
            EnsureAssetFolder("Assets/Monstrology/Fonts");
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                64,
                6,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                false);
            if (fontAsset == null)
            {
                Debug.LogError("ERROR: TMP SDF asset generation failed.");
                return null;
            }

            fontAsset.name = "MonstrologyRussian SDF";
            AssetDatabase.CreateAsset(fontAsset, TmpFontPath);

            Texture2D atlas = fontAsset.atlasTextures[0];
            atlas.name = "MonstrologyRussian Atlas";
            AssetDatabase.AddObjectToAsset(atlas, fontAsset);

            Material material = fontAsset.material;
            material.name = "MonstrologyRussian Material";
            AssetDatabase.AddObjectToAsset(material, fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        private static uint[] BuildCharacterSet()
        {
            HashSet<uint> characters = new HashSet<uint>();
            AddRange(characters, 0x0020, 0x007E);
            AddRange(characters, 0x00A0, 0x00FF);
            AddRange(characters, 0x0400, 0x052F);
            AddRange(characters, 0x2010, 0x2026);
            characters.Add(0x20BD);
            characters.Add(0x2116);
            return characters.OrderBy(value => value).ToArray();
        }

        private static void AddRange(
            HashSet<uint> characters,
            uint first,
            uint last)
        {
            for (uint value = first; value <= last; value++)
            {
                characters.Add(value);
            }
        }

        private static void EnsureRuntimeSettings(
            Font sourceFont,
            TMP_FontAsset tmpFont)
        {
            EnsureAssetFolder("Assets/Monstrology/Resources");
            MonstrologyFontSettings settings =
                AssetDatabase.LoadAssetAtPath<MonstrologyFontSettings>(
                    RuntimeSettingsPath);
            if (settings == null)
            {
                settings =
                    ScriptableObject.CreateInstance<MonstrologyFontSettings>();
                settings.name = "MonstrologyFontSettings";
                AssetDatabase.CreateAsset(settings, RuntimeSettingsPath);
            }

            settings.Configure(sourceFont, tmpFont);
            EditorUtility.SetDirty(settings);
        }

        private static void EnsureTmpSettings(TMP_FontAsset tmpFont)
        {
            EnsureAssetFolder("Assets/TextMesh Pro/Resources");
            TMP_Settings settings =
                AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<TMP_Settings>();
                settings.name = "TMP Settings";
                AssetDatabase.CreateAsset(settings, TmpSettingsPath);
            }

            SerializedObject serialized = new SerializedObject(settings);
            SerializedProperty defaultFont =
                serialized.FindProperty("m_defaultFontAsset");
            defaultFont.objectReferenceValue = tmpFont;

            SerializedProperty fallbackFonts =
                serialized.FindProperty("m_fallbackFontAssets");
            bool alreadyPresent = false;
            for (int index = 0; index < fallbackFonts.arraySize; index++)
            {
                if (fallbackFonts.GetArrayElementAtIndex(index)
                        .objectReferenceValue == tmpFont)
                {
                    alreadyPresent = true;
                    break;
                }
            }

            if (!alreadyPresent)
            {
                int index = fallbackFonts.arraySize;
                fallbackFonts.InsertArrayElementAtIndex(index);
                fallbackFonts.GetArrayElementAtIndex(index)
                    .objectReferenceValue = tmpFont;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static void CreateTestPrefab(
            Font sourceFont,
            TMP_FontAsset tmpFont)
        {
            EnsureAssetFolder("Assets/Monstrology/Editor/Tests");
            GameObject root = new GameObject("RussianFontSample");
            try
            {
                GameObject tmpObject = new GameObject(
                    "TMP",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                tmpObject.transform.SetParent(root.transform, false);
                TextMeshProUGUI tmpText =
                    tmpObject.GetComponent<TextMeshProUGUI>();
                tmpText.font = tmpFont;
                tmpText.text = SampleText;

                GameObject legacyObject = new GameObject(
                    "LegacyText",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                legacyObject.transform.SetParent(root.transform, false);
                Text legacyText = legacyObject.GetComponent<Text>();
                legacyText.font = sourceFont;
                legacyText.text = SampleText;

                GameObject meshObject = new GameObject("TextMesh");
                meshObject.transform.SetParent(root.transform, false);
                TextMesh textMesh = meshObject.AddComponent<TextMesh>();
                textMesh.font = sourceFont;
                textMesh.text = SampleText;
                MeshRenderer renderer =
                    meshObject.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = sourceFont.material;
                }

                PrefabUtility.SaveAsPrefabAsset(root, TestPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void FixProjectPrefabs(
            Font sourceFont,
            TMP_FontAsset tmpFont,
            FixStats stats)
        {
            foreach (string path in FindProjectPrefabs())
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (FixComponents(root, sourceFont, tmpFont, stats))
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        stats.prefabs++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void FixProjectScenes(
            Font sourceFont,
            TMP_FontAsset tmpFont,
            FixStats stats)
        {
            foreach (string path in FindProjectScenes())
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool openedHere = !scene.IsValid() || !scene.isLoaded;
                if (!openedHere && scene.isDirty)
                {
                    Debug.LogWarning(
                        "WARNING: Skipped dirty open scene: " + path);
                    continue;
                }

                if (openedHere)
                {
                    scene = EditorSceneManager.OpenScene(
                        path,
                        OpenSceneMode.Additive);
                }

                bool changed = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    changed |= FixComponents(
                        root,
                        sourceFont,
                        tmpFont,
                        stats);
                }

                if (changed)
                {
                    EditorSceneManager.SaveScene(scene);
                    stats.scenes++;
                }

                if (openedHere)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static bool FixComponents(
            GameObject root,
            Font sourceFont,
            TMP_FontAsset tmpFont,
            FixStats stats)
        {
            bool changed = false;
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                if (text.font != sourceFont &&
                    !IsSpecialFont(text.name, text.font))
                {
                    text.font = sourceFont;
                    EditorUtility.SetDirty(text);
                    stats.legacyTexts++;
                    changed = true;
                }
            }

            foreach (TextMesh text in
                root.GetComponentsInChildren<TextMesh>(true))
            {
                if (text.font != sourceFont &&
                    !IsSpecialFont(text.name, text.font))
                {
                    text.font = sourceFont;
                    MeshRenderer renderer =
                        text.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.sharedMaterial = sourceFont.material;
                        EditorUtility.SetDirty(renderer);
                    }

                    EditorUtility.SetDirty(text);
                    stats.textMeshes++;
                    changed = true;
                }
            }

            foreach (TMP_Text text in
                root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font != tmpFont &&
                    !IsSpecialFont(text.name, text.font))
                {
                    text.font = tmpFont;
                    EditorUtility.SetDirty(text);
                    stats.tmpTexts++;
                    changed = true;
                }
            }

            return changed;
        }

        private static bool IsSpecialFont(string objectName, UnityEngine.Object font)
        {
            string value = (
                objectName + " " + (font != null ? font.name : string.Empty))
                .ToLowerInvariant();
            return value.Contains("icon") ||
                value.Contains("symbol") ||
                value.Contains("emoji");
        }

        private static bool ValidateRussianFontsInternal()
        {
            bool hasErrors = false;
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            TMP_FontAsset tmpFont =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);

            Validate(
                sourceFont != null,
                "Embedded Noto Sans font is present.",
                "Embedded Noto Sans font is missing.",
                ref hasErrors);
            Validate(
                tmpFont != null,
                "MonstrologyRussian SDF is present.",
                "MonstrologyRussian SDF is missing.",
                ref hasErrors);

            if (sourceFont != null)
            {
                foreach (int character in RequiredCharacters)
                {
                    Validate(
                        sourceFont.HasCharacter((char)character),
                        "Legacy font contains " +
                        FormatCodePoint(character) +
                        ".",
                        "Legacy font is missing " +
                        FormatCodePoint(character) +
                        ".",
                        ref hasErrors);
                }
            }

            if (tmpFont != null)
            {
                foreach (int character in RequiredCharacters)
                {
                    Validate(
                        tmpFont.HasCharacter(character),
                        "TMP font contains " +
                        FormatCodePoint(character) +
                        ".",
                        "TMP font is missing " +
                        FormatCodePoint(character) +
                        ".",
                        ref hasErrors);
                }
            }

            MonstrologyFontSettings runtimeSettings =
                AssetDatabase.LoadAssetAtPath<MonstrologyFontSettings>(
                    RuntimeSettingsPath);
            Validate(
                runtimeSettings != null &&
                runtimeSettings.LegacyFont == sourceFont &&
                runtimeSettings.TmpFont == tmpFont,
                "Runtime font settings reference both embedded fonts.",
                "Runtime font settings are missing or invalid.",
                ref hasErrors);

            ValidateTmpSettings(tmpFont, ref hasErrors);
            ValidateTestPrefab(sourceFont, tmpFont, ref hasErrors);
            ValidateRuntimeSources(ref hasErrors);

            Debug.Log(
                hasErrors
                    ? "RUSSIAN FONT VALIDATION FAILED"
                    : "RUSSIAN FONT VALIDATION PASSED");
            return !hasErrors;
        }

        private static void ValidateTmpSettings(
            TMP_FontAsset tmpFont,
            ref bool hasErrors)
        {
            TMP_Settings settings =
                AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            bool defaultValid = false;
            bool fallbackValid = false;
            if (settings != null)
            {
                SerializedObject serialized = new SerializedObject(settings);
                defaultValid = serialized
                    .FindProperty("m_defaultFontAsset")
                    .objectReferenceValue == tmpFont;
                SerializedProperty fallbacks =
                    serialized.FindProperty("m_fallbackFontAssets");
                for (int index = 0; index < fallbacks.arraySize; index++)
                {
                    if (fallbacks.GetArrayElementAtIndex(index)
                            .objectReferenceValue == tmpFont)
                    {
                        fallbackValid = true;
                        break;
                    }
                }
            }

            Validate(
                defaultValid,
                "TMP global default font is MonstrologyRussian SDF.",
                "TMP global default font is not configured.",
                ref hasErrors);
            Validate(
                fallbackValid,
                "TMP global fallback contains MonstrologyRussian SDF.",
                "TMP global fallback does not contain the Russian font.",
                ref hasErrors);
        }

        private static void ValidateTestPrefab(
            Font sourceFont,
            TMP_FontAsset tmpFont,
            ref bool hasErrors)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(TestPrefabPath);
            Text legacy = prefab != null
                ? prefab.GetComponentInChildren<Text>(true)
                : null;
            TextMesh mesh = prefab != null
                ? prefab.GetComponentInChildren<TextMesh>(true)
                : null;
            TMP_Text tmp = prefab != null
                ? prefab.GetComponentInChildren<TMP_Text>(true)
                : null;
            bool valid = legacy != null &&
                mesh != null &&
                tmp != null &&
                legacy.font == sourceFont &&
                mesh.font == sourceFont &&
                tmp.font == tmpFont &&
                legacy.text == SampleText &&
                mesh.text == SampleText &&
                tmp.text == SampleText;
            Validate(
                valid,
                "Russian font test prefab is configured.",
                "Russian font test prefab is missing or invalid.",
                ref hasErrors);
        }

        private static void ValidateRuntimeSources(ref bool hasErrors)
        {
            string uiManager = File.ReadAllText(
                ToAbsolutePath(
                    "Assets/Monstrology/Scripts/UIManager.cs"));
            string follower = File.ReadAllText(
                ToAbsolutePath(
                    "Assets/Monstrology/Scripts/FollowPetController.cs"));
            string pickup = File.ReadAllText(
                ToAbsolutePath(
                    "Assets/Monstrology/Scripts/WorldPickup.cs"));
            bool valid = !uiManager.Contains(
                    "GetBuiltinResource<Font>(\"LegacyRuntime.ttf\")") &&
                uiManager.Contains("MonstrologyFontProvider.LegacyFont") &&
                follower.Contains("MonstrologyFontProvider.Apply(nameText)") &&
                !follower.Contains("rarityText") &&
                pickup.Contains("MonstrologyFontProvider.Apply(worldLabel)");
            Validate(
                valid,
                "Runtime-created UI Text and TextMesh use the font provider.",
                "A runtime text creation path still bypasses the font provider.",
                ref hasErrors);
        }

        private static IEnumerable<string> FindProjectPrefabs()
        {
            return AssetDatabase.FindAssets(
                    "t:Prefab",
                    new[] { "Assets/Monstrology" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !path.StartsWith(
                    "Assets/PluginYourGames/",
                    StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<string> FindProjectScenes()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .Where(path => !path.StartsWith(
                    "Assets/PluginYourGames/",
                    StringComparison.OrdinalIgnoreCase))
                .Distinct();
        }

        private static void EnsureAssetFolder(string path)
        {
            string absolute = ToAbsolutePath(path);
            if (!Directory.Exists(absolute))
            {
                Directory.CreateDirectory(absolute);
                AssetDatabase.Refresh();
            }
        }

        private static string ToAbsolutePath(string assetPath)
        {
            return Path.GetFullPath(
                Path.Combine(
                    Directory.GetParent(Application.dataPath).FullName,
                    assetPath));
        }

        private static string FormatCodePoint(int character)
        {
            return "'" +
                (char)character +
                "' (U+" +
                character.ToString("X4") +
                ")";
        }

        private static void Validate(
            bool condition,
            string pass,
            string error,
            ref bool hasErrors)
        {
            if (condition)
            {
                Debug.Log("PASS: " + pass);
                return;
            }

            hasErrors = true;
            Debug.LogError("ERROR: " + error);
        }

        private sealed class FixStats
        {
            public int legacyTexts;
            public int textMeshes;
            public int tmpTexts;
            public int prefabs;
            public int scenes;
        }
    }
}
#endif
