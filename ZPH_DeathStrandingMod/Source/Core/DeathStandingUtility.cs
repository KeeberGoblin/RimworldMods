using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using DeathStrandingMod.Core;
using DeathStrandingMod.Components;

namespace DeathStrandingMod.Core
{
    /// <summary>
    /// Utility class containing helper methods for Death Stranding mod functionality
    /// </summary>
    public static class DeathStrandingUtility
    {
        // ==================== COLONY ANALYSIS ====================
        
        /// <summary>
        /// Determines if a colony qualifies as a "Porter colony" with Death Stranding infrastructure
        /// </summary>
        public static bool IsPorterColony(Map map)
        {
            if (map?.mapPawns?.FreeColonists == null) return false;
            
            // Check for DOOMS carriers or chiral network infrastructure
            return map.mapPawns.FreeColonists.Any(HasDOOMSGene) ||
                   map.listerThings.ThingsOfDef(ThingDefOf_DeathStranding.ChiralNetworkNode).Any();
        }

        /// <summary>
        /// Calculates the overall "connection level" of a colony based on chiral network coverage
        /// </summary>
        public static float GetColonyConnectionLevel(Map map)
        {
            if (map == null) return 0f;
            
            var networkNodes = map.listerThings.ThingsOfDef(ThingDefOf_DeathStranding.ChiralNetworkNode);
            if (!networkNodes.Any()) return 0f;

            float totalCoverage = 0f;
            int activeNodes = 0;

            foreach (Thing node in networkNodes)
            {
                CompChiralProtection protection = node.GetComp<CompChiralProtection>();
                if (protection?.IsActive == true)
                {
                    totalCoverage += Mathf.PI * protection.ProtectionRadius * protection.ProtectionRadius;
                    activeNodes++;
                }
            }

            float mapArea = map.Area;
            float coveragePercentage = Math.Min(1f, totalCoverage / mapArea);
            
            // Bonus for multiple nodes (redundancy)
            float redundancyBonus = Math.Min(0.3f, activeNodes * 0.1f);
            
            return Math.Min(1f, coveragePercentage + redundancyBonus);
        }

        // ==================== DOOMS GENE DETECTION ====================
        
        /// <summary>
        /// Checks if a pawn has any DOOMS gene
        /// </summary>
        public static bool HasDOOMSGene(Pawn pawn)
        {
            if (pawn?.genes == null) return false;
            
            return pawn.genes.GenesListForReading
                .Any(g => g.def.defName.StartsWith("DOOMS_"));
        }

        /// <summary>
        /// Gets the DOOMS level of a pawn (0 if no DOOMS gene)
        /// </summary>
        public static int GetDOOMSLevel(Pawn pawn)
        {
            if (pawn?.genes == null) return 0;
            
            var doomsGene = pawn.genes.GenesListForReading
                .FirstOrDefault(g => g.def.defName.StartsWith("DOOMS_"));
                
            if (doomsGene?.def.GetModExtension<DOOMSProperties>() is DOOMSProperties props)
            {
                return props.level;
            }
            
            return 0;
        }

        /// <summary>
        /// Gets the highest DOOMS level among all colony members
        /// </summary>
        public static int GetHighestColonyDOOMSLevel(Map map)
        {
            if (map?.mapPawns?.FreeColonists == null) return 0;
            
            return map.mapPawns.FreeColonists
                .Select(GetDOOMSLevel)
                .DefaultIfEmpty(0)
                .Max();
        }

        /// <summary>
        /// Gets all DOOMS carriers in a colony
        /// </summary>
        public static IEnumerable<Pawn> GetDOOMSCarriers(Map map)
        {
            if (map?.mapPawns?.FreeColonists == null) 
                return Enumerable.Empty<Pawn>();
            
            return map.mapPawns.FreeColonists.Where(HasDOOMSGene);
        }

        /// <summary>
        /// Gets DOOMS properties for a pawn, or null if no DOOMS gene
        /// </summary>
        public static DOOMSProperties GetDOOMSProperties(Pawn pawn)
        {
            if (pawn?.genes == null) return null;
            
            var doomsGene = pawn.genes.GenesListForReading
                .FirstOrDefault(g => g.def.defName.StartsWith("DOOMS_"));
                
            return doomsGene?.def.GetModExtension<DOOMSProperties>();
        }

