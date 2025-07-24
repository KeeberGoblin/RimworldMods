using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using DeathStrandingMod.Core;
using DeathStrandingMod.Components;

namespace DeathStrandingMod.Weather
{
    /// <summary>
    /// Main timefall weather event that accelerates aging and spawns chiral crystals
    /// </summary>
    public class WeatherEvent_Timefall : WeatherEvent
    {
        private TimefallProperties props;
        private int lastChiralSpawnTick = 0;
        private int lastBTCheckTick = 0;
        private int lastAgingTick = 0;
        private List<IntVec3> protectedAreas = new List<IntVec3>();
        
        private const int CHIRAL_SPAWN_INTERVAL = 30000; // ~8 hours
        private const int BT_CHECK_INTERVAL = 15000; // ~4 hours  
        private const int AGING_INTERVAL = 2500; // ~1 hour

        public override bool Expired => age > duration;

        public override void WeatherEventTick()
        {
            base.WeatherEventTick();
            
            if (props == null)
            {
                props = map.weatherManager.curWeather.GetModExtension<TimefallProperties>() 
                       ?? new TimefallProperties();
            }
            
            // Update protected areas periodically
            if (Find.TickManager.TicksGame % 250 == 0) // Every ~4 seconds
            {
                UpdateProtectedAreas();
            }
            
            // Execute timefall effects
            if (Find.TickManager.TicksGame - lastChiralSpawnTick > CHIRAL_SPAWN_INTERVAL)
            {
                TrySpawnChiralCrystals();
                lastChiralSpawnTick = Find.TickManager.TicksGame;
            }
            
            if (Find.TickManager.TicksGame - lastBTCheckTick > BT_CHECK_INTERVAL)
            {
                ProcessBTInteractions();
                lastBTCheckTick = Find.TickManager.TicksGame;
            }
            
            if (Find.TickManager.TicksGame - lastAgingTick > AGING_INTERVAL)
            {
                ApplyTimefallAging();
                lastAgingTick = Find.TickManager.TicksGame;
            }
            
            // Continuous effects
            AccelerateCorpseConversion();
            CreateAtmosphericEffects();
        }

        private void UpdateProtectedAreas()
        {
            protectedAreas.Clear();
            
            // Find all active chiral network nodes
            foreach (Thing node in map.listerThings.ThingsOfDef(ThingDefOf_DeathStranding.ChiralNetworkNode))
            {
                CompChiralProtection protection = node.GetComp<CompChiralProtection>();
                if (protection?.IsActive == true)
                {
                    // Add all cells in protection radius
                    foreach (IntVec3 cell in GenRadial.RadialCellsAround(node.Position, protection.ProtectionRadius, true))
                    {
                        if (cell.InBounds(map))
                        {
                            protectedAreas.Add(cell);
                        }
                    }
                }
            }
        }

        private void TrySpawnChiralCrystals()
        {
            if (!props.chiralSpawnChance.HasValue || !Rand.Chance(props.chiralSpawnChance.Value))
                return;
            
            // Find suitable spawn locations (rocky/mountainous terrain)
            var spawnCandidates = new List<IntVec3>();
            
            for (int attempts = 0; attempts < 50; attempts++)
            {
                IntVec3 candidate = CellFinder.RandomCell(map);
                TerrainDef terrain = candidate.GetTerrain(map);
                
                if (IsValidChiralSpawnLocation(candidate, terrain))
                {
                    spawnCandidates.Add(candidate);
                }
            }
            
            // Spawn crystals at best locations
            int crystalsToSpawn = Mathf.RoundToInt(spawnCandidates.Count * 0.3f);
            foreach (IntVec3 spawnCell in spawnCandidates.Take(crystalsToSpawn))
            {
                SpawnChiralCrystal(spawnCell);
            }
            
            if (crystalsToSpawn > 0)
            {
                Messages.Message(
                    "ChiralCrystalsFormedDuringTimefall".Translate(crystalsToSpawn),
                    MessageTypeDefOf.PositiveEvent
                );
            }
        }

