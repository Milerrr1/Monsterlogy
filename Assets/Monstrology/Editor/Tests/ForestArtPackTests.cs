#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Monstrology.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.U2D;
using UnityEditor.U2D;

namespace Monstrology.Tests
{
    public class ForestArtPackTests
    {
        private const string DatabasePath =
            "Assets/Monstrology/Art/Resources/SpriteDatabase.asset";

        [SetUp]
        public void SetUp()
        {
            SpriteDatabase.Install(
                AssetDatabase.LoadAssetAtPath<SpriteDatabase>(DatabasePath));
        }

        [Test]
        public void ForestVisualConfig_HasExpectedLayers()
        {
            BiomeVisualConfig config =
                SpriteDatabase.Active.GetBiomeVisualConfig(BiomeType.Forest);

            Assert.That(config, Is.Not.Null);
            Assert.That(config.backgroundWorldSize, Is.EqualTo(new Vector2(30f, 18f)));
            Assert.That(config.groundDetail, Is.Not.Null);
            Assert.That(config.groundDetailEnabled, Is.True);
            Assert.That(config.groundDetailOpacity, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(config.groundTileWorldSize, Is.EqualTo(new Vector2(8f, 8f)));
        }

        [Test]
        public void ForestDecorations_HaveExpectedProfiles()
        {
            BiomeVisualConfig config =
                SpriteDatabase.Active.GetBiomeVisualConfig(BiomeType.Forest);
            BiomeDecorationEntry stump =
                config.decorations.Single(entry => entry.id == "forest_stump");
            BiomeDecorationEntry mushroom =
                config.decorations.Single(
                    entry => entry.id == "forest_big_mushroom");

            Assert.That(stump.targetWorldSize, Is.EqualTo(new Vector2(1.4f, 1.1f)));
            Assert.That(stump.minCount, Is.EqualTo(3));
            Assert.That(stump.maxCount, Is.EqualTo(6));
            Assert.That(stump.colliderSize.y, Is.LessThan(stump.targetWorldSize.y * 0.3f));
            Assert.That(mushroom.targetWorldSize, Is.EqualTo(new Vector2(1.3f, 1.7f)));
            Assert.That(mushroom.minCount, Is.EqualTo(2));
            Assert.That(mushroom.maxCount, Is.EqualTo(4));
            Assert.That(
                mushroom.colliderSize.x,
                Is.LessThan(mushroom.targetWorldSize.x * 0.45f));
        }

        [Test]
        public void DecorationScale_PreservesSpriteAspect()
        {
            BiomeDecorationEntry stump =
                SpriteDatabase.Active
                    .GetBiomeVisualConfig(BiomeType.Forest)
                    .decorations
                    .Single(entry => entry.id == "forest_stump");

            float scale = WorldDecoration.CalculateUniformScale(
                stump.sprite,
                stump.targetWorldSize);
            Vector2 scaled = stump.sprite.bounds.size * scale;

            Assert.That(scaled.x, Is.LessThanOrEqualTo(1.4f + 0.001f));
            Assert.That(scaled.y, Is.LessThanOrEqualTo(1.1f + 0.001f));
            Assert.That(scale, Is.GreaterThan(0f));
        }

        [Test]
        public void InfiniteMap_UsesBoundedPools()
        {
            GameObject mapObject = new GameObject("ForestMapTest");
            GameObject playerObject = new GameObject("ForestPlayerTest");
            try
            {
                BiomeVisualConfig config =
                    SpriteDatabase.Active.GetBiomeVisualConfig(BiomeType.Forest);
                InfiniteBiomeMap map =
                    mapObject.AddComponent<InfiniteBiomeMap>();
                map.Initialize(
                    BiomeType.Forest,
                    SpriteDatabase.Active.GetBiomeBackground(null, null),
                    Color.white,
                    new Vector2(30f, 18f),
                    config,
                    playerObject.transform,
                    Vector2.zero);

                Assert.That(map.PooledTileCount, Is.EqualTo(9));
                Assert.That(map.GroundPooledTileCount, Is.EqualTo(9));
                Assert.That(map.DecorationSegmentCount, Is.EqualTo(9));
                Assert.That(
                    mapObject.GetComponentsInChildren<WorldDecoration>(true).Length,
                    Is.LessThanOrEqualTo(90));
            }
            finally
            {
                Object.DestroyImmediate(mapObject);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void Decorations_DoNotOverlapProtectedStartOrEachOther()
        {
            GameObject mapObject = new GameObject("ForestOccupancyTest");
            GameObject playerObject = new GameObject("ForestPlayerTest");
            try
            {
                InfiniteBiomeMap map =
                    mapObject.AddComponent<InfiniteBiomeMap>();
                map.Initialize(
                    BiomeType.Forest,
                    SpriteDatabase.Active.GetBiomeBackground(null, null),
                    Color.white,
                    new Vector2(30f, 18f),
                    SpriteDatabase.Active.GetBiomeVisualConfig(BiomeType.Forest),
                    playerObject.transform,
                    Vector2.zero);

                WorldDecoration[] decorations =
                    mapObject.GetComponentsInChildren<WorldDecoration>(false);
                foreach (WorldDecoration decoration in decorations)
                {
                    Assert.That(
                        decoration.transform.position.sqrMagnitude,
                        Is.GreaterThanOrEqualTo(2.5f * 2.5f));
                }

                BoxCollider2D[] colliders = decorations
                    .Select(decoration =>
                        decoration.GetComponent<BoxCollider2D>())
                    .Where(collider => collider != null && collider.enabled)
                    .ToArray();
                for (int left = 0; left < colliders.Length; left++)
                {
                    for (int right = left + 1; right < colliders.Length; right++)
                    {
                        Assert.That(
                            colliders[left].bounds.Intersects(
                                colliders[right].bounds),
                            Is.False);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(mapObject);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void ForestCreatures_KeepStableIdsAndWeights()
        {
            GameContent content = DemoContentFactory.Create();
            try
            {
                CreatureData gribolis = content.creatures.Single(
                    creature => creature.id == ForestArtPackInstaller.MushroomFoxId);
                CreatureData lampcrab = content.creatures.Single(
                    creature => creature.id == ForestArtPackInstaller.LampCrabId);
                BiomeData forest = content.biomes.Single(
                    biome => biome.type == BiomeType.Forest);

                Assert.That(gribolis.biome, Is.EqualTo(BiomeType.Forest));
                Assert.That(lampcrab.biome, Is.EqualTo(BiomeType.Forest));
                Assert.That(lampcrab.rarity, Is.EqualTo(CreatureRarity.Rare));
                Assert.That(forest.availableCreatures, Does.Contain(gribolis));
                Assert.That(forest.availableCreatures, Does.Contain(lampcrab));
                Assert.That(
                    lampcrab.appearanceChance * 0.48f,
                    Is.LessThan(gribolis.appearanceChance));
            }
            finally
            {
                DestroyContent(content);
            }
        }

        [Test]
        public void CreatureRig_CreatesAllWardrobeAnchors()
        {
            GameObject root = new GameObject("CreatureRigTest");
            try
            {
                CreatureVisualRig rig = root.AddComponent<CreatureVisualRig>();
                rig.EnsureStructure();

                Assert.That(rig.HeadAnchor.name, Is.EqualTo("HeadAnchor"));
                Assert.That(rig.BodyAnchor.name, Is.EqualTo("BodyAnchor"));
                Assert.That(rig.LegAnchor.name, Is.EqualTo("LegAnchor"));
                Assert.That(rig.HasValidStructure(), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ForestAtlases_ExcludeLargeBackground()
        {
            SpriteAtlas world = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(
                "Assets/Monstrology/Art/Forest/Atlases/ForestWorldAtlas.spriteatlas");
            SpriteAtlas creatures = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(
                "Assets/Monstrology/Art/Forest/Atlases/ForestCreatureAtlas.spriteatlas");

            Assert.That(world, Is.Not.Null);
            Assert.That(creatures, Is.Not.Null);
            string[] worldPaths = SpriteAtlasExtensions.GetPackables(world)
                .Select(AssetDatabase.GetAssetPath)
                .ToArray();
            Assert.That(
                worldPaths,
                Does.Not.Contain(
                    "Assets/Monstrology/Art/Forest/Background/ForestBackground.png"));
            Assert.That(SpriteAtlasExtensions.GetPackables(creatures).Length, Is.EqualTo(2));
        }

        [Test]
        public void Validator_RejectsCurrentGribolisBackdrop()
        {
            string report = ForestArtPackInstaller.GetValidationReport();

            StringAssert.Contains(
                "Gribolis.png пригоден для world sprite",
                report);
            StringAssert.Contains("FOREST ART PACK NOT READY", report);
        }

        [UnityTest]
        public IEnumerator PooledMap_RemainsBoundedAfterMovementInPlayMode()
        {
            yield return new EnterPlayMode();

            GameObject mapObject = new GameObject("ForestPlayModeMap");
            GameObject playerObject = new GameObject("ForestPlayModePlayer");
            SpriteDatabase.Install(
                AssetDatabase.LoadAssetAtPath<SpriteDatabase>(DatabasePath));
            InfiniteBiomeMap map =
                mapObject.AddComponent<InfiniteBiomeMap>();
            map.Initialize(
                BiomeType.Forest,
                SpriteDatabase.Active.GetBiomeBackground(null, null),
                Color.white,
                new Vector2(30f, 18f),
                SpriteDatabase.Active.GetBiomeVisualConfig(BiomeType.Forest),
                playerObject.transform,
                Vector2.zero);

            int decorationPool =
                mapObject.GetComponentsInChildren<WorldDecoration>(true).Length;
            playerObject.transform.position = new Vector3(95f, -61f, 0f);
            yield return null;
            yield return null;

            Assert.That(map.PooledTileCount, Is.EqualTo(9));
            Assert.That(map.GroundPooledTileCount, Is.EqualTo(9));
            Assert.That(map.DecorationSegmentCount, Is.EqualTo(9));
            Assert.That(
                mapObject.GetComponentsInChildren<WorldDecoration>(true).Length,
                Is.EqualTo(decorationPool));

            Object.Destroy(mapObject);
            Object.Destroy(playerObject);
            yield return null;
            yield return new ExitPlayMode();
        }

        private static void DestroyContent(GameContent content)
        {
            if (content == null)
            {
                return;
            }

            List<Object> objects = new List<Object>();
            objects.AddRange(content.creatures);
            objects.AddRange(content.biomes);
            objects.AddRange(content.items);
            objects.AddRange(content.accessories);
            objects.AddRange(content.quests);
            objects.AddRange(content.speciesEvolutions);
            objects.AddRange(content.signatureSets);
            objects.AddRange(content.biomeEvents);
            objects.AddRange(content.creatureNests);
            objects.AddRange(content.biomes
                .Where(biome => biome != null && biome.mapData != null)
                .Select(biome => biome.mapData));
            foreach (Object value in objects.Where(value => value != null).Distinct())
            {
                Object.DestroyImmediate(value);
            }
        }
    }
}
#endif