        // ==================== CHIRAL PROTECTION ====================
        
        /// <summary>
        /// Checks if a position is under chiral protection
        /// </summary>
        public static bool IsUnderChiralProtection(IntVec3 position, Map map)
        {
            if (map == null) return false;
            
            foreach (Thing node in map.listerThings.ThingsOfDef(ThingDefOf_DeathStranding.ChiralNetworkNode))
            {
                CompChiralProtection protection = node.GetComp<CompChiralProtection>();
                if (protection?.IsActive == true && 
                    position.DistanceTo(node.Position) <= protection.ProtectionRadius)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the nearest active chiral network node to a position
        /// </summary>
        public static Thing GetNearestChiralNode(IntVec3 position, Map map)
        {
            if (map == null) return null;
            
            Thing nearestNode = null;
            float nearestDistance = float.MaxValue;
            
            foreach (Thing node in map.listerThings.ThingsOfDef(ThingDefOf_DeathStranding.ChiralNetworkNode))
            {
                CompChiralProtection protection = node.GetComp<CompChiralProtection>();
                if (protection?.IsActive == true)
                {
                    float distance = position.DistanceTo(node.Position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestNode = node;
                    }
                }
            }
            
            return nearestNode;
        }

        /// <summary>
        /// Calculates the protection strength at a specific position
        /// </summary>
        public static float GetChiralProtectionStrength(IntVec3 position, Map map)
        {
            if (map == null) return 0f;
            
            float totalProtection = 0f;
            
            foreach (Thing node in map.listerThings.ThingsOfDef(ThingDefOf_DeathStranding.ChiralNetworkNode))
            {
                CompChiralProtection protection = node.GetComp<CompChiralProtection>();
                if (protection?.IsActive == true)
                {
                    float distance = position.DistanceTo(node.Position);
                    if (distance <= protection.ProtectionRadius)
                    {
                        float strength = 1f - (distance / protection.ProtectionRadius);
                        totalProtection += strength;
                    }
                }
            }
            
            return Math.Min(1f, totalProtection);
        }

        // ==================== BT AND THREAT DETECTION ====================
        
        /// <summary>
        /// Counts active BTs on the map
        /// </summary>
        public static int CountActiveBTs(Map map)
        {
            if (map == null) return 0;
            
            return map.mapPawns.AllPawnsSpawned
                .Count(p => p.def.defName.StartsWith("BT_") && !p.Dead);
        }

        /// <summary>
        /// Gets all BTs within a certain range of a position
        /// </summary>
        public static IEnumerable<Pawn> GetNearbyBTs(IntVec3 position, Map map, float range)
        {
            if (map == null) yield break;
            
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.def.defName.StartsWith("BT_") && 
                    !pawn.Dead && 
                    pawn.Position.DistanceTo(position) <= range)
                {
                    yield return pawn;
                }
            }
        }

        /// <summary>
        /// Calculates the "Beach threat level" for a map based on various factors
        /// </summary>
        public static float CalculateBeachThreatLevel(Map map)
        {
            if (map == null) return 0f;
            
            float threatLevel = 0f;
            
            // Base threat from active BTs
            int btCount = CountActiveBTs(map);
            threatLevel += btCount * 0.1f;
            
            // Threat from decaying corpses
            int decayingCorpses = map.listerThings.ThingsOfDef(ThingDefOf.Corpse)
                .Cast<Corpse>()
                .Count(c => c.InnerPawn.RaceProps.Humanlike && 
                           c.GetComp<CompCorpseTimer>()?.IsDecaying == true);
            threatLevel += decayingCorpses * 0.05f;
            
            // Weather modifier
            if (IsTimefallActive(map))
            {
                threatLevel *= 1.5f;
            }
            
            // Protection reduction
            float connectionLevel = GetColonyConnectionLevel(map);
            threatLevel *= (1f - connectionLevel * 0.5f);
            
            return Math.Min(1f, threatLevel);
        }

        /// <summary>
        /// Checks if any pawn has critical BT tether levels
        /// </summary>
        public static bool HasCriticalTetherLevels(Map map)
        {
            if (map == null) return false;
            
            return map.mapPawns.FreeColonists.Any(pawn =>
            {
                Hediff tether = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
                return tether?.Severity >= 0.8f;
            });
        }