        private bool IsValidChiralSpawnLocation(IntVec3 cell, TerrainDef terrain)
        {
            // Prefer rocky/mountainous terrain
            if (!terrain.defName.Contains("Rock") && !terrain.defName.Contains("Mountain"))
                return false;
            
            // Avoid areas with existing crystals
            if (cell.GetThingList(map).Any(t => t.def == ThingDefOf_DeathStranding.ChiralCrystal))
                return false;
            
            // Avoid heavily protected areas (crystals form better in exposed locations)
            if (protectedAreas.Contains(cell))
                return Rand.Chance(0.3f); // Reduced chance in protected areas
            
            // Prefer areas not under roofs
            if (cell.Roofed(map))
                return Rand.Chance(0.5f);
            
            return true;
        }

        private void SpawnChiralCrystal(IntVec3 spawnCell)
        {
            Thing crystal = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
            crystal.stackCount = CalculateChiralYield(spawnCell);
            
            GenPlace.TryPlaceThing(crystal, spawnCell, map, ThingPlaceMode.Near);
            
            // Visual effects
            FleckMaker.Static(spawnCell, map, FleckDefOf.Mote_ItemSparkle, 2f);
            FleckMaker.ThrowLightningGlow(spawnCell.ToVector3Shifted(), map, 1f);
            
            // Sound effect
            if (Rand.Chance(0.3f))
            {
                SoundDefOf.Hive_Spawn.PlayOneShot(new TargetInfo(spawnCell, map));
            }
        }

        private int CalculateChiralYield(IntVec3 location)
        {
            int baseYield = Rand.Range(1, 6);
            
            // Bonus for isolated locations
            if (!protectedAreas.Contains(location))
            {
                baseYield += Rand.Range(0, 3);
            }
            
            // Bonus for high-elevation areas
            if (location.GetTerrain(map).defName.Contains("Mountain"))
            {
                baseYield += Rand.Range(1, 4);
            }
            
            // Reduced yield near existing crystals
            var nearbyCrystals = GenRadial.RadialDistinctThingsAround(location, map, 5f, true)
                .Count(t => t.def == ThingDefOf_DeathStranding.ChiralCrystal);
            
            baseYield = Math.Max(1, baseYield - nearbyCrystals);
            
            return baseYield;
        }

        private void ProcessBTInteractions()
        {
            // Increase BT activity during timefall
            var activeBTs = map.mapPawns.AllPawnsSpawned
                .Where(p => p.def.defName.StartsWith("BT_"))
                .ToList();
            
            foreach (Pawn bt in activeBTs)
            {
                EnhanceBTDuringTimefall(bt);
            }
            
            // Chance to spawn new BTs
            if (props.btSpawnChance.HasValue && Rand.Chance(props.btSpawnChance.Value))
            {
                TrySpawnTimefallBT();
            }
            
            // Alert DOOMS carriers about increased BT activity
            AlertDOOMSCarriersAboutBTActivity();
        }

        private void EnhanceBTDuringTimefall(Pawn bt)
        {
            // Increase BT tether strength during timefall
            CompBTTether tetherComp = bt.GetComp<CompBTTether>();
            if (tetherComp != null)
            {
                // Temporarily boost tether effectiveness
                // This would need to be implemented in the CompBTTether class
            }
            
            // Make BTs more visible during timefall
            CompBTVisualEffects visualComp = bt.GetComp<CompBTVisualEffects>();
            if (visualComp != null)
            {
                // Enhance visibility during timefall
                // This would need to be implemented in the CompBTVisualEffects class
            }
            
            // Occasional visual effect
            if (Rand.Chance(0.1f))
            {
                FleckMaker.ThrowLightningGlow(bt.DrawPos, map, 0.8f);
            }
        }

        private void TrySpawnTimefallBT()
        {
            // Find isolated areas away from chiral protection
            var spawnCandidates = new List<IntVec3>();
            
            for (int attempts = 0; attempts < 30; attempts++)
            {
                IntVec3 candidate = CellFinder.RandomCell(map);
                
                if (IsValidBTSpawnLocation(candidate))
                {
                    spawnCandidates.Add(candidate);
                }
            }
            
            if (spawnCandidates.Any())
            {
                IntVec3 spawnLocation = spawnCandidates.RandomElement();
                SpawnTimefallBT(spawnLocation);
            }
        }

