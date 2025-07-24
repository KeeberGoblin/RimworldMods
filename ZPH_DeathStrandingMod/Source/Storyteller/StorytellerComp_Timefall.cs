using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using DeathStrandingMod.Core;
using DeathStrandingMod.Weather;

namespace DeathStrandingMod.Storyteller
{
    /// <summary>
    /// Storyteller component specifically focused on timefall weather patterns and related events
    /// </summary>
    public class StorytellerComp_Timefall : StorytellerComp
    {
        private StorytellerCompProperties_Timefall Props => 
            (StorytellerCompProperties_Timefall)props;
        
        private int lastTimefallEventTick = 0;
        private int lastTimefallAssessmentTick = 0;
        private Dictionary<Map, TimefallTrackingData> mapTimefallData = new Dictionary<Map, TimefallTrackingData>();

        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            Map map = target as Map;
            if (map?.IsPlayerHome != true)
                yield break;
            
            UpdateTimefallTracking(map);
            
            // Generate timefall weather incidents
            foreach (var incident in GenerateTimefallWeatherEvents(map))
                yield return incident;
            
            // Generate timefall-related incidents
            foreach (var incident in GenerateTimefallRelatedEvents(map))
                yield return incident;
            
            // Generate post-timefall events
            foreach (var incident in GeneratePostTimefallEvents(map))
                yield return incident;
        }

        private void UpdateTimefallTracking(Map map)
        {
            if (!mapTimefallData.ContainsKey(map))
            {
                mapTimefallData[map] = new TimefallTrackingData();
            }
            
            TimefallTrackingData data = mapTimefallData[map];
            int currentTick = Find.TickManager.TicksGame;
            
            // Update timefall status
            bool isCurrentlyTimefall = TimefallWeatherManager.IsTimefallActive(map);
            
            if (isCurrentlyTimefall && !data.wasTimefallLastCheck)
            {
                // Timefall just started
                data.timefallStartTick = currentTick;
                data.timefallEventCount++;
                OnTimefallStarted(map, data);
            }
            else if (!isCurrentlyTimefall && data.wasTimefallLastCheck)
            {
                // Timefall just ended
                data.lastTimefallEndTick = currentTick;
                data.totalTimefallDuration += currentTick - data.timefallStartTick;
                OnTimefallEnded(map, data);
            }
            
            data.wasTimefallLastCheck = isCurrentlyTimefall;
            
            // Periodic assessment
            if (currentTick - lastTimefallAssessmentTick > 30000) // Every ~8 hours
            {
                AssessTimefallPatterns(map, data);
                lastTimefallAssessmentTick = currentTick;
            }
        }

        private void OnTimefallStarted(Map map, TimefallTrackingData data)
        {
            // Alert DOOMS carriers
            AlertDOOMSCarriersOfTimefall(map, true);
            
            // Create atmospheric buildup
            CreateTimefallStartEffects(map);
            
            // Check for special timefall variants
            CheckForSpecialTimefallEvents(map, data);
        }

        private void AlertDOOMSCarriersOfTimefall(Map map, bool starting)
        {
            string messageKey = starting ? "TimefallStartingAlert" : "TimefallEndingAlert";
            
            foreach (Pawn carrier in DeathStrandingUtility.GetDOOMSCarriers(map))
            {
                int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(carrier);
                if (doomsLevel >= 2)
                {
                    Messages.Message(
                        messageKey.Translate(carrier.LabelShort),
                        carrier,
                        starting ? MessageTypeDefOf.CautionInput : MessageTypeDefOf.PositiveEvent
                    );
                }
            }
        }

        private void CreateTimefallStartEffects(Map map)
        {
            // Atmospheric disturbances across the map
            for (int i = 0; i < 15; i++)
            {
                IntVec3 effectLocation = CellFinder.RandomCell(map);
                
                switch (Rand.Range(0, 4))
                {
                    case 0:
                        FleckMaker.ThrowSmoke(effectLocation.ToVector3Shifted(), map, 2f);
                        break;
                    case 1:
                        FleckMaker.Static(effectLocation, map, FleckDefOf.PsycastPsychicEffect, 1.5f);
                        break;
                    case 2:
                        FleckMaker.ThrowLightningGlow(effectLocation.ToVector3Shifted(), map, 1f);
                        break;
                    case 3:
                        FleckMaker.ThrowAirPuffUp(effectLocation.ToVector3Shifted(), map);
                        break;
                }
            }
        }

        private void CheckForSpecialTimefallEvents(Map map, TimefallTrackingData data)
        {
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            
            // Chance for timefall storm instead of regular timefall
            if (beachThreat > 0.6f && Rand.Chance(0.3f))
            {
                TriggerTimefallStormUpgrade(map);
            }
            
            // Chance for temporal distortion during high-threat timefall
            if (beachThreat > 0.8f && Rand.Chance(0.2f))
            {
                TriggerTemporalDistortionEvent(map);
            }
        }

