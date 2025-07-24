using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using DeathStrandingMod.Core;

namespace DeathStrandingMod.Weather
{
    /// <summary>
    /// Custom weather worker for timefall that handles transition logic and effects
    /// </summary>
    public class WeatherWorker_Timefall : WeatherWorker
    {
        public override bool CanDoNow(Map map, StringBuilder reasons = null)
        {
            // Basic environmental checks
            if (!base.CanDoNow(map, reasons))
                return false;
            
            // Don't start timefall too frequently
            if (HasRecentTimefall(map))
            {
                reasons?.AppendLine("RecentTimefallPreventingNew".Translate());
                return false;
            }
            
            // Check for environmental prerequisites
            if (!HasValidConditionsForTimefall(map))
            {
                reasons?.AppendLine("InvalidConditionsForTimefall".Translate());
                return false;
            }
            
            return true;
        }

        private bool HasRecentTimefall(Map map)
        {
            // Check if timefall occurred recently (within last 2 days)
            int recentPeriod = 120000; // 2 days in ticks
            int currentTick = Find.TickManager.TicksGame;
            
            // Simple check - look at recent weather changes
            return (currentTick - map.weatherManager.lastWeatherAge) < recentPeriod;
        }

        private bool HasValidConditionsForTimefall(Map map)
        {
            // Timefall is more likely with high "Beach activity"
            float beachThreatLevel = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            
            // Minimum threat level required
            if (beachThreatLevel < 0.1f)
                return Rand.Chance(0.2f); // Low chance without Beach activity
            
            // Higher threat = higher chance
            return Rand.Chance(0.5f + beachThreatLevel * 0.3f);
        }

        public override float ChanceOfEventPer10Days(Map map, int tile)
        {
            float baseChance = 2.5f; // Base 25% chance per 10 days
            
            // Increase chance based on storyteller
            if (Find.Storyteller?.def?.defName == "SamPorterBridges")
            {
                baseChance *= 2.5f; // Much more frequent with Death Stranding storyteller
            }
            
            // Increase chance based on Beach threat level
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            baseChance *= (1f + beachThreat);
            
            // Seasonal variations
            Season season = GenLocalDate.Season(map);
            baseChance *= GetSeasonalModifier(season);
            
            // Reduce chance if colony has strong chiral network coverage
            float connectionLevel = DeathStrandingUtility.GetColonyConnectionLevel(map);
            baseChance *= (1f - connectionLevel * 0.3f); // Max 30% reduction
            
            return Math.Max(0.1f, baseChance);
        }

        private float GetSeasonalModifier(Season season)
        {
            return season switch
            {
                Season.Spring => 1.2f, // More frequent in spring
                Season.Summer => 0.8f, // Less frequent in summer
                Season.Fall => 1.4f,   // Most frequent in fall
                Season.Winter => 1.0f, // Normal in winter
                _ => 1.0f
            };
        }

        public override void DrawWeather(Map map, LayerSubMesh subMesh)
        {
            // Custom rendering for timefall
            DrawTimefallEffect(map, subMesh);
        }

        private void DrawTimefallEffect(Map map, LayerSubMesh subMesh)
        {
            // Create a darker, more ominous rain effect
            Material timefallMaterial = GetTimefallMaterial();
            
            // Use base rain drawing but with modified appearance
            base.DrawWeather(map, subMesh);
            
            // Add additional timefall-specific effects
            if (Find.TickManager.TicksGame % 30 == 0) // Every half second
            {
                CreateTimefallVisualDisturbances(map);
            }
        }

        private Material GetTimefallMaterial()
        {
            // Create or cache timefall material
            return MaterialPool.MatFrom("Weather/SnowOverlay", ShaderDatabase.Transparent, 
                new Color(0.4f, 0.4f, 0.5f, 0.6f));
        }

        private void CreateTimefallVisualDisturbances(Map map)
        {
            // Occasional atmospheric disturbances during timefall
            if (Rand.Chance(0.1f))
            {
                IntVec3 disturbanceLocation = CellFinder.RandomCell(map);
                
                // Don't create effects in protected areas
                if (!DeathStrandingUtility.IsUnderChiralProtection(disturbanceLocation, map))
                {
                    FleckMaker.ThrowSmoke(disturbanceLocation.ToVector3Shifted(), map, 0.8f);
                }
            }
        }
    }