        private bool IsValidBTSpawnLocation(IntVec3 cell)
        {
            // Avoid protected areas
            if (protectedAreas.Contains(cell))
                return false;
            
            // Avoid areas with colonists nearby
            if (map.mapPawns.FreeColonists.Any(p => p.Position.DistanceTo(cell) <= 8f))
                return false;
            
            // Must be walkable
            if (!cell.Walkable(map) || !cell.Standable(map))
                return false;
            
            // Prefer areas with corpses or high "death energy"
            if (GenRadial.RadialDistinctThingsAround(cell, map, 10f, true)
                .Any(t => t is Corpse corpse && corpse.InnerPawn.RaceProps.Humanlike))
            {
                return true;
            }
            
            return Rand.Chance(0.3f);
        }

        private void SpawnTimefallBT(IntVec3 location)
        {
            // Choose BT type based on local conditions
            PawnKindDef btKind = ChooseBTTypeForTimefall(location);
            
            Pawn bt = PawnGenerator.GeneratePawn(btKind);
            GenSpawn.Spawn(bt, location, map);
            
            // Dramatic spawn effects
            CreateBTSpawnEffects(location);
            
            Messages.Message(
                "BTManifestedDuringTimefall".Translate(bt.kindDef.LabelCap),
                new TargetInfo(location, map),
                MessageTypeDefOf.ThreatBig
            );
        }

        private PawnKindDef ChooseBTTypeForTimefall(IntVec3 location)
        {
            // Check for nearby corpses to determine BT type
            var nearbyCorpses = GenRadial.RadialDistinctThingsAround(location, map, 15f, true)
                .OfType<Corpse>()
                .Where(c => c.InnerPawn.RaceProps.Humanlike)
                .ToList();
            
            if (nearbyCorpses.Any())
            {
                // More dangerous BTs spawn near multiple corpses
                if (nearbyCorpses.Count >= 3)
                {
                    return PawnKindDefOf_DeathStranding.BT_Hunter ?? PawnKindDefOf_DeathStranding.BT_Basic;
                }
                else if (nearbyCorpses.Count >= 2)
                {
                    return PawnKindDefOf_DeathStranding.BT_Catcher ?? PawnKindDefOf_DeathStranding.BT_Basic;
                }
            }
            
            return PawnKindDefOf_DeathStranding.BT_Basic;
        }

        private void CreateBTSpawnEffects(IntVec3 location)
        {
            // Dark energy manifestation
            FleckMaker.Static(location, map, FleckDefOf.ExplosionFlash, 3f);
            
            // Expanding darkness
            for (int i = 0; i < 8; i++)
            {
                Vector3 effectPos = location.ToVector3Shifted() + new Vector3(
                    Mathf.Cos(i * Mathf.PI / 4f) * 3f,
                    0f,
                    Mathf.Sin(i * Mathf.PI / 4f) * 3f
                );
                FleckMaker.ThrowSmoke(effectPos, map, 2f);
            }
            
            // Sound effect
            SoundDefOf.Hive_Spawn.PlayOneShot(new TargetInfo(location, map));
        }

        private void AlertDOOMSCarriersAboutBTActivity()
        {
            foreach (Pawn carrier in DeathStrandingUtility.GetDOOMSCarriers(map))
            {
                int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(carrier);
                if (doomsLevel >= 3 && Rand.Chance(0.2f))
                {
                    Messages.Message(
                        "DOOMSCarrierSensesTimefallBTActivity".Translate(carrier.LabelShort),
                        carrier,
                        MessageTypeDefOf.CautionInput
                    );
                }
            }
        }

