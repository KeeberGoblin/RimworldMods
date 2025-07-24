using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using DeathStrandingMod.Core;
using DeathStrandingMod.Abilities;

namespace DeathStrandingMod.Abilities
{
    // ==================== GRAVITY NUDGE - LEVEL 5 ====================
    
    /// <summary>
    /// Level 5 DOOMS ability - Push objects and pawns with controlled gravitational force
    /// </summary>
    public class Verb_GravityNudge : Verb_CastBase
    {
        private AbilityProperties_DOOMS Props => ability.def.GetModExtension<AbilityProperties_DOOMS>();

        protected override bool TryCastShot()
        {
            IntVec3 targetCell = currentTarget.Cell;
            Map map = caster.Map;
            
            if (!ValidateTarget(targetCell, map))
                return false;
            
            if (!ConsumeRequiredResources())
                return false;

            // Create gravity pulse effect
            CreateGravityPulseEffects(targetCell, map);
            
            // Apply gravity force to objects and pawns in radius
            float effectiveRange = AbilityPropertyUtility.CalculateEffectiveRange(3f, Props, caster as Pawn);
            ApplyGravityEffects(targetCell, map, effectiveRange);
            
            // Apply Beach drift risk
            ApplyBeachDriftRisk();
            
            return true;
        }

        private bool ValidateTarget(IntVec3 targetCell, Map map)
        {
            if (!targetCell.InBounds(map))
            {
                Messages.Message("TargetOutOfBounds".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            
            if (Props?.requiresLineOfSight == true && 
                !GenSight.LineOfSight(caster.Position, targetCell, map))
            {
                Messages.Message("TargetNotVisible".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            
            return true;
        }

        private void CreateGravityPulseEffects(IntVec3 center, Map map)
        {
            // Primary effect at target
            FleckMaker.Static(center, map, FleckDefOf.PsycastPsychicEffect, 2f);
            
            // Expanding gravity rings
            for (int ring = 1; ring <= 3; ring++)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, ring, false))
                {
                    if (cell.InBounds(map) && Rand.Chance(0.4f))
                    {
                        FleckMaker.ThrowAirPuffUp(cell.ToVector3Shifted(), map);
                    }
                }
            }
            
            // Sound effect
            SoundDefOf.PsycastPsychicPulse.PlayOneShot(new TargetInfo(center, map));
        }

        private void ApplyGravityEffects(IntVec3 center, Map map, float radius)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map)) continue;
                