    /// <summary>
    /// Weather worker for intense timefall storms
    /// </summary>
    public class WeatherWorker_TimefallStorm : WeatherWorker_Timefall
    {
        public override bool CanDoNow(Map map, StringBuilder reasons = null)
        {
            if (!base.CanDoNow(map, reasons))
                return false;
            
            // Storms require higher Beach threat levels
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            if (beachThreat < 0.5f)
            {
                reasons?.AppendLine("InsufficientBeachActivityForStorm".Translate());
                return false;
            }
            
            // Storms shouldn't occur too frequently
            if (HasRecentTimefallStorm(map))
            {
                reasons?.AppendLine("RecentTimefallStormPreventingNew".Translate());
                return false;
            }
            
            return true;
        }

        private bool HasRecentTimefallStorm(Map map)
        {
            // Check for recent storms (within last 5 days)
            int recentPeriod = 300000; // 5 days in ticks
            int currentTick = Find.TickManager.TicksGame;
            
            // More restrictive than regular timefall
            return (currentTick - map.weatherManager.lastWeatherAge) < recentPeriod;
        }

        public override float ChanceOfEventPer10Days(Map map, int tile)
        {
            float baseChance = 0.8f; // Much rarer than regular timefall
            
            // Require significant Beach activity
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            if (beachThreat < 0.3f)
                return 0f;
            
            baseChance *= beachThreat * 2f; // Scale heavily with threat
            
            // Storyteller modifier
            if (Find.Storyteller?.def?.defName == "SamPorterBridges")
            {
                baseChance *= 1.5f;
            }
            
            return baseChance;
        }

        public override void DrawWeather(Map map, LayerSubMesh subMesh)
        {
            base.DrawWeather(map, subMesh);
            
            // Enhanced storm effects
            CreateStormVisualEffects(map);
        }

