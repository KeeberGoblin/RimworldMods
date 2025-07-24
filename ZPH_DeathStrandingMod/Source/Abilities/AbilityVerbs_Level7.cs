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
    // ==================== GRAVITY FIELD - LEVEL 7 ====================
    
    /// <summary>
    /// Level 7 DOOMS ability - Create devastating gravitational distortion field
    /// </summary>
    public class Verb_GravityField : Verb_CastBase
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

            // Create massive gravity distortion
            float effectiveRange = AbilityPropertyUtility.CalculateEffectiveRange(8f, Props, caster as Pawn);
            CreateGravityField(targetCell, map, effectiveRange);
            
            // Apply significant Beach drift risk
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
            
            // Check for friendly fire risk
            var nearbyColonists = GenRadial.RadialDistinctThingsAround(targetCell, map, 8f, true)
                .OfType<Pawn>()
                .Where(p => p.Faction?.IsPlayer == true)
                .Count();
            
            if (nearbyColonists > 0)
            {
                Messages.Message("GravityFieldFriendlyFireWarning".Translate(nearbyColonists), 
                    MessageTypeDefOf.CautionInput);
            }
            
            return true;
        }

        private void CreateGravityField(IntVec3 center, Map map, float radius)
        {
            // Dramatic buildup effects
            CreateGravityFieldBuildup(center, map, radius);
            
            // Main gravitational effects
            ApplyGravitationalForces(center, map, radius);
            
            // Environmental destruction
            ApplyEnvironmentalEffects(center, map, radius);
            
            // Sound and visual climax
            CreateGravityFieldClimax(center, map);
        }

        private void CreateGravityFieldBuildup(IntVec3 center, Map map, float radius)
        {
            // Central energy buildup
            FleckMaker.Static(center, map, FleckDefOf.ExplosionFlash, 4f);
            
            // Expanding energy rings
            for (int ring = 1; ring <= 5; ring++)
            {
                float ringRadius = radius * ring / 5f;
                int pointsOnRing = Mathf.RoundToInt(ringRadius * 8);
                
                for (int point = 0; point < pointsOnRing; point++)
                {
                    float angle = (float)point / pointsOnRing * 2f * Mathf.PI;
                    Vector3 ringPos = center.ToVector3Shifted() + 
                        new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius);
                    
                    if (ringPos.ToIntVec3().InBounds(map))
                    {
                        FleckMaker.ThrowLightningGlow(ringPos, map, 1.5f);
                    }
                }
            }
        }

        private void ApplyGravitationalForces(IntVec3 center, Map map, float radius)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map)) continue;
                
                float distance = cell.DistanceTo(center);
                float intensity = 1f - (distance / radius);
                
                List<Thing> things = cell.GetThingList(map).ToList();
                foreach (Thing thing in things)
                {
                    if (thing is Pawn pawn)
                    {
                        ApplyGravityCrush(pawn, center, intensity);
                    }
                    else if (thing.def.category == ThingCategory.Item)
                    {
                        PullTowardCenter(thing, center, intensity);
                    }
                    else if (thing.def.category == ThingCategory.Building && 
                             !thing.def.building.isNaturalRock)
                    {
                        DamageFromGravity(thing, intensity);
                    }
                }
            }
        }

        private void ApplyGravityCrush(Pawn pawn, IntVec3 center, float intensity)
        {
            if (pawn == caster) return;
            
            // Calculate crush damage
            float baseDamage = intensity * 60f;
            
            // Scale with DOOMS level
            int casterDOOMS = DeathStrandingUtility.GetDOOMSLevel(caster as Pawn);
            baseDamage *= (1f + casterDOOMS * 0.15f);
            
            // Reduce damage for DOOMS carriers (partial resistance)
            int targetDOOMS = DeathStrandingUtility.GetDOOMSLevel(pawn);
            if (targetDOOMS > 0)
            {
                baseDamage *= (1f - targetDOOMS * 0.1f);
            }
            
            int damage = Mathf.RoundToInt(baseDamage);
            pawn.TakeDamage(new DamageInfo(DamageDefOf.Crush, damage));
            
            // Potential instant down at high intensity
            if (intensity > 0.7f && Rand.Chance(0.4f))
            {
                HealthUtility.DamageUntilDowned(pawn);
            }
            
            // Pull toward center
            Vector3 direction = (center - pawn.Position).ToVector3().normalized;
            IntVec3 pullDestination = pawn.Position + direction.ToIntVec3();
            
            if (pullDestination.InBounds(pawn.Map) && pullDestination.Walkable(pawn.Map))
            {
                pawn.Position = pullDestination;
                pawn.Notify_Teleported();
            }
            
            // Visual effects
            FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, 2f);
            FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1f);
        }

        private void PullTowardCenter(Thing item, IntVec3 center, float intensity)
        {
            Vector3 direction = (center - item.Position).ToVector3().normalized;
            float pullDistance = intensity * 4f;
            IntVec3 newPos = item.Position + (direction * pullDistance).ToIntVec3();
            
            if (newPos.InBounds(item.Map) && newPos.Walkable(item.Map))
            {
                item.Position = newPos;
                FleckMaker.ThrowDustPuff(newPos, item.Map, 1f);
            }
        }

        private void DamageFromGravity(Thing building, float intensity)
        {
            float damage = intensity * 150f;
            
            // Scale with DOOMS level
            int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(caster as Pawn);
            damage *= (1f + doomsLevel * 0.1f);
            
            building.TakeDamage(new DamageInfo(DamageDefOf.Crush, Mathf.RoundToInt(damage)));
        }

        private void ApplyEnvironmentalEffects(IntVec3 center, Map map, float radius)
        {
            // Create temporary gravity well effect
            for (int i = 0; i < 20; i++)
            {
                IntVec3 effectCell = center + GenRadial.RadialPattern[i];
                if (effectCell.InBounds(map))
                {
                    // Crack terrain effects
                    if (Rand.Chance(0.3f))
                    {
                        FleckMaker.ThrowSmoke(effectCell.ToVector3Shifted(), map, 2f);
                    }
                }
            }
        }

        private void CreateGravityFieldClimax(IntVec3 center, Map map)
        {
            // Massive explosion effect
            GenExplosion.DoExplosion(
                center: center,
                map: map,
                radius: 3f,
                damType: DamageDefOf.Bomb,
                instigator: caster,
                damAmount: 30,
                armorPenetration: 50f,
                explosionSound: SoundDefOf.Explosion_Bomb,
                weapon: null,
                projectile: null,
                intendedTarget: null,
                postExplosionSpawnThingDef: null,
                postExplosionSpawnChance: 0f,
                postExplosionSpawnThingCount: 1,
                applyDamageToExplosionCellsNeighbors: true,
                preExplosionSpawnThingDef: null,
                preExplosionSpawnChance: 0f,
                preExplosionSpawnThingCount: 1,
                chanceToStartFire: 0f,
                damageFalloff: true
            );
            
            // Lingering distortion effects
            for (int i = 0; i < 15; i++)
            {
                IntVec3 distortionCell = center + GenRadial.RadialPattern[i];
                if (distortionCell.InBounds(map))
                {
                    FleckMaker.ThrowLightningGlow(distortionCell.ToVector3Shifted(), map, 3f);
                }
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
            drift.Severity = Props?.beachDriftSeverity ?? 0.7f;
            pawn.health.AddHediff(drift);
            
            Messages.Message("BeachDriftTriggered".Translate(pawn.LabelShort),
                pawn, MessageTypeDefOf.NegativeEvent);
        }
    }

    // ==================== MATTER CONTROL - LEVEL 7 ====================
    
    /// <summary>
    /// Level 7 DOOMS ability - Reshape environment by telekinetically manipulating matter
    /// </summary>
    public class Verb_MatterControl : Verb_CastBase
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

            // Perform matter manipulation
            float effectiveRange = AbilityPropertyUtility.CalculateEffectiveRange(20f, Props, caster as Pawn);
            PerformMatterManipulation(targetCell, map, effectiveRange);
            
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
            
            return true;
        }

        private void PerformMatterManipulation(IntVec3 center, Map map, float range)
        {
            List<Thing> manipulableThings = GetManipulableThings(center, map, range);
            
            if (!manipulableThings.Any())
            {
                Messages.Message("NoManipulableMatter".Translate(), MessageTypeDefOf.CautionInput);
                return;
            }
            
            // Choose manipulation type based on available materials
            MatterManipulationType manipType = ChooseManipulationType(manipulableThings);
            
            // Execute manipulation
            ExecuteMatterManipulation(center, map, manipulableThings, manipType);
            
            // Create visual effects
            CreateMatterControlEffects(center, map, range);
        }

        private List<Thing> GetManipulableThings(IntVec3 center, Map map, float range)
        {
            return GenRadial.RadialDistinctThingsAround(center, map, range, true)
                .Where(t => CanManipulate(t))
                .ToList();
        }

        private bool CanManipulate(Thing thing)
        {
            // Can manipulate rocks, metals, and building materials
            if (thing.def.category == ThingCategory.Building && thing.def.building?.isNaturalRock == true)
                return true;
            
            if (thing.def.category == ThingCategory.Item && 
                (thing.def.IsStuff || thing.def.thingCategories?.Contains(ThingCategoryDefOf.StoneBlocks) == true))
                return true;
            
            if (thing.def.category == ThingCategory.Building && 
                thing.def.stuffCategories?.Contains(StuffCategoryDefOf.Metallic) == true)
                return true;
            
            return false;
        }

        private MatterManipulationType ChooseManipulationType(List<Thing> things)
        {
            // Prioritize based on available materials and tactical needs
            var rocks = things.Where(t => t.def.building?.isNaturalRock == true).ToList();
            var metals = things.Where(t => t.def.stuffCategories?.Contains(StuffCategoryDefOf.Metallic) == true).ToList();
            var blocks = things.Where(t => t.def.thingCategories?.Contains(ThingCategoryDefOf.StoneBlocks) == true).ToList();
            
            if (rocks.Any() && Rand.Chance(0.4f))
                return MatterManipulationType.CreateBarrier;
            
            if (metals.Any() && Rand.Chance(0.3f))
                return MatterManipulationType.CreateWeapon;
            
            if (blocks.Any())
                return MatterManipulationType.ReshapeStructure;
            
            return MatterManipulationType.MoveObjects;
        }

        private void ExecuteMatterManipulation(IntVec3 center, Map map, List<Thing> things, MatterManipulationType type)
        {
            switch (type)
            {
                case MatterManipulationType.CreateBarrier:
                    CreateBarrierFromMatter(center, map, things);
                    break;
                case MatterManipulationType.CreateWeapon:
                    CreateTemporaryWeapon(center, map, things);
                    break;
                case MatterManipulationType.ReshapeStructure:
                    ReshapeExistingStructures(center, map, things);
                    break;
                case MatterManipulationType.MoveObjects:
                    MoveObjectsTelekinetically(center, map, things);
                    break;
            }
        }

        private void CreateBarrierFromMatter(IntVec3 center, Map map, List<Thing> materials)
        {
            // Create a protective barrier around the target area
            var barrierCells = GenAdj.CellsAdjacent8Way(new TargetInfo(center, map))
                .Where(c => c.InBounds(map) && c.Standable(map))
                .Take(6)
                .ToList();
            
            foreach (IntVec3 cell in barrierCells)
            {
                if (materials.Any())
                {
                    Thing material = materials.First();
                    
                    // Create barrier segment
                    ThingDef barrierDef = DefDatabase<ThingDef>.GetNamedSilentFail("Wall") ?? ThingDefOf.Wall;
                    Thing barrier = ThingMaker.MakeThing(barrierDef, material.Stuff);
                    
                    GenPlace.TryPlaceThing(barrier, cell, map, ThingPlaceMode.Direct);
                    
                    // Consume some material
                    material.stackCount -= 1;
                    if (material.stackCount <= 0)
                    {
                        materials.Remove(material);
                        material.Destroy();
                    }
                    
                    // Visual effect
                    FleckMaker.ThrowDustPuff(cell, map, 1.5f);
                }
            }
            
            Messages.Message("MatterControlBarrierCreated".Translate(), 
                new TargetInfo(center, map), MessageTypeDefOf.PositiveEvent);
        }

        private void CreateTemporaryWeapon(IntVec3 center, Map map, List<Thing> materials)
        {
            // Create a temporary metal projectile weapon
            Thing weapon = ThingMaker.MakeThing(ThingDefOf.MeleeWeapon_Spear);
            
            // Enhance weapon based on materials used
            var metalMaterials = materials.Where(m => 
                m.def.stuffCategories?.Contains(StuffCategoryDefOf.Metallic) == true).ToList();
            
            if (metalMaterials.Any())
            {
                Thing metal = metalMaterials.First();
                weapon = ThingMaker.MakeThing(ThingDefOf.MeleeWeapon_Spear, metal.Stuff);
                
                // Consume material
                metal.stackCount -= 5;
                if (metal.stackCount <= 0)
                {
                    metal.Destroy();
                }
            }
            
            GenPlace.TryPlaceThing(weapon, center, map, ThingPlaceMode.Near);
            
            // Schedule weapon dissolution
            Find.TickManager.later.ScheduleCallback(() => {
                if (!weapon.Destroyed)
                {
                    FleckMaker.ThrowSmoke(weapon.Position.ToVector3Shifted(), weapon.Map, 1f);
                    weapon.Destroy();
                }
            }, 60000); // 1 minute
            
            Messages.Message("MatterControlWeaponCreated".Translate(), 
                new TargetInfo(center, map), MessageTypeDefOf.PositiveEvent);
        }

        private void ReshapeExistingStructures(IntVec3 center, Map map, List<Thing> structures)
        {
            // Repair or enhance existing structures
            var buildings = structures.Where(s => s.def.category == ThingCategory.Building).ToList();
            
            foreach (Thing building in buildings.Take(3))
            {
                if (building.HitPoints < building.MaxHitPoints)
                {
                    // Repair structure
                    int healAmount = Mathf.RoundToInt(building.MaxHitPoints * 0.5f);
                    building.HitPoints = Math.Min(building.MaxHitPoints, building.HitPoints + healAmount);
                    
                    FleckMaker.ThrowSmoke(building.Position.ToVector3Shifted(), map, 1f);
                    FleckMaker.Static(building.Position, map, FleckDefOf.Mote_ItemSparkle, 2f);
                }
            }
            
            Messages.Message("MatterControlStructuresRepaired".Translate(buildings.Count), 
                new TargetInfo(center, map), MessageTypeDefOf.PositiveEvent);
        }

        private void MoveObjectsTelekinetically(IntVec3 center, Map map, List<Thing> objects)
        {
            // Move objects in a defensive pattern around the caster
            Pawn casterPawn = caster as Pawn;
            if (casterPawn == null) return;
            
            foreach (Thing obj in objects.Take(8))
            {
                if (obj.def.category == ThingCategory.Item)
                {
                    // Move object to form a protective circle
                    Vector3 direction = (obj.Position - casterPawn.Position).ToVector3().normalized;
                    IntVec3 newPos = casterPawn.Position + (direction * 3f).ToIntVec3();
                    
                    if (newPos.InBounds(map) && newPos.Walkable(map))
                    {
                        obj.Position = newPos;
                        FleckMaker.ThrowDustPuff(newPos, map, 1f);
                    }
                }
            }
            
            Messages.Message("MatterControlObjectsMoved".Translate(), 
                new TargetInfo(center, map), MessageTypeDefOf.PositiveEvent);
        }

        private void CreateMatterControlEffects(IntVec3 center, Map map, float range)
        {
            // Central manipulation point
            FleckMaker.Static(center, map, FleckDefOf.PsycastPsychicEffect, 3f);
            
            // Telekinetic energy streams
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6f;
                Vector3 streamPos = center.ToVector3Shifted() + 
                    new Vector3(Mathf.Cos(angle) * range * 0.5f, 0f, Mathf.Sin(angle) * range * 0.5f);
                
                if (streamPos.ToIntVec3().InBounds(map))
                {
                    FleckMaker.ConnectingLine(center.ToVector3Shifted(), streamPos, FleckDefOf.AirPuff, map);
                }
            }
            
            // Sound effect
            SoundDefOf.PsycastPsychicPulse.PlayOneShot(new TargetInfo(center, map));
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
            drift.Severity = Props?.beachDriftSeverity ?? 0.6f;
            pawn.health.AddHediff(drift);
            
            Messages.Message("BeachDriftTriggered".Translate(pawn.LabelShort),
                pawn, MessageTypeDefOf.NegativeEvent);
        }
    }

    // ==================== BEACH STEP - LEVEL 7 ====================
    
    /// <summary>
    /// Level 7 DOOMS ability - Teleport through the Beach dimension
    /// </summary>
    public class Verb_BeachStep : Verb_CastBase
    {
        private AbilityProperties_DOOMS Props => ability.def.GetModExtension<AbilityProperties_DOOMS>();

        protected override bool TryCastShot()
        {
            IntVec3 targetCell = currentTarget.Cell;
            Map map = caster.Map;
            Pawn pawn = caster as Pawn;
            
            if (pawn == null) return false;
            
            if (!ValidateTarget(targetCell, map, pawn))
                return false;
            
            if (!ConsumeRequiredResources())
                return false;

            // Execute Beach teleportation
            ExecuteBeachStep(pawn, targetCell, map);
            
            return true;
        }

        private bool ValidateTarget(IntVec3 targetCell, Map map, Pawn pawn)
        {
            if (!targetCell.InBounds(map))
            {
                Messages.Message("TargetOutOfBounds".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            
            if (!targetCell.Walkable(map) || !targetCell.Standable(map))
            {
                Messages.Message("CannotTeleportHere".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            
            // Check distance for drift risk calculation
            float distance = pawn.Position.DistanceTo(targetCell);
            float maxSafeDistance = 15f + DeathStrandingUtility.GetDOOMSLevel(pawn) * 2f;
            
            if (distance > maxSafeDistance)
            {
                Messages.Message("BeachStepDistanceWarning".Translate(), MessageTypeDefOf.CautionInput);
            }
            
            return true;
        }

        private void ExecuteBeachStep(Pawn pawn, IntVec3 destination, Map map)
        {
            IntVec3 origin = pawn.Position;
            float distance = origin.DistanceTo(destination);
            
            // Pre-teleportation effects
            CreateBeachEntryEffects(origin, map);
            
            // Brief Beach drift during teleportation
            ApplyTeleportationDrift(pawn);
            
            // Execute teleportation
            pawn.Position = destination;
            pawn.Notify_Teleported();
            
            // Post-teleportation effects
            CreateBeachExitEffects(destination, map);
            
            // Calculate and apply drift risk
            float driftRisk = CalculateTeleportationDriftRisk(distance, pawn);
            if (Rand.Chance(driftRisk))
            {
                ApplyExtendedBeachDrift(pawn, distance);
            }
            
            Messages.Message("BeachStepCompleted".Translate(pawn.LabelShort), 
                new TargetInfo(destination, map), MessageTypeDefOf.PositiveEvent);
        }

        private void CreateBeachEntryEffects(IntVec3 position, Map map)
        {
            // Dimensional rift opening
            FleckMaker.Static(position, map, FleckDefOf.PsycastPsychicEffect, 2f);
            
            // Expanding dark energy
            for (int i = 0; i < 8; i++)
            {
                Vector3 effectPos = position.ToVector3Shifted() + new Vector3(
                    Mathf.Cos(i * Mathf.PI / 4f) * 2f,
                    0f,
                    Mathf.Sin(i * Mathf.PI / 4f) * 2f
                );
                FleckMaker.ThrowSmoke(effectPos, map, 1.5f);
            }
            
            // Sound effect
            SoundDefOf.PsycastPsychicEffect.PlayOneShot(new TargetInfo(position, map));
        }

        private void CreateBeachExitEffects(IntVec3 position, Map map)
        {
            // Materialization effects
            FleckMaker.Static(position, map, FleckDefOf.ExplosionFlash, 1.5f);
            
            // Beach mist dissipating
            for (int i = 0; i < 5; i++)
            {
                Vector3 mistPos = position.ToVector3Shifted() + new Vector3(
                    Rand.Range(-1.5f, 1.5f),
                    0f,
                    Rand.Range(-1.5f, 1.5f)
                );
                FleckMaker.ThrowAirPuffUp(mistPos, map);
            }
        }

        private void ApplyTeleportationDrift(Pawn pawn)
        {
            // Brief, mild drift during teleportation
            Hediff tempDrift = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.BeachDrift, pawn);
            tempDrift.Severity = 0.1f;
            pawn.health.AddHediff(tempDrift);
            
            // Schedule automatic removal
            Find.TickManager.later.ScheduleCallback(() => {
                if (!tempDrift.pawn.Dead && tempDrift.pawn.health.hediffSet.HasHediff(tempDrift.def))
                {
                    pawn.health.RemoveHediff(tempDrift);
                }
            }, 300); // 5 seconds
        }

        private float CalculateTeleportationDriftRisk(float distance, Pawn pawn)
        {
            float baseRisk = distance * 0.02f; // 2% per cell
            
            // DOOMS level provides some protection
            int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(pawn);
            float protection = doomsLevel * 0.01f; // 1% protection per level
            
            baseRisk = Math.Max(0.01f, baseRisk - protection);
            
            // Environmental modifiers
            if (DeathStrandingUtility.IsTimefallActive(pawn.Map))
            {
                baseRisk *= 1.5f; // Higher risk during timefall
            }
            
            if (DeathStrandingUtility.IsUnderChiralProtection(pawn.Position, pawn.Map))
            {
                baseRisk *= 0.7f; // Lower risk under protection
            }
            
            return Math.Min(0.5f, baseRisk);
        }

        private void ApplyExtendedBeachDrift(Pawn pawn, float distance)
        {
            float severity = Math.Min(0.8f, 0.2f + distance * 0.02f);
            
            Hediff drift = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.BeachDrift, pawn);
            drift.Severity = severity;
            pawn.health.AddHediff(drift);
            
            Messages.Message("BeachStepDriftExtended".Translate(pawn.LabelShort, (severity * 100f).ToString("F0")),
                pawn, MessageTypeDefOf.NegativeEvent);
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

    // ==================== SUPPORTING ENUMS ====================
    
    public enum MatterManipulationType
    {
        CreateBarrier,
        CreateWeapon,
        ReshapeStructure,
        MoveObjects
    }
}