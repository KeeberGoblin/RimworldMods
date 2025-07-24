using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using DeathStrandingMod.Core;

namespace DeathStrandingMod.Storyteller
{
    /// <summary>
    /// Manages adaptive narrative progression based on player actions and colony state
    /// </summary>
    public static class AdaptiveNarrativeManager
    {
        private static Dictionary<Map, NarrativeProgression> mapProgressions = new Dictionary<Map, NarrativeProgression>();
        private static int lastGlobalUpdateTick = 0;
        
        /// <summary>
        /// Updates narrative progression for all colonies
        /// </summary>
        public static void UpdateNarrativeProgression()
        {
            int currentTick = Find.TickManager.TicksGame;
            
            // Global update every day
            if (currentTick - lastGlobalUpdateTick > 60000)
            {
                UpdateGlobalNarrative();
                lastGlobalUpdateTick = currentTick;
            }
            
            // Update each map's progression
            foreach (Map map in Find.Maps)
            {
                if (map.IsPlayerHome)
                {
                    UpdateMapProgression(map);
                }
            }
        }

        private static void UpdateGlobalNarrative()
        {
            // Assess overall player performance across all colonies
            GlobalNarrativeMetrics metrics = CalculateGlobalMetrics();
            
            // Adjust global narrative tension
            AdjustGlobalNarrativeTension(metrics);
            
            // Trigger cross-colony events if appropriate
            ConsiderCrossColonyEvents(metrics);
        }

        private static GlobalNarrativeMetrics CalculateGlobalMetrics()
        {
            var metrics = new GlobalNarrativeMetrics();
            
            foreach (Map map in Find.Maps.Where(m => m.IsPlayerHome))
            {
                metrics.totalColonies++;
                metrics.totalPopulation += map.mapPawns.FreeColonists.Count();
                metrics.totalConnectionLevel += DeathStrandingUtility.GetColonyConnectionLevel(map);
                metrics.totalBeachThreat += DeathStrandingUtility.CalculateBeachThreatLevel(map);
                
                // Count DOOMS carriers
                metrics.totalDOOMSCarriers += DeathStrandingUtility.GetDOOMSCarriers(map).Count();
                
                // Assess colony stability
                float stabilityScore = CalculateColonyStability(map);
                metrics.averageStability += stabilityScore;
            }
            
            if (metrics.totalColonies > 0)
            {
                metrics.averageConnectionLevel = metrics.totalConnectionLevel / metrics.totalColonies;
                metrics.averageBeachThreat = metrics.totalBeachThreat / metrics.totalColonies;
                metrics.averageStability = metrics.averageStability / metrics.totalColonies;
            }
            
            return metrics;
        }

        private static float CalculateColonyStability(Map map)
        {
            float stability = 0.5f; // Base stability
            
            // Population stability
            int colonistCount = map.mapPawns.FreeColonists.Count();
            if (colonistCount >= 8)
                stability += 0.2f;
            else if (colonistCount <= 3)
                stability -= 0.2f;
            
            // Mood stability
            float averageMood = 0f;
            int moodCount = 0;
            foreach (Pawn colonist in map.mapPawns.FreeColonists)
            {
                if (colonist.needs?.mood != null)
                {
                    averageMood += colonist.needs.mood.CurLevel;
                    moodCount++;
                }
            }
            
            if (moodCount > 0)
            {
                averageMood /= moodCount;
                stability += (averageMood - 0.5f) * 0.4f; // Mood impact
            }
            
            // Resource stability
            int totalFood = map.resourceCounter.TotalHumanEdibleNutrition;
            if (totalFood > colonistCount * 20)
                stability += 0.1f;
            else if (totalFood < colonistCount * 5)
                stability -= 0.2f;
            
            // Threat level impact
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            stability -= beachThreat * 0.3f;
            
            return Mathf.Clamp01(stability);
        }

        private static void AdjustGlobalNarrativeTension(GlobalNarrativeMetrics metrics)
        {
            // Calculate desired narrative tension based on player performance
            float targetTension = CalculateTargetNarrativeTension(metrics);
            
            // Gradually adjust current tension toward target
            float currentTension = GetCurrentNarrativeTension();
            float adjustedTension = Mathf.Lerp(currentTension, targetTension, 0.1f);
            
            SetGlobalNarrativeTension(adjustedTension);
            
            // Log significant tension changes
            if (Mathf.Abs(adjustedTension - currentTension) > 0.1f)
            {
                Log.Message($"Death Stranding: Global narrative tension adjusted to {adjustedTension:F2}");
            }
        }

