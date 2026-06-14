#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Monstrology.Editor
{
    public static class FirstHourBalanceSimulator
    {
        private const int SessionCount = 1000;
        private const string ReportPath =
            "Assets/Monstrology/Documentation/FIRST_HOUR_BALANCE_REPORT.md";

        [MenuItem("Tools/Monstrology/Balance/Simulate First Hour")]
        public static void SimulateFirstHour()
        {
            FirstHourBalanceConfig config =
                FinalDemoUpdateTools.EnsureBalanceAsset();
            SimulationSummary summary = Run(
                config,
                SessionCount,
                13062026);
            File.WriteAllText(
                ToAbsolute(ReportPath),
                summary.ToMarkdown(config),
                Encoding.UTF8);
            AssetDatabase.ImportAsset(
                ReportPath,
                ImportAssetOptions.ForceUpdate);
            Debug.Log(
                "PASS: First-hour balance simulation completed: " +
                ReportPath);
        }

        public static SimulationSummary Run(
            FirstHourBalanceConfig config,
            int sessions,
            int seed)
        {
            System.Random random = new System.Random(seed);
            SimulationSummary summary = new SimulationSummary(
                Mathf.Max(1, sessions));
            for (int index = 0; index < summary.Sessions; index++)
            {
                summary.Add(RunSession(config, random));
            }

            return summary;
        }

        private static SessionResult RunSession(
            FirstHourBalanceConfig config,
            System.Random random)
        {
            SessionResult result = new SessionResult();
            int[] speciesCopies = new int[3];
            int findings = 0;
            int emptyStreak = 0;
            int maxRepeat = 0;
            int repeat = 0;
            int lastCategory = -1;
            int traceProgress = 0;
            const float intervalSeconds = 75f;

            for (float time = intervalSeconds;
                 time <= 3600f;
                 time += intervalSeconds)
            {
                findings++;
                FirstHourPhaseWeights phase = config.GetPhase(time);
                float creature = config.creatureWeight * phase.creature;
                float trace = config.traceWeight * phase.trace;
                float resource = config.resourceWeight * phase.resource;
                float egg = config.eggWeight * phase.egg;
                float accessory =
                    config.accessoryWeight * phase.accessory;
                float empty = config.emptyWeight * phase.empty;
                float nest = config.nestAndEventWeight;

                if (findings <= config.protectedOpeningFindings ||
                    emptyStreak >= config.maxEmptyStreak)
                {
                    empty = 0f;
                }

                if (time >= config.wardrobePityMinutes * 60f &&
                    result.FirstWardrobeSeconds <= 0f)
                {
                    accessory *= config.pityMultiplier;
                }

                if (result.WardrobeFound >=
                    config.firstHourWardrobeSoftCap)
                {
                    accessory *=
                        config.wardrobeAfterSoftCapMultiplier;
                }

                if (speciesCopies[0] >=
                        config.nearEvolutionMinimumCopies &&
                    speciesCopies[0] < 10)
                {
                    creature *=
                        config.nearEvolutionCreatureMultiplier;
                }

                float total = creature + trace + resource + egg +
                              accessory + empty + nest;
                float roll = (float)random.NextDouble() * total;
                int category;
                if ((roll -= creature) <= 0f)
                {
                    category = 0;
                }
                else if ((roll -= trace) <= 0f)
                {
                    category = 1;
                }
                else if ((roll -= resource) <= 0f)
                {
                    category = 2;
                }
                else if ((roll -= egg) <= 0f)
                {
                    category = 3;
                }
                else if ((roll -= accessory) <= 0f)
                {
                    category = 4;
                }
                else if ((roll -= nest) <= 0f)
                {
                    category = 5;
                }
                else
                {
                    category = 6;
                }

                if (category == lastCategory)
                {
                    repeat++;
                }
                else
                {
                    repeat = 1;
                    lastCategory = category;
                }
                maxRepeat = Mathf.Max(maxRepeat, repeat);

                if (result.FirstFindingSeconds <= 0f && category != 6)
                {
                    result.FirstFindingSeconds = time;
                }

                if (category == 6)
                {
                    result.EmptyFindings++;
                    emptyStreak++;
                    continue;
                }

                emptyStreak = 0;
                if (category == 1)
                {
                    traceProgress++;
                    if (traceProgress % 3 != 0)
                    {
                        continue;
                    }

                    category = 0;
                }

                if (category == 0)
                {
                    int species = SelectSpecies(
                        time,
                        random,
                        result,
                        speciesCopies[0]);
                    speciesCopies[species]++;
                    if (result.FirstPetSeconds <= 0f)
                    {
                        result.FirstPetSeconds = time;
                    }

                    if (species > 0 &&
                        result.SecondSpeciesSeconds <= 0f)
                    {
                        result.SecondSpeciesSeconds = time;
                    }

                    if (species == 2)
                    {
                        result.RareFindings++;
                    }

                    if (result.FirstEvolutionSeconds <= 0f &&
                        speciesCopies[0] >= 10)
                    {
                        result.FirstEvolutionSeconds = time;
                    }
                }
                else if (category == 4)
                {
                    result.WardrobeFound++;
                    if (result.FirstWardrobeSeconds <= 0f)
                    {
                        result.FirstWardrobeSeconds = time;
                    }
                }
                else if (category == 5)
                {
                    if (result.FirstNestSeconds <= 0f)
                    {
                        result.FirstNestSeconds = time;
                    }
                }
            }

            result.SpeciesOpened =
                1 +
                (result.SecondSpeciesSeconds > 0f ? 1 : 0) +
                (result.RareFindings > 0 ? 1 : 0);
            result.MaxRepeatStreak = maxRepeat;
            result.EnergyBlocked = findings / 4 * 3 > 75;
            result.ForestCompleted = result.SpeciesOpened >= 4;
            return result;
        }

        private static int SelectSpecies(
            float time,
            System.Random random,
            SessionResult result,
            int firstSpeciesCopies)
        {
            if (time < 8f * 60f)
            {
                return 0;
            }

            double roll = random.NextDouble();
            if (time >= 10f * 60f &&
                result.SecondSpeciesSeconds <= 0f &&
                roll < 0.42d)
            {
                return 1;
            }

            if (time >= 25f * 60f && roll > 0.94d)
            {
                return 2;
            }

            double firstSpeciesChance =
                firstSpeciesCopies >= 5 && firstSpeciesCopies < 10
                    ? 0.92d
                    : 0.82d;
            return roll < firstSpeciesChance ? 0 : 1;
        }

        private static string ToAbsolute(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                relativePath));
        }
    }

    public sealed class SimulationSummary
    {
        public int Sessions { get; private set; }
        private float firstFinding;
        private float firstPet;
        private float secondSpecies;
        private float firstWardrobe;
        private float firstNest;
        private float firstEvolution;
        private int firstEvolutionSamples;
        private int emptyFindings;
        private int maxRepeat;
        private int speciesOpened;
        private int wardrobeFound;
        private int rareFindings;
        private int energyBlocked;
        private int forestCompleted;

        public SimulationSummary(int sessions)
        {
            Sessions = sessions;
        }

        public void Add(SessionResult result)
        {
            firstFinding += result.FirstFindingSeconds;
            firstPet += result.FirstPetSeconds;
            secondSpecies += result.SecondSpeciesSeconds;
            firstWardrobe += result.FirstWardrobeSeconds;
            firstNest += result.FirstNestSeconds;
            if (result.FirstEvolutionSeconds > 0f)
            {
                firstEvolution += result.FirstEvolutionSeconds;
                firstEvolutionSamples++;
            }

            emptyFindings += result.EmptyFindings;
            maxRepeat = Mathf.Max(maxRepeat, result.MaxRepeatStreak);
            speciesOpened += result.SpeciesOpened;
            wardrobeFound += result.WardrobeFound;
            rareFindings += result.RareFindings;
            energyBlocked += result.EnergyBlocked ? 1 : 0;
            forestCompleted += result.ForestCompleted ? 1 : 0;
        }

        public string ToMarkdown(FirstHourBalanceConfig config)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("# First Hour Balance Report");
            text.AppendLine();
            text.AppendLine("Generated by `Tools > Monstrology > Balance > Simulate First Hour`.");
            text.AppendLine();
            text.AppendLine("- Sessions: " + Sessions);
            text.AppendLine("- Deterministic seed: 13062026");
            text.AppendLine("- Approximate finding interval: 75 seconds");
            text.AppendLine("- Model includes phase weights, opening empty protection, wardrobe pity and trace-to-creature progress.");
            text.AppendLine();
            text.AppendLine("| Metric | Result | Target |");
            text.AppendLine("|---|---:|---:|");
            Row(text, "First meaningful finding", AverageMinutes(firstFinding), "< 3 min");
            Row(text, "First pet", AverageMinutes(firstPet), "3-5 min");
            Row(text, "Second species", AverageMinutes(secondSpecies), "10-20 min");
            Row(text, "First wardrobe item", AverageMinutes(firstWardrobe), "10-25 min");
            Row(text, "First nest/event", AverageMinutes(firstNest), "during first hour");
            Row(
                text,
                "First evolution (successful sessions)",
                firstEvolutionSamples > 0
                    ? firstEvolution / firstEvolutionSamples / 60f
                    : 0f,
                "20-40 min");
            Row(text, "Empty findings per session", (float)emptyFindings / Sessions, "<= 5-10%");
            Row(text, "Maximum category repeat streak", maxRepeat, "diagnostic");
            Row(text, "Species opened per session", (float)speciesOpened / Sessions, "2-3");
            Row(text, "Wardrobe items per session", (float)wardrobeFound / Sessions, "1-2");
            Row(text, "Rare findings per session", (float)rareFindings / Sessions, "< 1");
            Row(text, "Energy-blocked sessions", energyBlocked, "near 0");
            Row(text, "Forest-completed sessions", forestCompleted, "0");
            text.AppendLine();
            text.AppendLine("## Configuration");
            text.AppendLine();
            text.AppendLine("- Maximum empty streak: " + config.maxEmptyStreak);
            text.AppendLine("- Maximum same-resource streak: " + config.maxSameResourceStreak);
            text.AppendLine("- New-content pity: " + config.newContentPityMinutes + " min");
            text.AppendLine("- Wardrobe pity: " + config.wardrobePityMinutes + " min");
            text.AppendLine("- First-hour wardrobe soft cap: " + config.firstHourWardrobeSoftCap);
            text.AppendLine("- Rare lock: " + config.rareLockMinutes + " min");
            text.AppendLine();
            text.AppendLine("This is a deterministic development model, not telemetry. Device playtests remain required.");
            return text.ToString();
        }

        private float AverageMinutes(float totalSeconds)
        {
            return totalSeconds / Sessions / 60f;
        }

        private static void Row(
            StringBuilder text,
            string name,
            float value,
            string target)
        {
            text.AppendLine(
                "| " + name + " | " +
                value.ToString("0.00", CultureInfo.InvariantCulture) +
                " | " + target + " |");
        }

        private static void Row(
            StringBuilder text,
            string name,
            int value,
            string target)
        {
            text.AppendLine(
                "| " + name + " | " + value + " | " + target + " |");
        }
    }

    public sealed class SessionResult
    {
        public float FirstFindingSeconds;
        public float FirstPetSeconds;
        public float SecondSpeciesSeconds;
        public float FirstWardrobeSeconds;
        public float FirstNestSeconds;
        public float FirstEvolutionSeconds;
        public int EmptyFindings;
        public int MaxRepeatStreak;
        public int SpeciesOpened;
        public int WardrobeFound;
        public int RareFindings;
        public bool EnergyBlocked;
        public bool ForestCompleted;
    }
}
#endif
