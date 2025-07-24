using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using DeathStrandingMod.Core;

namespace DeathStrandingMod.Incidents
{
    /// <summary>
    /// Handles voidout catastrophe events - the ultimate consequence of BT tether overload
    /// </summary>
    public class IncidentWorker_Voidout : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            
            // Require at least one colonist with critical tether or high DOOMS level
            return map.mapPawns.FreeColonists.Any(p => 
                HasCriticalTether(p) || HasHighDOOMSLevel(p));
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            
            // Find trigger source (critically tethered pawn or high-level DOOMS user)
            Pawn triggerPawn = FindVoidoutTrigger(map, parms);
            if (triggerPawn == null)
            {
                return false;
            }

            VoidoutProperties props = def.GetModExtension<VoidoutProperties>() ?? new VoidoutProperties();
            
            // Calculate voidout parameters
            IntVec3 center = parms.spawnCenter.IsValid ? parms.spawnCenter : triggerPawn.Position;
            float radius = CalculateVoidoutRadius(triggerPawn, props);
            bool isGameEnding = ShouldBeGameEnding(triggerPawn, radius, map, props);

            // Execute the voidout
            ExecuteVoidout(center, radius, map, triggerPawn, isGameEnding, props);
            
            return true;
        }

        private Pawn FindVoidoutTrigger(Map map, IncidentParms parms)
        {
            // Priority 1: Pawns with critical BT tether
            var criticalTetherPawns = map.mapPawns.AllPawnsSpawned
                .Where(p => HasCriticalTether(p))
                .ToList();
            
            if (criticalTetherPawns.Any())
            {
                return criticalTetherPawns.RandomElement();
            }
            
            // Priority 2: High-level DOOMS users who recently used dangerous abilities
            var doomsPawns = map.mapPawns.FreeColonists
                .Where(p => HasHighDOOMSLevel(p))
                .ToList();
            
            if (doomsPawns.Any())
            {
                return doomsPawns.RandomElement();
            }
            
            return null;
        }

        private bool HasCriticalTether(Pawn pawn)
        {
            Hediff tether = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
            return tether?.Severity >= 0.9f;
        }

        private bool HasHighDOOMSLevel(Pawn pawn)
        {
            if (pawn.genes == null) return false;
            
            return pawn.genes.GenesListForReading
                .Any(g => g.def.defName.StartsWith("DOOMS_") && 
                     g.def.GetModExtension<DOOMSProperties>()?.level >= 6);
        }

        private float CalculateVoidoutRadius(Pawn triggerPawn, VoidoutProperties props)
        {
            float baseRadius = props.baseRadius;
            
            // Increase radius based on DOOMS level
            int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(triggerPawn);
            if (doomsLevel >= 8)
            {
                baseRadius *= 2.5f; // Higgs-level causes massive voidouts
            }
            else if (doomsLevel >= 6)
            {
                baseRadius *= 1.8f;
            }
            
            // Increase radius based on tether severity
            Hediff tether = triggerPawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
            if (tether != null)
            {
                baseRadius *= (1f + tether.Severity);
            }
            
            return Math.Min(baseRadius, props.maxRadius);
        }

        private bool ShouldBeGameEnding(Pawn triggerPawn, float radius, Map map, VoidoutProperties props)
        {
            if (!props.gameEndingRisk) return false;
            
            // Game ending if:
            // 1. Radius is very large (>30 cells)
            // 2. Trigger pawn is Level 8 DOOMS
            // 3. Multiple critical infrastructure destroyed
            
            int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(triggerPawn);
            bool massiveRadius = radius > 30f;
            bool higgsLevel = doomsLevel >= 8;
            
            // Check if voidout would destroy critical colony infrastructure
            var criticalBuildings = GenRadial.RadialDistinctThingsAround(triggerPawn.Position, map, radius, true)
                .Where(t => t.def.category == ThingCategory.Building && 
                           (t.def.building?.isNaturalRock == false))
                .Count();
            
            bool destroysColony = criticalBuildings > 20; // Arbitrary threshold
            
            return (massiveRadius && higgsLevel) || (destroysColony && higgsLevel) || Rand.Chance(0.1f);
        }

        private void ExecuteVoidout(IntVec3 center, float radius, Map map, Pawn triggerPawn, bool isGameEnding, VoidoutProperties props)
        {
            // Pre-voidout warning
            SendVoidoutWarning(triggerPawn, radius, isGameEnding);
            
            // Create dramatic buildup effects
            CreateVoidoutBuildup(center, map, radius);
            
            // Wait a moment for dramatic effect
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            
            // Execute the actual voidout
            PerformVoidoutExplosion(center, radius, map, props);
            
            // Handle aftermath
            HandleVoidoutAftermath(center, radius, map, triggerPawn, isGameEnding, props);
        }

        private void SendVoidoutWarning(Pawn triggerPawn, float radius, bool isGameEnding)
        {
            string messageKey = isGameEnding ? "VoidoutWarningGameEnding" : "VoidoutWarningMajor";
            MessageTypeDef messageType = isGameEnding ? MessageTypeDefOf.ThreatBig : MessageTypeDefOf.ThreatBig;
            
            Messages.Message(
                messageKey.Translate(triggerPawn.LabelShort, radius.ToString("F0")),
                triggerPawn,
                messageType
            );
            
            // Force pause for dramatic effect
            if (isGameEnding)
            {
                Find.TickManager.Pause();
            }
        }

        private void CreateVoidoutBuildup(IntVec3 center, Map map, float radius)
        {
            // Create expanding energy rings
            for (int i = 0; i < 5; i++)
            {
                float ringRadius = radius * (i + 1) / 5f;
                for (int j = 0; j < 8; j++)
                {
                    Vector3 ringPos = center.ToVector3Shifted() + 
                        new Vector3(
                            Mathf.Cos(j * Mathf.PI / 4f) * ringRadius,
                            0f,
                            Mathf.Sin(j * Mathf.PI / 4f) * ringRadius
                        );
                    
                    FleckMaker.Static(ringPos.ToIntVec3(), map, FleckDefOf.PsycastPsychicEffect, 2f);
                }
            }
            
            // Central energy buildup
            FleckMaker.Static(center, map, FleckDefOf.ExplosionFlash, 5f);
            
            // Sound buildup
            SoundDefOf.PsycastPsychicPulse.PlayOneShot(new TargetInfo(center, map));
        }

        private void PerformVoidoutExplosion(IntVec3 center, float radius, Map map, VoidoutProperties props)
        {
            // Main explosion effect
            GenExplosion.DoExplosion(
                center: center,
                map: map,
                radius: radius,
                damType: DamageDefOf_DeathStranding.VoidoutDamage ?? DamageDefOf.Bomb,
                instigator: null,
                damAmount: -1, // Use default
                armorPenetration: 999f, // Penetrates all armor
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
                damageFalloff: false,
                direction: null,
                ignoredThings: null
            );
            
            // Additional visual effects
            CreateVoidoutVisualEffects(center, radius, map);
            
            // Destroy terrain and create crater
            if (props.leaveCrater)
            {
                CreateVoidoutCrater(center, radius, map);
            }
        }

        private void CreateVoidoutVisualEffects(IntVec3 center, float radius, Map map)
        {
            // Massive central flash
            FleckMaker.Static(center, map, FleckDefOf.ExplosionFlash, 10f);
            
            // Expanding shockwave rings
            for (int ring = 1; ring <= 5; ring++)
            {
                float ringRadius = radius * ring / 5f;
                int pointsOnRing = Mathf.RoundToInt(ringRadius * 8);
                
                for (int point = 0; point < pointsOnRing; point++)
                {
                    float angle = (float)point / pointsOnRing * 2f * Mathf.PI;
                    Vector3 effectPos = center.ToVector3Shifted() + 
                        new Vector3(
                            Mathf.Cos(angle) * ringRadius,
                            0f,
                            Mathf.Sin(angle) * ringRadius
                        );
                    
                    if (effectPos.ToIntVec3().InBounds(map))
                    {
                        FleckMaker.Static(effectPos.ToIntVec3(), map, FleckDefOf.Smoke, 3f);
                    }
                }
            }
            
            // Lingering energy distortion at center
            for (int i = 0; i < 20; i++)
            {
                IntVec3 distortionCell = center + GenRadial.RadialPattern[i];
                if (distortionCell.InBounds(map))
                {
                    FleckMaker.ThrowLightningGlow(distortionCell.ToVector3Shifted(), map, 2f);
                }
            }
        }

        private void CreateVoidoutCrater(IntVec3 center, float radius, Map map)
        {
            // Create crater terrain
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius * 0.7f, true))
            {
                if (!cell.InBounds(map)) continue;
                
                // Remove any remaining structures
                List<Thing> thingsToDestroy = cell.GetThingList(map)
                    .Where(t => t.def.category == ThingCategory.Building || 
                               t.def.category == ThingCategory.Plant)
                    .ToList();
                
                foreach (Thing thing in thingsToDestroy)
                {
                    thing.Destroy(DestroyMode.KillFinalize);
                }
                
                // Change terrain to crater
                TerrainDef craterTerrain = DefDatabase<TerrainDef>.GetNamedSilentFail("VoidoutCrater") ?? 
                                         TerrainDefOf.Sand;
                map.terrainGrid.SetTerrain(cell, craterTerrain);
                
                // Remove roof
                if (cell.Roofed(map))
                {
                    map.roofGrid.SetRoof(cell, null);
                }
            }
            
            // Spawn chiral residue around crater edge
            SpawnChiralResidue(center, radius, map);
        }

        private void SpawnChiralResidue(IntVec3 center, float radius, Map map)
        {
            int residueCount = Mathf.RoundToInt(radius * 2f);
            
            for (int i = 0; i < residueCount; i++)
            {
                IntVec3 spawnCell = center + GenRadial.RadialPattern[
                    Rand.Range(Mathf.RoundToInt(radius * 0.8f), Mathf.RoundToInt(radius * 1.2f))];
                
                if (spawnCell.InBounds(map) && spawnCell.Walkable(map))
                {
                    Thing chiralResidue = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
                    chiralResidue.stackCount = Rand.Range(5, 20);
                    GenPlace.TryPlaceThing(chiralResidue, spawnCell, map, ThingPlaceMode.Near);
                    
                    // Special "voidout residue" with enhanced properties
                    if (Rand.Chance(0.2f))
                    {
                        CompQuality quality = chiralResidue.TryGetComp<CompQuality>();
                        quality?.SetQuality(QualityCategory.Excellent, ArtGenerationContext.Colony);
                    }
                }
            }
        }

        private void HandleVoidoutAftermath(IntVec3 center, float radius, Map map, Pawn triggerPawn, bool isGameEnding, VoidoutProperties props)
        {
            // Kill or severely injure the trigger pawn
            if (triggerPawn != null && !triggerPawn.Dead)
            {
                if (isGameEnding || Rand.Chance(0.8f))
                {
                    triggerPawn.Kill(null);
                    Messages.Message(
                        "VoidoutTriggerPawnDied".Translate(triggerPawn.LabelShort),
                        MessageTypeDefOf.Death
                    );
                }
                else
                {
                    // Severe radiation damage
                    Hediff radiation = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.VoidoutRadiation, triggerPawn);
                    radiation.Severity = 0.8f;
                    triggerPawn.health.AddHediff(radiation);
                }
            }
            
            // Apply radiation to survivors in outer radius
            ApplyRadiationToSurvivors(center, radius * 1.5f, map);
            
            // Send aftermath message
            SendAftermathMessage(center, radius, map, isGameEnding);
            
            // Game ending check
            if (isGameEnding)
            {
                HandleGameEndingVoidout(map);
            }
            
            // Long-term effects
            ScheduleLongTermEffects(center, radius, map);
        }

        private void ApplyRadiationToSurvivors(IntVec3 center, float radius, Map map)
        {
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Dead) continue;
                
                float distance = pawn.Position.DistanceTo(center);
                if (distance <= radius)
                {
                    float radiationSeverity = 1f - (distance / radius);
                    radiationSeverity *= 0.5f; // Scale down for survivability
                    
                    if (radiationSeverity > 0.1f)
                    {
                        Hediff radiation = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.VoidoutRadiation, pawn);
                        radiation.Severity = radiationSeverity;
                        pawn.health.AddHediff(radiation);
                    }
                }
            }
        }

        private void SendAftermathMessage(IntVec3 center, float radius, Map map, bool isGameEnding)
        {
            string messageKey = isGameEnding ? "VoidoutAftermathGameEnding" : "VoidoutAftermathMajor";
            
            Find.LetterStack.ReceiveLetter(
                "VoidoutEventTitle".Translate(),
                messageKey.Translate(radius.ToString("F0")),
                isGameEnding ? LetterDefOf.ThreatBig : LetterDefOf.NegativeEvent,
                new TargetInfo(center, map)
            );
        }

        private void HandleGameEndingVoidout(Map map)
        {
            // Check if any colonists survived
            bool anyColonistsSurvived = map.mapPawns.FreeColonists.Any(p => !p.Dead);
            
            if (!anyColonistsSurvived)
            {
                // Total game over
                GameOverUtility.EndGame("Your colony was consumed by a catastrophic voidout. The Beach has claimed all.", false);
            }
            else
            {
                // Severe but not total loss
                Messages.Message(
                    "VoidoutGameEndingButSurvived".Translate(),
                    MessageTypeDefOf.ThreatBig
                );
            }
        }

        private void ScheduleLongTermEffects(IntVec3 center, float radius, Map map)
        {
            // Schedule ongoing BT manifestations in the crater area
            for (int i = 0; i < 5; i++)
            {
                int delay = Rand.Range(60000, 300000); // 1-5 days
                
                Find.Storyteller.incidentQueue.Add(
                    IncidentDefOf_DeathStranding.BTManifestation,
                    Find.TickManager.TicksGame + delay,
                    new IncidentParms 
                    { 
                        target = map, 
                        spawnCenter = center,
                        points = 200f // Moderate threat
                    }
                );
            }
            
            // Schedule chiral crystal formations
            for (int i = 0; i < 3; i++)
            {
                int delay = Rand.Range(120000, 600000); // 2-10 days
                
                Find.Storyteller.incidentQueue.Add(
                    IncidentDefOf_DeathStranding.ChiralCrystalFormation,
                    Find.TickManager.TicksGame + delay,
                    new IncidentParms 
                    { 
                        target = map, 
                        spawnCenter = center 
                    }
                );
            }
        }
    }

    /// <summary>
    /// Specialized voidout for major catastrophic events
    /// </summary>
    public class IncidentWorker_MajorVoidout : IncidentWorker_Voidout
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            // Force larger radius and higher game ending chance
            var props = new VoidoutProperties
            {
                baseRadius = 25f,
                maxRadius = 50f,
                gameEndingRisk = true,
                structureDamageMultiplier = 3.0f,
                leaveCrater = true,
                spawnChiralResidue = true
            };
            
            // Override the def's mod extension temporarily
            var originalExtension = def.modExtensions?.FirstOrDefault(e => e is VoidoutProperties);
            if (originalExtension != null)
            {
                def.modExtensions.Remove(originalExtension);
            }
            def.modExtensions = def.modExtensions ?? new List<DefModExtension>();
            def.modExtensions.Add(props);
            
            bool result = base.TryExecuteWorker(parms);
            
            // Restore original extension
            def.modExtensions.Remove(props);
            if (originalExtension != null)
            {
                def.modExtensions.Add(originalExtension);
            }
            
            return result;
        }
    }
}