                List<Thing> things = cell.GetThingList(map);
                foreach (Thing thing in things)
                {
                    if (thing is Pawn pawn && pawn != caster)
                    {
                        ApplyGravityToPawn(pawn, center);
                    }
                    else if (thing.def.category == ThingCategory.Item && thing.stackCount <= 75)
                    {
                        LaunchItemWithGravity(thing, center);
                    }
                    else if (thing.def.category == ThingCategory.Projectile)
                    {
                        DeflectProjectile(thing, center);
                    }
                }
            }
        }

        private void ApplyGravityToPawn(Pawn pawn, IntVec3 center)
        {
            // Calculate knockback direction (away from center)
            Vector3 direction = (pawn.Position - center).ToVector3().normalized;
            IntVec3 knockbackCell = pawn.Position + direction.ToIntVec3();
            
            // Ensure valid destination
            if (knockbackCell.InBounds(pawn.Map) && knockbackCell.Walkable(pawn.Map))
            {
                pawn.Position = knockbackCell;
                pawn.Notify_Teleported();
                
                // Apply minor damage based on mass and distance
                float damage = CalculateGravityDamage(pawn, center);
                if (damage > 0)
                {
                    pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, damage));
                }
                
                // Knockdown chance
                if (Rand.Chance(0.3f))
                {
                    pawn.stances.SetStance(new Stance_Cooldown(120, pawn, null));
                }
                
                FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, 1.5f);
            }
        }

        private float CalculateGravityDamage(Pawn pawn, IntVec3 center)
        {
            float distance = pawn.Position.DistanceTo(center);
            float baseDamage = 5f + (10f / Math.Max(1f, distance));
            
            // Scale with caster's DOOMS level
            int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(caster as Pawn);
            baseDamage *= (1f + doomsLevel * 0.1f);
            
            // Reduce damage for high DOOMS targets (they have some resistance)
            int targetDOOMS = DeathStrandingUtility.GetDOOMSLevel(pawn);
            if (targetDOOMS > 0)
            {
                baseDamage *= (1f - targetDOOMS * 0.05f);
            }
            
            return Mathf.RoundToInt(Math.Max(1f, baseDamage));
        }

        private void LaunchItemWithGravity(Thing item, IntVec3 center)
        {
            Vector3 direction = (item.Position - center).ToVector3().normalized;
            IntVec3 destination = item.Position + (direction * Rand.Range(2, 6)).ToIntVec3();
            
            if (destination.InBounds(item.Map) && destination.Walkable(item.Map))
            {
                item.Position = destination;
                FleckMaker.ThrowDustPuff(destination, item.Map, 0.8f);
            }
        }

        private void DeflectProjectile(Thing projectile, IntVec3 center)
        {
            // Simple projectile deflection - more complex implementation would modify trajectory
            if (projectile is Projectile proj)
            {
                Vector3 deflectionDirection = (projectile.Position - center).ToVector3().normalized;
                // This would need more complex projectile manipulation in a full implementation
                FleckMaker.ThrowLightningGlow(projectile.Position.ToVector3Shifted(), projectile.Map, 0.5f);
            }
        }

        private bool ConsumeRequiredResources()
        {
            if (Props == null) return true;
            
            int effectiveCost = AbilityPropertyUtility.CalculateEffectiveCost(Props, caster as Pawn);
            
            if (effectiveCost > 0)
            {
                if (!DeathStrandingUtility.TryConsumeChiralCrystals(caster.Map, effectiveCost))
                {
                    Messages.Message("NotEnoughChiralCrystals".Translate(effectiveCost), 
                        caster, MessageTypeDefOf.RejectInput);
                    return false;
                }
            }
            
            return true;
        }

        private void ApplyBeachDriftRisk()
        {
            if (Props == null) return;
            
            float driftRisk = AbilityPropertyUtility.CalculateBeachDriftRisk(Props, caster as Pawn);
            
            if (Rand.Chance(driftRisk))
            {
                ApplyBeachDrift(caster as Pawn);
            }
        }

        private void ApplyBeachDrift(Pawn pawn)
        {
            if (pawn == null) return;
            
            Hediff drift = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.BeachDrift, pawn);
            drift.Severity = Props?.beachDriftSeverity ?? 0.5f;
            pawn.health.AddHediff(drift);
            
            Messages.Message("BeachDriftTriggered".Translate(pawn.LabelShort),
                pawn, MessageTypeDefOf.NegativeEvent);
        }
    }

    // ==================== MATTER SENSE - LEVEL 5 ====================
    
    /// <summary>
    /// Level 5 DOOMS ability - Perceive molecular composition and detect hidden materials
    /// </summary>
    public class Verb_MatterSense : Verb_CastBase
    {
        private AbilityProperties_DOOMS Props => ability.def.GetModExtension<AbilityProperties_DOOMS>();

        protected override bool TryCastShot()
        {
            IntVec3 targetCell = currentTarget.HasThing ? currentTarget.Thing.Position : currentTarget.Cell;
            Map map = caster.Map;
            
            if (!ConsumeRequiredResources())
                return false;

            // Perform matter analysis
            float effectiveRange = AbilityPropertyUtility.CalculateEffectiveRange(15f, Props, caster as Pawn);
            PerformMatterAnalysis(targetCell, map, effectiveRange);
            
            // Apply Beach drift risk
            ApplyBeachDriftRisk();
            
            return true;
        }

        private void PerformMatterAnalysis(IntVec3 center, Map map, float range)
        {
            List<string> discoveries = new List<string>();
            
            // Reveal hidden ore deposits
            RevealOreDeposits(center, map, range, discoveries);
            
            // Analyze existing structures
            AnalyzeStructures(center, map, range, discoveries);
            
            // Detect valuable items
            DetectValuableItems(center, map, range, discoveries);
            
            // Create visual effect
            CreateMatterSenseEffects(center, map, range);
            
            // Report findings
            ReportFindings(center, map, discoveries);
        }

        private void RevealOreDeposits(IntVec3 center, Map map, float range, List<string> discoveries)
        {
            int depositsFound = 0;
            
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, range, true))
            {
                if (!cell.InBounds(map)) continue;
                
                TerrainDef terrain = cell.GetTerrain(map);
                if (terrain.defName.Contains("Rock") && Rand.Chance(0.15f))
                {
                    // Create ore deposit
                    ThingDef oreDef = GetRandomOreType();
                    if (oreDef != null)
                    {
                        Thing ore = ThingMaker.MakeThing(oreDef);
                        GenPlace.TryPlaceThing(ore, cell, map, ThingPlaceMode.Direct);
                        
                        FleckMaker.Static(cell, map, FleckDefOf.Mote_ItemSparkle, 1f);
                        depositsFound++;
                    }
                }
            }
            
            if (depositsFound > 0)
            {
                discoveries.Add($"OreDepositsFound".Translate(depositsFound));
            }
        }

        private ThingDef GetRandomOreType()
        {
            List<ThingDef> oreTypes = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.category == ThingCategory.Building && 
                           d.building?.mineableThing != null &&
                           d.building.mineableThing.IsStuff)
                .ToList();
                
            return oreTypes.RandomElementWithFallback();
        }

        private void AnalyzeStructures(IntVec3 center, Map map, float range, List<string> discoveries)
        {
            var buildings = GenRadial.RadialDistinctThingsAround(center, map, range, true)
                .Where(t => t.def.category == ThingCategory.Building)
                .ToList();
            
            foreach (Thing building in buildings.Take(5)) // Limit to 5 for performance
            {
                string analysis = AnalyzeBuilding(building);
                if (!string.IsNullOrEmpty(analysis))
                {
                    discoveries.Add(analysis);
                }
            }
        }

        private string AnalyzeBuilding(Thing building)
        {
            float integrity = (float)building.HitPoints / building.MaxHitPoints;
            string material = building.Stuff?.LabelCap ?? "Unknown";
            
        private string AnalyzeBuilding(Thing building)
        {
            float integrity = (float)building.HitPoints / building.MaxHitPoints;
            string material = building.Stuff?.LabelCap ?? "Unknown";
            
            string condition = integrity switch
            {
                > 0.9f => "Excellent",
                > 0.7f => "Good",
                > 0.5f => "Damaged",
                > 0.3f => "Poor",
                _ => "Critical"
            };
            
            return $"BuildingAnalysis".Translate(building.LabelCap, material, condition, (integrity * 100f).ToString("F0"));
        }

        private void DetectValuableItems(IntVec3 center, Map map, float range, List<string> discoveries)
        {
            var valuableItems = GenRadial.RadialDistinctThingsAround(center, map, range, true)
                .Where(t => t.def.category == ThingCategory.Item && t.MarketValue > 10f)
                .OrderByDescending(t => t.MarketValue)
                .Take(3)
                .ToList();
            
            foreach (Thing item in valuableItems)
            {
                discoveries.Add($"ValuableItemDetected".Translate(item.LabelCap, item.MarketValue.ToString("F0")));
                
                // Highlight valuable items
                FleckMaker.Static(item.Position, map, FleckDefOf.Mote_ItemSparkle, 2f);
            }
        }

        private void CreateMatterSenseEffects(IntVec3 center, Map map, float range)
        {
            // Central analysis point
            FleckMaker.Static(center, map, FleckDefOf.PsycastPsychicEffect, 3f);
            
            // Scanning wave effect
            for (int wave = 1; wave <= 3; wave++)
            {
                float waveRadius = range * wave / 3f;
                int pointsOnWave = Mathf.RoundToInt(waveRadius * 6);
                
                for (int i = 0; i < pointsOnWave; i++)
                {
                    float angle = (float)i / pointsOnWave * 2f * Mathf.PI;
                    Vector3 wavePos = center.ToVector3Shifted() + 
                        new Vector3(Mathf.Cos(angle) * waveRadius, 0f, Mathf.Sin(angle) * waveRadius);
                    
                    if (wavePos.ToIntVec3().InBounds(map))
                    {
                        FleckMaker.ThrowLightningGlow(wavePos, map, 0.3f);
                    }
                }
            }
        }

        private void ReportFindings(IntVec3 center, Map map, List<string> discoveries)
        {
            if (discoveries.Any())
            {
                string findingsText = string.Join("\n", discoveries);
                
                Find.LetterStack.ReceiveLetter(
                    "MatterSenseResults".Translate(),
                    "MatterSenseResultsDesc".Translate((caster as Pawn)?.LabelShort, findingsText),
                    LetterDefOf.PositiveEvent,
                    new TargetInfo(center, map)
                );
            }
            else
            {
                Messages.Message("MatterSenseNoFindings".Translate(),
                    new TargetInfo(center, map), MessageTypeDefOf.CautionInput);
            }
        }

        private bool ConsumeRequiredResources()
        {
            if (Props == null) return true;
            
            int effectiveCost = AbilityPropertyUtility.CalculateEffectiveCost(Props, caster as Pawn);
            
            if (effectiveCost > 0)
            {
                if (!DeathStrandingUtility.TryConsumeChiralCrystals(caster.Map, effectiveCost))
                {
                    Messages.Message("NotEnoughChiralCrystals".Translate(effectiveCost), 
                        caster, MessageTypeDefOf.RejectInput);
                    return false;
                }
            }
            
            return true;
        }

        private void ApplyBeachDriftRisk()
        {
            if (Props == null) return;
            
            float driftRisk = AbilityPropertyUtility.CalculateBeachDriftRisk(Props, caster as Pawn);
            
            if (Rand.Chance(driftRisk))
            {
                ApplyBeachDrift(caster as Pawn);
            }
        }

        private void ApplyBeachDrift(Pawn pawn)
        {
            if (pawn == null) return;
            
            Hediff drift = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.BeachDrift, pawn);
            drift.Severity = Props?.beachDriftSeverity ?? 0.3f;
            pawn.health.AddHediff(drift);
            
            Messages.Message("BeachDriftTriggered".Translate(pawn.LabelShort),
                pawn, MessageTypeDefOf.NegativeEvent);
        }
    }

    // ==================== BEACH GLIMPSE - LEVEL 5 ====================
    
    /// <summary>
    /// Level 5 DOOMS ability - Peer into the Beach dimension for prophetic insights
    /// </summary>
    public class Verb_BeachGlimpse : Verb_CastBase
    {
        private AbilityProperties_DOOMS Props => ability.def.GetModExtension<AbilityProperties_DOOMS>();

        protected override bool TryCastShot()
        {
            Pawn pawn = caster as Pawn;
            if (pawn == null) return false;
            
            if (!ConsumeRequiredResources())
                return false;

            // Apply temporary Beach drift for the vision
            ApplyVisionDrift(pawn);
            
            // Generate prophetic insights
            GeneratePropheticVision(pawn);
            
            return true;
        }

        private void ApplyVisionDrift(Pawn pawn)
        {
            Hediff glimpse = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.BeachDrift, pawn);
            glimpse.Severity = 0.2f; // Light drift for vision
            pawn.health.AddHediff(glimpse);
            
            // Visual effect during vision
            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastPsychicEffect, 4f);
            
            for (int i = 0; i < 8; i++)
            {
                Vector3 effectPos = pawn.DrawPos + new Vector3(
                    Mathf.Cos(i * Mathf.PI / 4f) * 2f,
                    0f,
                    Mathf.Sin(i * Mathf.PI / 4f) * 2f
                );
                FleckMaker.ThrowLightningGlow(effectPos, pawn.Map, 1f);
            }
        }

        private void GeneratePropheticVision(Pawn pawn)
        {
            List<string> visions = new List<string>();
            Map map = pawn.Map;
            
            // Predict incoming threats
            if (Rand.Chance(0.5f))
            {
                PredictThreats(map, visions);
            }
            
            // Reveal hidden opportunities
            if (Rand.Chance(0.4f))
            {
                RevealOpportunities(map, visions);
            }
            
            // Predict weather changes
            if (Rand.Chance(0.6f))
            {
                PredictWeather(map, visions);
            }
            
            // Predict colonist events
            if (Rand.Chance(0.3f))
            {
                PredictColonistEvents(map, visions);
            }
            
            // Sense BT activity
            if (Rand.Chance(0.7f))
            {
                SenseBTActivity(map, visions);
            }
            
            // Display visions
            DisplayVisions(pawn, visions);
        }

        private void PredictThreats(Map map, List<string> visions)
        {
            // Check for likely raids
            if (Rand.Chance(0.4f))
            {
                visions.Add("VisionIncomingRaid".Translate());
            }
            
            // Check for mechanical breakdowns
            var breakdownCandidates = map.listerThings.AllThings
                .Where(t => t.def.category == ThingCategory.Building && 
                           t.def.building.isMachine &&
                           t.HitPoints < t.MaxHitPoints * 0.6f)
                .ToList();
            
            if (breakdownCandidates.Any())
            {
                Thing candidate = breakdownCandidates.RandomElement();
                visions.Add("VisionMechanicalFailure".Translate(candidate.LabelCap));
            }
        }

        private void RevealOpportunities(Map map, List<string> visions)
        {
            // Suggest beneficial quest locations
            if (Rand.Chance(0.5f))
            {
                SpawnQuestCache(map);
                visions.Add("VisionHiddenTreasure".Translate());
            }
            
            // Predict beneficial events
            if (Rand.Chance(0.3f))
            {
                visions.Add("VisionPositiveEvent".Translate());
            }
            
            // Reveal optimal building locations
            if (Rand.Chance(0.4f))
            {
                IntVec3 optimalLocation = FindOptimalBuildingSpot(map);
                if (optimalLocation.IsValid)
                {
                    visions.Add("VisionOptimalLocation".Translate());
                    MarkOptimalLocation(optimalLocation, map);
                }
            }
        }

        private void PredictWeather(Map map, List<string> visions)
        {
            bool timefallComing = WillTimefallOccur();
            if (timefallComing)
            {
                visions.Add("VisionTimefallApproaching".Translate());
                
                // Schedule actual timefall event
                IncidentParms timefallParms = new IncidentParms
                {
                    target = map,
                    forced = false
                };
                
                Find.Storyteller.incidentQueue.Add(
                    DefDatabase<IncidentDef>.GetNamedSilentFail("WeatherChange"),
                    Find.TickManager.TicksGame + Rand.Range(60000, 180000), // 1-3 hours
                    timefallParms
                );
            }
            else
            {
                visions.Add("VisionClearSkies".Translate());
            }
        }

        private void PredictColonistEvents(Map map, List<string> visions)
        {
            Pawn randomColonist = map.mapPawns.FreeColonists.RandomElementWithFallback();
            if (randomColonist != null)
            {
                // Predict mood changes
                if (randomColonist.needs.mood.CurLevel < 0.3f)
                {
                    visions.Add("VisionColonistMentalBreak".Translate(randomColonist.LabelShort));
                }
                else if (randomColonist.needs.mood.CurLevel > 0.8f)
                {
                    visions.Add("VisionColonistInspiration".Translate(randomColonist.LabelShort));
                }
                
                // Predict health issues
                if (randomColonist.health.HasHediffsNeedingTend())
                {
                    visions.Add("VisionHealthConcern".Translate(randomColonist.LabelShort));
                }
            }
        }

        private void SenseBTActivity(Map map, List<string> visions)
        {
            int btCount = DeathStrandingUtility.CountActiveBTs(map);
            
            if (btCount > 0)
            {
                visions.Add("VisionBTPresence".Translate(btCount));
            }
            
            // Check for decaying corpses
            var decayingCorpses = map.listerThings.ThingsOfDef(ThingDefOf.Corpse)
                .Cast<Corpse>()
                .Where(c => c.InnerPawn.RaceProps.Humanlike)
                .Count();
            
            if (decayingCorpses > 0)
            {
                visions.Add("VisionCorpseWarning".Translate(decayingCorpses));
            }
            
            // Sense potential voidout risks
            if (DeathStrandingUtility.HasCriticalTetherLevels(map))
            {
                visions.Add("VisionVoidoutRisk".Translate());
            }
        }

        private void SpawnQuestCache(Map map)
        {
            IntVec3 spawnCell = CellFinder.RandomNotEdgeCell(5, map);
            
            Thing cache = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
            cache.stackCount = Rand.Range(3, 12);
            
            GenPlace.TryPlaceThing(cache, spawnCell, map, ThingPlaceMode.Near);
            
            // Mark with special effect
            FleckMaker.Static(spawnCell, map, FleckDefOf.Mote_ItemSparkle, 5f);
        }

        private IntVec3 FindOptimalBuildingSpot(Map map)
        {
            // Simple implementation - find a good flat area
            for (int attempts = 0; attempts < 20; attempts++)
            {
                IntVec3 candidate = CellFinder.RandomNotEdgeCell(10, map);
                if (candidate.GetTerrain(map).affordances.Contains(TerrainAffordanceDefOf.Heavy) &&
                    candidate.Standable(map) &&
                    !candidate.Roofed(map))
                {
                    return candidate;
                }
            }
            return IntVec3.Invalid;
        }

        private void MarkOptimalLocation(IntVec3 location, Map map)
        {
            FleckMaker.Static(location, map, FleckDefOf.Mote_ItemSparkle, 10f);
            
            // Create a temporary marker
            Thing marker = ThingMaker.MakeThing(ThingDefOf.MinifiedThing);
            GenPlace.TryPlaceThing(marker, location, map, ThingPlaceMode.Direct);
        }

        private bool WillTimefallOccur()
        {
            // Check storyteller and current conditions
            var storyteller = Find.Storyteller?.def;
            if (storyteller?.defName == "SamPorterBridges")
            {
                return Rand.Chance(0.4f);
            }
            
            return Rand.Chance(0.2f);
        }

        private void DisplayVisions(Pawn pawn, List<string> visions)
        {
            if (visions.Any())
            {
                string visionText = string.Join("\n\n", visions);
                
                Find.LetterStack.ReceiveLetter(
                    "BeachVision".Translate(),
                    "BeachVisionDesc".Translate(pawn.LabelShort, visionText),
                    LetterDefOf.PositiveEvent,
                    pawn
                );
                
                // Sound effect
                SoundDefOf.PsycastPsychicPulse.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            }
            else
            {
                Messages.Message("BeachVisionEmpty".Translate(pawn.LabelShort),
                    pawn, MessageTypeDefOf.CautionInput);
            }
        }

        private bool ConsumeRequiredResources()
        {
            if (Props == null) return true;
            
            int effectiveCost = AbilityPropertyUtility.CalculateEffectiveCost(Props, caster as Pawn);
            
            if (effectiveCost > 0)
            {
                if (!DeathStrandingUtility.TryConsumeChiralCrystals(caster.Map, effectiveCost))
                {
                    Messages.Message("NotEnoughChiralCrystals".Translate(effectiveCost), 
                        caster, MessageTypeDefOf.RejectInput);
                    return false;
                }
            }
            
            return true;
        }
    }
}