        private static float CalculateTargetNarrativeTension(GlobalNarrativeMetrics metrics)
        {
            float baseTension = 0.5f;
            
            // High performance = lower tension
            if (metrics.averageStability > 0.7f && metrics.averageConnectionLevel > 0.6f)
            {
                baseTension -= 0.2f;
            }
            // Poor performance = higher tension
            else if (metrics.averageStability < 0.3f || metrics.averageBeachThreat > 0.7f)
            {
                baseTension += 0.3f;
            }
            
            // Many DOOMS carriers = higher baseline tension
            float doomsRatio = (float)metrics.totalDOOMSCarriers / Math.Max(1, metrics.totalPopulation);
            baseTension += doomsRatio * 0.2f;
            
            // Multiple colonies = complexity bonus
            if (metrics.totalColonies > 1)
            {
                baseTension += metrics.totalColonies * 0.05f;
            }
            
            return Mathf.Clamp01(baseTension);
        }

        private static void ConsiderCrossColonyEvents(GlobalNarrativeMetrics metrics)
        {
            // Only trigger cross-colony events with multiple colonies
            if (metrics.totalColonies < 2)
                return;
            
            // Check for network connection events
            if (metrics.averageConnectionLevel > 0.5f && Rand.Chance(0.1f))
            {
                TriggerCrossColonyNetworkEvent(metrics);
            }
            
            // Check for crisis events requiring cooperation
            if (metrics.averageBeachThreat > 0.6f && Rand.Chance(0.08f))
            {
                TriggerCrossColonyCrisisEvent(metrics);
            }
        }

        private static void TriggerCrossColonyNetworkEvent(GlobalNarrativeMetrics metrics)
        {
            Find.LetterStack.ReceiveLetter(
                "CrossColonyNetworkEventTitle".Translate(),
                "CrossColonyNetworkEventDesc".Translate(metrics.totalColonies),
                LetterDefOf.PositiveEvent
            );
            
            // Reward all colonies with bonus connection
            foreach (Map map in Find.Maps.Where(m => m.IsPlayerHome))
            {
                RewardColonyConnection(map);
            }
        }

        private static void TriggerCrossColonyCrisisEvent(GlobalNarrativeMetrics metrics)
        {
            Find.LetterStack.ReceiveLetter(
                "CrossColonyCrisisEventTitle".Translate(),
                "CrossColonyCrisisEventDesc".Translate(metrics.averageBeachThreat.ToStringPercent()),
                LetterDefOf.ThreatBig
            );
            
            // All colonies face increased threat
            foreach (Map map in Find.Maps.Where(m => m.IsPlayerHome))
            {
                ApplyCrisisEffects(map);
            }
        }

        private static void RewardColonyConnection(Map map)
        {
            // Spawn bonus chiral crystals
            Thing bonus = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
            bonus.stackCount = Rand.Range(3, 8);
            
            IntVec3 dropLocation = DropCellFinder.RandomDropSpot(map);
            GenPlace.TryPlaceThing(bonus, dropLocation, map, ThingPlaceMode.Near);
            
            Messages.Message(
                "NetworkSynergyBonus".Translate(bonus.stackCount),
                new TargetInfo(dropLocation, map),
                MessageTypeDefOf.PositiveEvent
            );
        }

        private static void ApplyCrisisEffects(Map map)
        {
            // Increased BT activity
            if (Rand.Chance(0.3f))
            {
                IncidentParms btParms = new IncidentParms
                {
                    target = map,
                    points = 400f
                };
                
                var btIncident = IncidentDefOf_DeathStranding.BTManifestation;
                btIncident?.Worker?.TryExecute(btParms);
            }
        }

        private static void UpdateMapProgression(Map map)
        {
            if (!mapProgressions.ContainsKey(map))
            {
                mapProgressions[map] = new NarrativeProgression();
            }
            
            NarrativeProgression progression = mapProgressions[map];
            
            // Update progression metrics
            UpdateProgressionMetrics(map, progression);
            
            // Check for narrative milestones
            CheckNarrativeMilestones(map, progression);
            
            // Update character arcs
            UpdateCharacterArcs(map, progression);
            
            // Adjust local narrative elements
            AdjustLocalNarrative(map, progression);
        }

