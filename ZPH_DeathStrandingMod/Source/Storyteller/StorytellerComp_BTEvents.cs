using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using DeathStrandingMod.Core;
using DeathStrandingMod.Events;

namespace DeathStrandingMod.Storyteller
{
    /// <summary>
    /// Storyteller component dedicated to managing BT-related incidents and progression
    /// </summary>
    public class StorytellerComp_BTEvents : StorytellerComp
    {
        private StorytellerCompProperties_BTEvents Props => 
            (StorytellerCompProperties_BTEvents)props;
        
        private int lastBTAssessmentTick = 0;
        private Dictionary<Map, BTEventTrackingData> mapBTData = new Dictionary<Map, BTEventTrackingData>();

        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            Map map = target as Map;
            if (map?.IsPlayerHome != true)
                yield break;
            
            UpdateBTTracking(map);
            
            // Generate BT manifestation events
            foreach (var incident in GenerateBTManifestationEvents(map))
                yield return incident;
            
            // Generate tether progression events
            foreach (var incident in GenerateTetherEvents(map))
                yield return incident;
            
            // Generate BT behavioral events
            foreach (var incident in GenerateBTBehaviorEvents(map))
                yield return incident;
            
            // Generate corpse conversion events
            foreach (var incident in GenerateCorpseConversionEvents(map))
                yield return incident;
        }

        private void UpdateBTTracking(Map map)
        {
            if (!mapBTData.ContainsKey(map))
            {
                mapBTData[map] = new BTEventTrackingData();
            }
            
            BTEventTrackingData data = mapBTData[map];
            int currentTick = Find.TickManager.TicksGame;
            
            // Update BT population tracking
            UpdateBTPopulationData(map, data);
            
            // Update tether severity tracking
            UpdateTetherSeverityData(map, data);
            
            // Periodic assessment
            if (currentTick - lastBTAssessmentTick > 15000) // Every ~4 hours
            {
                AssessBTThreatProgression(map, data);
                lastBTAssessmentTick = currentTick;
            }
        }

        private void UpdateBTPopulationData(Map map, BTEventTrackingData data)
        {
            var currentBTs = map.mapPawns.AllPawnsSpawned
                .Where(p => p.def.defName.StartsWith("BT_"))
                .ToList();
            
            int currentBTCount = currentBTs.Count;
            
            // Track population changes
            if (currentBTCount != data.lastBTCount)
            {
                data.btPopulationHistory.Add(new BTPopulationSnapshot
                {
                    tick = Find.TickManager.TicksGame,
                    btCount = currentBTCount,
                    btTypes = currentBTs.GroupBy(bt => bt.def.defName)
                        .ToDictionary(g => g.Key, g => g.Count())
                });
                
                // Limit history size
                if (data.btPopulationHistory.Count > 20)
                {
                    data.btPopulationHistory.RemoveAt(0);
                }
                
                data.lastBTCount = currentBTCount;
            }
            
            // Update peak tracking
            if (currentBTCount > data.peakBTCount)
            {
                data.peakBTCount = currentBTCount;
                CheckForBTPopulationMilestone(map, data, currentBTCount);
            }
        }

        private void CheckForBTPopulationMilestone(Map map, BTEventTrackingData data, int count)
        {
            if (count >= 10 && !data.hasReachedHighBTPopulation)
            {
                data.hasReachedHighBTPopulation = true;
                TriggerBTPopulationMilestone(map, "HighBTPopulation", count);
            }
            else if (count >= 5 && !data.hasReachedModerateBTPopulation)
            {
                data.hasReachedModerateBTPopulation = true;
                TriggerBTPopulationMilestone(map, "ModerateBTPopulation", count);
            }
        }

        private void TriggerBTPopulationMilestone(Map map, string milestoneType, int count)
        {
            Find.LetterStack.ReceiveLetter(
                $"BTPopulationMilestone{milestoneType}Title".Translate(),
                $"BTPopulationMilestone{milestoneType}Desc".Translate(count),
                LetterDefOf.ThreatBig,
                map
            );
        }

        private void UpdateTetherSeverityData(Map map, BTEventTrackingData data)
        {
            float totalTetherSeverity = 0f;
            int tetherCount = 0;
            float maxSeverity = 0f;
            
            foreach (Pawn colonist in map.mapPawns.FreeColonists)
            {
                Hediff tether = colonist.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
                if (tether != null)
                {
                    totalTetherSeverity += tether.Severity;
                    tetherCount++;
                    maxSeverity = Math.Max(maxSeverity, tether.Severity);
                }
            }
            
            data.averageTetherSeverity = tetherCount > 0 ? totalTetherSeverity / tetherCount : 0f;
            data.maxTetherSeverity = maxSeverity;
            data.tetherAffectedColonists = tetherCount;
            
            // Check for critical tether situations
            if (maxSeverity >= 0.9f && !data.hasCriticalTetherWarning)
            {
                data.hasCriticalTetherWarning = true;
                TriggerCriticalTetherWarning(map, maxSeverity);
            }
        }

        private void TriggerCriticalTetherWarning(Map map, float severity)
        {
            Find.LetterStack.ReceiveLetter(
                "CriticalTetherWarningTitle".Translate(),
                "CriticalTetherWarningDesc".Translate((severity * 100f).ToString("F0")),
                LetterDefOf.ThreatBig,
                map
            );
        }

        private void AssessBTThreatProgression(Map map, BTEventTrackingData data)
        {
            // Calculate threat escalation factors
            float threatEscalation = CalculateThreatEscalation(data);
            
            // Adjust BT event frequencies based on progression
            AdjustBTEventRates(data, threatEscalation);
            
            // Check for threat pattern events