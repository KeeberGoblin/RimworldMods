using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using DeathStrandingMod.Core;
using DeathStrandingMod.Components;

namespace DeathStrandingMod.Events
{
    /// <summary>
    /// Manages BT proxy events using existing RimWorld creatures
    /// </summary>
    public static class BTProxyManager
    {
        // Map BT types to existing creature kinds
        private static readonly Dictionary<string, PawnKindDef> BTProxyTypes = new Dictionary<string, PawnKindDef>
        {
            ["BT_Basic"] = PawnKindDefOf.Human,         // Use human corpses
            ["BT_Catcher"] = PawnKindDefOf.Bear,        // Aggressive animal
            ["BT_Hunter"] = PawnKindDefOf.Thrumbo,      // Rare, dangerous
            ["BT_Swarm"] = PawnKindDefOf.Rat,           // Multiple small creatures
            ["BT_Tech"] = PawnKindDefOf.Scyther,        // Corrupted mechanoid
            ["BT_Sentinel"] = PawnKindDefOf.Pikeman,    // Stationary guardian
            ["BT_Titan"] = PawnKindDefOf.Centipede      // Massive threat
        };

        // Special BT faction for proxy creatures
        private static Faction btProxyFaction;

        /// <summary>
        /// Gets or creates the BT proxy faction
        /// </summary>
        public static Faction GetBTProxyFaction()
        {
            if (btProxyFaction == null || !btProxyFaction.def.isPlayer)
            {
                btProxyFaction = Find.FactionManager.AllFactions
                    .FirstOrDefault(f => f.def == FactionDefOf.AncientsHostile);
                
                if (btProxyFaction == null)
                {
                    btProxyFaction = FactionGenerator.NewGeneratedFaction(FactionDefOf.AncientsHostile);
                }
            }
            return btProxyFaction;
        }

        /// <summary>
        /// Creates a BT proxy using existing creature types
        /// </summary>
        public static Pawn CreateBTProxy(string btType, IntVec3 position, Map map)
        {
            PawnKindDef proxyKind = BTProxyTypes.TryGetValue(btType, out var kind) 
                ? kind 
                : PawnKindDefOf.Human;
            
            // Generate the proxy creature
            Pawn btProxy = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind: proxyKind,
                faction: GetBTProxyFaction(),
                context: PawnGenerationContext.NonPlayer,
                tile: map.Tile,
                forceGenerateNewPawn: true,
                allowDead: btType == "BT_Basic", // Basic BTs can be corpses
                allowDowned: false,
                canGeneratePawnRelations: false,
                fixedBiologicalAge: btType == "BT_Basic" ? 30f : (float?)null,
                fixedChronologicalAge: btType == "BT_Basic" ? 30f : (float?)null
            ));
            
            // Apply BT modifications
            ApplyBTModifications(btProxy, btType);
            
            // Spawn the proxy
            GenSpawn.Spawn(btProxy, position, map);
            
            // Create manifestation effects
            CreateBTManifestationEffects(position, map);
            
