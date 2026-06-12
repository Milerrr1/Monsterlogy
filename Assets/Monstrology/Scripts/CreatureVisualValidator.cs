using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Monstrology
{
    [DisallowMultipleComponent]
    public class CreatureVisualValidator : MonoBehaviour
    {
        private const float PositionTolerance = 0.001f;
        private const float PivotTolerance = 0.02f;

        public string LastReport { get; private set; }

        [ContextMenu("Validate Creature Template")]
        public bool ValidateCreatureTemplate()
        {
            List<string> warnings = new List<string>();
            CreatureBaseTemplate template = CreatureBaseTemplate.Active;
            GameManager game = FindObjectOfType<GameManager>();

            if (template == null || !template.HasValidStandard())
            {
                warnings.Add(
                    "CreatureBaseTemplate должен использовать холст 2048x2048, " +
                    "безопасную зону 10% и пропорции 40/40/20.");
            }

            if (game == null || game.Content == null)
            {
                warnings.Add("GameManager и данные существ не инициализированы.");
            }
            else
            {
                ValidateCreatureData(game.Content.creatures, warnings);
            }

            ValidateRuntimeRigs(template, warnings);
            ValidateUiAnchors(warnings);
            return Finish(warnings);
        }

        private static void ValidateCreatureData(
            IEnumerable<CreatureData> creatures,
            ICollection<string> warnings)
        {
            SpriteDatabase database = SpriteDatabase.Active;
            HashSet<string> ids = new HashSet<string>();
            HashSet<Sprite> checkedSprites = new HashSet<Sprite>();
            foreach (CreatureData creature in creatures)
            {
                if (creature == null)
                {
                    warnings.Add("В списке существ есть пустая ссылка.");
                    continue;
                }

                string label = string.IsNullOrEmpty(creature.creatureName)
                    ? creature.name
                    : creature.creatureName;
                if (string.IsNullOrEmpty(creature.id))
                {
                    warnings.Add(label + ": отсутствует стабильный id.");
                }
                else if (!ids.Add(creature.id))
                {
                    warnings.Add(label + ": дублируется id " + creature.id + ".");
                }

                Sprite portrait = database.GetCreaturePortrait(creature);
                Sprite world = database.GetCreatureWorld(creature);
                Sprite evolution = database.GetCreatureEvolution(creature);
                if (portrait == null)
                {
                    warnings.Add(label + ": PortraitSprite не разрешён.");
                }

                if (world == null)
                {
                    warnings.Add(label + ": WorldSprite не разрешён.");
                }

                if (evolution == null)
                {
                    warnings.Add(label + ": EvolutionSprite fallback не разрешён.");
                }

                ValidateSprite(portrait, label + " / PortraitSprite", checkedSprites, warnings);
                ValidateSprite(world, label + " / WorldSprite", checkedSprites, warnings);
                ValidateSprite(
                    evolution,
                    label + " / EvolutionSprite",
                    checkedSprites,
                    warnings);
            }
        }

        private static void ValidateSprite(
            Sprite sprite,
            string label,
            ISet<Sprite> checkedSprites,
            ICollection<string> warnings)
        {
            if (sprite == null || !checkedSprites.Add(sprite) || IsRuntimePlaceholder(sprite))
            {
                return;
            }

            Texture2D texture = sprite.texture;
            if (texture == null ||
                texture.width != CreatureBaseTemplate.TargetTextureSize ||
                texture.height != CreatureBaseTemplate.TargetTextureSize)
            {
                warnings.Add(label + ": исходная текстура должна быть 2048x2048.");
            }

            Vector2 normalizedPivot = new Vector2(
                sprite.pivot.x / Mathf.Max(1f, sprite.rect.width),
                sprite.pivot.y / Mathf.Max(1f, sprite.rect.height));
            if (Mathf.Abs(normalizedPivot.x - 0.5f) > PivotTolerance ||
                Mathf.Abs(normalizedPivot.y - 0.5f) > PivotTolerance)
            {
                warnings.Add(label + ": pivot должен находиться в центре.");
            }
        }

        private static bool IsRuntimePlaceholder(Sprite sprite)
        {
            return (sprite.hideFlags & HideFlags.DontSave) != 0 ||
                   (sprite.texture != null &&
                    (sprite.texture.hideFlags & HideFlags.DontSave) != 0);
        }

        private static void ValidateRuntimeRigs(
            CreatureBaseTemplate template,
            ICollection<string> warnings)
        {
            CreatureVisualRig[] rigs = FindObjectsOfType<CreatureVisualRig>(true);
            if (rigs.Length == 0)
            {
                warnings.Add("Не найдено ни одного CreatureVisualRig.");
                return;
            }

            foreach (CreatureVisualRig rig in rigs)
            {
                rig.EnsureStructure();
                if (!rig.HasValidStructure())
                {
                    warnings.Add(rig.name + ": структура CreatureVisualRig неполна.");
                    continue;
                }

                ValidateAnchor(
                    rig,
                    AccessorySlot.Head,
                    template,
                    warnings);
                ValidateAnchor(
                    rig,
                    AccessorySlot.Body,
                    template,
                    warnings);
                ValidateAnchor(
                    rig,
                    AccessorySlot.Legs,
                    template,
                    warnings);

                if (rig.CreatureRenderer == null ||
                    rig.CreatureRenderer.transform.name != CreatureVisualRig.VisualName ||
                    rig.CreatureRenderer.transform.parent != rig.transform)
                {
                    warnings.Add(
                        rig.name + ": основной SpriteRenderer должен находиться в Visual.");
                }
            }
        }

        private static void ValidateAnchor(
            CreatureVisualRig rig,
            AccessorySlot slot,
            CreatureBaseTemplate template,
            ICollection<string> warnings)
        {
            Transform anchor = rig.GetAnchor(slot);
            string expectedName = CreatureVisualRig.GetAnchorName(slot);
            Vector3 expectedPosition = template.GetAnchorPosition(slot);
            if (anchor == null ||
                anchor.name != expectedName ||
                anchor.parent != rig.transform ||
                Vector3.Distance(anchor.localPosition, expectedPosition) >
                    PositionTolerance)
            {
                warnings.Add(rig.name + ": некорректный " + expectedName + ".");
                return;
            }

            for (int index = 0; index < anchor.childCount; index++)
            {
                if (anchor.GetChild(index).GetComponent<SpriteRenderer>() == null)
                {
                    warnings.Add(
                        rig.name + ": одежда в " + expectedName +
                        " должна иметь отдельный SpriteRenderer.");
                }
            }
        }

        private static void ValidateUiAnchors(ICollection<string> warnings)
        {
            RectTransform[] rects = FindObjectsOfType<RectTransform>(true);
            foreach (RectTransform root in rects.Where(rect =>
                         rect != null && rect.name == "EquippedAccessories"))
            {
                foreach (AccessorySlot slot in
                         (AccessorySlot[])Enum.GetValues(typeof(AccessorySlot)))
                {
                    string anchorName = CreatureVisualRig.GetAnchorName(slot);
                    Transform anchor = root.Find(anchorName);
                    if (anchor == null)
                    {
                        warnings.Add(
                            "UI гардероба: отсутствует " + anchorName + ".");
                    }
                }
            }
        }

        private bool Finish(ICollection<string> warnings)
        {
            bool valid = warnings.Count == 0;
            StringBuilder report = new StringBuilder();
            report.AppendLine("=== CREATURE TEMPLATE VALIDATION ===");
            report.AppendLine(
                "Стандарт: 2048x2048, safe zone 10%, голова/тело/ноги 40/40/20.");
            if (valid)
            {
                report.AppendLine(
                    "✓ Спрайты, Visual, HeadAnchor, BodyAnchor, LegAnchor и гардероб проверены.");
                report.Append("CREATURE_TEMPLATE_VALIDATION_PASS");
            }
            else
            {
                foreach (string warning in warnings)
                {
                    report.AppendLine("⚠ " + warning);
                }

                report.Append("CREATURE_TEMPLATE_VALIDATION_FAIL");
            }

            LastReport = report.ToString();
            if (valid)
            {
                Debug.Log(LastReport, this);
            }
            else
            {
                Debug.LogWarning(LastReport, this);
            }

            return valid;
        }
    }
}
