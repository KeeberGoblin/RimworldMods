using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using DeathStrandingMod.Core;
using DeathStrandingMod.Components;

namespace DeathStrandingMod.Resources
{
    /// <summary>
    /// Main chiral crystal resource management system
    /// </summary>
    public static class ChiralCrystalManager
    {
        private static Dictionary<Map, ChiralCrystalData> mapCrystalData = new Dictionary<Map, ChiralCrystalData>();
        
        /// <summary>
        /// Initializes chiral crystal tracking for a map
        /// </summary>
        public static void InitializeMap(Map map)
        {
            if (!mapCrystalData.ContainsKey(map))
            {
                mapCrystalData[map] = new ChiralCrystalData();
            }
        }

        /// <summary>
        /// Updates chiral crystal systems for all maps
        /// </summary>
        public static void ChiralCrystalTick()
        {
            foreach (Map map in Find.Maps)
            {
                if (map.IsPlayerHome)
                {
                    InitializeMap(map);
                    UpdateMapCrystalSystems(map);
                }
            }
        }

        private static void UpdateMapCrystalSystems(Map map)
        {
            ChiralCrystalData crystalData = mapCrystalData[map];
            int currentTick = Find.TickManager.TicksGame;
            
            // Update crystal resonance networks
            if (currentTick - crystalData.lastResonanceUpdateTick > 2500) // Every ~1 hour
            {
                UpdateCrystalResonanceNetworks(map);
                crystalData.lastResonanceUpdateTick = currentTick;
            }
            
            // Check for natural crystal formation
            if (currentTick - crystalData.lastFormationCheckTick > 30000) // Every ~8 hours
            {
                CheckNaturalCrystalFormation(map);
                crystalData.lastFormationCheckTick = currentTick;
            }
            
            // Update crystal degradation
            if (currentTick - crystalData.lastDegradationCheckTick > 60000) // Every ~16 hours
            {
                CheckCrystalDegradation(map);
                crystalData.lastDegradationCheckTick = currentTick;
            }
            
            // Update crystal energy fluctuations
            if (currentTick - crystalData.lastEnergyFluctuationTick > 15000) // Every ~4 hours
            {
                UpdateCrystalEnergyFluctuations(map);
                crystalData.lastEnergyFluctuationTick = currentTick;
            }
        }

        /// <summary>
        /// Updates the chiral resonance network between crystals
        /// </summary>
        private static void UpdateCrystalResonanceNetworks(Map map)
        {
            var allCrystals = map.listerThings.ThingsOfDef(ThingDefOf_DeathStranding.ChiralCrystal);
            var crystalNetworks = FindResonanceNetworks(allCrystals, map);
            
            foreach (var network in crystalNetworks)
            {
                UpdateNetworkResonance(network, map);
            }
        }

        private static List<List<Thing>> FindResonanceNetworks(IEnumerable<Thing> crystals, Map map)
        {
            var networks = new List<List<Thing>>();
            var processedCrystals = new HashSet<Thing>();
            
            foreach (Thing crystal in crystals)
            {
                if (processedCrystals.Contains(crystal))
                    continue;
                
                var network = new List<Thing>();
                FindConnectedCrystals(crystal, network, processedCrystals, crystals, map);
                
                if (network.Count > 1)
                {
                    networks.Add(network);
                }
            }
            
            return networks;
        }

        private static void FindConnectedCrystals(Thing crystal, List<Thing> network, HashSet<Thing> processed, 
            IEnumerable<Thing> allCrystals, Map map)
        {
            if (processed.Contains(crystal))
                return;
            
            processed.Add(crystal);
            network.Add(crystal);
            
            // Find crystals within resonance range
            foreach (Thing otherCrystal in allCrystals)
            {
                if (otherCrystal == crystal || processed.Contains(otherCrystal))
                    continue;
                
                float distance = crystal.Position.DistanceTo(otherCrystal.Position);
                if (distance <= GetResonanceRange(crystal, otherCrystal))
                {
                    FindConnectedCrystals(otherCrystal, network, processed, allCrystals, map);
                }
            }
        }