        private void CreateStormVisualEffects(Map map)
        {
            // More intense visual effects for storms
            if (Find.TickManager.TicksGame % 15 == 0) // Every quarter second
            {
                for (int i = 0; i < 2; i++)
                {
                    IntVec3 effectLocation = CellFinder.RandomCell(map);
                    
                    if (!DeathStrandingUtility.IsUnderChiralProtection(effectLocation, map))
                    {
                        // More dramatic effects
                        switch (Rand.Range(0, 4))
                        {
                            case 0:
                                FleckMaker.ThrowLightningGlow(effectLocation.ToVector3Shifted(), map, 1.5f);
                                break;
                            case 1:
                                FleckMaker.ThrowSmoke(effectLocation.ToVector3Shifted(), map, 2f);
                                break;
                            case 2:
                                FleckMaker.Static(effectLocation, map, FleckDefOf.PsycastPsychicEffect, 1f);
                                break;
                            case 3:
                                FleckMaker.ThrowAirPuffUp(effectLocation.ToVector3Shifted(), map);
                                break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Manages weather transitions and special events related to timefall
    /// </summary>
    public static class TimefallWeatherManager
    {
        /// <summary>
        /// Attempts to force timefall weather on a map
        /// </summary>
        public static bool TryForceTimefall(Map map, int durationTicks = -1)
        {
            WeatherDef timefallDef = WeatherDefOf_DeathStranding.Timefall;
            if (timefallDef == null)
            {
                Log.Error("DeathStranding: Timefall weather def not found!");
                return false;
            }
            
            // Check if weather change is possible
            if (!timefallDef.Worker.CanDoNow(map))
            {
                return false;
            }
            
            // Apply timefall
            map.weatherManager.TransitionTo(timefallDef);
            
            // Custom duration would require additional implementation
            if (durationTicks > 0)
            {
                // Note: Setting custom duration might require Harmony patches
                Log.Message($"DeathStranding: Timefall forced for {durationTicks} ticks");
            }
            
            return true;
        }

        /// <summary>
        /// Predicts when the next timefall is likely to occur
        /// </summary>
        public static int PredictNextTimefallTicks(Map map)
        {
            if (map?.weatherManager == null)
                return -1;
            
            WeatherDef timefallDef = WeatherDefOf_DeathStranding.Timefall;
            if (timefallDef?.Worker == null)
                return -1;
            
            // Calculate based on weather worker's chance algorithm
            float chancePerDay = timefallDef.Worker.ChanceOfEventPer10Days(map, map.Tile) / 10f;
            
            if (chancePerDay <= 0f)
                return -1;
            
            // Estimate average ticks until event
            float averageDaysUntilEvent = 1f / chancePerDay;
            int averageTicksUntilEvent = Mathf.RoundToInt(averageDaysUntilEvent * 60000f); // 60k ticks per day
            
            // Add randomization
            return Mathf.RoundToInt(averageTicksUntilEvent * Rand.Range(0.5f, 1.5f));
        }

        /// <summary>
        /// Checks if conditions are building toward timefall
        /// </summary>
        public static float GetTimefallProbability(Map map)
        {
            WeatherDef timefallDef = WeatherDefOf_DeathStranding.Timefall;
            if (timefallDef?.Worker == null)
                return 0f;
            
            // Check if timefall can occur now
            if (!timefallDef.Worker.CanDoNow(map))
                return 0f;
            
            // Base probability from weather system
            float baseProbability = timefallDef.Worker.ChanceOfEventPer10Days(map, map.Tile) / 10f;
            
            // Modify based on current conditions
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            baseProbability *= (1f + beachThreat);
            
            // Recent weather patterns
            if (HasTimefallBuildupWeather(map))
            {
                baseProbability *= 1.3f;
            }
            
            return Math.Min(1f, baseProbability);
        }

        /// <summary>
        /// Checks if recent weather patterns suggest timefall buildup
        /// </summary>
        private static bool HasTimefallBuildupWeather(Map map)
        {
            // Simple implementation - check current weather for buildup indicators
            WeatherDef currentWeather = map.weatherManager.curWeather;
            string weatherName = currentWeather?.defName?.ToLower() ?? "";
            
            // Buildup indicated by overcast, foggy, or rainy conditions
            return weatherName.Contains("fog") || 
                   weatherName.Contains("overcast") || 
                   weatherName.Contains("cloudy") ||
                   weatherName.Contains("rain");
        }

        /// <summary>
        /// Creates atmospheric warnings before timefall begins
        /// </summary>
        public static void CreateTimefallWarnings(Map map, int warningPeriodTicks)
        {
            // Alert DOOMS carriers first
            AlertDOOMSCarriersOfApproachingTimefall(map);
            
            // Create atmospheric disturbances
            CreateAtmosphericWarnings(map);
            
            // Send general warning to players
            Messages.Message(
                "TimefallApproachingWarning".Translate(),
                MessageTypeDefOf.CautionInput
            );
            
            // Schedule final warning
            Find.TickManager.later.ScheduleCallback(() => {
                Messages.Message(
                    "TimefallImminent".Translate(),
                    MessageTypeDefOf.ThreatBig
                );
            }, warningPeriodTicks - 5000); // 5 seconds before timefall
        }

        private static void AlertDOOMSCarriersOfApproachingTimefall(Map map)
        {
            foreach (Pawn carrier in DeathStrandingUtility.GetDOOMSCarriers(map))
            {
                int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(carrier);
                
                // Higher DOOMS levels get earlier and more detailed warnings
                if (doomsLevel >= 3)
                {
                    Messages.Message(
                        "DOOMSCarrierSensesApproachingTimefall".Translate(
                            carrier.LabelShort, 
                            GetTimefallIntensityPrediction(map)
                        ),
                        carrier,
                        MessageTypeDefOf.CautionInput
                    );
                }
            }
        }

        private static string GetTimefallIntensityPrediction(Map map)
        {
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            
            if (beachThreat > 0.7f)
                return "Severe";
            else if (beachThreat > 0.4f)
                return "Moderate";
            else
                return "Light";
        }

        private static void CreateAtmosphericWarnings(Map map)
        {
            // Create warning effects across the map
            for (int i = 0; i < 10; i++)
            {
                IntVec3 effectLocation = CellFinder.RandomCell(map);
                
                // Various atmospheric disturbances
                switch (Rand.Range(0, 4))
                {
                    case 0:
                        FleckMaker.ThrowSmoke(effectLocation.ToVector3Shifted(), map, 2f);
                        break;
                    case 1:
                        FleckMaker.Static(effectLocation, map, FleckDefOf.PsycastPsychicEffect, 1f);
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

        /// <summary>
        /// Handles the end of timefall events
        /// </summary>
        public static void OnTimefallEnded(Map map)
        {
            // Clear atmospheric effects
            ClearTimefallEffects(map);
            
            // Send completion message
            Messages.Message(
                "TimefallEndedMessage".Translate(),
                MessageTypeDefOf.PositiveEvent
            );
            
            // Update colony connection level based on how well they handled timefall
            UpdateColonyConnectionAfterTimefall(map);
        }

        private static void ClearTimefallEffects(Map map)
        {
            // Remove any lingering timefall-specific effects
            // Clear atmospheric disturbances with dispersal effects
            for (int i = 0; i < 20; i++)
            {
                IntVec3 clearLocation = CellFinder.RandomCell(map);
                if (Rand.Chance(0.1f))
                {
                    FleckMaker.ThrowAirPuffUp(clearLocation.ToVector3Shifted(), map);
                }
            }
        }

        private static void UpdateColonyConnectionAfterTimefall(Map map)
        {
            // Assess how well the colony handled the timefall
            float protectionCoverage = DeathStrandingUtility.GetColonyConnectionLevel(map);
            int survivingColonists = map.mapPawns.FreeColonists.Count(p => !p.Dead && !p.Downed);
            
            if (protectionCoverage > 0.7f && survivingColonists > 0)
            {
                // Reward for good preparation
                Messages.Message(
                    "ColonyHandledTimefallWell".Translate(),
                    MessageTypeDefOf.PositiveEvent
                );
                
                // Possible small reward
                RewardGoodTimefallPreparation(map);
            }
            else if (survivingColonists == 0)
            {
                // Colony was devastated
                Messages.Message(
                    "ColonyDevastatedByTimefall".Translate(),
                    MessageTypeDefOf.ThreatBig
                );
            }
        }

        private static void RewardGoodTimefallPreparation(Map map)
        {
            // Small bonus chiral crystals
            Thing bonus = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
            bonus.stackCount = Rand.Range(2, 8);
            
            IntVec3 dropLocation = DropCellFinder.RandomDropSpot(map);
            GenPlace.TryPlaceThing(bonus, dropLocation, map, ThingPlaceMode.Near);
            
            FleckMaker.Static(dropLocation, map, FleckDefOf.Mote_ItemSparkle, 3f);
            
            Messages.Message(
                "TimefallPreparationReward".Translate(bonus.stackCount),
                new TargetInfo(dropLocation, map),
                MessageTypeDefOf.PositiveEvent
            );
        }

        /// <summary>
        /// Checks if timefall is currently active on a map
        /// </summary>
        public static bool IsTimefallActive(Map map)
        {
            return DeathStrandingUtility.IsTimefallActive(map);
        }

        /// <summary>
        /// Gets the current timefall intensity (0-1 scale)
        /// </summary>
        public static float GetCurrentTimefallIntensity(Map map)
        {
            if (!IsTimefallActive(map))
                return 0f;
            
            string weatherName = map.weatherManager.curWeather.defName;
            if (weatherName == "TimefallStorm")
                return 1f;
            else if (weatherName == "Timefall")
                return 0.7f;
            
            return 0f;
        }

        /// <summary>
        /// Estimates remaining timefall duration in ticks
        /// </summary>
        public static int GetRemainingTimefallDuration(Map map)
        {
            if (!IsTimefallActive(map))
                return 0;
            
            // Estimate based on typical weather duration
            int currentAge = map.weatherManager.curWeatherAge;
            int typicalDuration = 60000; // Approximate 1 day
            
            return Math.Max(0, typicalDuration - currentAge);
        }
    }

    /// <summary>
    /// Specialized weather event for timefall storms with enhanced effects
    /// </summary>
    public class WeatherEvent_TimefallStorm : WeatherEvent_Timefall
    {
        private int lastVoidoutCheckTick = 0;
        private const int VOIDOUT_CHECK_INTERVAL = 10000; // ~2.5 hours

        public override void WeatherEventTick()
        {
            base.WeatherEventTick();
            
            // Enhanced storm effects
            if (Find.TickManager.TicksGame - lastVoidoutCheckTick > VOIDOUT_CHECK_INTERVAL)
            {
                CheckForStormVoidouts();
                lastVoidoutCheckTick = Find.TickManager.TicksGame;
            }
            
            // More frequent and intense effects
            CreateStormAtmosphericEffects();
        }

        private void CheckForStormVoidouts()
        {
            // Storms can trigger spontaneous voidouts
            if (Rand.Chance(0.05f)) // 5% chance per check
            {
                TryTriggerStormVoidout();
            }
        }

        private void TryTriggerStormVoidout()
        {
            // Find vulnerable areas (high BT activity, critical tethers)
            var vulnerableAreas = FindVulnerableAreas();
            
            if (vulnerableAreas.Any())
            {
                IntVec3 voidoutLocation = vulnerableAreas.RandomElement();
                TriggerStormVoidout(voidoutLocation);
            }
        }

        private List<IntVec3> FindVulnerableAreas()
        {
            var vulnerableAreas = new List<IntVec3>();
            
            // Areas with critical BT tethers
            foreach (Pawn colonist in map.mapPawns.FreeColonists)
            {
                Hediff tether = colonist.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
                if (tether?.Severity >= 0.8f)
                {
                    vulnerableAreas.Add(colonist.Position);
                }
            }
            
            // Areas with multiple BTs
            var btPositions = map.mapPawns.AllPawnsSpawned
                .Where(p => p.def.defName.StartsWith("BT_"))
                .Select(bt => bt.Position)
                .ToList();
            
            // Find clusters of BTs
            foreach (IntVec3 btPos in btPositions)
            {
                int nearbyBTs = btPositions.Count(pos => pos.DistanceTo(btPos) <= 10f);
                if (nearbyBTs >= 2)
                {
                    vulnerableAreas.Add(btPos);
                }
            }
            
            return vulnerableAreas;
        }

        private void TriggerStormVoidout(IntVec3 location)
        {
            Messages.Message(
                "TimefallStormTriggersVoidout".Translate(),
                new TargetInfo(location, map),
                MessageTypeDefOf.ThreatBig
            );
            
            // Trigger a smaller voidout than normal
            IncidentParms voidoutParms = new IncidentParms
            {
                target = map,
                spawnCenter = location,
                points = 500f // Moderate voidout
            };
            
            var voidoutIncident = IncidentDefOf_DeathStranding.Voidout;
            if (voidoutIncident?.Worker != null)
            {
                voidoutIncident.Worker.TryExecute(voidoutParms);
            }
        }

        private void CreateStormAtmosphericEffects()
        {
            // More frequent and intense atmospheric effects than regular timefall
            if (Find.TickManager.TicksGame % 150 == 0) // Every 2.5 seconds
            {
                CreateIntenseAtmosphericEffects();
            }
        }

        private void CreateIntenseAtmosphericEffects()
        {
            // Multiple simultaneous effects
            for (int i = 0; i < 3; i++)
            {
                IntVec3 effectLocation = CellFinder.RandomCell(map);
                
                // More dramatic effects than regular timefall
                switch (Rand.Range(0, 6))
                {
                    case 0:
                        FleckMaker.Static(effectLocation, map, FleckDefOf.ExplosionFlash, 2f);
                        break;
                    case 1:
                        FleckMaker.ThrowLightningGlow(effectLocation.ToVector3Shifted(), map, 2f);
                        break;
                    case 2:
                        FleckMaker.ThrowSmoke(effectLocation.ToVector3Shifted(), map, 3f);
                        break;
                    case 3:
                        FleckMaker.Static(effectLocation, map, FleckDefOf.PsycastPsychicEffect, 2f);
                        break;
                    case 4:
                        // Lightning-like effect
                        for (int j = 0; j < 5; j++)
                        {
                            Vector3 lightningPos = effectLocation.ToVector3Shifted() + 
                                new Vector3(Rand.Range(-3f, 3f), 0f, Rand.Range(-3f, 3f));
                            FleckMaker.ThrowLightningGlow(lightningPos, map, 1f);
                        }
                        break;
                    case 5:
                        // Temporal distortion effect
                        FleckMaker.Static(effectLocation, map, FleckDefOf.PsycastPsychicEffect, 3f);
                        break;
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastVoidoutCheckTick, "lastVoidoutCheckTick");
        }
    }
}