            return btProxy;
        }

        /// <summary>
        /// Applies BT-specific modifications to a proxy creature
        /// </summary>
        private static void ApplyBTModifications(Pawn proxy, string btType)
        {
            // Add BT behavior component
            if (proxy.GetComp<CompBTBehavior>() == null)
            {
                proxy.AllComps.Add(new CompBTBehavior());
            }
            
            // Add visual effects component
            if (proxy.GetComp<CompBTVisualEffects>() == null)
            {
                proxy.AllComps.Add(new CompBTVisualEffects());
            }
            
            // Add tether component for interaction with colonists
            if (proxy.GetComp<CompBTTether>() == null)
            {
                proxy.AllComps.Add(new CompBTTether());
            }
            
            // Modify stats and behavior based on BT type
            ModifyBTStats(proxy, btType);
            
            // Apply visual modifications
            ApplyBTAppearance(proxy, btType);
            
            // Set up BT-specific AI
            ConfigureBTAI(proxy, btType);
        }

        /// <summary>
        /// Modifies creature stats to match BT characteristics
        /// </summary>
        private static void ModifyBTStats(Pawn proxy, string btType)
        {
            if (proxy.health?.hediffSet == null) return;
            
            // Apply stat modifications based on BT type
            switch (btType)
            {
                case "BT_Basic":
                    // Slower but persistent
                    ApplyStatModification(proxy, StatDefOf.MoveSpeed, 0.7f);
                    ApplyStatModification(proxy, StatDefOf.MeleeHitChance, 0.8f);
                    break;
                    
                case "BT_Catcher":
                    // Fast and aggressive
                    ApplyStatModification(proxy, StatDefOf.MoveSpeed, 1.3f);
                    ApplyStatModification(proxy, StatDefOf.MeleeDamage, 1.2f);
                    break;
                    
                case "BT_Hunter":
                    // Balanced but dangerous
                    ApplyStatModification(proxy, StatDefOf.MoveSpeed, 1.1f);
                    ApplyStatModification(proxy, StatDefOf.MeleeDamage, 1.5f);
                    ApplyStatModification(proxy, StatDefOf.ArmorRating_Sharp, 1.3f);
                    break;
                    
                case "BT_Tech":
                    // Mechanoid corruption effects
                    ApplyStatModification(proxy, StatDefOf.EnergyShieldEnergyMax, 0.5f);
                    break;
            }
            
            // All BTs are harder to kill
            ApplyStatModification(proxy, StatDefOf.PainShockThreshold, 2f);
            
            // BTs don't feel pain normally
            Hediff painBlock = HediffMaker.MakeHediff(HediffDefOf.Painstopper, proxy);
            proxy.health.AddHediff(painBlock);
        }

        /// <summary>
        /// Applies a stat modification to a pawn
        /// </summary>
        private static void ApplyStatModification(Pawn pawn, StatDef stat, float multiplier)
        {
            // Create a hediff that modifies the stat
            HediffDef statHediff = DefDatabase<HediffDef>.GetNamedSilentFail("BTStatModification");
            if (statHediff == null)
            {
                // Fallback to a generic buff/debuff
                statHediff = multiplier > 1f ? HediffDefOf.Go : HediffDefOf.PsychicHangover;
            }
            
            Hediff modification = HediffMaker.MakeHediff(statHediff, pawn);
            modification.Severity = Math.Abs(multiplier - 1f);
            pawn.health.AddHediff(modification);
        }

        /// <summary>
        /// Modifies appearance to look more BT-like
        /// </summary>
        private static void ApplyBTAppearance(Pawn proxy, string btType)
        {
            // For basic BTs (humans), make them look corpse-like
            if (btType == "BT_Basic" && proxy.RaceProps.Humanlike)
            {
                // Add decay effects
                if (proxy.health?.hediffSet != null)
                {
                    Hediff decay = HediffMaker.MakeHediff(HediffDefOf.Scaria, proxy);
                    decay.Severity = 0.5f;
                    proxy.health.AddHediff(decay);
                }
                
                // Modify clothing to look tattered
                if (proxy.apparel?.WornApparel != null)
                {
                    foreach (Apparel clothing in proxy.apparel.WornApparel.ToList())
                    {
                        clothing.HitPoints = (int)(clothing.HitPoints * 0.3f);
                    }
                }
            }
        }

        /// <summary>
        /// Configures AI behavior for BT proxies
        /// </summary>
        private static void ConfigureBTAI(Pawn proxy, string btType)
        {
            if (proxy.mindState == null) return;
            
            // BTs don't flee
            proxy.mindState.canFleeIndividual = false;
            proxy.mindState.maxDistToSquadFlag = 999f;
            
            // Configure aggression based on type
            switch (btType)
            {
                case "BT_Basic":
                    // Shambling behavior - only aggressive when very close
                    proxy.mindState.aggression = 0.3f;
                    break;
                    
                case "BT_Catcher":
                case "BT_Hunter":
                    // Highly aggressive
                    proxy.mindState.aggression = 0.9f;
                    break;
                    
                case "BT_Sentinel":
                    // Defensive but territorial
                    proxy.mindState.aggression = 0.6f;
                    break;
            }
        }

        /// <summary>
        /// Creates visual effects when a BT manifests
        /// </summary>
        private static void CreateBTManifestationEffects(IntVec3 position, Map map)
        {
            // Dark energy manifestation
            FleckMaker.Static(position, map, FleckDefOf.ExplosionFlash, 3f);
            
            // Expanding darkness
            for (int i = 0; i < 8; i++)
            {
                Vector3 effectPos = position.ToVector3Shifted() + new Vector3(
                    Mathf.Cos(i * Mathf.PI / 4f) * Rand.Range(2f, 4f),
                    0f,
                    Mathf.Sin(i * Mathf.PI / 4f) * Rand.Range(2f, 4f)
                );
                FleckMaker.ThrowSmoke(effectPos, map, 2f);
            }
            
            // Sound effect
            SoundDefOf.Hive_Spawn.PlayOneShot(new TargetInfo(position, map));
        }

        /// <summary>
        /// Converts a regular corpse into a BT proxy
        /// </summary>
        public static bool TryConvertCorpseToBT(Corpse corpse)
        {
            if (corpse?.InnerPawn?.RaceProps?.Humanlike != true)
                return false;
            
            Map map = corpse.Map;
            IntVec3 position = corpse.Position;
            
            // Create manifestation effects
            CreateCorpseConversionEffects(position, map);
            
            // Destroy the corpse
            corpse.Destroy();
            
            // Create BT proxy at the same location
            Pawn btProxy = CreateBTProxy("BT_Basic", position, map);
            
            // Notification
            Messages.Message(
                "CorpseConvertedToBTProxy".Translate(corpse.InnerPawn.LabelShort),
                new TargetInfo(position, map),
                MessageTypeDefOf.ThreatBig
            );
            
            return true;
        }

        /// <summary>
        /// Creates effects when a corpse converts to BT
        /// </summary>
        private static void CreateCorpseConversionEffects(IntVec3 position, Map map)
        {
            // Conversion energy burst
            FleckMaker.Static(position, map, FleckDefOf.ExplosionFlash, 4f);
            
            // Rising dark energy
            for (int i = 0; i < 12; i++)
            {
                Vector3 effectPos = position.ToVector3Shifted() + new Vector3(
                    Rand.Range(-3f, 3f),
                    Rand.Range(0f, 2f),
                    Rand.Range(-3f, 3f)
                );
                FleckMaker.ThrowSmoke(effectPos, map, 3f);
            }
        }

        /// <summary>
        /// Spawns a BT swarm using existing raid mechanics
        /// </summary>
        public static void SpawnBTSwarm(Map map, IntVec3 spawnCenter, int count)
        {
            List<Pawn> swarmMembers = new List<Pawn>();
            
            for (int i = 0; i < count; i++)
            {
                // Vary BT types in swarm
                string btType = ChooseBTTypeForSwarm(i, count);
                
                // Find spawn position near center
                IntVec3 spawnPos = CellFinder.RandomSpawnCellForPawnNear(spawnCenter, map, 4);
                if (!spawnPos.IsValid)
                    spawnPos = spawnCenter;
                
                // Create BT proxy
                Pawn btProxy = CreateBTProxy(btType, spawnPos, map);
                swarmMembers.Add(btProxy);
            }
            
            // Make them work together
            CoordinateSwarmBehavior(swarmMembers);
            
            Messages.Message(
                "BTSwarmManifested".Translate(count),
                new TargetInfo(spawnCenter, map),
                MessageTypeDefOf.ThreatBig
            );
        }

        /// <summary>
        /// Chooses appropriate BT type for swarm composition
        /// </summary>
        private static string ChooseBTTypeForSwarm(int index, int totalCount)
        {
            float ratio = (float)index / totalCount;
            
            // Most are basic BTs
            if (ratio < 0.7f)
                return "BT_Basic";
            // Some catchers
            else if (ratio < 0.9f)
                return "BT_Catcher";
            // Rare hunters
            else
                return "BT_Hunter";
        }

        /// <summary>
        /// Makes swarm members coordinate their behavior
        /// </summary>
        private static void CoordinateSwarmBehavior(List<Pawn> swarmMembers)
        {
            if (!swarmMembers.Any()) return;
            
            // Set them all to the same lord (group AI)
            LordMaker.MakeNewLord(
                GetBTProxyFaction(),
                new LordJob_AssaultColony(GetBTProxyFaction()),
                swarmMembers[0].Map,
                swarmMembers
            );
        }
    }

    /// <summary>
    /// Manages all BT-related events and incidents using proxy system
    /// </summary>
    public static class BTEventManager
    {
        private static Dictionary<Map, BTEventData> mapEventData = new Dictionary<Map, BTEventData>();
        
        /// <summary>
        /// Initializes BT event tracking for a map
        /// </summary>
        public static void InitializeMap(Map map)
        {
            if (!mapEventData.ContainsKey(map))
            {
                mapEventData[map] = new BTEventData();
            }
        }

        /// <summary>
        /// Updates BT events for all maps using proxy system
        /// </summary>
        public static void BTEventTick()
        {
            foreach (Map map in Find.Maps)
            {
                if (map.IsPlayerHome)
                {
                    InitializeMap(map);
                    UpdateMapBTEvents(map);
                }
            }
        }

        private static void UpdateMapBTEvents(Map map)
        {
            BTEventData eventData = mapEventData[map];
            int currentTick = Find.TickManager.TicksGame;
            
            // Check for corpse conversion events
            if (currentTick - eventData.lastCorpseCheckTick > 15000) // Every ~4 hours
            {
                CheckCorpseConversionEvents(map);
                eventData.lastCorpseCheckTick = currentTick;
            }
            
            // Check for BT manifestation events
            if (currentTick - eventData.lastManifestationCheckTick > 30000) // Every ~8 hours
            {
                CheckBTManifestationEvents(map);
                eventData.lastManifestationCheckTick = currentTick;
            }
            
            // Check for tether escalation events
            if (currentTick - eventData.lastTetherCheckTick > 7500) // Every ~2 hours
            {
                CheckTetherEscalationEvents(map);
                eventData.lastTetherCheckTick = currentTick;
            }
        }

        /// <summary>
        /// Checks for corpse conversion to BT proxy events
        /// </summary>
        private static void CheckCorpseConversionEvents(Map map)
        {
            var eligibleCorpses = map.listerThings.ThingsOfDef(ThingDefOf.Corpse)
                .Cast<Corpse>()
                .Where(c => c.InnerPawn.RaceProps.Humanlike && 
                           !DeathStrandingUtility.IsUnderChiralProtection(c.Position, map))
                .ToList();

            foreach (Corpse corpse in eligibleCorpses)
            {
                // Check conversion conditions
                if (ShouldCorpseConvert(corpse))
                {
                    BTProxyManager.TryConvertCorpseToBT(corpse);
                }
            }
        }

        /// <summary>
        /// Determines if a corpse should convert to BT
        /// </summary>
        private static bool ShouldCorpseConvert(Corpse corpse)
        {
            // Base conversion chance
            float conversionChance = 0.05f; // 5% per check
            
            // Increase chance based on Beach threat
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(corpse.Map);
            conversionChance *= (1f + beachThreat);
            
            // Increase chance during timefall
            if (DeathStrandingUtility.IsTimefallActive(corpse.Map))
            {
                conversionChance *= 2f;
            }
            
            // Increase chance based on corpse age
            int corpseAge = Find.TickManager.TicksGame - corpse.timeOfDeath;
            if (corpseAge > 120000) // 2 days
            {
                conversionChance *= 1.5f;
            }
            
            return Rand.Chance(conversionChance);
        }

        /// <summary>
        /// Checks for spontaneous BT manifestation events
        /// </summary>
        private static void CheckBTManifestationEvents(Map map)
        {
            float beachThreatLevel = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            
            // Base manifestation chance
            float manifestationChance = beachThreatLevel * 0.1f;
            
            // Increase during timefall
            if (DeathStrandingUtility.IsTimefallActive(map))
            {
                manifestationChance *= 3f;
            }
            
            // Reduce if well protected
            float protectionLevel = DeathStrandingUtility.GetColonyConnectionLevel(map);
            manifestationChance *= (1f - protectionLevel * 0.7f);
            
            if (Rand.Chance(manifestationChance))
            {
                TriggerBTManifestation(map);
            }
        }

        /// <summary>
        /// Triggers a BT manifestation using proxy system
        /// </summary>
        private static void TriggerBTManifestation(Map map)
        {
            // Find manifestation location
            IntVec3 manifestationSite = FindManifestationSite(map);
            if (manifestationSite == IntVec3.Invalid)
                return;
            
            // Determine BT type based on threat level
            string btType = ChooseManifestationBTType(map);
            
            // Create BT proxy
            BTProxyManager.CreateBTProxy(btType, manifestationSite, map);
            
            // Alert DOOMS carriers
            AlertDOOMSCarriersOfNewBT(map, btType);
        }

        /// <summary>
        /// Finds a suitable location for BT manifestation
        /// </summary>
        private static IntVec3 FindManifestationSite(Map map)
        {
            // Prefer areas away from chiral protection
            for (int attempts = 0; attempts < 50; attempts++)
            {
                IntVec3 candidate = CellFinder.RandomCell(map);
                
                if (IsValidManifestationSite(candidate, map))
                {
                    return candidate;
                }
            }
            
            return IntVec3.Invalid;
        }

        /// <summary>
        /// Validates a manifestation site
        /// </summary>
        private static bool IsValidManifestationSite(IntVec3 cell, Map map)
        {
            // Must be walkable
            if (!cell.Walkable(map) || !cell.Standable(map))
                return false;
            
            // Avoid protected areas
            if (DeathStrandingUtility.IsUnderChiralProtection(cell, map))
                return false;
            
            // Avoid areas too close to colonists
            if (map.mapPawns.FreeColonists.Any(p => p.Position.DistanceTo(cell) <= 10f))
                return false;
            
            return true;
        }

        /// <summary>
        /// Chooses BT type for manifestation
        /// </summary>
        private static string ChooseManifestationBTType(Map map)
        {
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            
            if (beachThreat > 0.8f && Rand.Chance(0.2f))
                return "BT_Hunter";
            else if (beachThreat > 0.5f && Rand.Chance(0.4f))
                return "BT_Catcher";
            else
                return "BT_Basic";
        }

        /// <summary>
        /// Checks for tether escalation events
        /// </summary>
        private static void CheckTetherEscalationEvents(Map map)
        {
            foreach (Pawn colonist in map.mapPawns.FreeColonists)
            {
                Hediff tether = colonist.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
                if (tether != null && tether.Severity >= 0.8f)
                {
                    TriggerTetherEscalationEvent(colonist);
                }
            }
        }

        /// <summary>
        /// Triggers tether escalation event
        /// </summary>
        private static void TriggerTetherEscalationEvent(Pawn pawn)
        {
            // Spawn BT near high-tether colonist
            IntVec3 spawnPos = CellFinder.RandomSpawnCellForPawnNear(pawn.Position, pawn.Map, 8);
            if (spawnPos.IsValid)
            {
                BTProxyManager.CreateBTProxy("BT_Catcher", spawnPos, pawn.Map);
                
                Messages.Message(
                    "TetherAttractsBT".Translate(pawn.LabelShort),
                    pawn,
                    MessageTypeDefOf.ThreatBig
                );
            }
        }

        /// <summary>
        /// Alerts DOOMS carriers about new BT
        /// </summary>
        private static void AlertDOOMSCarriersOfNewBT(Map map, string btType)
        {
            foreach (Pawn carrier in DeathStrandingUtility.GetDOOMSCarriers(map))
            {
                int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(carrier);
                if (doomsLevel >= 2)
                {
                    Messages.Message(
                        "DOOMSCarrierDetectsNewBTProxy".Translate(carrier.LabelShort, btType),
                        carrier,
                        MessageTypeDefOf.CautionInput
                    );
                }
            }
        }
    }

    /// <summary>
    /// Data class for tracking BT events per map
    /// </summary>
    public class BTEventData : IExposable
    {
        public int lastCorpseCheckTick = 0;
        public int lastManifestationCheckTick = 0;
        public int lastTetherCheckTick = 0;
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref lastCorpseCheckTick, "lastCorpseCheckTick");
            Scribe_Values.Look(ref lastManifestationCheckTick, "lastManifestationCheckTick");
            Scribe_Values.Look(ref lastTetherCheckTick, "lastTetherCheckTick");
        }
    }
}