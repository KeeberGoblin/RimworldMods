using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using DeathStrandingMod.Core;

namespace DeathStrandingMod.Components
{
    /// <summary>
    /// Component that makes existing creatures behave like BTs
    /// </summary>
    public class CompBTBehavior : ThingComp
    {
        private int lastBehaviorTick = 0;
        private Pawn targetColonist = null;
        private int lastTargetUpdateTick = 0;
        private string btType = "BT_Basic";
        private bool isActive = true;
        
        private const int BEHAVIOR_UPDATE_INTERVAL = 250; // Every ~4 seconds
        private const int TARGET_UPDATE_INTERVAL = 500;   // Every ~8 seconds

        public string BTType 
        { 
            get => btType; 
            set => btType = value; 
        }

        public bool IsActive 
        { 
            get => isActive; 
            set => isActive = value; 
        }

        public override void CompTick()
        {
            base.CompTick();
            
            if (!isActive || !(parent is Pawn btProxy))
                return;
            
            int currentTick = Find.TickManager.TicksGame;
            
            // Update target periodically
            if (currentTick - lastTargetUpdateTick > TARGET_UPDATE_INTERVAL)
            {
                UpdateTarget(btProxy);
                lastTargetUpdateTick = currentTick;
            }
            
            // Update behavior periodically
            if (currentTick - lastBehaviorTick > BEHAVIOR_UPDATE_INTERVAL)
            {
                UpdateBTBehavior(btProxy);
                lastBehaviorTick = currentTick;
            }
        }

        /// <summary>
        /// Updates the BT's target selection
        /// </summary>
        private void UpdateTarget(Pawn btProxy)
        {
            // Find nearest colonist
            Pawn nearestColonist = FindNearestColonist(btProxy);
            
            // BTs are more likely to stick to their current target
            if (targetColonist != null && targetColonist.Map == btProxy.Map && !targetColonist.Dead)
            {
                float currentDistance = btProxy.Position.DistanceTo(targetColonist.Position);
                if (nearestColonist != null)
                {
                    float nearestDistance = btProxy.Position.DistanceTo(nearestColonist.Position);
                    
                    // Only switch if new target is significantly closer
                    if (nearestDistance < currentDistance * 0.7f)
                    {
                        targetColonist = nearestColonist;
                    }
                }
            }
            else
            {
                targetColonist = nearestColonist;
            }
        }

        /// <summary>
        /// Finds the nearest colonist to the BT
        /// </summary>
        private Pawn FindNearestColonist(Pawn btProxy)
        {
            return btProxy.Map.mapPawns.FreeColonists
                .Where(p => !p.Dead && !p.Downed)
                .OrderBy(p => btProxy.Position.DistanceTo(p.Position))
                .FirstOrDefault();
        }

        /// <summary>
        /// Updates BT behavior based on type and situation
        /// </summary>
        private void UpdateBTBehavior(Pawn btProxy)
        {
            if (targetColonist == null)
            {
                HandleIdleBehavior(btProxy);
                return;
            }
            
            float distance = btProxy.Position.DistanceTo(targetColonist.Position);
            
            // Apply BT-specific behavior
            switch (btType)
            {
                case "BT_Basic":
                    HandleBasicBTBehavior(btProxy, targetColonist, distance);
                    break;
                case "BT_Catcher":
                    HandleCatcherBTBehavior(btProxy, targetColonist, distance);
                    break;
                case "BT_Hunter":
                    HandleHunterBTBehavior(btProxy, targetColonist, distance);
                    break;
                case "BT_Tech":
                    HandleTechBTBehavior(btProxy, targetColonist, distance);
                    break;
                default:
                    HandleBasicBTBehavior(btProxy, targetColonist, distance);
                    break;
            }
            
            // Apply tether effects
            ApplyTetherEffects(btProxy, targetColonist, distance);
            
            // Create environmental effects
            CreateEnvironmentalEffects(btProxy);
        }

        /// <summary>
        /// Handles behavior when BT has no target
        /// </summary>
        private void HandleIdleBehavior(Pawn btProxy)
        {
            // BTs wander aimlessly when no target
            if (btProxy.mindState?.duty?.def != DutyDefOf.Wander)
            {
                btProxy.mindState.duty = new PawnDuty(DutyDefOf.Wander)
                {
                    locomotion = LocomotionUrgency.Walk
                };
            }
        }