        // ==================== WEATHER AND ENVIRONMENTAL ====================
        
        /// <summary>
        /// Checks if timefall is currently active
        /// </summary>
        public static bool IsTimefallActive(Map map)
        {
            if (map?.weatherManager?.curWeather == null) return false;
            
            return map.weatherManager.curWeather.defName.Contains("Timefall");
        }

        /// <summary>
        /// Gets the intensity of current timefall (0-1)
        /// </summary>
        public static float GetTimefallIntensity(Map map)
        {
            if (!IsTimefallActive(map)) return 0f;
            
            // Could be extended to support different timefall intensities
            string weatherName = map.weatherManager.curWeather.defName;
            if (weatherName.Contains("Storm"))
            {
                return 1f; // Maximum intensity
            }
            
            return 0.7f; // Standard timefall intensity
        }

        /// <summary>
        /// Predicts if timefall is likely to occur soon based on conditions
        /// </summary>
        public static bool IsTimefallLikely(Map map)
        {
            if (map == null) return false;
            
            // Check storyteller settings
            var storyteller = Find.Storyteller?.def;
            if (storyteller?.defName == "SamPorterBridges")
            {
                return Rand.Chance(0.3f); // Higher chance with custom storyteller
            }
            
            // Check for high Beach threat levels
            float threatLevel = CalculateBeachThreatLevel(map);
            return threatLevel > 0.5f && Rand.Chance(threatLevel * 0.2f);
        }

        // ==================== MESSAGING AND NOTIFICATIONS ====================
        
        /// <summary>
        /// Sends a Porter-themed message to the player
        /// </summary>
        public static void SendPorterMessage(string messageKey, MessageTypeDef messageType, params object[] args)
        {
            try
            {
                string message = messageKey.Translate(args);
                Messages.Message(message, messageType);
            }
            catch (Exception ex)
            {
                Log.Warning($"DeathStranding: Failed to send message '{messageKey}': {ex.Message}");
                // Fallback message
                Messages.Message($"Death Stranding event occurred.", messageType);
            }
        }

        /// <summary>
        /// Sends a message with a specific target for camera focus
        /// </summary>
        public static void SendPorterMessage(string messageKey, LookTargets target, MessageTypeDef messageType, params object[] args)
        {
            try
            {
                string message = messageKey.Translate(args);
                Messages.Message(message, target, messageType);
            }
            catch (Exception ex)
            {
                Log.Warning($"DeathStranding: Failed to send targeted message '{messageKey}': {ex.Message}");
                Messages.Message($"Death Stranding event occurred.", messageType);
            }
        }

        /// <summary>
        /// Alerts all DOOMS carriers about a significant event
        /// </summary>
        public static void AlertDOOMSCarriers(Map map, string messageKey, MessageTypeDef messageType, IntVec3 eventLocation, params object[] args)
        {
            if (map == null) return;
            
            foreach (Pawn doomsCarrier in GetDOOMSCarriers(map))
            {
                int doomsLevel = GetDOOMSLevel(doomsCarrier);
                float distance = doomsCarrier.Position.DistanceTo(eventLocation);
                float detectionRange = doomsLevel * 10f; // Higher levels detect from further away
                
                if (distance <= detectionRange)
                {
                    SendPorterMessage(messageKey, doomsCarrier, messageType, args);
                }
            }
        }

        // ==================== RESOURCE MANAGEMENT ====================
        
        /// <summary>
        /// Counts total chiral crystals available to the colony
        /// </summary>
        public static int GetAvailableChiralCrystals(Map map)
        {
            if (map == null) return 0;
            
            int total = 0;
            
            // Count crystals in stockpiles
            foreach (Thing thing in map.listerThings.ThingsOfDef(ThingDefOf_DeathStranding.ChiralCrystal))
            {
                total += thing.stackCount;
            }
            
            // Count crystals carried by pawns
            foreach (Pawn pawn in map.mapPawns.FreeColonists)
            {
                total += pawn.inventory.innerContainer
                    .Where(t => t.def == ThingDefOf_DeathStranding.ChiralCrystal)
                    .Sum(t => t.stackCount);
            }
            
            return total;
        }

