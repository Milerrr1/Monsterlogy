using System;
using System.Collections.Generic;
using UnityEngine;

namespace Monstrology
{
    public enum CreatureRarity
    {
        Common,
        Rare,
        Epic,
        Legendary,
        Secret
    }

    public enum CreatureElement
    {
        Nature,
        Sand,
        Ice,
        Fire,
        Water,
        Space,
        Electric,
        Mystery
    }

    public enum AppearanceConditionType
    {
        None,
        CurrentBiome,
        HasItem,
        FoundCreaturesOfElement,
        ExplorationCount
    }

    [Serializable]
    public class AppearanceCondition
    {
        public AppearanceConditionType type;
        public BiomeType requiredBiome;
        public string requiredItemId;
        public CreatureElement requiredElement;
        [Min(1)] public int requiredCount = 1;

        public string GetHint()
        {
            switch (type)
            {
                case AppearanceConditionType.CurrentBiome:
                    return "Исследуйте нужный биом.";
                case AppearanceConditionType.HasItem:
                    return "Найдите особый предмет.";
                case AppearanceConditionType.FoundCreaturesOfElement:
                    return "Соберите существ одной стихии.";
                case AppearanceConditionType.ExplorationCount:
                    return "Продолжайте исследования.";
                default:
                    return "Особых условий нет.";
            }
        }
    }

    [CreateAssetMenu(fileName = "Creature", menuName = "Monstrology/Creature")]
    public class CreatureData : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string creatureName;
        [TextArea(2, 5)] public string description;
        public CreatureRarity rarity;
        public CreatureElement element;
        public BiomeType biome;

        [Header("Discovery")]
        public Sprite icon;
        [Min(0.01f)] public float appearanceChance = 1f;
        public List<AppearanceCondition> appearanceConditions = new List<AppearanceCondition>();

        [Header("Editor preview only")]
        [Tooltip("Runtime progress is stored by GameManager. This flag is convenient only for authoring previews.")]
        public bool found;
    }
}