        /// <summary>
        /// Basic BT behavior - slow, shambling, only aggressive when very close
        /// </summary>
        private void HandleBasicBTBehavior(Pawn btProxy, Pawn target, float distance)
        {
            if (distance <= 3f)
            {
                // Become aggressive when very close
                if (!btProxy.InMentalState)
                {
                    btProxy.mindState.mentalStateHandler.TryStartMentalState(
                        MentalStateDefOf.Manhunter, 
                        "BT activation"
                    );
                }
            }
            else if (distance <= 12f)
            {
                // Slowly shamble toward target
                btProxy.mindState.duty = new PawnDuty(DutyDefOf.AttackMelee)
                {
                    focus = target,
                    locomotion = LocomotionUrgency.Walk
                };
            }
            else
            {
                // Wander in general direction
                IntVec3 wanderTarget = target.Position + IntVec3Utility.RandomHorizontalOffset(10);
                btProxy.mindState.duty = new PawnDuty(DutyDefOf.Goto)
                {
                    focus = wanderTarget,
                    locomotion = LocomotionUrgency.Walk
                };
            }
        }

        /// <summary>
        /// Catcher BT behavior - fast, aggressive, charges at targets
        /// </summary>
        private void HandleCatcherBTBehavior(Pawn btProxy, Pawn target, float distance)
        {
            if (distance <= 15f)
            {
                // Charge at target aggressively
                btProxy.mindState.duty = new PawnDuty(DutyDefOf.AttackMelee)
                {
                    focus = target,
                    locomotion = LocomotionUrgency.Sprint
                };
                
                if (!btProxy.InMentalState)
                {
                    btProxy.mindState.mentalStateHandler.TryStartMentalState(
                        MentalStateDefOf.Manhunter,
                        "BT catcher activation"
                    );
                }
            }
            else
            {
                // Hunt target persistently
                btProxy.mindState.duty = new PawnDuty(DutyDefOf.Goto)
                {
                    focus = target,
                    locomotion = LocomotionUrgency.Jog
                };
            }
        }

        /// <summary>
        /// Hunter BT behavior - intelligent, tactical, dangerous
        /// </summary>
        private void HandleHunterBTBehavior(Pawn btProxy, Pawn target, float distance)
        {
            if (distance <= 20f)
            {
                // Tactical approach - try to flank or ambush
                if (Rand.Chance(0.3f)) // 30% chance to try flanking
                {
                    IntVec3 flankPosition = CalculateFlankingPosition(btProxy, target);
                    btProxy.mindState.duty = new PawnDuty(DutyDefOf.Goto)
                    {
                        focus = flankPosition,
                        locomotion = LocomotionUrgency.Jog
                    };
                }
                else
                {
                    // Direct assault
                    btProxy.mindState.duty = new PawnDuty(DutyDefOf.AttackMelee)
                    {
                        focus = target,
                        locomotion = LocomotionUrgency.Sprint
                    };
                }
                
                if (!btProxy.InMentalState)
                {
                    btProxy.mindState.mentalStateHandler.TryStartMentalState(
                        MentalStateDefOf.Manhunter,
                        "BT hunter activation"
                    );
                }
            }
            else
            {
                // Stalk target
                btProxy.mindState.duty = new PawnDuty(DutyDefOf.Goto)
                {
                    focus = target,
                    locomotion = LocomotionUrgency.Walk
                };
            }
        }

        /// <summary>
        /// Tech BT behavior - mechanoid-like, with interference effects
        /// </summary>
        private void HandleTechBTBehavior(Pawn btProxy, Pawn target, float distance)
        {
            // Tech BTs interfere with electronics
            CreateTechInterference(btProxy);
            
            if (distance <= 25f)
            {
                // Long-range engagement like mechanoids
                btProxy.mindState.duty = new PawnDuty(DutyDefOf.AttackStatic)
                {
                    focus = target,
                    locomotion = LocomotionUrgency.Walk
                };
                
                if (!btProxy.InMentalState)
                {
                    btProxy.mindState.mentalStateHandler.TryStartMentalState(
                        MentalStateDefOf.Manhunter,
                        "BT tech activation"
                    );
                }
            }
        }

        /// <summary>
        /// Calculates a flanking position around the target
        /// </summary>
        private IntVec3 CalculateFlankingPosition(Pawn btProxy, Pawn target)
        {
            // Try to get behind or to the side of the target
            Vector3 toTarget = (target.Position - btProxy.Position).ToVector3();
            Vector3 perpendicular = new Vector3(-toTarget.z, 0f, toTarget.x).normalized;
            
            IntVec3 flankPos = target.Position + (perpendicular * 5f).ToIntVec3();
            
            if (flankPos.InBounds(btProxy.Map) && flankPos.Walkable(btProxy.Map))
            {
                return flankPos;
            }
            
            // Fallback to direct approach
            return target.Position;
        }