        private static void UpdateProgressionMetrics(Map map, NarrativeProgression progression)
        {
            // Track colony growth
            int currentPopulation = map.mapPawns.FreeColonists.Count();
            if (currentPopulation > progression.peakPopulation)
            {
                progression.peakPopulation = currentPopulation;
                progression.populationGrowthEvents++;
            }
            
            // Track connection development
            float currentConnection = DeathStrandingUtility.GetColonyConnectionLevel(map);
            if (currentConnection > progression.peakConnectionLevel)
            {
                progression.peakConnectionLevel = currentConnection;
            }
            
            // Track Beach encounters
            float currentBeachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            if (currentBeachThreat > progression.maxBeachThreatEncountered)
            {
                progression.maxBeachThreatEncountered = currentBeachThreat;
            }
            
            // Track DOOMS development
            int doomsCarriers = DeathStrandingUtility.GetDOOMSCarriers(map).Count();
            progression.currentDOOMSCarriers = doomsCarriers;
            
            // Update play time
            progression.colonySurvivalDays = (Find.TickManager.TicksGame - progression.colonyStartTick) / 60000;
        }

        private static void CheckNarrativeMilestones(Map map, NarrativeProgression progression)
        {
            // Population milestones
            if (progression.peakPopulation >= 10 && !progression.hasReachedLargeColony)
            {
                progression.hasReachedLargeColony = true;
                TriggerNarrativeMilestone(map, "LargeColonyEstablished", progression.peakPopulation.ToString());
            }
            
            // Connection milestones
            if (progression.peakConnectionLevel >= 0.8f && !progression.hasAchievedFullConnection)
            {
                progression.hasAchievedFullConnection = true;
                TriggerNarrativeMilestone(map, "FullConnectionAchieved", progression.peakConnectionLevel.ToStringPercent());
            }
            
            // Survival milestones
            if (progression.colonySurvivalDays >= 365 && !progression.hasSurvivedOneYear)
            {
                progression.hasSurvivedOneYear = true;
                TriggerNarrativeMilestone(map, "OneYearSurvival", progression.colonySurvivalDays.ToString());
            }
            
            // DOOMS milestones
            if (progression.currentDOOMSCarriers >= 3 && !progression.hasMultipleDOOMSCarriers)
            {
                progression.hasMultipleDOOMSCarriers = true;
                TriggerNarrativeMilestone(map, "MultipleDOOMSCarriers", progression.currentDOOMSCarriers.ToString());
            }
        }

        private static void TriggerNarrativeMilestone(Map map, string milestoneType, string parameter)
        {
            Find.LetterStack.ReceiveLetter(
                $"NarrativeMilestone{milestoneType}Title".Translate(),
                $"NarrativeMilestone{milestoneType}Desc".Translate(parameter),
                LetterDefOf.PositiveEvent,
                map
            );
        }

        private static void UpdateCharacterArcs(Map map, NarrativeProgression progression)
        {
            // Track individual DOOMS carrier development
            foreach (Pawn carrier in DeathStrandingUtility.GetDOOMSCarriers(map))
            {
                UpdateDOOMSCarrierArc(carrier, progression);
            }
            
            // Track colony leader development
            Pawn leader = FindColonyLeader(map);
            if (leader != null)
            {
                UpdateLeadershipArc(leader, progression);
            }
        }

        private static void UpdateDOOMSCarrierArc(Pawn carrier, NarrativeProgression progression)
        {
            int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(carrier);
            
            // Track DOOMS progression
            if (!progression.doomsCarrierProgression.ContainsKey(carrier.thingIDNumber))
            {
                progression.doomsCarrierProgression[carrier.thingIDNumber] = new DOOMSCarrierArc
                {
                    carrierId = carrier.thingIDNumber,
                    initialDOOMSLevel = doomsLevel,
                    currentDOOMSLevel = doomsLevel,
                    firstManifestationTick = Find.TickManager.TicksGame
                };
            }
            
            DOOMSCarrierArc arc = progression.doomsCarrierProgression[carrier.thingIDNumber];
            
            // Check for DOOMS level progression
            if (doomsLevel > arc.currentDOOMSLevel)
            {
                arc.currentDOOMSLevel = doomsLevel;
                arc.levelProgressionEvents++;
                
                TriggerDOOMSProgressionEvent(carrier, doomsLevel);
            }
            
            // Track Beach drift incidents
            if (carrier.health.hediffSet.HasHediff(HediffDefOf_DeathStranding.BeachDrift))
            {
                arc.beachDriftIncidents++;
                
                if (arc.beachDriftIncidents >= 5 && !arc.hasExperiencedSevereBeachDrift)
                {
                    arc.hasExperiencedSevereBeachDrift = true;
                    TriggerSevereBeachDriftEvent(carrier);
                }
            }
        }

