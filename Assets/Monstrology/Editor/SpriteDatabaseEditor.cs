#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Monstrology.Editor
{
    public static class SpriteDatabaseEditor
    {
        private const string AssetPath =
            "Assets/Monstrology/Art/Resources/SpriteDatabase.asset";
        private const string TemplateAssetPath =
            "Assets/Monstrology/Art/Resources/CreatureBaseTemplate.asset";

        [InitializeOnLoadMethod]
        private static void EnsureAfterLoad()
        {
            EditorApplication.delayCall += () =>
            {
                EnsureDefaultDatabase();
                EnsureDefaultTemplate();
            };
        }

        [MenuItem("Tools/Monstrology/Open Sprite Database")]
        public static void OpenSpriteDatabase()
        {
            SpriteDatabase database = EnsureDefaultDatabase();
            Selection.activeObject = database;
            EditorGUIUtility.PingObject(database);
        }

        [MenuItem("Tools/Monstrology/Open Creature Base Template")]
        public static void OpenCreatureBaseTemplate()
        {
            CreatureBaseTemplate template = EnsureDefaultTemplate();
            Selection.activeObject = template;
            EditorGUIUtility.PingObject(template);
        }

        private static SpriteDatabase EnsureDefaultDatabase()
        {
            SpriteDatabase database =
                AssetDatabase.LoadAssetAtPath<SpriteDatabase>(AssetPath);
            if (database != null)
            {
                return database;
            }

            EnsureFolder("Assets/Monstrology", "Art");
            EnsureFolder("Assets/Monstrology/Art", "Resources");
            database = ScriptableObject.CreateInstance<SpriteDatabase>();
            AssetDatabase.CreateAsset(database, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Created default SpriteDatabase at " + AssetPath + ".");
            return database;
        }

        private static CreatureBaseTemplate EnsureDefaultTemplate()
        {
            CreatureBaseTemplate template =
                AssetDatabase.LoadAssetAtPath<CreatureBaseTemplate>(
                    TemplateAssetPath);
            if (template != null)
            {
                return template;
            }

            EnsureFolder("Assets/Monstrology", "Art");
            EnsureFolder("Assets/Monstrology/Art", "Resources");
            template = ScriptableObject.CreateInstance<CreatureBaseTemplate>();
            AssetDatabase.CreateAsset(template, TemplateAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Created default CreatureBaseTemplate at " +
                TemplateAssetPath + ".");
            return template;
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
#endif