        private void AccelerateCorpseConversion()
        {
            if (!props.accelerateCorpseConversion) return;
            
            // Find all corpses with conversion timers
            var corpses = map.listerThings.ThingsOfDef(ThingDefOf.Corpse)
                .Cast<Corpse>()
                .Where(c => c.InnerPawn.RaceProps.Humanlike)
                .ToList();
            
            foreach (Corpse corpse in corpses)
            {
                // Skip protected corpses
                if (protectedAreas.Contains(corpse.Position))
                    continue;
                
                CompCorpseTimer timer = corpse.GetComp<CompCorpseTimer>();
                if (timer != null)
                {
                    // Accelerate conversion during timefall
                    timer.AccelerateConversion(props.conversionSpeedMultiplier);
                }
            }
        }

        private void ApplyTimefallAging()
        {
            // Age all unprotected items and structures
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (ShouldAgeFromTimefall(thing))
                {
                    ApplyAgingToThing(thing);
                }
            }
        }

        private bool ShouldAgeFromTimefall(Thing thing)
        {
            // Skip protected areas
            if (protectedAreas.Contains(thing.Position))
                return false;
            
            // Skip things under roofs
            if (thing.Position.Roofed(map))
                return false;
            
            // Only age things with hit points
            if (!thing.def.useHitPoints)
                return false;
            
            // Don't age natural rock formations
            if (thing.def.building?.isNaturalRock == true)
                return false;
            
            return true;
        }

        private void ApplyAgingToThing(Thing thing)
        {
            if (thing.HitPoints <= 1) return;
            
            // Calculate aging damage
            float agingDamage = CalculateAgingDamage(thing);
            
            if (agingDamage > 0)
            {
                thing.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, agingDamage));
                
                // Visual aging effect
                if (Rand.Chance(0.1f))
                {
                    FleckMaker.ThrowSmoke(thing.Position.ToVector3Shifted(), map, 0.5f);
                }
            }
        }

        private float CalculateAgingDamage(Thing thing)
        {
            float baseDamage = 1f;
            
            // Apply aging multiplier
            baseDamage *= props.agingMultiplier;
            
            // Material resistance
            if (thing.Stuff != null)
            {
                if (thing.Stuff.stuffProps.categories.Contains(StuffCategoryDefOf.Metallic))
                {
                    baseDamage *= 0.8f; // Metals age slower
                }
                else if (thing.Stuff.stuffProps.categories.Contains(StuffCategoryDefOf.Woody))
                {
                    baseDamage *= 1.3f; // Wood ages faster
                }
            }
            
            // Random variation
            baseDamage *= Rand.Range(0.5f, 1.5f);
            
            return Mathf.RoundToInt(Math.Max(0f, baseDamage));
        }

        private void CreateAtmosphericEffects()
        {
            // Periodic atmospheric effects during timefall
            if (Find.TickManager.TicksGame % 300 == 0) // Every 5 seconds
            {
                CreateTimefallAtmosphere();
            }
        }

        private void CreateTimefallAtmosphere()
        {
            // Random atmospheric disturbances
            if (Rand.Chance(0.3f))
            {
                IntVec3 effectLocation = CellFinder.RandomCell(map);
                
                if (!protectedAreas.Contains(effectLocation))
                {
                    // Choose random atmospheric effect
                    switch (Rand.Range(0, 4))
                    {
                        case 0:
                            FleckMaker.ThrowSmoke(effectLocation.ToVector3Shifted(), map, 1f);
                            break;
                        case 1:
                            FleckMaker.ThrowAirPuffUp(effectLocation.ToVector3Shifted(), map);
                            break;
                        case 2:
                            FleckMaker.ThrowLightningGlow(effectLocation.ToVector3Shifted(), map, 0.5f);
                            break;
                        case 3:
                            FleckMaker.Static(effectLocation, map, FleckDefOf.PsycastPsychicEffect, 0.8f);
                            break;
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastChiralSpawnTick, "lastChiralSpawnTick");
            Scribe_Values.Look(ref lastBTCheckTick, "lastBTCheckTick");
            Scribe_Values.Look(ref lastAgingTick, "lastAgingTick");
            Scribe_Collections.Look(ref protectedAreas, "protectedAreas", LookMode.Value);
        }
    }
}