        /// <summary>
        /// Attempts to consume chiral crystals from the colony's supplies
        /// </summary>
        public static bool TryConsumeChiralCrystals(Map map, int amount)
        {
            if (GetAvailableChiralCrystals(map) < amount)
                return false;
            
            int remaining = amount;
            
            // First, consume from stockpiles
            var stockpiledCrystals = map.listerThings.ThingsOfDef(ThingDefOf_DeathStranding.ChiralCrystal).ToList();
            foreach (Thing crystal in stockpiledCrystals)
            {
                if (remaining <= 0) break;
                
                int toConsume = Math.Min(remaining, crystal.stackCount);
                crystal.stackCount -= toConsume;
                remaining -= toConsume;
                
                if (crystal.stackCount <= 0)
                {
                    crystal.Destroy();
                }
            }
            
            // Then, consume from pawn inventories if needed
            if (remaining > 0)
            {
                foreach (Pawn pawn in map.mapPawns.FreeColonists)
                {
                    if (remaining <= 0) break;
                    
                    var carriedCrystals = pawn.inventory.innerContainer
                        .Where(t => t.def == ThingDefOf_DeathStranding.ChiralCrystal)
                        .ToList();
                    
                    foreach (Thing crystal in carriedCrystals)
                    {
                        if (remaining <= 0) break;
                        
                        int toConsume = Math.Min(remaining, crystal.stackCount);
                        crystal.stackCount -= toConsume;
                        remaining -= toConsume;
                        
                        if (crystal.stackCount <= 0)
                        {
                            crystal.Destroy();
                        }
                    }
                }
            }
            
            return remaining == 0;
        }

        // ==================== VALIDATION AND ERROR HANDLING ====================
        
        /// <summary>
        /// Validates that all required Death Stranding defs are loaded
        /// </summary>
        public static bool ValidateModIntegrity()
        {
            try
            {
                // Check critical ThingDefs
                if (ThingDefOf_DeathStranding.ChiralCrystal == null)
                {
                    Log.Error("DeathStranding: ChiralCrystal ThingDef not found!");
                    return false;
                }
                
                if (ThingDefOf_DeathStranding.ChiralNetworkNode == null)
                {
                    Log.Warning("DeathStranding: ChiralNetworkNode ThingDef not found!");
                }
                
                // Check critical HediffDefs
                if (HediffDefOf_DeathStranding.BTTether == null)
                {
                    Log.Error("DeathStranding: BTTether HediffDef not found!");
                    return false;
                }
                
                Log.Message("DeathStranding: Mod integrity validation passed.");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"DeathStranding: Mod integrity validation failed: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Safe method to get a DefOf reference with fallback
        /// </summary>
        public static T GetDefSafe<T>(string defName, T fallback = default(T)) where T : Def
        {
            try
            {
                return DefDatabase<T>.GetNamedSilentFail(defName) ?? fallback;
            }
            catch (Exception ex)
            {
                Log.Warning($"DeathStranding: Failed to get def '{defName}': {ex.Message}");
                return fallback;
            }
        }

        // ==================== DEBUG AND DEVELOPMENT ====================
        
        /// <summary>
        /// Logs detailed colony state for debugging (Dev mode only)
        /// </summary>
        public static void LogColonyState(Map map)
        {
            if (!Prefs.DevMode) return;
            
            Log.Message($"=== Death Stranding Colony State ===");
            Log.Message($"Map: {map?.info?.name ?? "null"}");
            Log.Message($"Is Porter Colony: {IsPorterColony(map)}");
            Log.Message($"Connection Level: {GetColonyConnectionLevel(map):F2}");
            Log.Message($"DOOMS Carriers: {GetDOOMSCarriers(map).Count()}");
            Log.Message($"Highest DOOMS Level: {GetHighestColonyDOOMSLevel(map)}");
            Log.Message($"Active BTs: {CountActiveBTs(map)}");
            Log.Message($"Beach Threat Level: {CalculateBeachThreatLevel(map):F2}");
            Log.Message($"Timefall Active: {IsTimefallActive(map)}");
            Log.Message($"Available Chiral Crystals: {GetAvailableChiralCrystals(map)}");
            Log.Message($"Critical Tether Levels: {HasCriticalTetherLevels(map)}");
            Log.Message($"=====================================");
        }
    }
}