        private static void TriggerDOOMSProgressionEvent(Pawn carrier, int newLevel)
        {
            Find.LetterStack.ReceiveLetter(
                "DOOMSProgressionTitle".Translate(),
                "DOOMSProgressionDesc".Translate(carrier.LabelShort, newLevel),
                LetterDefOf.PositiveEvent,
                carrier
            );
        }

        private static void TriggerSevereBeachDriftEvent(Pawn carrier)
        {
            Find.LetterStack.ReceiveLetter(
                "SevereBeachDriftTitle".Translate(),
                "SevereBeachDriftDesc".Translate(carrier.LabelShort),
                LetterDefOf.NegativeEvent,
                carrier
            );
        }

        private static Pawn FindColonyLeader(Map map)
        {
            // Find pawn with highest social skill and good mood
            return map.mapPawns.FreeColonists
                .Where(p => p.skills?.GetSkill(SkillDefOf.Social) != null)
                .OrderByDescending(p => p.skills.GetSkill(SkillDefOf.Social).Level)
                .ThenByDescending(p => p.needs?.mood?.CurLevel ?? 0f)
                .FirstOrDefault();
        }

        private static void UpdateLeadershipArc(Pawn leader, NarrativeProgression progression)
        {
            if (progression.currentLeader != leader.thingIDNumber)
            {
                progression.previousLeader = progression.currentLeader;
                progression.currentLeader = leader.thingIDNumber;
                progression.leadershipChanges++;
                
                if (progression.leadershipChanges > 1)
                {
                    TriggerLeadershipChangeEvent(leader);
                }
            }
        }

        private static void TriggerLeadershipChangeEvent(Pawn newLeader)
        {
            Find.LetterStack.ReceiveLetter(
                "LeadershipChangeTitle".Translate(),
                "LeadershipChangeDesc".Translate(newLeader.LabelShort),
                LetterDefOf.NeutralEvent,
                newLeader
            );
        }

        private static void AdjustLocalNarrative(Map map, NarrativeProgression progression)
        {
            // Adjust incident frequencies based on progression
            float progressionFactor = CalculateProgressionDifficulty(progression);
            
            // Store adjustment factors for use by storyteller components
            SetMapNarrativeAdjustment(map, progressionFactor);
        }

        private static float CalculateProgressionDifficulty(NarrativeProgression progression)
        {
            float difficulty = 1f;
            
            // Increase difficulty with time
            difficulty += progression.colonySurvivalDays / 365f * 0.2f;
            
            // Increase difficulty with population
            difficulty += progression.peakPopulation / 20f * 0.1f;
            
            // Increase difficulty with DOOMS development
            difficulty += progression.currentDOOMSCarriers * 0.1f;
            
            // Reduce difficulty if struggling
            if (progression.maxBeachThreatEncountered > 0.8f && progression.peakConnectionLevel < 0.3f)
            {
                difficulty *= 0.8f; // Ease up on struggling colonies
            }
            
            return Mathf.Clamp(difficulty, 0.5f, 2f);
        }

        // ==================== UTILITY METHODS ====================
        
        private static float GetCurrentNarrativeTension()
        {
            return Find.World.GetComponent<WorldComponent_DeathStranding>()?.GlobalNarrativeTension ?? 0.5f;
        }

        private static void SetGlobalNarrativeTension(float tension)
        {
            var worldComp = Find.World.GetComponent<WorldComponent_DeathStranding>();
            if (worldComp != null)
            {
                worldComp.GlobalNarrativeTension = tension;
            }
        }

        private static void SetMapNarrativeAdjustment(Map map, float adjustment)
        {
            var mapComp = map.GetComponent<MapComponent_DeathStranding>();
            if (mapComp != null)
            {
                mapComp.NarrativeDifficultyAdjustment = adjustment;
            }
        }

