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
    // ==================== REALITY FRACTURE - LEVEL 8 HIGGS ====================
    
    /// <summary>
    /// Level 8 Higgs ability - Split reality to allow multiple simultaneous actions
    /// </summary>
    public class Verb_RealityFracture : Verb_CastBase
    {
        private AbilityProperties_DOOMS Props => ability.def.GetModExtension<AbilityProperties_DOOMS>();

        protected override bool TryCastShot()
        {
            Pawn pawn = caster as Pawn;
            if (pawn == null) return false;
            
            if (!ConsumeRequiredResources())
                return false;

            // Execute reality fracture
            ExecuteRealityFracture(pawn);
            
            // Guaranteed Beach drift afterward
            ApplyGuaranteedBeachDrift(pawn);
            
            return true;
        }

        private void ExecuteRealityFracture(Pawn pawn)
        {
            // Create reality fracture effects
            CreateRealityFractureEffects(pawn.Position, pawn.Map);
            
            // Grant multiple action points
            GrantMultipleActions(pawn);
            
            // Apply reality distortion field
            ApplyRealityDistortionField(pawn);
            
            Messages.Message("RealityFractureActivated".Translate(pawn.LabelShort), 
                pawn, MessageTypeDefOf.PositiveEvent);
        }

        private void CreateRealityFractureEffects(IntVec3 position, Map map)
        {
            // Central reality tear
            FleckMaker.Static(position, map, FleckDefOf.ExplosionFlash, 5f);
            
            // Reality fracture lines spreading outward
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI / 8f;
                for (int dist = 1; dist <= 8; dist++)
                {
                    Vector3 fracturePos = position.ToVector3Shifted() + 
                        new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                    
                    if (fracturePos.ToIntVec3().InBounds(map))
                    {
                        FleckMaker.ThrowLightningGlow(fracturePos, map, 2f);
                    }
                }
            }
            
            // Dimensional distortion waves
            for (int wave = 1; wave <= 5; wave++)
            {
                int pointsOnWave = wave * 8;
                for (int point = 0; point < pointsOnWave; point++)
                {
                    float angle = (float)point / pointsOnWave * 2f * Mathf.PI;
                    Vector3 wavePos = position.ToVector3Shifted() + 
                        new Vector3(Mathf.Cos(angle) * wave * 3f, 0f, Mathf.Sin(angle) * wave * 3f);
                    
                    if (wavePos.ToIntVec3().InBounds(map))
                    {
                        FleckMaker.Static(wavePos.ToIntVec3(), map, FleckDefOf.PsycastPsychicEffect, 1f);
                    }
                }
            }
            
            // Reality-bending sound
            SoundDefOf.PsycastPsychicPulse.PlayOneShot(new TargetInfo(position, map));
        }

        private void GrantMultipleActions(Pawn pawn)
        {
            // Grant 3 additional action points through custom effect
            Hediff realityBoost = HediffMaker.MakeHediff(
                DefDatabase<HediffDef>.GetNamedSilentFail("RealityFractureBoost"), pawn);
            
            if (realityBoost != null)
            {
                realityBoost.Severity = 1.0f;
                pawn.health.AddHediff(realityBoost);
            }
            
            // Alternative: Modify pawn's action economy directly
            if (pawn.drafter != null)
            {
                pawn.drafter.Drafted = true;
            }
            
            // Reset cooldowns on other abilities
            ResetAbilityCooldowns(pawn);
        }

        private void ResetAbilityCooldowns(Pawn pawn)
        {
            if (pawn.abilities?.abilities == null) return;
            
            foreach (var ability in pawn.abilities.abilities)
            {
                if (ability.def.defName.StartsWith("DOOMS_"))
                {
                    ability.StartCooldown(0); // Reset cooldown
                }
            }
        }

        private void ApplyRealityDistortionField(Pawn pawn)
        {
            // Create a field that affects the local area
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, 10f, true))
            {
                if (!cell.InBounds(pawn.Map)) continue;
                
                // Apply temporal distortion effects to the area
                List<Thing> things = cell.GetThingList(pawn.Map);
                foreach (Thing thing in things)
                {
                    if (thing is Pawn otherPawn && otherPawn != pawn)
                    {
                        ApplyRealityDistortionToPawn(otherPawn);
                    }
                    else if (thing.def.category == ThingCategory.Building)
                    {
                        ApplyRealityDistortionToBuilding(thing);
                    }
                }
            }
        }

        private void ApplyRealityDistortionToPawn(Pawn target)
        {
            // Slow down other pawns in the field
            Hediff slowness = HediffMaker.MakeHediff(HediffDefOf.PsychicHangover, target);
            slowness.Severity = 0.5f;
            target.health.AddHediff(slowness);
            
            // Visual effect on affected pawn
            FleckMaker.ThrowAirPuffUp(target.DrawPos, target.Map);
        }

        private void ApplyRealityDistortionToBuilding(Thing building)
        {
            // Temporarily disable some building functions
            if (building.TryGetComp<CompPowerTrader>() is CompPowerTrader powerComp)
            {
                // Temporarily reduce power efficiency
                powerComp.PowerOutput *= 0.5f;
                
                // Schedule restoration
                Find.TickManager.later.ScheduleCallback(() => {
                    if (!building.Destroyed && powerComp != null)
                    {
                        powerComp.PowerOutput *= 2f; // Restore
                    }
                }, 30000); // 30 seconds
            }
        }

        private void ApplyGuaranteedBeachDrift(Pawn pawn)
        {
            Hediff drift = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.BeachDrift, pawn);
            drift.Severity = 1.0f; // Maximum drift
            pawn.health.AddHediff(drift);
            
            Messages.Message("RealityFractureBeachDrift".Translate(pawn.LabelShort),
                pawn, MessageTypeDefOf.ThreatBig);
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

    // ==================== MASS LEVITATION - LEVEL 8 HIGGS ====================
    
    /// <summary>
    /// Level 8 Higgs ability - Control everything in large radius simultaneously
    /// </summary>
    public class Verb_MassLevitation : Verb_CastBase
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

            // Execute mass levitation
            float effectiveRange = AbilityPropertyUtility.CalculateEffectiveRange(15f, Props, caster as Pawn);
            ExecuteMassLevitation(targetCell, map, effectiveRange);
            
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
            
            // Warn about massive area effect
            Messages.Message("MassLevitationWarning".Translate(), MessageTypeDefOf.CautionInput);
            
            return true;
        }

        private void ExecuteMassLevitation(IntVec3 center, Map map, float radius)
        {
            // Create massive levitation field
            CreateLevitationFieldEffects(center, map, radius);
            
            // Levitate everything in range
            var levitatedThings = LevitateObjectsInRange(center, map, radius);
            
            // Control levitated objects
            ControlLevitatedObjects(center, map, levitatedThings);
            
            // Schedule object return
            ScheduleObjectReturn(levitatedThings);
        }

        private void CreateLevitationFieldEffects(IntVec3 center, Map map, float radius)
        {
            // Central levitation nexus
            FleckMaker.Static(center, map, FleckDefOf.ExplosionFlash, 6f);
            
            // Levitation field visualization
            for (int ring = 1; ring <= 5; ring++)
            {
                float ringRadius = radius * ring / 5f;
                int pointsOnRing = Mathf.RoundToInt(ringRadius * 6);
                
                for (int point = 0; point < pointsOnRing; point++)
                {
                    float angle = (float)point / pointsOnRing * 2f * Mathf.PI;
                    Vector3 ringPos = center.ToVector3Shifted() + 
                        new Vector3(Mathf.Cos(angle) * ringRadius, 1f, Mathf.Sin(angle) * ringRadius);
                    
                    if (ringPos.ToIntVec3().InBounds(map))
                    {
                        FleckMaker.ThrowLightningGlow(ringPos, map, 2f);
                    }
                }
            }
            
            // Upward energy streams
            for (int i = 0; i < 20; i++)
            {
                Vector3 streamPos = center.ToVector3Shifted() + new Vector3(
                    Rand.Range(-radius, radius),
                    0f,
                    Rand.Range(-radius, radius)
                );
                
                if (streamPos.ToIntVec3().InBounds(map))
                {
                    FleckMaker.ThrowAirPuffUp(streamPos, map);
                }
            }
        }

        private List<Thing> LevitateObjectsInRange(IntVec3 center, Map map, float radius)
        {
            var levitatedThings = new List<Thing>();
            
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map)) continue;
                
                List<Thing> cellThings = cell.GetThingList(map).ToList();
                foreach (Thing thing in cellThings)
                {
                    if (CanLevitate(thing))
                    {
                        LevitateObject(thing);
                        levitatedThings.Add(thing);
                    }
                }
            }
            
            Messages.Message("MassLevitationObjectsLifted".Translate(levitatedThings.Count), 
                new TargetInfo(center, map), MessageTypeDefOf.PositiveEvent);
            
            return levitatedThings;
        }

        private bool CanLevitate(Thing thing)
        {
            // Can levitate most things except very heavy structures
            if (thing.def.category == ThingCategory.Pawn && thing is Pawn pawn)
            {
                return pawn != caster; // Don't levitate self
            }
            
            if (thing.def.category == ThingCategory.Item)
                return true;
            
            if (thing.def.category == ThingCategory.Building && 
                !thing.def.building.isNaturalRock && 
                thing.def.size.x <= 2 && thing.def.size.z <= 2)
                return true;
            
            return false;
        }

        private void LevitateObject(Thing thing)
        {
            // Apply levitation effect
            FleckMaker.ThrowAirPuffUp(thing.DrawPos, thing.Map, 2f);
            
            if (thing is Pawn pawn)
            {
                // Apply levitation hediff to pawns
                Hediff levitation = HediffMaker.MakeHediff(
                    DefDatabase<HediffDef>.GetNamedSilentFail("Levitation") ?? HediffDefOf.PsychicHangover, 
                    pawn);
                levitation.Severity = 0.8f;
                pawn.health.AddHediff(levitation);
                
                // Prevent movement
                pawn.stances.SetStance(new Stance_Cooldown(1800, pawn, null)); // 30 seconds
            }
        }

        private void ControlLevitatedObjects(IntVec3 center, Map map, List<Thing> objects)
        {
            // Organize objects into defensive/offensive patterns
            OrganizeDefensiveFormation(center, map, objects.OfType<Pawn>().ToList());
            OrganizeProjectileFormation(center, map, objects.Where(t => t.def.category == ThingCategory.Item).ToList());
            
            // Launch coordinated attacks if enemies present
            var enemies = map.mapPawns.AllPawnsSpawned
                .Where(p => p.HostileTo(caster.Faction))
                .ToList();
            
            if (enemies.Any())
            {
                LaunchCoordinatedAttack(enemies, objects);
            }
        }

        private void OrganizeDefensiveFormation(IntVec3 center, Map map, List<Pawn> levitatedPawns)
        {
            // Arrange friendly pawns in protective circle
            var friendlyPawns = levitatedPawns.Where(p => !p.HostileTo(caster.Faction)).ToList();
            
            for (int i = 0; i < friendlyPawns.Count; i++)
            {
                float angle = (float)i / friendlyPawns.Count * 2f * Mathf.PI;
                IntVec3 formationPos = center + new Vector3(
                    Mathf.Cos(angle) * 4f,
                    0f,
                    Mathf.Sin(angle) * 4f
                ).ToIntVec3();
                
                if (formationPos.InBounds(map) && formationPos.Walkable(map))
                {
                    friendlyPawns[i].Position = formationPos;
                    friendlyPawns[i].Notify_Teleported();
                    
                    FleckMaker.ThrowDustPuff(formationPos, map, 1f);
                }
            }
        }

        private void OrganizeProjectileFormation(IntVec3 center, Map map, List<Thing> items)
        {
            // Arrange items as floating projectiles
            var projectileItems = items.Where(t => t.def.projectile != null || t.MarketValue > 5f).Take(10).ToList();
            
            for (int i = 0; i < projectileItems.Count; i++)
            {
                float angle = (float)i / projectileItems.Count * 2f * Mathf.PI;
                IntVec3 orbitPos = center + new Vector3(
                    Mathf.Cos(angle) * 6f,
                    0f,
                    Mathf.Sin(angle) * 6f
                ).ToIntVec3();
                
                if (orbitPos.InBounds(map))
                {
                    projectileItems[i].Position = orbitPos;
                    FleckMaker.ThrowLightningGlow(orbitPos.ToVector3Shifted(), map, 1f);
                }
            }
        }

        private void LaunchCoordinatedAttack(List<Pawn> enemies, List<Thing> weaponizedObjects)
        {
            var projectiles = weaponizedObjects
                .Where(t => t.def.category == ThingCategory.Item)
                .Take(enemies.Count * 2)
                .ToList();
            
            foreach (Pawn enemy in enemies.Take(5)) // Limit for performance
            {
                // Launch multiple objects at each enemy
                for (int i = 0; i < 2 && projectiles.Any(); i++)
                {
                    Thing projectile = projectiles.First();
                    projectiles.Remove(projectile);
                    
                    LaunchProjectileAtTarget(projectile, enemy);
                }
            }
        }

        private void LaunchProjectileAtTarget(Thing projectile, Pawn target)
        {
            // Calculate damage based on projectile mass and speed
            float damage = Math.Min(30f, projectile.def.BaseMass * 20f);
            
            // Apply damage to target
            target.TakeDamage(new DamageInfo(DamageDefOf.Blunt, damage));
            
            // Visual effect
            FleckMaker.ConnectingLine(
                projectile.DrawPos, 
                target.DrawPos, 
                FleckDefOf.AirPuff, 
                projectile.Map
            );
            
            // Destroy projectile
            projectile.Destroy();
        }

        private void ScheduleObjectReturn(List<Thing> objects)
        {
            // Return objects to ground after 30 seconds
            Find.TickManager.later.ScheduleCallback(() => {
                foreach (Thing thing in objects)
                {
                    if (!thing.Destroyed)
                    {
                        if (thing is Pawn pawn)
                        {
                            // Remove levitation effects
                            var levitationHediff = pawn.health.hediffSet.hediffs
                                .FirstOrDefault(h => h.def.defName.Contains("Levitation"));
                            if (levitationHediff != null)
                            {
                                pawn.health.RemoveHediff(levitationHediff);
                            }
                        }
                        
                        FleckMaker.ThrowDustPuff(thing.Position, thing.Map, 1.5f);
                    }
                }
            }, 30000);
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
            drift.Severity = Props?.beachDriftSeverity ?? 0.8f;
            pawn.health.AddHediff(drift);
            
            Messages.Message("BeachDriftTriggered".Translate(pawn.LabelShort),
                pawn, MessageTypeDefOf.NegativeEvent);
        }
    }

    // ==================== TEMPORAL DISPLACEMENT - LEVEL 8 HIGGS ====================
    
    /// <summary>
    /// Level 8 Higgs ability - Slow time for everyone except the user
    /// </summary>
    public class Verb_TemporalDisplacement : Verb_CastBase
    {
        private AbilityProperties_DOOMS Props => ability.def.GetModExtension<AbilityProperties_DOOMS>();

        protected override bool TryCastShot()
        {
            Pawn pawn = caster as Pawn;
            if (pawn == null) return false;
            
            if (!ConsumeRequiredResources())
                return false;

            // Execute temporal displacement
            ExecuteTemporalDisplacement(pawn);
            
            // Apply Beach drift risk
            ApplyBeachDriftRisk();
            
            return true;
        }

        private void ExecuteTemporalDisplacement(Pawn caster)
        {
            // Create temporal field effects
            CreateTemporalFieldEffects(caster.Position, caster.Map);
            
            // Apply time dilation to all entities except caster
            ApplyTimeDialation(caster);
            
            // Grant caster enhanced time perception
            GrantTimePerceptionBonus(caster);
            
            // Schedule effect end
            ScheduleTemporalReturn(caster);
            
            Messages.Message("TemporalDisplacementActivated".Translate(caster.LabelShort), 
                caster, MessageTypeDefOf.PositiveEvent);
        }

        private void CreateTemporalFieldEffects(IntVec3 center, Map map)
        {
            // Central temporal nexus
            FleckMaker.Static(center, map, FleckDefOf.ExplosionFlash, 4f);
            
            // Time ripple effects expanding outward
            for (int wave = 1; wave <= 8; wave++)
            {
                float waveRadius = wave * 5f;
                int pointsOnWave = Mathf.RoundToInt(waveRadius * 4);
                
                for (int point = 0; point < pointsOnWave; point++)
                {
                    float angle = (float)point / pointsOnWave * 2f * Mathf.PI;
                    Vector3 wavePos = center.ToVector3Shifted() + 
                        new Vector3(Mathf.Cos(angle) * waveRadius, 0f, Mathf.Sin(angle) * waveRadius);
                    
                    if (wavePos.ToIntVec3().InBounds(map))
                    {
                        FleckMaker.Static(wavePos.ToIntVec3(), map, FleckDefOf.PsycastPsychicEffect, 
                            1f + wave * 0.2f);
                    }
                }
            }
            
            // Temporal distortion grid
            for (int x = -20; x <= 20; x += 4)
            {
                for (int z = -20; z <= 20; z += 4)
                {
                    IntVec3 gridPos = center + new IntVec3(x, 0, z);
                    if (gridPos.InBounds(map))
                    {
                        FleckMaker.ThrowLightningGlow(gridPos.ToVector3Shifted(), map, 0.5f);
                    }
                }
            }
        }

        private void ApplyTimeDialation(Pawn caster)
        {
            Map map = caster.Map;
            
            // Slow down all pawns except caster
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn == caster) continue;
                
                ApplyTimeSlowness(pawn);
            }
            
            // Slow down mechanical processes
            SlowMechanicalProcesses(map);
            
            // Affect projectiles and other moving objects
            SlowMovingObjects(map);
        }

        private void ApplyTimeSlowness(Pawn target)
        {
            // Apply severe movement reduction
            Hediff timeSlowness = HediffMaker.MakeHediff(
                DefDatabase<HediffDef>.GetNamedSilentFail("TemporalSlowness") ?? HediffDefOf.PsychicHangover, 
                target);
            timeSlowness.Severity = 0.9f; // Severe slowness
            target.health.AddHediff(timeSlowness);
            
            // Visual effect on slowed pawns
            FleckMaker.ThrowAirPuffUp(target.DrawPos, target.Map);
            
            // Freeze them in place temporarily
            target.stances.SetStance(new Stance_Cooldown(1800, target, null)); // 30 seconds
        }

        private void SlowMechanicalProcesses(Map map)
        {
            // Slow down buildings with mechanical processes
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (building.def.building.isMachine)
                {
                    // Temporarily reduce efficiency
                    var powerComp = building.TryGetComp<CompPowerTrader>();
                    if (powerComp != null)
                    {
                        powerComp.PowerOutput *= 0.1f; // 10% speed
                    }
                    
                    // Visual effect
                    FleckMaker.ThrowSmoke(building.Position.ToVector3Shifted(), map, 1f);
                }
            }
        }

        private void SlowMovingObjects(Map map)
        {
            // Affect projectiles and other kinetic objects
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing.def.category == ThingCategory.Projectile)
                {
                    // Dramatically slow projectiles
                    FleckMaker.ThrowLightningGlow(thing.DrawPos, map, 0.5f);
                }
            }
        }

        private void GrantTimePerceptionBonus(Pawn caster)
        {
            // Grant enhanced perception and reaction time
            Hediff timePerception = HediffMaker.MakeHediff(
                DefDatabase<HediffDef>.GetNamedSilentFail("EnhancedTimePerception") ?? HediffDefOf.PsychicHangover, 
                caster);
            timePerception.Severity = -0.8f; // Negative severity for bonus
            caster.health.AddHediff(timePerception);
            
            // Reset ability cooldowns for enhanced action economy
            if (caster.abilities?.abilities != null)
            {
                foreach (var ability in caster.abilities.abilities)
                {
                    if (ability.def.defName.StartsWith("DOOMS_"))
                    {
                        ability.StartCooldown(ability.def.cooldownTicksRange.min / 4); // 75% cooldown reduction
                    }
                }
            }
        }

        private void ScheduleTemporalReturn(Pawn caster)
        {
            // Return time to normal after 30 seconds
            Find.TickManager.later.ScheduleCallback(() => {
                RestoreNormalTime(caster.Map);
                
                Messages.Message("TemporalDisplacementEnded".Translate(), 
                    MessageTypeDefOf.PositiveEvent);
                    
            }, 30000); // 30 seconds
        }

        private void RestoreNormalTime(Map map)
        {
            // Remove time effects from all pawns
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                var timeEffects = pawn.health.hediffSet.hediffs
                    .Where(h => h.def.defName.Contains("Temporal") || h.def.defName.Contains("TimePerception"))
                    .ToList();
                
                foreach (Hediff hediff in timeEffects)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
            
            // Restore building efficiency
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                var powerComp = building.TryGetComp<CompPowerTrader>();
                if (powerComp != null && powerComp.PowerOutput < 0)
                {
                    powerComp.PowerOutput *= 10f; // Restore to normal
                }
            }
            
            // Final restoration effect
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(map.Center, 50f, false))
            {
                if (cell.InBounds(map) && Rand.Chance(0.1f))
                {
                    FleckMaker.ThrowAirPuffUp(cell.ToVector3Shifted(), map);
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
            drift.Severity = Props?.beachDriftSeverity ?? 0.9f;
            pawn.health.AddHediff(drift);
            
            Messages.Message("BeachDriftTriggered".Translate(pawn.LabelShort),
                pawn, MessageTypeDefOf.NegativeEvent);
        }
    }
}