        /// <summary>
        /// Applies tether effects between BT and target
        /// </summary>
        private void ApplyTetherEffects(Pawn btProxy, Pawn target, float distance)
        {
            // Tether effects increase as BT gets closer
            if (distance <= 8f)
            {
                float tetherStrength = (8f - distance) / 8f; // 0-1 based on proximity
                
                // Apply or increase tether hediff
                Hediff existingTether = target.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
                
                if (existingTether != null)
                {
                    // Increase existing tether
                    existingTether.Severity = Math.Min(1f, existingTether.Severity + tetherStrength * 0.01f);
                }
                else if (Rand.Chance(tetherStrength * 0.1f)) // Chance to start new tether
                {
                    Hediff newTether = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.BTTether, target);
                    newTether.Severity = tetherStrength * 0.1f;
                    target.health.AddHediff(newTether);
                }
                
                // Visual tether effect
                if (Rand.Chance(0.1f))
                {
                    FleckMaker.ConnectingLine(btProxy.DrawPos, target.DrawPos, FleckDefOf.AirPuff, btProxy.Map);
                }
            }
        }

        /// <summary>
        /// Creates environmental effects around the BT
        /// </summary>
        private void CreateEnvironmentalEffects(Pawn btProxy)
        {
            // Occasional atmospheric effects
            if (Rand.Chance(0.05f)) // 5% chance per tick
            {
                switch (Rand.Range(0, 3))
                {
                    case 0:
                        FleckMaker.ThrowSmoke(btProxy.DrawPos, btProxy.Map, 1f);
                        break;
                    case 1:
                        FleckMaker.ThrowAirPuffUp(btProxy.DrawPos, btProxy.Map);
                        break;
                    case 2:
                        FleckMaker.Static(btProxy.Position, btProxy.Map, FleckDefOf.PsycastPsychicEffect, 0.5f);
                        break;
                }
            }
            
            // Aging effect on nearby items
            if (Rand.Chance(0.02f))
            {
                ApplyAgingEffect(btProxy);
            }
        }

