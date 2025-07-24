using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using DeathStrandingMod.Core;
using DeathStrandingMod.Weather;

namespace DeathStrandingMod.Scenarios
{
    /// <summary>
    /// Scenario part that starts the game during a timefall event
    /// </summary>
    public class ScenPart_TimefallStart : ScenPart
    {
        private TimefallIntensity timefallIntensity = TimefallIntensity.Moderate;
        private int timefallDurationDays = 2;
        private bool includeTimefallEffects = true;
        private bool spawnChiralCrystals = true;
        private bool addTimefallKnowledge = true;
        private float beachThreatLevel = 0.3f;
        private bool guaranteeTimefallShelter = true;
        
        public override string Summary(Scenario scen)
        {
            string intensityText = GetTimefallIntensityDescription(timefallIntensity);
            return "ScenPart_TimefallStartSummary".Translate(intensityText, timefallDurationDays);
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 8f);
            
            // Title
            Rect titleRect = new Rect(scenPartRect.x, scenPartRect.y, scenPartRect.width, ScenPart.RowHeight);
            Widgets.Label(titleRect, "ScenPart_TimefallStartTitle".Translate());
            
            // Timefall intensity selection
            Rect intensityRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight, 
                scenPartRect.width * 0.7f, ScenPart.RowHeight);
            
            string intensityLabel = "ScenPart_TimefallIntensity".Translate() + ": " + GetTimefallIntensityDescription(timefallIntensity);
            
            if (Widgets.ButtonText(intensityRect, intensityLabel))
            {
                List<FloatMenuOption> intensityOptions = new List<FloatMenuOption>();
                
                foreach (TimefallIntensity intensity in Enum.GetValues(typeof(TimefallIntensity)))
                {
                    TimefallIntensity localIntensity = intensity;
                    intensityOptions.Add(new FloatMenuOption(
                        GetTimefallIntensityDescription(intensity),
                        () => timefallIntensity = localIntensity
                    ));
                }
                
                Find.WindowStack.Add(new FloatMenu(intensityOptions));
            }
            