        private void TriggerTimefallStormUpgrade(Map map)
        {
            // Upgrade current timefall to storm
            WeatherDef stormWeather = WeatherDefOf_DeathStranding.TimefallStorm;
            if (stormWeather != null)
            {
                map.weatherManager.TransitionTo(stormWeather);
                
                Messages.Message(
                    "TimefallUpgradedToStorm".Translate(),
                    MessageTypeDefOf.ThreatBig
                );
            }
        }

        private void TriggerTemporalDistortionEvent(Map map)
        {
            // Apply temporary time distortion effects
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.RaceProps.Humanlike && Rand.Chance(0.4f))
                {
                    Hediff timeDistortion = HediffMaker.MakeHediff(
                        DefDatabase<HediffDef>.GetNamedSilentFail("TemporalDisorientation") ?? HediffDefOf.PsychicHangover,
                        pawn
                    );
                    timeDistortion.Severity = 0.3f;
                    pawn.health.AddHediff(timeDistortion);
                }
            }
            
            Messages.Message(
                "TemporalDistortionDuringTimefall".Translate(),
                MessageTypeDefOf.NegativeEvent
            );
        }

        private void OnTimefallEnded(Map map, TimefallTrackingData data)
        {
            // Alert DOOMS carriers
            AlertDOOMSCarriersOfTimefall(map, false);
            
            // Create clearing effects
            CreateTimefallEndEffects(map);
            
            // Check for post-timefall benefits
            CheckForPostTimefallBenefits(map, data);
        }

        private void CreateTimefallEndEffects(Map map)
        {
            // Clearing atmospheric effects
            for (int i = 0; i < 10; i++)
            {
                IntVec3 clearLocation = CellFinder.RandomCell(map);
                
                if (Rand.Chance(0.3f))
                {
                    FleckMaker.ThrowAirPuffUp(clearLocation.ToVector3Shifted(), map);
                }
            }
        }

        private void CheckForPostTimefallBenefits(Map map, TimefallTrackingData data)
        {
            // Enhanced chiral crystal formation after timefall
            if (Rand.Chance(0.4f))
            {
                SpawnPostTimefallCrystals(map);
            }
            
            // Technology insights from temporal exposure
            if (Rand.Chance(0.2f))
            {
                GrantTimefallResearchBonus(map);
            }
        }

        private void SpawnPostTimefallCrystals(Map map)
        {
            int crystalCount = Rand.Range(2, 6);
            
            for (int i = 0; i < crystalCount; i++)
            {
                IntVec3 spawnLocation = CellFinder.RandomCell(map);
                
                if (spawnLocation.Standable(map) && !spawnLocation.Roofed(map))
                {
                    Thing crystal = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
                    crystal.stackCount = Rand.Range(1, 4);
                    
                    GenPlace.TryPlaceThing(crystal, spawnLocation, map, ThingPlaceMode.Near);
                    FleckMaker.Static(spawnLocation, map, FleckDefOf.Mote_ItemSparkle, 2f);
                }
            }
            
            Messages.Message(
                "PostTimefallCrystalsFormed".Translate(crystalCount),
                MessageTypeDefOf.PositiveEvent
            );
        }

        private void GrantTimefallResearchBonus(Map map)
        {
            float researchBonus = Rand.Range(200f, 500f);
            Find.ResearchManager.ResearchPerformed(researchBonus, null);
            
            Messages.Message(
                "TimefallResearchInsights".Translate(researchBonus.ToString("F0")),
                MessageTypeDefOf.PositiveEvent
            );
        }

        private void AssessTimefallPatterns(Map map, TimefallTrackingData data)
        {
            // Calculate timefall frequency and intensity patterns
            float recentTimefallFrequency = CalculateRecentTimefallFrequency(data);
            float beachThreatLevel = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            
            // Adjust future timefall probability based on patterns
            AdjustTimefallProbability(map, data, recentTimefallFrequency, beachThreatLevel);
            
            // Check for pattern-based events
            CheckForPatternEvents(map, data, recentTimefallFrequency);
        }

        private float CalculateRecentTimefallFrequency(TimefallTrackingData data)
        {
            int recentPeriod = 240000; // 4 days in ticks
            int currentTick = Find.TickManager.TicksGame;
            
            // Count timefall events in recent period
            int recentEvents = 0;
            if (currentTick - data.lastTimefallEndTick < recentPeriod)
            {
                recentEvents = data.timefallEventCount;
            }
            
            return (float)recentEvents / 4f; // Events per day
        }

        private void AdjustTimefallProbability(Map map, TimefallTrackingData data, float frequency, float beachThreat)
        {
            // Store adjustment factors for weather system
            data.frequencyAdjustment = CalculateFrequencyAdjustment(frequency, beachThreat);
            data.intensityAdjustment = CalculateIntensityAdjustment(frequency, beachThreat);
        }

        private float CalculateFrequencyAdjustment(float frequency, float beachThreat)
        {
            float adjustment = 1f;
            
            // Reduce frequency if too frequent recently
            if (frequency > 1.5f)
            {
                adjustment *= 0.6f;
            }
            // Increase frequency if Beach threat is high but timefall rare
            else if (frequency < 0.5f && beachThreat > 0.6f)
            {
                adjustment *= 1.4f;
            }
            
            return adjustment;
        }

        private float CalculateIntensityAdjustment(float frequency, float beachThreat)
        {
            float adjustment = 1f;
            
            // Higher intensity with higher Beach threat
            adjustment += beachThreat * 0.5f;
            
            // Lower intensity if very frequent
            if (frequency > 2f)
            {
                adjustment *= 0.8f;
            }
            
            return adjustment;
        }

        private void CheckForPatternEvents(Map map, TimefallTrackingData data, float frequency)
        {
            // Prolonged timefall absence
            if (frequency < 0.2f && data.timefallEventCount > 0)
            {
                TriggerTimefallAbsenceEvent(map);
            }
            
            // Frequent timefall pattern
            if (frequency > 2f)
            {
                TriggerFrequentTimefallEvent(map);
            }
        }

        private void TriggerTimefallAbsenceEvent(Map map)
        {
            Messages.Message(
                "TimefallAbsenceNoticed".Translate(),
                MessageTypeDefOf.CautionInput
            );
            
            // Reduce Beach threat level slightly
            // This would integrate with the Beach threat system
        }

        private void TriggerFrequentTimefallEvent(Map map)
        {
            Messages.Message(
                "FrequentTimefallPattern".Translate(),
                MessageTypeDefOf.NegativeEvent
            );
            
            // Apply stress to colonists
            foreach (Pawn colonist in map.mapPawns.FreeColonists.Take(3))
            {
                ThoughtDef stressThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("FrequentTimefallStress");
                if (stressThought != null)
                {
                    colonist.needs.mood.thoughts.memories.TryGainMemory(stressThought);
                }
            }
        }

        private IEnumerable<FiringIncident> GenerateTimefallWeatherEvents(Map map)
        {
            TimefallTrackingData data = mapTimefallData[map];
            
            // Check if timefall should occur
            if (ShouldTriggerTimefall(map, data))
            {
                yield return CreateTimefallWeatherIncident(map, data);
            }
        }

        private bool ShouldTriggerTimefall(Map map, TimefallTrackingData data)
        {
            // Base chance modified by storyteller settings
            float baseChance = Props.baseTimefallChance;
            
            // Apply frequency adjustment
            baseChance *= data.frequencyAdjustment;
            
            // Beach threat influence
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            baseChance *= (1f + beachThreat * Props.beachThreatMultiplier);
            
            // Don't trigger if timefall is already active
            if (TimefallWeatherManager.IsTimefallActive(map))
                return false;
            
            // Time since last timefall
            int timeSinceLastTimefall = Find.TickManager.TicksGame - data.lastTimefallEndTick;
            if (timeSinceLastTimefall < Props.minimumTimeBetweenTimefall)
                return false;
            
            return Rand.Chance(baseChance);
        }

        private FiringIncident CreateTimefallWeatherIncident(Map map, TimefallTrackingData data)
        {
            // Choose timefall type based on intensity
            WeatherDef timefallType = ChooseTimefallType(map, data);
            
            IncidentDef weatherIncident = IncidentDefOf_DeathStranding.Timefall ?? IncidentDefOf.WeatherChange;
            
            IncidentParms parms = new IncidentParms
            {
                target = map,
                forced = true
            };
            
            return new FiringIncident(weatherIncident, this, parms);
        }

        private WeatherDef ChooseTimefallType(Map map, TimefallTrackingData data)
        {
            float intensity = data.intensityAdjustment;
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            
            // Storm chance increases with intensity and threat
            float stormChance = (intensity + beachThreat) * 0.3f;
            
            if (Rand.Chance(stormChance))
            {
                return WeatherDefOf_DeathStranding.TimefallStorm ?? WeatherDefOf_DeathStranding.Timefall;
            }
            else
            {
                return WeatherDefOf_DeathStranding.Timefall;
            }
        }

        private IEnumerable<FiringIncident> GenerateTimefallRelatedEvents(Map map)
        {
            // Only generate events during active timefall
            if (!TimefallWeatherManager.IsTimefallActive(map))
                yield break;
            
            // Crystal formation events
            if (Rand.Chance(Props.crystalFormationChance))
            {
                yield return CreateCrystalFormationIncident(map);
            }
            
            // BT manifestation events (enhanced during timefall)
            if (Rand.Chance(Props.btManifestationChance))
            {
                yield return CreateTimefallBTIncident(map);
            }
        }

        private FiringIncident CreateCrystalFormationIncident(Map map)
        {
            IncidentDef formationIncident = IncidentDefOf_DeathStranding.ChiralCrystalFormation 
                ?? IncidentDefOf.ResourcePodCrash;
            
            IncidentParms parms = new IncidentParms
            {
                target = map,
                points = 200f
            };
            
            return new FiringIncident(formationIncident, this, parms);
        }

        private FiringIncident CreateTimefallBTIncident(Map map)
        {
            IncidentDef btIncident = IncidentDefOf_DeathStranding.BTManifestation;
            
            IncidentParms parms = new IncidentParms
            {
                target = map,
                points = 300f
            };
            
            return new FiringIncident(btIncident, this, parms);
        }

        private IEnumerable<FiringIncident> GeneratePostTimefallEvents(Map map)
        {
            TimefallTrackingData data = mapTimefallData[map];
            
            // Only trigger within short time after timefall ends
            int timeSinceEnd = Find.TickManager.TicksGame - data.lastTimefallEndTick;
            if (timeSinceEnd > 15000 || timeSinceEnd < 0) // Within 4 hours after end
                yield break;
            
            // Cleanup and recovery events
            if (Rand.Chance(Props.postTimefallRecoveryChance))
            {
                yield return CreateRecoveryIncident(map);
            }
        }

        private FiringIncident CreateRecoveryIncident(Map map)
        {
            IncidentDef recoveryIncident = IncidentDefOf_DeathStranding.PostTimefallRecovery 
                ?? IncidentDefOf.ResourcePodCrash;
            
            IncidentParms parms = new IncidentParms
            {
                target = map,
                points = 150f
            };
            
            return new FiringIncident(recoveryIncident, this, parms);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastTimefallEventTick, "lastTimefallEventTick");
            Scribe_Values.Look(ref lastTimefallAssessmentTick, "lastTimefallAssessmentTick");
            Scribe_Collections.Look(ref mapTimefallData, "mapTimefallData", LookMode.Reference, LookMode.Deep);
        }
    }

    /// <summary>
    /// Properties for timefall storyteller component
    /// </summary>
    public class StorytellerCompProperties_Timefall : StorytellerCompProperties
    {
        public float baseTimefallChance = 0.15f;
        public float beachThreatMultiplier = 1.5f;
        public int minimumTimeBetweenTimefall = 60000; // 1 day
        public float crystalFormationChance = 0.3f;
        public float btManifestationChance = 0.2f;
        public float postTimefallRecoveryChance = 0.25f;
        
        public StorytellerCompProperties_Timefall()
        {
            compClass = typeof(StorytellerComp_Timefall);
        }
    }

    /// <summary>
    /// Tracks timefall patterns and events for a specific map
    /// </summary>
    public class TimefallTrackingData : IExposable
    {
        public int timefallEventCount = 0;
        public int timefallStartTick = 0;
        public int lastTimefallEndTick = 0;
        public int totalTimefallDuration = 0;
        public bool wasTimefallLastCheck = false;
        
        // Pattern adjustments
        public float frequencyAdjustment = 1f;
        public float intensityAdjustment = 1f;
        
        // Historical data
        public List<int> timefallEventTicks = new List<int>();
        public int longestTimefallDuration = 0;
        public int shortestTimeBetweenTimefall = int.MaxValue;

        public void ExposeData()
        {
            Scribe_Values.Look(ref timefallEventCount, "timefallEventCount");
            Scribe_Values.Look(ref timefallStartTick, "timefallStartTick");
            Scribe_Values.Look(ref lastTimefallEndTick, "lastTimefallEndTick");
            Scribe_Values.Look(ref totalTimefallDuration, "totalTimefallDuration");
            Scribe_Values.Look(ref wasTimefallLastCheck, "wasTimefallLastCheck");
            
            Scribe_Values.Look(ref frequencyAdjustment, "frequencyAdjustment", 1f);
            Scribe_Values.Look(ref intensityAdjustment, "intensityAdjustment", 1f);
            
            Scribe_Collections.Look(ref timefallEventTicks, "timefallEventTicks", LookMode.Value);
            Scribe_Values.Look(ref longestTimefallDuration, "longestTimefallDuration");
            Scribe_Values.Look(ref shortestTimeBetweenTimefall, "shortestTimeBetweenTimefall", int.MaxValue);
        }
    }
}