        public static NarrativeProgression GetMapProgression(Map map)
        {
            if (!mapProgressions.ContainsKey(map))
            {
                mapProgressions[map] = new NarrativeProgression();
            }
            
            return mapProgressions[map];
        }

        public static void ExposeData()
        {
            Scribe_Collections.Look(ref mapProgressions, "mapProgressions", LookMode.Reference, LookMode.Deep);
            Scribe_Values.Look(ref lastGlobalUpdateTick, "lastGlobalUpdateTick");
        }
    }

    // ==================== DATA CLASSES ====================
    
    public class GlobalNarrativeMetrics
    {
        public int totalColonies = 0;
        public int totalPopulation = 0;
        public int totalDOOMSCarriers = 0;
        public float totalConnectionLevel = 0f;
        public float totalBeachThreat = 0f;
        public float averageConnectionLevel = 0f;
        public float averageBeachThreat = 0f;
        public float averageStability = 0f;
    }

    public class NarrativeProgression : IExposable
    {
        // Colony metrics
        public int colonyStartTick = 0;
        public int colonySurvivalDays = 0;
        public int peakPopulation = 0;
        public int populationGrowthEvents = 0;
        public float peakConnectionLevel = 0f;
        public float maxBeachThreatEncountered = 0f;
        public int currentDOOMSCarriers = 0;
        
        // Milestone tracking
        public bool hasReachedLargeColony = false;
        public bool hasAchievedFullConnection = false;
        public bool hasSurvivedOneYear = false;
        public bool hasMultipleDOOMSCarriers = false;
        
        // Character progression
        public Dictionary<int, DOOMSCarrierArc> doomsCarrierProgression = new Dictionary<int, DOOMSCarrierArc>();
        public int currentLeader = -1;
        public int previousLeader = -1;
        public int leadershipChanges = 0;
        
        public NarrativeProgression()
        {
            colonyStartTick = Find.TickManager.TicksGame;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref colonyStartTick, "colonyStartTick");
            Scribe_Values.Look(ref colonySurvivalDays, "colonySurvivalDays");
            Scribe_Values.Look(ref peakPopulation, "peakPopulation");
            Scribe_Values.Look(ref populationGrowthEvents, "populationGrowthEvents");
            Scribe_Values.Look(ref peakConnectionLevel, "peakConnectionLevel");
            Scribe_Values.Look(ref maxBeachThreatEncountered, "maxBeachThreatEncountered");
            Scribe_Values.Look(ref currentDOOMSCarriers, "currentDOOMSCarriers");
            
            Scribe_Values.Look(ref hasReachedLargeColony, "hasReachedLargeColony");
            Scribe_Values.Look(ref hasAchievedFullConnection, "hasAchievedFullConnection");
            Scribe_Values.Look(ref hasSurvivedOneYear, "hasSurvivedOneYear");
            Scribe_Values.Look(ref hasMultipleDOOMSCarriers, "hasMultipleDOOMSCarriers");
            
            Scribe_Collections.Look(ref doomsCarrierProgression, "doomsCarrierProgression", LookMode.Value, LookMode.Deep);
            Scribe_Values.Look(ref currentLeader, "currentLeader");
            Scribe_Values.Look(ref previousLeader, "previousLeader");
            Scribe_Values.Look(ref leadershipChanges, "leadershipChanges");
        }
    }

    public class DOOMSCarrierArc : IExposable
    {
        public int carrierId = -1;
        public int initialDOOMSLevel = 0;
        public int currentDOOMSLevel = 0;
        public int levelProgressionEvents = 0;
        public int beachDriftIncidents = 0;
        public int firstManifestationTick = 0;
        public bool hasExperiencedSevereBeachDrift = false;

        public void ExposeData()
        {
            Scribe_Values.Look(ref carrierId, "carrierId");
            Scribe_Values.Look(ref initialDOOMSLevel, "initialDOOMSLevel");
            Scribe_Values.Look(ref currentDOOMSLevel, "currentDOOMSLevel");
            Scribe_Values.Look(ref levelProgressionEvents, "levelProgressionEvents");
            Scribe_Values.Look(ref beachDriftIncidents, "beachDriftIncidents");
            Scribe_Values.Look(ref firstManifestationTick, "firstManifestationTick");
            Scribe_Values.Look(ref hasExperiencedSevereBeachDrift, "hasExperiencedSevereBeachDrift");
        }
    }
}