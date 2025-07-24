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
    // ==================== CONNECTION BUILDING INCIDENTS ====================
    
    /// <summary>
    /// Incident that provides opportunities to build connections with other settlements
    /// </summary>
    public class IncidentWorker_ConnectionOpportunity : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            
            // Need at least some colonists and basic infrastructure
            if (map.mapPawns.FreeColonists.Count() < 2)
                return false;
            
            // Don't fire if already well-connected
            float connectionLevel = DeathStrandingUtility.GetColonyConnectionLevel(map);
            if (connectionLevel > 0.8f)
                return false;
            
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            
            // Choose type of connection opportunity
            ConnectionOpportunityType opportunityType = ChooseOpportunityType(map);
            
            switch (opportunityType)
            {
                case ConnectionOpportunityType.TradeRoute:
                    return ExecuteTradeRouteOpportunity(map, parms);
                case ConnectionOpportunityType.ChiralNetworkExpansion:
                    return ExecuteNetworkExpansionOpportunity(map, parms);
                case ConnectionOpportunityType.SettlementAlliance:
                    return ExecuteAllianceOpportunity(map, parms);
                case ConnectionOpportunityType.TechnicalAssistance:
                    return ExecuteTechnicalAssistanceOpportunity(map, parms);
                default:
                    return false;
            }
        }

        private ConnectionOpportunityType ChooseOpportunityType(Map map)
        {
            float connectionLevel = DeathStrandingUtility.GetColonyConnectionLevel(map);
            
            // Early game: focus on basic connections
            if (connectionLevel < 0.3f)
            {
                return Rand.Bool ? ConnectionOpportunityType.TradeRoute : ConnectionOpportunityType.TechnicalAssistance;
            }
            // Mid game: network expansion
            else if (connectionLevel < 0.6f)
            {
                return Rand.Bool ? ConnectionOpportunityType.ChiralNetworkExpansion : ConnectionOpportunityType.SettlementAlliance;
            }
            // Late game: advanced connections
            else
            {
                return ConnectionOpportunityType.SettlementAlliance;
            }
        }

        private bool ExecuteTradeRouteOpportunity(Map map, IncidentParms parms)
        {
            // Find suitable faction for trade route
            Faction tradeFaction = Find.FactionManager.AllFactions
                .Where(f => !f.IsPlayer && !f.HostileTo(Faction.OfPlayer) && f.def.humanlikeFaction)
                .RandomElementWithFallback();
            
            if (tradeFaction == null)
                return false;
            
            // Create trade caravan
            IncidentParms caravanParms = new IncidentParms
            {
                target = map,
                faction = tradeFaction,
                points = 1000f,
                traderKind = TraderKindDefOf.Orbital
            };
            
            bool success = IncidentDefOf.TraderCaravanArrival.Worker.TryExecute(caravanParms);
            
            if (success)
            {
                Find.LetterStack.ReceiveLetter(
                    "TradeRouteOpportunityTitle".Translate(),
                    "TradeRouteOpportunityDesc".Translate(tradeFaction.Name),
                    LetterDefOf.PositiveEvent,
                    map
                );
                
                // Improve faction relations
                tradeFaction.TryAffectGoodwillWith(Faction.OfPlayer, Rand.Range(5, 15));
            }
            
            return success;
        }

        private bool ExecuteNetworkExpansionOpportunity(Map map, IncidentParms parms)
        {
            // Drop chiral network components
            List<Thing> networkComponents = new List<Thing>();
            
            Thing chiralCrystals = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
            chiralCrystals.stackCount = Rand.Range(8, 20);
            networkComponents.Add(chiralCrystals);
            
            Thing networkNode = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralNetworkNode);
            networkComponents.Add(networkNode);
            
            IntVec3 dropLocation = DropCellFinder.RandomDropSpot(map);
            
            foreach (Thing component in networkComponents)
            {
                GenPlace.TryPlaceThing(component, dropLocation, map, ThingPlaceMode.Near);
            }
            
            Find.LetterStack.ReceiveLetter(
                "NetworkExpansionOpportunityTitle".Translate(),
                "NetworkExpansionOpportunityDesc".Translate(),
                LetterDefOf.PositiveEvent,
                new TargetInfo(dropLocation, map)
            );
            
            return true;
        }

        private bool ExecuteAllianceOpportunity(Map map, IncidentParms parms)
        {
            // Create alliance quest
            var validFactions = Find.FactionManager.AllFactions
                .Where(f => !f.IsPlayer && f.RelationKindWith(Faction.OfPlayer) == FactionRelationKind.Neutral)
                .ToList();
            
            if (!validFactions.Any())
                return false;
            
            Faction targetFaction = validFactions.RandomElement();
            
            Find.LetterStack.ReceiveLetter(
                "AllianceOpportunityTitle".Translate(),
                "AllianceOpportunityDesc".Translate(targetFaction.Name),
                LetterDefOf.PositiveEvent,
                map
            );
            
            // Create simple alliance quest
            targetFaction.TryAffectGoodwillWith(Faction.OfPlayer, Rand.Range(20, 40));
            
            return true;
        }

        private bool ExecuteTechnicalAssistanceOpportunity(Map map, IncidentParms parms)
        {
            // Spawn helpful refugee or trader with technical knowledge
            PawnKindDef engineerKind = PawnKindDefOf.Villager; // Would be custom engineer in full implementation
            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.OutlanderCivil);
            
            Pawn engineer = PawnGenerator.GeneratePawn(engineerKind, faction);
            
            // Boost relevant skills
            if (engineer.skills != null)
            {
                engineer.skills.GetSkill(SkillDefOf.Intellectual).Level = Rand.Range(8, 15);
                engineer.skills.GetSkill(SkillDefOf.Crafting).Level = Rand.Range(6, 12);
            }
            
            IntVec3 spawnLocation = CellFinder.RandomClosewalkCellNear(map.Center, map, 8);
            GenSpawn.Spawn(engineer, spawnLocation, map);
            
            Find.LetterStack.ReceiveLetter(
                "TechnicalAssistanceTitle".Translate(),
                "TechnicalAssistanceDesc".Translate(engineer.LabelShort),
                LetterDefOf.PositiveEvent,
                engineer
            );
            
            return true;
        }
    }

    // ==================== BEACH THREAT INCIDENTS ====================
    
    /// <summary>
    /// Major BT swarm incident that tests colony defenses
    /// </summary>
    public class IncidentWorker_BTSwarm : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            
            // Require minimum Beach threat level
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            if (beachThreat < 0.4f)
                return false;
            
            // Don't fire too frequently
            if (map.gameConditionManager.ConditionIsActive(GameConditionDefOf.PsychicDrone))
                return false;
            
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            
            // Calculate swarm size
            int swarmSize = CalculateSwarmSize(map, beachThreat, parms.points);
            
            // Find spawn locations away from chiral protection
            List<IntVec3> spawnLocations = FindBTSpawnLocations(map, swarmSize);
            
            if (spawnLocations.Count == 0)
                return false;
            
            // Spawn BT swarm
            List<Pawn> spawnedBTs = SpawnBTSwarm(map, spawnLocations, swarmSize);
            
            // Create swarm effects
            CreateSwarmEffects(map, spawnLocations);
            
            // Send notification
            SendSwarmNotification(map, spawnedBTs.Count);
            
            return true;
        }

        private int CalculateSwarmSize(Map map, float beachThreat, float points)
        {
            int baseSize = Mathf.RoundToInt(points / 100f);
            float threatMultiplier = 1f + beachThreat;
            
            // Reduce size if colony has good protection
            float protectionLevel = DeathStrandingUtility.GetColonyConnectionLevel(map);
            float protectionReduction = protectionLevel * 0.4f;
            
            int finalSize = Mathf.RoundToInt(baseSize * threatMultiplier * (1f - protectionReduction));
            return Mathf.Clamp(finalSize, 2, 12);
        }

        private List<IntVec3> FindBTSpawnLocations(Map map, int count)
        {
            List<IntVec3> locations = new List<IntVec3>();
            
            for (int attempts = 0; attempts < count * 5; attempts++)
            {
                IntVec3 candidate = CellFinder.RandomCell(map);
                
                if (IsValidBTSpawnLocation(candidate, map) && 
                    !locations.Any(loc => loc.DistanceTo(candidate) < 8f))
                {
                    locations.Add(candidate);
                    
                    if (locations.Count >= count)
                        break;
                }
            }
            
            return locations;
        }

        private bool IsValidBTSpawnLocation(IntVec3 cell, Map map)
        {
            // Must be walkable
            if (!cell.Walkable(map) || !cell.Standable(map))
                return false;
            
            // Avoid chiral protection
            if (DeathStrandingUtility.IsUnderChiralProtection(cell, map))
                return false;
            
            // Avoid areas too close to colonists
            if (map.mapPawns.FreeColonists.Any(p => p.Position.DistanceTo(cell) <= 12f))
                return false;
            
            return true;
        }

        private List<Pawn> SpawnBTSwarm(Map map, List<IntVec3> locations, int totalCount)
        {
            List<Pawn> spawnedBTs = new List<Pawn>();
            
            for (int i = 0; i < totalCount && i < locations.Count; i++)
            {
                IntVec3 spawnPos = locations[i];
                
                // Choose BT type based on swarm progression
                PawnKindDef btType = ChooseBTTypeForSwarm(i, totalCount);
                
                Pawn bt = PawnGenerator.GeneratePawn(btType);
                GenSpawn.Spawn(bt, spawnPos, map);
                spawnedBTs.Add(bt);
                
                // Spawn effects
                FleckMaker.Static(spawnPos, map, FleckDefOf.ExplosionFlash, 2f);
                FleckMaker.ThrowSmoke(spawnPos.ToVector3Shifted(), map, 2f);
            }
            
            return spawnedBTs;
        }

        private PawnKindDef ChooseBTTypeForSwarm(int index, int totalCount)
        {
            // More dangerous BTs later in swarm
            float dangerRatio = (float)index / totalCount;
            
            if (dangerRatio > 0.8f && Rand.Chance(0.4f))
            {
                return PawnKindDefOf_DeathStranding.BT_Hunter ?? PawnKindDefOf_DeathStranding.BT_Basic;
            }
            else if (dangerRatio > 0.5f && Rand.Chance(0.6f))
            {
                return PawnKindDefOf_DeathStranding.BT_Catcher ?? PawnKindDefOf_DeathStranding.BT_Basic;
            }
            else
            {
                return PawnKindDefOf_DeathStranding.BT_Basic;
            }
        }

        private void CreateSwarmEffects(Map map, List<IntVec3> spawnLocations)
        {
            // Atmospheric disturbance across the map
            for (int i = 0; i < 20; i++)
            {
                IntVec3 effectLocation = CellFinder.RandomCell(map);
                FleckMaker.Static(effectLocation, map, FleckDefOf.PsycastPsychicEffect, 1f);
            }
            
            // Connecting dark energy between spawn points
            for (int i = 0; i < spawnLocations.Count - 1; i++)
            {
                FleckMaker.ConnectingLine(
                    spawnLocations[i].ToVector3Shifted(),
                    spawnLocations[i + 1].ToVector3Shifted(),
                    FleckDefOf.AirPuff,
                    map
                );
            }
        }

        private void SendSwarmNotification(Map map, int btCount)
        {
            Find.LetterStack.ReceiveLetter(
                "BTSwarmTitle".Translate(),
                "BTSwarmDesc".Translate(btCount),
                LetterDefOf.ThreatBig,
                map
            );
            
            // Alert DOOMS carriers
            foreach (Pawn carrier in DeathStrandingUtility.GetDOOMSCarriers(map))
            {
                if (DeathStrandingUtility.GetDOOMSLevel(carrier) >= 3)
                {
                    Messages.Message(
                        "DOOMSCarrierDetectsBTSwarm".Translate(carrier.LabelShort, btCount),
                        carrier,
                        MessageTypeDefOf.ThreatBig
                    );
                }
            }
        }
    }

    /// <summary>
    /// Voidout incident - ultimate Beach threat
    /// </summary>
    public class IncidentWorker_Voidout : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            
            // Require very high Beach threat level
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            if (beachThreat < 0.7f)
                return false;
            
            // Check for critical tether conditions
            bool hasCriticalTethers = map.mapPawns.FreeColonists.Any(p => 
            {
                Hediff tether = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
                return tether?.Severity >= 0.8f;
            });
            
            return hasCriticalTethers || beachThreat > 0.9f;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            
            // Find voidout epicenter
            IntVec3 epicenter = FindVoidoutEpicenter(map);
            if (epicenter == IntVec3.Invalid)
                return false;
            
            // Calculate voidout scale
            float voidoutScale = CalculateVoidoutScale(map, parms.points);
            
            // Pre-voidout warnings
            SendVoidoutWarnings(map, epicenter);
            
            // Execute voidout after brief delay
            Find.TickManager.later.ScheduleCallback(() => {
                ExecuteVoidout(map, epicenter, voidoutScale);
            }, 300); // 5 second warning
            
            return true;
        }

        private IntVec3 FindVoidoutEpicenter(Map map)
        {
            // Prioritize areas with critical tethers
            var criticalTetherPawns = map.mapPawns.FreeColonists
                .Where(p => 
                {
                    Hediff tether = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
                    return tether?.Severity >= 0.8f;
                })
                .ToList();
            
            if (criticalTetherPawns.Any())
            {
                return criticalTetherPawns.RandomElement().Position;
            }
            
            // Otherwise find area with high BT concentration
            var btClusters = map.mapPawns.AllPawnsSpawned
                .Where(p => p.def.defName.StartsWith("BT_"))
                .GroupBy(bt => new { X = bt.Position.x / 10, Z = bt.Position.z / 10 })
                .Where(group => group.Count() >= 2)
                .ToList();
            
            if (btClusters.Any())
            {
                return btClusters.RandomElement().First().Position;
            }
            
            // Fallback to random location
            return CellFinder.RandomCell(map);
        }

        private float CalculateVoidoutScale(Map map, float points)
        {
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            float baseScale = points / 1000f;
            
            return baseScale * (1f + beachThreat);
        }

        private void SendVoidoutWarnings(Map map, IntVec3 epicenter)
        {
            Find.LetterStack.ReceiveLetter(
                "VoidoutImminentTitle".Translate(),
                "VoidoutImminentDesc".Translate(),
                LetterDefOf.ThreatBig,
                new TargetInfo(epicenter, map)
            );
            
            // Visual warning effects
            for (int i = 0; i < 10; i++)
            {
                Vector3 warningPos = epicenter.ToVector3Shifted() + new Vector3(
                    Rand.Range(-15f, 15f),
                    0f,
                    Rand.Range(-15f, 15f)
                );
                FleckMaker.Static(warningPos.ToIntVec3(), map, FleckDefOf.PsycastPsychicEffect, 3f);
            }
        }

        private void ExecuteVoidout(Map map, IntVec3 epicenter, float scale)
        {
            // Calculate voidout radius
            float radius = 8f + scale * 3f;
            
            // Create voidout crater
            CreateVoidoutCrater(map, epicenter, radius);
            
            // Apply voidout effects to area
            ApplyVoidoutEffects(map, epicenter, radius);
            
            // Dramatic visual effects
            CreateVoidoutVisualEffects(map, epicenter, radius);
            
            // Send completion notification
            Find.LetterStack.ReceiveLetter(
                "VoidoutOccurredTitle".Translate(),
                "VoidoutOccurredDesc".Translate(radius.ToString("F0")),
                LetterDefOf.ThreatBig,
                new TargetInfo(epicenter, map)
            );
        }

        private void CreateVoidoutCrater(Map map, IntVec3 epicenter, float radius)
        {
            // Create crater by destroying terrain and structures
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(epicenter, radius, true))
            {
                if (!cell.InBounds(map))
                    continue;
                
                float distance = cell.DistanceTo(epicenter);
                float destructionChance = 1f - (distance / radius);
                
                if (Rand.Chance(destructionChance))
                {
                    // Destroy everything in the cell
                    List<Thing> thingsToDestroy = cell.GetThingList(map).ToList();
                    foreach (Thing thing in thingsToDestroy)
                    {
                        if (thing.def.destroyable)
                        {
                            thing.Destroy();
                        }
                    }
                    
                    // Change terrain to crater
                    TerrainDef craterTerrain = DefDatabase<TerrainDef>.GetNamedSilentFail("VoidoutCrater") 
                        ?? TerrainDefOf.Gravel;
                    map.terrainGrid.SetTerrain(cell, craterTerrain);
                }
            }
        }

        private void ApplyVoidoutEffects(Map map, IntVec3 epicenter, float radius)
        {
            // Apply radiation-like effects to survivors
            foreach (Pawn pawn in GenRadial.RadialDistinctThingsAround(epicenter, map, radius, true).OfType<Pawn>())
            {
                if (pawn.RaceProps.Humanlike)
                {
                    // Apply voidout exposure
                    Hediff exposure = HediffMaker.MakeHediff(
                        DefDatabase<HediffDef>.GetNamedSilentFail("VoidoutExposure") ?? HediffDefOf.ToxicBuildup,
                        pawn
                    );
                    exposure.Severity = 0.5f;
                    pawn.health.AddHediff(exposure);
                }
            }
            
            // Clear BTs in the area (they're destroyed by voidout)
            var btsInArea = GenRadial.RadialDistinctThingsAround(epicenter, map, radius, true)
                .OfType<Pawn>()
                .Where(p => p.def.defName.StartsWith("BT_"))
                .ToList();
            
            foreach (Pawn bt in btsInArea)
            {
                bt.Destroy();
            }
        }

        private void CreateVoidoutVisualEffects(Map map, IntVec3 epicenter, float radius)
        {
            // Central explosion
            FleckMaker.Static(epicenter, map, FleckDefOf.ExplosionFlash, 8f);
            
            // Expanding energy waves
            for (int wave = 1; wave <= 5; wave++)
            {
                float waveRadius = radius * wave / 5f;
                int pointsOnWave = Mathf.RoundToInt(waveRadius * 8);
                
                for (int point = 0; point < pointsOnWave; point++)
                {
                    float angle = (float)point / pointsOnWave * 2f * Mathf.PI;
                    Vector3 wavePos = epicenter.ToVector3Shifted() + 
                        new Vector3(Mathf.Cos(angle) * waveRadius, 0f, Mathf.Sin(angle) * waveRadius);
                    
                    if (wavePos.ToIntVec3().InBounds(map))
                    {
                        FleckMaker.Static(wavePos.ToIntVec3(), map, FleckDefOf.ExplosionFlash, 2f);
                    }
                }
            }
            
            // Sound effect
            SoundDefOf.Explosion_Bomb.PlayOneShot(new TargetInfo(epicenter, map));
        }
    }

    // ==================== DELIVERY AND PORTER INCIDENTS ====================
    
    /// <summary>
    /// Incident that creates delivery missions between settlements
    /// </summary>
    public class IncidentWorker_DeliveryMission : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            
            // Need capable colonists for delivery
            if (!map.mapPawns.FreeColonists.Any(p => p.health.capacities.CanBeAwake))
                return false;
            
            // Need some connection level to receive delivery requests
            float connectionLevel = DeathStrandingUtility.GetColonyConnectionLevel(map);
            if (connectionLevel < 0.2f)
                return false;
            
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            
            // Choose delivery mission type
            DeliveryMissionType missionType = ChooseDeliveryMissionType(map);
            
            switch (missionType)
            {
                case DeliveryMissionType.UrgentMedical:
                    return ExecuteUrgentMedicalDelivery(map, parms);
                case DeliveryMissionType.SupplyRun:
                    return ExecuteSupplyRunDelivery(map, parms);
                case DeliveryMissionType.DataTransfer:
                    return ExecuteDataTransferDelivery(map, parms);
                case DeliveryMissionType.PersonRescue:
                    return ExecutePersonRescueDelivery(map, parms);
                default:
                    return false;
            }
        }

        private DeliveryMissionType ChooseDeliveryMissionType(Map map)
        {
            // Weight mission types based on colony state
            List<DeliveryMissionType> weightedOptions = new List<DeliveryMissionType>();
            
            // Always possible
            weightedOptions.Add(DeliveryMissionType.SupplyRun);
            weightedOptions.Add(DeliveryMissionType.DataTransfer);
            
            // Medical if colony has medical facilities
            if (map.listerBuildings.ColonistsHaveBuildingWithDef(ThingDefOf.HospitalBed))
            {
                weightedOptions.Add(DeliveryMissionType.UrgentMedical);
            }
            
            // Rescue if colony is well-established
            if (map.mapPawns.FreeColonists.Count() >= 4)
            {
                weightedOptions.Add(DeliveryMissionType.PersonRescue);
            }
            
            return weightedOptions.RandomElement();
        }

        private bool ExecuteUrgentMedicalDelivery(Map map, IncidentParms parms)
        {
            // Create medical supply package
            Thing medicalSupplies = ThingMaker.MakeThing(ThingDefOf.MedicineIndustrial);
            medicalSupplies.stackCount = Rand.Range(8, 20);
            
            Thing chiralReward = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
            chiralReward.stackCount = Rand.Range(5, 12);
            
            // Drop near medical facilities
            Building hospitalBed = map.listerBuildings.AllBuildingsColonistOfDef(ThingDefOf.HospitalBed)
                .FirstOrDefault();
            
            IntVec3 dropLocation = hospitalBed?.Position ?? DropCellFinder.RandomDropSpot(map);
            
            GenPlace.TryPlaceThing(medicalSupplies, dropLocation, map, ThingPlaceMode.Near);
            
            Find.LetterStack.ReceiveLetter(
                "UrgentMedicalDeliveryTitle".Translate(),
                "UrgentMedicalDeliveryDesc".Translate(medicalSupplies.stackCount, chiralReward.stackCount),
                LetterDefOf.PositiveEvent,
                new TargetInfo(dropLocation, map)
            );
            
            // Schedule reward delivery
            Find.TickManager.later.ScheduleCallback(() => {
                IntVec3 rewardLocation = DropCellFinder.RandomDropSpot(map);
                GenPlace.TryPlaceThing(chiralReward, rewardLocation, map, ThingPlaceMode.Near);
                
                Messages.Message(
                    "DeliveryRewardReceived".Translate(chiralReward.stackCount),
                    new TargetInfo(rewardLocation, map),
                    MessageTypeDefOf.PositiveEvent
                );
            }, Rand.Range(15000, 30000)); // 4-8 hours later
            
            return true;
        }

        private bool ExecuteSupplyRunDelivery(Map map, IncidentParms parms)
        {
            // Create supply package with random useful items
            List<Thing> supplies = new List<Thing>();
            
            // Food supplies
            Thing meals = ThingMaker.MakeThing(ThingDefOf.MealSurvivalPack);
            meals.stackCount = Rand.Range(10, 25);
            supplies.Add(meals);
            
            // Material components
            Thing components = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
            components.stackCount = Rand.Range(3, 8);
            supplies.Add(components);
            
            // Drop supplies
            IntVec3 dropLocation = DropCellFinder.RandomDropSpot(map);
            foreach (Thing supply in supplies)
            {
                GenPlace.TryPlaceThing(supply, dropLocation, map, ThingPlaceMode.Near);
            }
            
            Find.LetterStack.ReceiveLetter(
                "SupplyRunDeliveryTitle".Translate(),
                "SupplyRunDeliveryDesc".Translate(),
                LetterDefOf.PositiveEvent,
                new TargetInfo(dropLocation, map)
            );
            
            return true;
        }

        private bool ExecuteDataTransferDelivery(Map map, IncidentParms parms)
        {
            // Create research data package
            Find.ResearchManager.ResearchPerformed(500f, null); // Bonus research
            
            Thing chiralReward = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
            chiralReward.stackCount = Rand.Range(3, 8);
            
            IntVec3 dropLocation = DropCellFinder.RandomDropSpot(map);
            GenPlace.TryPlaceThing(chiralReward, dropLocation, map, ThingPlaceMode.Near);
            
            Find.LetterStack.ReceiveLetter(
                "DataTransferDeliveryTitle".Translate(),
                "DataTransferDeliveryDesc".Translate(chiralReward.stackCount),
                LetterDefOf.PositiveEvent,
                new TargetInfo(dropLocation, map)
            );
            
            return true;
        }

        private bool ExecutePersonRescueDelivery(Map map, IncidentParms parms)
        {
            // Spawn refugee who needs help
            PawnKindDef refugeeKind = PawnKindDefOf.Villager;
            Faction refugeeFaction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.OutlanderCivil);
            
            Pawn refugee = PawnGenerator.GeneratePawn(refugeeKind, refugeeFaction);
            
            // Apply injuries/conditions requiring rescue
            BodyPartRecord randomPart = refugee.health.hediffSet.GetNotMissingParts().RandomElement();
            Hediff injury = HediffMaker.MakeHediff(HediffDefOf.Bruise, refugee, randomPart);
            injury.Severity = 0.6f;
            refugee.health.AddHediff(injury);
            
            IntVec3 spawnLocation = CellFinder.RandomClosewalkCellNear(map.Center, map, 12);
            GenSpawn.Spawn(refugee, spawnLocation, map);
            
            Find.LetterStack.ReceiveLetter(
                "PersonRescueDeliveryTitle".Translate(),
                "PersonRescueDeliveryDesc".Translate(refugee.LabelShort),
                LetterDefOf.PositiveEvent,
                refugee
            );
            
            return true;
        }
    }

    // ==================== SUPPORTING ENUMS ====================
    
    public enum ConnectionOpportunityType
    {
        TradeRoute,
        ChiralNetworkExpansion,
        SettlementAlliance,
        TechnicalAssistance
    }

    public enum DeliveryMissionType
    {
        UrgentMedical,
        SupplyRun,
        DataTransfer,
        PersonRescue
    }
}
            