            // Duration slider
            Rect durationRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight * 2, 
                scenPartRect.width, ScenPart.RowHeight);
            
            string durationLabel = "ScenPart_TimefallDuration".Translate() + ": " + timefallDurationDays + " " + "DaysLower".Translate();
            Widgets.Label(durationRect, durationLabel);
            
            Rect durationSliderRect = new Rect(durationRect.x + durationRect.width * 0.5f, durationRect.y, 
                durationRect.width * 0.5f, durationRect.height);
            timefallDurationDays = Mathf.RoundToInt(Widgets.HorizontalSlider(durationSliderRect, timefallDurationDays, 1f, 10f, true));
            
            // Beach threat level slider
            Rect threatRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight * 3, 
                scenPartRect.width, ScenPart.RowHeight);
            
            string threatLabel = "ScenPart_BeachThreatLevel".Translate() + ": " + (beachThreatLevel * 100f).ToString("F0") + "%";
            Widgets.Label(threatRect, threatLabel);
            
            Rect threatSliderRect = new Rect(threatRect.x + threatRect.width * 0.5f, threatRect.y, 
                threatRect.width * 0.5f, threatRect.height);
            beachThreatLevel = Widgets.HorizontalSlider(threatSliderRect, beachThreatLevel, 0f, 1f, true);
            
            // Include timefall effects checkbox
            Rect effectsRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight * 4, 
                scenPartRect.width, ScenPart.RowHeight);
            Widgets.CheckboxLabeled(effectsRect, "ScenPart_IncludeTimefallEffects".Translate(), ref includeTimefallEffects);
            
            // Spawn chiral crystals checkbox
            Rect crystalsRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight * 5, 
                scenPartRect.width, ScenPart.RowHeight);
            Widgets.CheckboxLabeled(crystalsRect, "ScenPart_SpawnChiralCrystals".Translate(), ref spawnChiralCrystals);
            
            // Add timefall knowledge checkbox
            Rect knowledgeRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight * 6, 
                scenPartRect.width, ScenPart.RowHeight);
            Widgets.CheckboxLabeled(knowledgeRect, "ScenPart_AddTimefallKnowledge".Translate(), ref addTimefallKnowledge);
            
            // Guarantee timefall shelter checkbox
            Rect shelterRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight * 7, 
                scenPartRect.width, ScenPart.RowHeight);
            Widgets.CheckboxLabeled(shelterRect, "ScenPart_GuaranteeTimefallShelter".Translate(), ref guaranteeTimefallShelter);
        }

        public override void GenerateIntoMap(Map map)
        {
            base.GenerateIntoMap(map);
            
            // Start timefall weather
            InitiateTimefallWeather(map);
            
            // Apply timefall effects to colonists if enabled
            if (includeTimefallEffects)
            {
                ApplyTimefallEffectsToColonists(map);
            }
            
            // Spawn chiral crystals if enabled
            if (spawnChiralCrystals)
            {
                SpawnTimefallCrystals(map);
            }
            
            // Add timefall knowledge to research if enabled
            if (addTimefallKnowledge)
            {
                GrantTimefallKnowledge();
            }
            
            // Ensure timefall shelter exists if enabled
            if (guaranteeTimefallShelter)
            {
                EnsureTimefallShelter(map);
            }
            
            // Set initial Beach threat level
            SetInitialBeachThreat(map);
            
            // Send scenario start notification
            SendTimefallStartNotification(map);
        }

        private void InitiateTimefallWeather(Map map)
        {
            // Create timefall weather condition
            GameConditionDef timefallCondition = DefDatabase<GameConditionDef>.GetNamedSilentFail("Timefall");
            
            if (timefallCondition != null)
            {
                int durationTicks = timefallDurationDays * GenDate.TicksPerDay;
                
                GameCondition_Timefall timefall = (GameCondition_Timefall)GameConditionMaker.MakeCondition(timefallCondition, durationTicks);
                
                // Set timefall intensity
                if (timefall != null)
                {
                    timefall.SetIntensity(GetTimefallIntensityValue(timefallIntensity));
                }
                
                map.gameConditionManager.RegisterCondition(timefall);
            }
            else
            {
                // Fallback: use toxic fallout as a substitute
                GameConditionDef fallback = GameConditionDefOf.ToxicFallout;
                int durationTicks = timefallDurationDays * GenDate.TicksPerDay;
                
                GameCondition fallbackCondition = GameConditionMaker.MakeCondition(fallback, durationTicks);
                map.gameConditionManager.RegisterCondition(fallbackCondition);
            }
        }

        private void ApplyTimefallEffectsToColonists(Map map)
        {
            foreach (Pawn colonist in map.mapPawns.FreeColonists)
            {
                // Apply timefall exposure effects based on intensity
                ApplyTimefallExposure(colonist, timefallIntensity);
                
                // Add memories of the timefall start
                AddTimefallStartMemory(colonist);
                
                // Modify skills based on timefall experience
                ModifySkillsForTimefallStart(colonist);
            }
        }

        private void ApplyTimefallExposure(Pawn pawn, TimefallIntensity intensity)
        {
            // Apply timefall exposure hediff
            Hediff exposure = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.TimefallExposure, pawn);
            
            float exposureSeverity = intensity switch
            {
                TimefallIntensity.Light => 0.1f,
                TimefallIntensity.Moderate => 0.2f,
                TimefallIntensity.Heavy => 0.4f,
                TimefallIntensity.Extreme => 0.6f,
                _ => 0.2f
            };
            
            exposure.Severity = exposureSeverity;
            pawn.health.AddHediff(exposure);
            
            // Add timefall aging effects for higher intensities
            if (intensity >= TimefallIntensity.Heavy)
            {
                Hediff aging = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.TimefallAging, pawn);
                aging.Severity = exposureSeverity * 0.5f;
                pawn.health.AddHediff(aging);
            }
        }

        private void AddTimefallStartMemory(Pawn pawn)
        {
            ThoughtDef timefallMemory = DefDatabase<ThoughtDef>.GetNamedSilentFail("TimefallStartExperience");
            if (timefallMemory != null && pawn.needs?.mood?.thoughts?.memories != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(timefallMemory);
            }
            
            // Add fear or fascination based on pawn traits
            if (pawn.story?.traits != null)
            {
                if (pawn.story.traits.HasTrait(TraitDefOf_DeathStranding.DOOMS))
                {
                    ThoughtDef doomsFascination = DefDatabase<ThoughtDef>.GetNamedSilentFail("DOOMSTimefallFascination");
                    if (doomsFascination != null)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(doomsFascination);
                    }
                }
                else if (pawn.story.traits.HasTrait(TraitDefOf.Neurotic))
                {
                    ThoughtDef timefallAnxiety = DefDatabase<ThoughtDef>.GetNamedSilentFail("TimefallAnxiety");
                    if (timefallAnxiety != null)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(timefallAnxiety);
                    }
                }
            }
        }

        private void ModifySkillsForTimefallStart(Pawn pawn)
        {
            if (pawn.skills == null) return;
            
            // Timefall survivors develop certain skills
            pawn.skills.GetSkill(SkillDefOf.Medicine).Level += 1; // Understanding of timefall effects
            pawn.skills.GetSkill(SkillDefOf.Construction).Level += 1; // Building shelter
            
            // DOOMS carriers get additional intellectual boost
            if (pawn.story?.traits?.HasTrait(TraitDefOf_DeathStranding.DOOMS) == true)
            {
                pawn.skills.GetSkill(SkillDefOf.Intellectual).Level += 2;
            }
        }

        private void SpawnTimefallCrystals(Map map)
        {
            int crystalFormations = CalculateCrystalFormations(timefallIntensity);
            
            for (int i = 0; i < crystalFormations; i++)
            {
                IntVec3 crystalLocation = FindCrystalSpawnLocation(map);
                if (crystalLocation != IntVec3.Invalid)
                {
                    SpawnCrystalFormation(map, crystalLocation, timefallIntensity);
                }
            }
        }

        private int CalculateCrystalFormations(TimefallIntensity intensity)
        {
            return intensity switch
            {
                TimefallIntensity.Light => Rand.Range(1, 3),
                TimefallIntensity.Moderate => Rand.Range(2, 4),
                TimefallIntensity.Heavy => Rand.Range(3, 6),
                TimefallIntensity.Extreme => Rand.Range(4, 8),
                _ => 2
            };
        }

        private IntVec3 FindCrystalSpawnLocation(Map map)
        {
            for (int attempts = 0; attempts < 50; attempts++)
            {
                IntVec3 candidate = CellFinder.RandomCell(map);
                
                if (candidate.Standable(map) && 
                    !candidate.Roofed(map) &&
                    !candidate.GetThingList(map).Any(t => t.def.category == ThingCategory.Item))
                {
                    return candidate;
                }
            }
            
            return IntVec3.Invalid;
        }

        private void SpawnCrystalFormation(Map map, IntVec3 location, TimefallIntensity intensity)
        {
            // Main crystal deposit
            Thing crystal = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
            
            int crystalAmount = intensity switch
            {
                TimefallIntensity.Light => Rand.Range(3, 6),
                TimefallIntensity.Moderate => Rand.Range(5, 10),
                TimefallIntensity.Heavy => Rand.Range(8, 15),
                TimefallIntensity.Extreme => Rand.Range(12, 20),
                _ => 5
            };
            
            crystal.stackCount = crystalAmount;
            GenPlace.TryPlaceThing(crystal, location, map, ThingPlaceMode.Direct);
            
            // Create formation effects
            CreateCrystalFormationEffects(location, map, intensity);
            
            // Chance for additional crystals nearby
            if (intensity >= TimefallIntensity.Heavy && Rand.Chance(0.6f))
            {
                for (int i = 0; i < 2; i++)
                {
                    IntVec3 nearbyLocation = location + IntVec3Utility.RandomHorizontalOffset(4);
                    if (nearbyLocation.InBounds(map) && nearbyLocation.Standable(map))
                    {
                        Thing nearbyCrystal = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
                        nearbyCrystal.stackCount = Rand.Range(2, 6);
                        GenPlace.TryPlaceThing(nearbyCrystal, nearbyLocation, map, ThingPlaceMode.Direct);
                    }
                }
            }
        }

        private void CreateCrystalFormationEffects(IntVec3 location, Map map, TimefallIntensity intensity)
        {
            float effectIntensity = GetTimefallIntensityValue(intensity);
            
            // Crystal formation energy burst
            FleckMaker.Static(location, map, FleckDefOf.Mote_ItemSparkle, 2f * effectIntensity);
            
            // Timefall crystallization effects
            for (int ring = 1; ring <= 3; ring++)
            {
                int points = ring * 6;
                for (int i = 0; i < points; i++)
                {
                    float angle = (float)i / points * 2f * Mathf.PI;
                    Vector3 ripplePos = location.ToVector3Shifted() + 
                        new Vector3(Mathf.Cos(angle) * ring * 2f, 0f, Mathf.Sin(angle) * ring * 2f);
                    
                    if (ripplePos.ToIntVec3().InBounds(map))
                    {
                        FleckMaker.Static(ripplePos.ToIntVec3(), map, FleckDefOf.PsycastPsychicEffect, effectIntensity);
                    }
                }
            }
        }

        private void GrantTimefallKnowledge()
        {
            // Grant research progress related to timefall and chiral technology
            float researchPoints = timefallIntensity switch
            {
                TimefallIntensity.Light => 200f,
                TimefallIntensity.Moderate => 400f,
                TimefallIntensity.Heavy => 600f,
                TimefallIntensity.Extreme => 800f,
                _ => 300f
            };
            
            Find.ResearchManager.ResearchPerformed(researchPoints, null);
            
            // Send notification about gained knowledge
            Messages.Message(
                "TimefallKnowledgeGained".Translate(researchPoints.ToString("F0")),
                MessageTypeDefOf.PositiveEvent
            );
        }

        private void EnsureTimefallShelter(Map map)
        {
            // Find or create a roofed area for timefall protection
            IntVec3 shelterLocation = FindBestShelterLocation(map);
            
            if (shelterLocation != IntVec3.Invalid)
            {
                CreateBasicTimefallShelter(map, shelterLocation);
            }
        }

        private IntVec3 FindBestShelterLocation(Map map)
        {
            // Look for existing roofed areas first
            foreach (Pawn colonist in map.mapPawns.FreeColonists)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(colonist.Position, 15, true))
                {
                    if (cell.InBounds(map) && cell.Roofed(map) && cell.Walkable(map))
                    {
                        return cell; // Found existing shelter
                    }
                }
            }
            
            // No existing shelter, find good location for new one
            IntVec3 center = map.Center;
            for (int radius = 5; radius <= 20; radius += 5)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, false))
                {
                    if (cell.InBounds(map) && 
                        cell.Standable(map) && 
                        !cell.GetThingList(map).Any(t => t.def.category == ThingCategory.Building))
                    {
                        return cell;
                    }
                }
            }
            
            return IntVec3.Invalid;
        }

        private void CreateBasicTimefallShelter(Map map, IntVec3 center)
        {
            // Create a simple 5x5 roofed shelter
            int shelterSize = 5;
            IntVec3 corner = center - new IntVec3(shelterSize / 2, 0, shelterSize / 2);
            
            // Build walls
            for (int x = 0; x < shelterSize; x++)
            {
                for (int z = 0; z < shelterSize; z++)
                {
                    IntVec3 cell = corner + new IntVec3(x, 0, z);
                    if (!cell.InBounds(map)) continue;
                    
                    // Build walls on perimeter, leave center open
                    if (x == 0 || x == shelterSize - 1 || z == 0 || z == shelterSize - 1)
                    {
                        // Create wall if on perimeter and not a door location
                        if (!(x == shelterSize / 2 && z == 0)) // Leave door opening
                        {
                            Thing wall = ThingMaker.MakeThing(ThingDefOf.Wall);
                            wall.SetFactionDirect(Faction.OfPlayer);
                            GenSpawn.Spawn(wall, cell, map);
                        }
                    }
                    
                    // Add roof to all cells
                    map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
                }
            }
            
            // Add door
            IntVec3 doorCell = corner + new IntVec3(shelterSize / 2, 0, 0);
            if (doorCell.InBounds(map))
            {
                Thing door = ThingMaker.MakeThing(ThingDefOf.Door);
                door.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(door, doorCell, map);
            }
            
            // Add basic furniture inside
            IntVec3 interiorCenter = center;
            if (interiorCenter.InBounds(map) && interiorCenter.Standable(map))
            {
                // Simple bed
                Thing bed = ThingMaker.MakeThing(ThingDefOf.SleepingSpot);
                bed.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(bed, interiorCenter + new IntVec3(1, 0, 1), map);
                
                // Storage area
                Zone_Stockpile stockpile = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
                stockpile.AddCell(interiorCenter + new IntVec3(-1, 0, -1));
                stockpile.AddCell(interiorCenter + new IntVec3(-1, 0, 0));
                map.zoneManager.RegisterZone(stockpile);
            }
        }

        private void SetInitialBeachThreat(Map map)
        {
            // Set Beach threat level for the map
            // This would integrate with the Beach threat tracking system
            DeathStrandingUtility.SetBeachThreatLevel(map, beachThreatLevel);
        }

        private void SendTimefallStartNotification(Map map)
        {
            string intensityText = GetTimefallIntensityDescription(timefallIntensity);
            string durationText = timefallDurationDays.ToString();
            
            Find.LetterStack.ReceiveLetter(
                "TimefallStartEventTitle".Translate(),
                "TimefallStartEventDesc".Translate(intensityText, durationText),
                LetterDefOf.ThreatBig,
                new TargetInfo(map.Center, map)
            );
            
            // Special notification for DOOMS carriers
            var doomsCarriers = map.mapPawns.FreeColonists
                .Where(p => p.story?.traits?.HasTrait(TraitDefOf_DeathStranding.DOOMS) == true)
                .ToList();
            
            if (doomsCarriers.Any())
            {
                Messages.Message(
                    "DOOMSCarriersDetectTimefall".Translate(doomsCarriers.Count),
                    doomsCarriers.First(),
                    MessageTypeDefOf.CautionInput
                );
            }
        }

        public override void Randomize()
        {
            base.Randomize();
            
            // Randomize timefall parameters
            timefallIntensity = (TimefallIntensity)Rand.Range(0, 4);
            timefallDurationDays = Rand.Range(1, 6);
            beachThreatLevel = Rand.Range(0.1f, 0.7f);
            
            includeTimefallEffects = Rand.Bool;
            spawnChiralCrystals = Rand.Bool;
            addTimefallKnowledge = Rand.Bool;
            guaranteeTimefallShelter = Rand.Chance(0.7f); // Usually want shelter
        }

        private string GetTimefallIntensityDescription(TimefallIntensity intensity)
        {
            return intensity switch
            {
                TimefallIntensity.Light => "TimefallIntensityLight".Translate(),
                TimefallIntensity.Moderate => "TimefallIntensityModerate".Translate(),
                TimefallIntensity.Heavy => "TimefallIntensityHeavy".Translate(),
                TimefallIntensity.Extreme => "TimefallIntensityExtreme".Translate(),
                _ => "Unknown"
            };
        }

        private float GetTimefallIntensityValue(TimefallIntensity intensity)
        {
            return intensity switch
            {
                TimefallIntensity.Light => 0.3f,
                TimefallIntensity.Moderate => 0.5f,
                TimefallIntensity.Heavy => 0.8f,
                TimefallIntensity.Extreme => 1f,
                _ => 0.5f
            };
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref timefallIntensity, "timefallIntensity", TimefallIntensity.Moderate);
            Scribe_Values.Look(ref timefallDurationDays, "timefallDurationDays", 2);
            Scribe_Values.Look(ref includeTimefallEffects, "includeTimefallEffects", true);
            Scribe_Values.Look(ref spawnChiralCrystals, "spawnChiralCrystals", true);
            Scribe_Values.Look(ref addTimefallKnowledge, "addTimefallKnowledge", true);
            Scribe_Values.Look(ref beachThreatLevel, "beachThreatLevel", 0.3f);
            Scribe_Values.Look(ref guaranteeTimefallShelter, "guaranteeTimefallShelter", true);
        }

        public override bool TryMerge(ScenPart other)
        {
            if (other is ScenPart_TimefallStart otherTimefall)
            {
                // Merge by taking the more intense settings
                if ((int)otherTimefall.timefallIntensity > (int)timefallIntensity)
                {
                    timefallIntensity = otherTimefall.timefallIntensity;
                }
                
                timefallDurationDays = Math.Max(timefallDurationDays, otherTimefall.timefallDurationDays);
                beachThreatLevel = Math.Max(beachThreatLevel, otherTimefall.beachThreatLevel);
                
                includeTimefallEffects = includeTimefallEffects || otherTimefall.includeTimefallEffects;
                spawnChiralCrystals = spawnChiralCrystals || otherTimefall.spawnChiralCrystals;
                addTimefallKnowledge = addTimefallKnowledge || otherTimefall.addTimefallKnowledge;
                guaranteeTimefallShelter = guaranteeTimefallShelter || otherTimefall.guaranteeTimefallShelter;
                
                return true;
            }
            
            return false;
        }

        public override int GetHashCode()
        {
            return Gen.HashCombine(base.GetHashCode(), timefallIntensity.GetHashCode());
        }
    }

    // ==================== SUPPORTING ENUMS ====================
    
    public enum TimefallIntensity
    {
        Light = 0,
        Moderate = 1,
        Heavy = 2,
        Extreme = 3
    }
}