        /// <summary>
        /// Creates technology interference for tech BTs
        /// </summary>
        private void CreateTechInterference(Pawn btProxy)
        {
            // Interfere with nearby electronics
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(btProxy.Position, btProxy.Map, 8f, true))
            {
                if (thing.def.category == ThingCategory.Building && thing.def.building?.isMachine == true)
                {
                    CompPowerTrader powerComp = thing.TryGetComp<CompPowerTrader>();
                    if (powerComp != null && Rand.Chance(0.01f))
                    {
                        // Temporarily disrupt power
                        powerComp.PowerOutput *= 0.8f;
                        
                        // Visual interference effect
                        FleckMaker.ThrowLightningGlow(thing.DrawPos, btProxy.Map, 0.5f);
                    }
                }
            }
        }

        /// <summary>
        /// Applies aging effects to nearby items
        /// </summary>
        private void ApplyAgingEffect(Pawn btProxy)
        {
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(btProxy.Position, btProxy.Map, 3f, true))
            {
                if (thing.def.useHitPoints && thing.HitPoints > 1)
                {
                    thing.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, 1f));
                    
                    if (Rand.Chance(0.3f))
                    {
                        FleckMaker.ThrowSmoke(thing.DrawPos, btProxy.Map, 0.5f);
                    }
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref btType, "btType", "BT_Basic");
            Scribe_Values.Look(ref isActive, "isActive", true);
            Scribe_Values.Look(ref lastBehaviorTick, "lastBehaviorTick");
            Scribe_Values.Look(ref lastTargetUpdateTick, "lastTargetUpdateTick");
            Scribe_References.Look(ref targetColonist, "targetColonist");
        }
    }

    /// <summary>
    /// Component that handles visual effects for BT proxies
    /// </summary>
    public class CompBTVisualEffects : ThingComp
    {
        private int lastEffectTick = 0;
        private Material cachedBTMaterial = null;
        private float effectIntensity = 1f;
        
        private const int EFFECT_UPDATE_INTERVAL = 60; // Every second

        public float EffectIntensity 
        { 
            get => effectIntensity; 
            set => effectIntensity = Mathf.Clamp01(value); 
        }

        public override void CompTick()
        {
            base.CompTick();
            
            if (!(parent is Pawn btProxy))
                return;
            
            int currentTick = Find.TickManager.TicksGame;
            
            if (currentTick - lastEffectTick > EFFECT_UPDATE_INTERVAL)
            {
                UpdateVisualEffects(btProxy);
                lastEffectTick = currentTick;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            
            if (!(parent is Pawn btProxy))
                return;
            
            // Draw BT aura
            DrawBTAura(btProxy);
            
            // Apply ghostly appearance
            if (ShouldBeVisible(btProxy))
            {
                ApplyGhostlyRendering(btProxy);
            }
        }

        /// <summary>
        /// Updates visual effects based on BT state
        /// </summary>
        private void UpdateVisualEffects(Pawn btProxy)
        {
            // Adjust effect intensity based on activity
            CompBTBehavior behaviorComp = btProxy.GetComp<CompBTBehavior>();
            if (behaviorComp != null)
            {
                effectIntensity = behaviorComp.IsActive ? 1f : 0.3f;
            }
            
            // Create periodic effects
            CreatePeriodicEffects(btProxy);
        }

        /// <summary>
        /// Draws dark aura around the BT
        /// </summary>
        private void DrawBTAura(Pawn btProxy)
        {
            if (cachedBTMaterial == null)
            {
                cachedBTMaterial = MaterialPool.MatFrom("Effects/BTAura", ShaderDatabase.Transparent, 
                    new Color(0.2f, 0.2f, 0.4f, 0.3f * effectIntensity));
            }
            
            Vector3 drawPos = btProxy.DrawPos;
            drawPos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
            
            float auraSize = 2f + Mathf.Sin(Find.TickManager.TicksGame * 0.1f) * 0.3f;
            Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Quaternion.identity, Vector3.one * auraSize * effectIntensity);
            
            Graphics.DrawMesh(MeshPool.plane10, matrix, cachedBTMaterial, 0);
        }

        /// <summary>
        /// Determines if BT should be visible to player
        /// </summary>
        private bool ShouldBeVisible(Pawn btProxy)
        {
            // BTs are always visible to DOOMS carriers
            foreach (Pawn colonist in btProxy.Map.mapPawns.FreeColonists)
            {
                if (DeathStrandingUtility.GetDOOMSLevel(colonist) > 0)
                {
                    float distance = colonist.Position.DistanceTo(btProxy.Position);
                    float detectionRange = DeathStrandingUtility.GetDOOMSLevel(colonist) * 10f;
                    
                    if (distance <= detectionRange)
                    {
                        return true;
                    }
                }
            }
            
            // Also visible when very close to any colonist
            return btProxy.Map.mapPawns.FreeColonists.Any(p => 
                p.Position.DistanceTo(btProxy.Position) <= 5f);
        }

        /// <summary>
        /// Applies ghostly rendering effects
        /// </summary>
        private void ApplyGhostlyRendering(Pawn btProxy)
        {
            // This would require more complex rendering modifications
            // For now, we'll use particle effects to suggest otherworldly nature
            
            if (Rand.Chance(0.1f))
            {
                Vector3 particlePos = btProxy.DrawPos + new Vector3(
                    Rand.Range(-1f, 1f),
                    0f,
                    Rand.Range(-1f, 1f)
                );
                
                FleckMaker.ThrowAirPuffUp(particlePos, btProxy.Map);
            }
        }

        /// <summary>
        /// Creates periodic visual effects
        /// </summary>
        private void CreatePeriodicEffects(Pawn btProxy)
        {
            // Breathing/pulsing effect
            if (Rand.Chance(0.05f))
            {
                FleckMaker.Static(btProxy.Position, btProxy.Map, FleckDefOf.PsycastPsychicEffect, 
                    0.5f * effectIntensity);
            }
            
            // Handprint trails
            if (btProxy.pather?.Moving == true && Rand.Chance(0.1f))
            {
                CreateHandprintTrail(btProxy);
            }
        }

        /// <summary>
        /// Creates handprint effects behind moving BTs
        /// </summary>
        private void CreateHandprintTrail(Pawn btProxy)
        {
            // Create handprint effect at previous position
            IntVec3 previousPos = btProxy.Position - btProxy.pather.nextCell + btProxy.Position;
            
            if (previousPos.InBounds(btProxy.Map))
            {
                FleckMaker.Static(previousPos, btProxy.Map, FleckDefOf.Mote_ItemSparkle, 
                    1f * effectIntensity);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref effectIntensity, "effectIntensity", 1f);
            Scribe_Values.Look(ref lastEffectTick, "lastEffectTick");
        }
    }

    /// <summary>
    /// Component that manages tether connections between BTs and colonists
    /// </summary>
    public class CompBTTether : ThingComp
    {
        private List<TetherConnection> activeConnections = new List<TetherConnection>();
        private int lastTetherUpdateTick = 0;
        
        private const int TETHER_UPDATE_INTERVAL = 500; // Every ~8 seconds
        private const float MAX_TETHER_RANGE = 15f;

        public IReadOnlyList<TetherConnection> ActiveConnections => activeConnections.AsReadOnly();

        public override void CompTick()
        {
            base.CompTick();
            
            if (!(parent is Pawn btProxy))
                return;
            
            int currentTick = Find.TickManager.TicksGame;
            
            if (currentTick - lastTetherUpdateTick > TETHER_UPDATE_INTERVAL)
            {
                UpdateTetherConnections(btProxy);
                lastTetherUpdateTick = currentTick;
            }
        }

        /// <summary>
        /// Updates all tether connections
        /// </summary>
        private void UpdateTetherConnections(Pawn btProxy)
        {
            // Remove expired connections
            activeConnections.RemoveAll(connection => 
                connection.TargetPawn.Dead || 
                connection.TargetPawn.Map != btProxy.Map ||
                btProxy.Position.DistanceTo(connection.TargetPawn.Position) > MAX_TETHER_RANGE);
            
            // Look for new tether targets
            foreach (Pawn colonist in btProxy.Map.mapPawns.FreeColonists)
            {
                if (colonist.Dead || colonist.Downed)
                    continue;
                
                float distance = btProxy.Position.DistanceTo(colonist.Position);
                if (distance <= MAX_TETHER_RANGE)
                {
                    UpdateTetherConnection(btProxy, colonist, distance);
                }
            }
        }

        /// <summary>
        /// Updates or creates a tether connection with a specific colonist
        /// </summary>
        private void UpdateTetherConnection(Pawn btProxy, Pawn colonist, float distance)
        {
            TetherConnection existing = activeConnections.FirstOrDefault(c => c.TargetPawn == colonist);
            
            if (existing != null)
            {
                // Update existing connection
                existing.Distance = distance;
                existing.LastUpdateTick = Find.TickManager.TicksGame;
                existing.Strength = CalculateTetherStrength(distance);
            }
            else
            {
                // Create new connection
                TetherConnection newConnection = new TetherConnection
                {
                    TargetPawn = colonist,
                    Distance = distance,
                    Strength = CalculateTetherStrength(distance),
                    StartTick = Find.TickManager.TicksGame,
                    LastUpdateTick = Find.TickManager.TicksGame
                };
                
                activeConnections.Add(newConnection);
            }
        }

        /// <summary>
        /// Calculates tether strength based on distance and other factors
        /// </summary>
        private float CalculateTetherStrength(float distance)
        {
            // Strength decreases with distance
            float baseStrength = 1f - (distance / MAX_TETHER_RANGE);
            
            // Apply modifiers
            baseStrength = Mathf.Clamp01(baseStrength);
            
            return baseStrength;
        }

        /// <summary>
        /// Gets the strongest tether connection
        /// </summary>
        public TetherConnection GetStrongestConnection()
        {
            return activeConnections.OrderByDescending(c => c.Strength).FirstOrDefault();
        }

        /// <summary>
        /// Applies tether effects to connected colonists
        /// </summary>
        public void ApplyTetherEffects()
        {
            foreach (TetherConnection connection in activeConnections)
            {
                ApplyTetherToColonist(connection);
            }
        }

        /// <summary>
        /// Applies tether effects to a specific colonist
        /// </summary>
        private void ApplyTetherToColonist(TetherConnection connection)
        {
            Hediff existingTether = connection.TargetPawn.health.hediffSet
                .GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
            
            if (existingTether != null)
            {
                // Increase existing tether
                float increase = connection.Strength * 0.001f; // Very slow buildup
                existingTether.Severity = Math.Min(1f, existingTether.Severity + increase);
            }
            else if (Rand.Chance(connection.Strength * 0.01f))
            {
                // Create new tether
                Hediff newTether = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.BTTether, connection.TargetPawn);
                newTether.Severity = connection.Strength * 0.05f;
                connection.TargetPawn.health.AddHediff(newTether);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref activeConnections, "activeConnections", LookMode.Deep);
            Scribe_Values.Look(ref lastTetherUpdateTick, "lastTetherUpdateTick");
        }
    }

    /// <summary>
    /// Represents a tether connection between a BT and a colonist
    /// </summary>
    public class TetherConnection : IExposable
    {
        public Pawn TargetPawn;
        public float Distance;
        public float Strength;
        public int StartTick;
        public int LastUpdateTick;

        public void ExposeData()
        {
            Scribe_References.Look(ref TargetPawn, "targetPawn");
            Scribe_Values.Look(ref Distance, "distance");
            Scribe_Values.Look(ref Strength, "strength");
            Scribe_Values.Look(ref StartTick, "startTick");
            Scribe_Values.Look(ref LastUpdateTick, "lastUpdateTick");
        }
    }
}