        private static float GetResonanceRange(Thing crystal1, Thing crystal2)
        {
            // Base resonance range
            float baseRange = 25f;
            
            // Larger stacks have longer range
            float stackBonus = (crystal1.stackCount + crystal2.stackCount) * 0.5f;
            
            return baseRange + stackBonus;
        }

        private static void UpdateNetworkResonance(List<Thing> network, Map map)
        {
            if (network.Count < 2)
                return;
            
            // Calculate network strength
            int totalCrystals = network.Sum(c => c.stackCount);
            float networkStrength = Mathf.Log(totalCrystals) * network.Count * 0.1f;
            
            // Apply network effects
            ApplyNetworkEffects(network, networkStrength, map);
            
            // Visual resonance effects
            if (Rand.Chance(0.1f))
            {
                CreateResonanceVisualEffects(network, map);
            }
        }

        private static void ApplyNetworkEffects(List<Thing> network, float strength, Map map)
        {
            // Strengthen chiral protection in network area
            foreach (Thing crystal in network)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(crystal.Position, 15f, true))
                {
                    if (!cell.InBounds(map))
                        continue;
                    
                    // Apply temporary protection boost
                    ApplyChiralProtectionBoost(cell, map, strength);
                    
                    // Chance to repel BTs
                    Pawn bt = cell.GetFirstPawn(map);
                    if (bt?.def.defName.StartsWith("BT_") == true && Rand.Chance(strength * 0.02f))
                    {
                        RepelBTFromResonance(bt, crystal.Position);
                    }
                }
            }
        }

        private static void ApplyChiralProtectionBoost(IntVec3 cell, Map map, float strength)
        {
            // This would integrate with the chiral protection system
            // For now, just create occasional visual effects
            if (Rand.Chance(0.01f))
            {
                FleckMaker.Static(cell, map, FleckDefOf.Mote_ItemSparkle, 0.5f);
            }
        }

        private static void RepelBTFromResonance(Pawn bt, IntVec3 crystalPos)
        {
            // Push BT away from crystal
            Vector3 repelDirection = (bt.Position - crystalPos).ToVector3().normalized;
            IntVec3 newPos = bt.Position + (repelDirection * 3f).ToIntVec3();
            
            if (newPos.InBounds(bt.Map) && newPos.Walkable(bt.Map))
            {
                bt.Position = newPos;
                bt.Notify_Teleported();
                
                FleckMaker.ThrowDustPuff(newPos, bt.Map, 1f);
                
                Messages.Message(
                    "BTRepelledByChiralResonance".Translate(bt.def.LabelCap),
                    new TargetInfo(newPos, bt.Map),
                    MessageTypeDefOf.PositiveEvent
                );
            }
        }

        private static void CreateResonanceVisualEffects(List<Thing> network, Map map)
        {
            // Create connecting lines between crystals
            for (int i = 0; i < network.Count - 1; i++)
            {
                Thing crystal1 = network[i];
                Thing crystal2 = network[i + 1];
                
                FleckMaker.ConnectingLine(
                    crystal1.DrawPos,
                    crystal2.DrawPos,
                    FleckDefOf.AirPuff,
                    map
                );
            }
            
            // Pulsing effect on each crystal
            foreach (Thing crystal in network)
            {
                FleckMaker.Static(crystal.Position, map, FleckDefOf.Mote_ItemSparkle, 2f);
            }
        }

        /// <summary>
        /// Checks for natural crystal formation in suitable conditions
        /// </summary>
        private static void CheckNaturalCrystalFormation(Map map)
        {
            float beachActivity = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            float formationChance = beachActivity * 0.15f;
            
            // Increase chance during timefall
            if (DeathStrandingUtility.IsTimefallActive(map))
            {
                formationChance *= 3f;
            }
            
            // Reduce chance in heavily networked areas
            float networkCoverage = CalculateNetworkCoverage(map);
            formationChance *= (1f - networkCoverage * 0.5f);
            
            if (Rand.Chance(formationChance))
            {
                TryFormNaturalCrystal(map);
            }
        }

        private static float CalculateNetworkCoverage(Map map)
        {
            var crystals = map.listerThings.ThingsOfDef(ThingDefOf_DeathStranding.ChiralCrystal);
            var protectedCells = new HashSet<IntVec3>();
            
            foreach (Thing crystal in crystals)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(crystal.Position, 20f, true))
                {
                    if (cell.InBounds(map))
                    {
                        protectedCells.Add(cell);
                    }
                }
            }
            
            return (float)protectedCells.Count / map.Area;
        }

        private static void TryFormNaturalCrystal(Map map)
        {
            IntVec3 formationSite = FindCrystalFormationSite(map);
            
            if (formationSite == IntVec3.Invalid)
                return;
            
            // Create formation effects
            CreateCrystalFormationEffects(formationSite, map);
            
            // Spawn crystal
            Thing newCrystal = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
            newCrystal.stackCount = CalculateNaturalCrystalYield(formationSite, map);
            
            GenPlace.TryPlaceThing(newCrystal, formationSite, map, ThingPlaceMode.Direct);
            
            Messages.Message(
                "NaturalChiralCrystalFormed".Translate(newCrystal.stackCount),
                new TargetInfo(formationSite, map),
                MessageTypeDefOf.PositiveEvent
            );
        }

        private static IntVec3 FindCrystalFormationSite(Map map)
        {
            for (int attempts = 0; attempts < 50; attempts++)
            {
                IntVec3 candidate = CellFinder.RandomCell(map);
                
                if (IsValidCrystalFormationSite(candidate, map))
                {
                    return candidate;
                }
            }
            
            return IntVec3.Invalid;
        }

        private static bool IsValidCrystalFormationSite(IntVec3 cell, Map map)
        {
            // Prefer rocky terrain
            TerrainDef terrain = cell.GetTerrain(map);
            if (!terrain.defName.Contains("Rock") && !terrain.defName.Contains("Stone"))
            {
                if (!Rand.Chance(0.3f))
                    return false;
            }
            
            // Avoid areas with existing crystals
            if (GenRadial.RadialDistinctThingsAround(cell, map, 10f, true)
                .Any(t => t.def == ThingDefOf_DeathStranding.ChiralCrystal))
            {
                return false;
            }
            
            // Avoid heavily built areas
            if (GenRadial.RadialDistinctThingsAround(cell, map, 8f, true)
                .Any(t => t.def.category == ThingCategory.Building && t.def.building?.isNaturalRock != true))
            {
                return Rand.Chance(0.2f);
            }
            
            // Prefer areas with "Beach energy"
            if (GenRadial.RadialDistinctThingsAround(cell, map, 15f, true)
                .Any(t => t is Corpse || (t is Pawn p && p.def.defName.StartsWith("BT_"))))
            {
                return true;
            }
            
            return Rand.Chance(0.4f);
        }

        private static int CalculateNaturalCrystalYield(IntVec3 location, Map map)
        {
            int baseYield = Rand.Range(1, 4);
            
            // Bonus for high Beach activity
            float beachActivity = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            baseYield += Mathf.RoundToInt(beachActivity * 3f);
            
            // Bonus during timefall
            if (DeathStrandingUtility.IsTimefallActive(map))
            {
                baseYield += Rand.Range(1, 4);
            }
            
            // Bonus for isolated locations
            if (!DeathStrandingUtility.IsUnderChiralProtection(location, map))
            {
                baseYield += Rand.Range(0, 2);
            }
            
            return Math.Max(1, baseYield);
        }

        private static void CreateCrystalFormationEffects(IntVec3 position, Map map)
        {
            // Crystallization energy burst
            FleckMaker.Static(position, map, FleckDefOf.ExplosionFlash, 3f);
            
            // Geometric formation patterns
            for (int i = 0; i < 6; i++)
            {
                Vector3 patternPos = position.ToVector3Shifted() + new Vector3(
                    Mathf.Cos(i * Mathf.PI / 3f) * 2f,
                    0f,
                    Mathf.Sin(i * Mathf.PI / 3f) * 2f
                );
                FleckMaker.Static(patternPos.ToIntVec3(), map, FleckDefOf.Mote_ItemSparkle, 1.5f);
            }
            
            // Sound effect
            SoundDefOf.Building_Complete.PlayOneShot(new TargetInfo(position, map));
        }

        /// <summary>
        /// Checks for crystal degradation over time
        /// </summary>
        private static void CheckCrystalDegradation(Map map)
        {
            var crystals = map.listerThings.ThingsOfDef(ThingDefOf_DeathStranding.ChiralCrystal);
            
            foreach (Thing crystal in crystals)
            {
                CheckIndividualCrystalDegradation(crystal);
            }
        }

        private static void CheckIndividualCrystalDegradation(Thing crystal)
        {
            // Crystals degrade slower in stable conditions
            float degradationChance = 0.02f; // 2% base chance
            
            // Reduce degradation if part of a network
            var nearbyCrystals = GenRadial.RadialDistinctThingsAround(crystal.Position, crystal.Map, 25f, true)
                .Where(t => t.def == ThingDefOf_DeathStranding.ChiralCrystal && t != crystal)
                .Count();
            
            if (nearbyCrystals > 0)
            {
                degradationChance *= (1f - nearbyCrystals * 0.1f); // Stabilized by network
            }
            
            // Increase degradation during timefall
            if (DeathStrandingUtility.IsTimefallActive(crystal.Map))
            {
                degradationChance *= 0.5f; // Actually more stable during timefall
            }
            
            // Increase degradation if exposed to BT activity
            var nearbyBTs = GenRadial.RadialDistinctThingsAround(crystal.Position, crystal.Map, 15f, true)
                .OfType<Pawn>()
                .Where(p => p.def.defName.StartsWith("BT_"))
                .Count();
            
            if (nearbyBTs > 0)
            {
                degradationChance *= (1f + nearbyBTs * 0.2f);
            }
            
            if (Rand.Chance(degradationChance))
            {
                ApplyCrystalDegradation(crystal);
            }
        }

        private static void ApplyCrystalDegradation(Thing crystal)
        {
            if (crystal.stackCount <= 1)
            {
                // Final degradation - crystal disappears
                FleckMaker.ThrowSmoke(crystal.Position.ToVector3Shifted(), crystal.Map, 1.5f);
                
                Messages.Message(
                    "ChiralCrystalFullyDegraded".Translate(),
                    new TargetInfo(crystal.Position, crystal.Map),
                    MessageTypeDefOf.NegativeEvent
                );
                
                crystal.Destroy();
            }
            else
            {
                // Partial degradation
                crystal.stackCount--;
                
                FleckMaker.Static(crystal.Position, crystal.Map, FleckDefOf.Mote_ItemSparkle, 1f);
                
                if (Rand.Chance(0.3f))
                {
                    Messages.Message(
                        "ChiralCrystalDegrading".Translate(),
                        new TargetInfo(crystal.Position, crystal.Map),
                        MessageTypeDefOf.CautionInput
                    );
                }
            }
        }

        /// <summary>
        /// Updates energy fluctuations in chiral crystals
        /// </summary>
        private static void UpdateCrystalEnergyFluctuations(Map map)
        {
            var crystals = map.listerThings.ThingsOfDef(ThingDefOf_DeathStranding.ChiralCrystal);
            
            foreach (Thing crystal in crystals)
            {
                if (Rand.Chance(0.1f))
                {
                    CreateEnergyFluctuationEffects(crystal);
                }
            }
        }

        private static void CreateEnergyFluctuationEffects(Thing crystal)
        {
            // Random energy pulses
            switch (Rand.Range(0, 4))
            {
                case 0:
                    FleckMaker.Static(crystal.Position, crystal.Map, FleckDefOf.Mote_ItemSparkle, 1f);
                    break;
                case 1:
                    FleckMaker.ThrowLightningGlow(crystal.DrawPos, crystal.Map, 0.8f);
                    break;
                case 2:
                    FleckMaker.Static(crystal.Position, crystal.Map, FleckDefOf.PsycastPsychicEffect, 0.5f);
                    break;
                case 3:
                    // Harmonic resonance effect
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 harmonic = crystal.DrawPos + new Vector3(
                            Mathf.Cos(i * Mathf.PI / 2f) * 1.5f,
                            0f,
                            Mathf.Sin(i * Mathf.PI / 2f) * 1.5f
                        );
                        FleckMaker.ThrowAirPuffUp(harmonic, crystal.Map);
                    }
                    break;
            }
        }

        /// <summary>
        /// Gets total chiral crystal count for a map
        /// </summary>
        public static int GetTotalChiralCrystals(Map map)
        {
            return map.listerThings.ThingsOfDef(ThingDefOf_DeathStranding.ChiralCrystal)
                .Sum(c => c.stackCount);
        }

        /// <summary>
        /// Gets the largest crystal network on a map
        /// </summary>
        public static int GetLargestNetworkSize(Map map)
        {
            var crystals = map.listerThings.ThingsOfDef(ThingDefOf_DeathStranding.ChiralCrystal);
            var networks = FindResonanceNetworks(crystals, map);
            
            return networks.Any() ? networks.Max(n => n.Count) : 0;
        }

        /// <summary>
        /// Calculates crystal density for a map
        /// </summary>
        public static float GetCrystalDensity(Map map)
        {
            int totalCrystals = GetTotalChiralCrystals(map);
            return (float)totalCrystals / map.Area;
        }

        /// <summary>
        /// Finds the nearest crystal to a position
        /// </summary>
        public static Thing FindNearestCrystal(IntVec3 position, Map map, float maxRange = 9999f)
        {
            return GenClosest.ClosestThingReachable(
                position,
                map,
                ThingRequest.ForDef(ThingDefOf_DeathStranding.ChiralCrystal),
                PathEndMode.ClosestTouch,
                TraverseParms.For(TraverseMode.NoPassClosedDoors),
                maxRange
            );
        }

        /// <summary>
        /// Checks if crystals can support a specific chiral cost
        /// </summary>
        public static bool CanSupportChiralCost(Map map, int cost)
        {
            int availableCrystals = GetTotalChiralCrystals(map);
            return availableCrystals >= cost;
        }

        /// <summary>
        /// Attempts to consume chiral crystals for a cost
        /// </summary>
        public static bool TryConsumeChiralCrystals(Map map, int cost)
        {
            if (!CanSupportChiralCost(map, cost))
                return false;
            
            var crystals = map.listerThings.ThingsOfDef(ThingDefOf_DeathStranding.ChiralCrystal)
                .OrderBy(c => c.Position.DistanceToSquared(map.Center))
                .ToList();
            
            int remaining = cost;
            foreach (Thing crystal in crystals)
            {
                if (remaining <= 0)
                    break;
                
                int consumed = Math.Min(remaining, crystal.stackCount);
                crystal.stackCount -= consumed;
                remaining -= consumed;
                
                // Visual consumption effect
                FleckMaker.Static(crystal.Position, map, FleckDefOf.Mote_ItemSparkle, 1f);
                
                if (crystal.stackCount <= 0)
                {
                    crystal.Destroy();
                }
            }
            
            return remaining <= 0;
        }
    }

    /// <summary>
    /// Data class for tracking chiral crystal systems per map
    /// </summary>
    public class ChiralCrystalData : IExposable
    {
        public int lastResonanceUpdateTick = 0;
        public int lastFormationCheckTick = 0;
        public int lastDegradationCheckTick = 0;
        public int lastEnergyFluctuationTick = 0;
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref lastResonanceUpdateTick, "lastResonanceUpdateTick");
            Scribe_Values.Look(ref lastFormationCheckTick, "lastFormationCheckTick");
            Scribe_Values.Look(ref lastDegradationCheckTick, "lastDegradationCheckTick");
            Scribe_Values.Look(ref lastEnergyFluctuationTick, "lastEnergyFluctuationTick");
        }
    }

    /// <summary>
    /// Special chiral crystal variants with enhanced properties
    /// </summary>
    public static class SpecialChiralCrystals
    {
        /// <summary>
        /// Creates a resonance-enhanced crystal from multiple smaller ones
        /// </summary>
        public static Thing CreateResonanceEnhancedCrystal(List<Thing> sourceCrystals, Map map)
        {
            int totalCrystals = sourceCrystals.Sum(c => c.stackCount);
            IntVec3 position = sourceCrystals.First().Position;
            
            // Destroy source crystals with dramatic effects
            foreach (Thing crystal in sourceCrystals)
            {
                FleckMaker.Static(crystal.Position, map, FleckDefOf.ExplosionFlash, 2f);
                crystal.Destroy();
            }
            
            // Create enhanced crystal
            Thing enhancedCrystal = ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamedSilentFail("ChiralCrystalEnhanced") ?? 
                ThingDefOf_DeathStranding.ChiralCrystal
            );
            
            enhancedCrystal.stackCount = Mathf.RoundToInt(totalCrystals * 0.8f); // Some loss in fusion
            
            GenPlace.TryPlaceThing(enhancedCrystal, position, map, ThingPlaceMode.Direct);
            
            // Dramatic fusion effects
            CreateCrystalFusionEffects(position, map);
            
            return enhancedCrystal;
        }

        private static void CreateCrystalFusionEffects(IntVec3 position, Map map)
        {
            // Central fusion blast
            FleckMaker.Static(position, map, FleckDefOf.ExplosionFlash, 5f);
            
            // Expanding energy rings
            for (int ring = 1; ring <= 4; ring++)
            {
                int pointsOnRing = ring * 6;
                for (int point = 0; point < pointsOnRing; point++)
                {
                    float angle = (float)point / pointsOnRing * 2f * Mathf.PI;
                    Vector3 ringPos = position.ToVector3Shifted() + 
                        new Vector3(Mathf.Cos(angle) * ring * 2f, 0f, Mathf.Sin(angle) * ring * 2f);
                    
                    if (ringPos.ToIntVec3().InBounds(map))
                    {
                        FleckMaker.ThrowLightningGlow(ringPos, map, 2f);
                    }
                }
            }
            
            // Sound effect
            SoundDefOf.Building_Complete.PlayOneShot(new TargetInfo(position, map));
        }

        /// <summary>
        /// Creates a time-charged crystal during timefall
        /// </summary>
        public static Thing CreateTimefallChargedCrystal(IntVec3 position, Map map)
        {
            Thing chargedCrystal = ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamedSilentFail("ChiralCrystalTimefall") ?? 
                ThingDefOf_DeathStranding.ChiralCrystal
            );
            
            chargedCrystal.stackCount = Rand.Range(3, 8);
            
            GenPlace.TryPlaceThing(chargedCrystal, position, map, ThingPlaceMode.Direct);
            
            // Timefall charging effects
            CreateTimefallChargingEffects(position, map);
            
            return chargedCrystal;
        }

        private static void CreateTimefallChargingEffects(IntVec3 position, Map map)
        {
            // Temporal distortion effects
            FleckMaker.Static(position, map, FleckDefOf.PsycastPsychicEffect, 3f);
            
            // Time ripples
            for (int i = 0; i < 8; i++)
            {
                Vector3 ripplePos = position.ToVector3Shifted() + new Vector3(
                    Mathf.Cos(i * Mathf.PI / 4f) * 3f,
                    0f,
                    Mathf.Sin(i * Mathf.PI / 4f) * 3f
                );
                FleckMaker.Static(ripplePos.ToIntVec3(), map, FleckDefOf.PsycastPsychicEffect, 1f);
            }
        }
    }
}