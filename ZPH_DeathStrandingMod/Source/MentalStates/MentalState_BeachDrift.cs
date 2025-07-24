using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using DeathStrandingMod.Core;

namespace DeathStrandingMod.MentalStates
{
    /// <summary>
    /// Mental state where pawn drifts between dimensions, becoming detached from reality
    /// </summary>
    public class MentalState_BeachDrift : MentalState
    {
        private int driftIntensity = 1;
        private int lastDriftTick = 0;
        private bool hasFoundTarget = false;
        private IntVec3 driftDestination = IntVec3.Invalid;
        private List<IntVec3> visitedLocations = new List<IntVec3>();
        private int phaseShiftTimer = 0;
        private bool isPhaseShifted = false;
        
        private const int DRIFT_UPDATE_INTERVAL = 150; // Every ~2.5 seconds
        private const int PHASE_SHIFT_DURATION = 300;  // ~5 seconds
        private const int MAX_DRIFT_DURATION = 7200;   // ~2 hours max

        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off; // Beach drifters avoid social interaction
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref driftIntensity, "driftIntensity", 1);
            Scribe_Values.Look(ref lastDriftTick, "lastDriftTick", 0);
            Scribe_Values.Look(ref hasFoundTarget, "hasFoundTarget", false);
            Scribe_Values.Look(ref driftDestination, "driftDestination", IntVec3.Invalid);
            Scribe_Collections.Look(ref visitedLocations, "visitedLocations", LookMode.Value);
            Scribe_Values.Look(ref phaseShiftTimer, "phaseShiftTimer", 0);
            Scribe_Values.Look(ref isPhaseShifted, "isPhaseShifted", false);
        }

        public override void MentalStateTick()
        {
            base.MentalStateTick();
            
            int currentTick = Find.TickManager.TicksGame;
            
            // Update drift behavior periodically
            if (currentTick - lastDriftTick > DRIFT_UPDATE_INTERVAL)
            {
                UpdateDriftBehavior();
                lastDriftTick = currentTick;
            }
            
            // Handle phase shifting
            HandlePhaseShifting();
            
            // Create atmospheric effects
            CreateDriftEffects();
            
            // Check for recovery conditions
            if (ShouldRecover())
            {
                RecoverFromState = true;
            }
            
            // Force recovery if too long
            if (age > MAX_DRIFT_DURATION)
            {
                RecoverFromState = true;
            }
        }

        public override void PreStart()
        {
            base.PreStart();
            
            // Determine drift intensity based on pawn's condition
            CalculateDriftIntensity();
            
            // Initial drift destination
            FindNewDriftDestination();
            
            // Add initial drift effects
            ApplyInitialDriftEffects();
            
            // Notification
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                Messages.Message(
                    "BeachDriftStarted".Translate(pawn.LabelShort),
                    pawn,
                    MessageTypeDefOf.NegativeEvent
                );
            }
        }

        public override void PostEnd()
        {
            base.PostEnd();
            
            // Remove drift effects
            RemoveDriftEffects();
            
            // Apply recovery effects
            ApplyRecoveryEffects();
            
            // Notification
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                Messages.Message(
                    "BeachDriftEnded".Translate(pawn.LabelShort),
                    pawn,
                    MessageTypeDefOf.PositiveEvent
                );
            }
        }

        private void UpdateDriftBehavior()
        {
            // Increase drift intensity over time
            if (age > 1800 && driftIntensity < 3) // After 30 minutes
            {
                driftIntensity = 2;
            }
            if (age > 3600 && driftIntensity < 4) // After 1 hour
            {
                driftIntensity = 3;
            }
            
            // Find new destination if current one is reached or invalid
            if (!hasFoundTarget || pawn.Position.DistanceTo(driftDestination) <= 3f)
            {
                FindNewDriftDestination();
            }
            
            // Apply drift movement behavior
            ApplyDriftMovement();
            
            // Chance for phase shifting at higher intensities
            if (driftIntensity >= 2 && !isPhaseShifted && Rand.Chance(0.1f))
            {
                StartPhaseShift();
            }
        }

        private void CalculateDriftIntensity()
        {
            driftIntensity = 1; // Base intensity
            
            // Increase with Beach-related hediffs
            if (pawn.health?.hediffSet != null)
            {
                Hediff beachDrift = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BeachDrift);
                if (beachDrift != null)
                {
                    driftIntensity += Mathf.RoundToInt(beachDrift.Severity * 2f);
                }
                
                Hediff btTether = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
                if (btTether != null)
                {
                    driftIntensity += Mathf.RoundToInt(btTether.Severity);
                }
            }
            
            // DOOMS carriers have different drift patterns
            if (DeathStrandingUtility.GetDOOMSLevel(pawn) > 0)
            {
                driftIntensity += 1;
            }
            
            // Environmental factors
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(pawn.Map);
            if (beachThreat > 0.5f)
            {
                driftIntensity += 1;
            }
            
            driftIntensity = Mathf.Clamp(driftIntensity, 1, 5);
        }

        private void FindNewDriftDestination()
        {
            // Beach drifters are drawn to specific types of locations
            List<IntVec3> candidates = FindDriftCandidates();
            
            if (candidates.Any())
            {
                // Prefer unvisited locations
                var unvisited = candidates.Where(c => !visitedLocations.Contains(c)).ToList();
                if (unvisited.Any())
                {
                    driftDestination = unvisited.RandomElement();
                }
                else
                {
                    driftDestination = candidates.RandomElement();
                }
                
                hasFoundTarget = true;
                
                // Clear visited locations if we've been to too many
                if (visitedLocations.Count > 8)
                {
                    visitedLocations.RemoveRange(0, 4);
                }
            }
            else
            {
                // Random wandering as fallback
                driftDestination = pawn.Position + IntVec3Utility.RandomHorizontalOffset(20);
                hasFoundTarget = false;
            }
        }

        private List<IntVec3> FindDriftCandidates()
        {
            List<IntVec3> candidates = new List<IntVec3>();
            Map map = pawn.Map;
            
            // Beach drifters are attracted to:
            // 1. Quiet, isolated areas
            // 2. Areas with negative energy (corpses, ruins)
            // 3. Water or reflective surfaces
            // 4. High places with views
            // 5. Areas away from bright lights
            
            // Find isolated areas
            foreach (IntVec3 cell in map.AllCells.Where(c => c.Walkable(map)))
            {
                if (pawn.Position.DistanceTo(cell) > 50f) continue; // Too far
                if (pawn.Position.DistanceTo(cell) < 10f) continue; // Too close
                
                float isolationScore = CalculateIsolationScore(cell, map);
                float negativeEnergyScore = CalculateNegativeEnergyScore(cell, map);
                float aestheticScore = CalculateAestheticScore(cell, map);
                
                float totalScore = isolationScore + negativeEnergyScore + aestheticScore;
                
                if (totalScore > 2f && Rand.Chance(totalScore / 10f))
                {
                    candidates.Add(cell);
                }
                
                if (candidates.Count > 20) break; // Limit search
            }
            
            return candidates;
        }

        private float CalculateIsolationScore(IntVec3 cell, Map map)
        {
            float score = 0f;
            
            // Distance from other pawns
            var nearbyPawns = GenRadial.RadialDistinctThingsAround(cell, map, 15f, true)
                .OfType<Pawn>()
                .Where(p => p != pawn && p.RaceProps.Humanlike)
                .Count();
            
            if (nearbyPawns == 0) score += 2f;
            else if (nearbyPawns <= 2) score += 1f;
            
            // Distance from buildings
            var nearbyBuildings = GenRadial.RadialDistinctThingsAround(cell, map, 10f, true)
                .Where(t => t.def.category == ThingCategory.Building && t.def.building?.isInert == false)
                .Count();
            
            if (nearbyBuildings == 0) score += 1f;
            
            return score;
        }

        private float CalculateNegativeEnergyScore(IntVec3 cell, Map map)
        {
            float score = 0f;
            
            // Corpses nearby
            var nearbyCorpses = GenRadial.RadialDistinctThingsAround(cell, map, 8f, true)
                .OfType<Corpse>()
                .Count();
            
            score += nearbyCorpses * 0.5f;
            
            // Ruins and destroyed buildings
            var nearbyRuins = GenRadial.RadialDistinctThingsAround(cell, map, 8f, true)
                .Where(t => t.def.category == ThingCategory.Building && t.HitPoints < t.MaxHitPoints * 0.5f)
                .Count();
            
            score += nearbyRuins * 0.3f;
            
            // Filth and decay
            if (cell.GetFirstBuilding(map)?.def?.filthAcceptanceMask != null)
            {
                score += 0.2f;
            }
            
            return score;
        }

        private float CalculateAestheticScore(IntVec3 cell, Map map)
        {
            float score = 0f;
            
            // Water or marsh terrain
            if (cell.GetTerrain(map).affordances.Contains(TerrainAffordanceDefOf.Bridgeable))
            {
                score += 1f;
            }
            
            // Higher elevation (if terrain supports it)
            if (map.terrainGrid.TerrainAt(cell) == TerrainDefOf.Gravel ||
                map.terrainGrid.TerrainAt(cell) == TerrainDefOf.Granite_Rough)
            {
                score += 0.5f;
            }
            
            // Away from artificial lighting
            bool nearLight = GenRadial.RadialDistinctThingsAround(cell, map, 12f, true)
                .Any(t => t.def.building?.artificialForMissing == true);
            
            if (!nearLight) score += 0.5f;
            
            return score;
        }

        private void ApplyDriftMovement()
        {
            if (!hasFoundTarget || driftDestination == IntVec3.Invalid)
                return;
            
            // Beach drifters move in a specific pattern
            if (pawn.jobs?.curJob?.def != JobDefOf.Goto)
            {
                Job driftJob = JobMaker.MakeJob(JobDefOf.Goto, driftDestination);
                driftJob.locomotionUrgency = GetDriftLocomotionUrgency();
                driftJob.canBash = false; // Drifters don't break things
                
                pawn.jobs.TryTakeOrderedJob(driftJob);
            }
            
            // Mark location as visited when close
            if (pawn.Position.DistanceTo(driftDestination) <= 3f)
            {
                if (!visitedLocations.Contains(driftDestination))
                {
                    visitedLocations.Add(driftDestination);
                }
                hasFoundTarget = false;
            }
        }

        private LocomotionUrgency GetDriftLocomotionUrgency()
        {
            return driftIntensity switch
            {
                1 => LocomotionUrgency.Amble,
                2 => LocomotionUrgency.Walk,
                3 => LocomotionUrgency.Jog,
                4 or 5 => LocomotionUrgency.Sprint,
                _ => LocomotionUrgency.Walk
            };
        }

        private void HandlePhaseShifting()
        {
            if (isPhaseShifted)
            {
                phaseShiftTimer--;
                
                if (phaseShiftTimer <= 0)
                {
                    EndPhaseShift();
                }
                else
                {
                    // Phase shift effects
                    CreatePhaseShiftEffects();
                }
            }
        }

        private void StartPhaseShift()
        {
            isPhaseShifted = true;
            phaseShiftTimer = PHASE_SHIFT_DURATION;
            
            // Visual effects
            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastPsychicEffect, 2f);
            
            // Temporary stat modifications
            ApplyPhaseShiftEffects();
            
            Messages.Message(
                "BeachDriftPhaseShift".Translate(pawn.LabelShort),
                pawn,
                MessageTypeDefOf.CautionInput
            );
        }

        private void EndPhaseShift()
        {
            isPhaseShifted = false;
            
            // Remove phase shift effects
            RemovePhaseShiftEffects();
            
            // Visual effects
            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.Mote_ItemSparkle, 1f);
        }

        private void CreatePhaseShiftEffects()
        {
            if (Rand.Chance(0.1f))
            {
                FleckMaker.ThrowAirPuffUp(pawn.DrawPos, pawn.Map);
            }
            
            // Occasional position "glitching"
            if (Rand.Chance(0.05f))
            {
                // Very brief visual displacement
                FleckMaker.Static(pawn.Position + IntVec3Utility.RandomHorizontalOffset(2), 
                    pawn.Map, FleckDefOf.Mote_ItemSparkle, 0.5f);
            }
        }

        private void ApplyPhaseShiftEffects()
        {
            // Phase shifted pawns move through some obstacles
            if (pawn.health?.hediffSet != null)
            {
                Hediff phaseShift = HediffMaker.MakeHediff(
                    DefDatabase<HediffDef>.GetNamedSilentFail("BeachPhaseShift") ?? HediffDefOf.Go, 
                    pawn
                );
                phaseShift.Severity = 1f;
                pawn.health.AddHediff(phaseShift);
            }
        }

        private void RemovePhaseShiftEffects()
        {
            if (pawn.health?.hediffSet != null)
            {
                Hediff phaseShift = pawn.health.hediffSet.hediffs
                    .FirstOrDefault(h => h.def.defName == "BeachPhaseShift");
                
                if (phaseShift != null)
                {
                    pawn.health.RemoveHediff(phaseShift);
                }
            }
        }

        private void CreateDriftEffects()
        {
            // Atmospheric effects around the drifting pawn
            if (Rand.Chance(0.02f))
            {
                Vector3 effectPos = pawn.DrawPos + new Vector3(
                    Rand.Range(-2f, 2f),
                    0f,
                    Rand.Range(-2f, 2f)
                );
                
                FleckMaker.ThrowSmoke(effectPos, pawn.Map, 0.8f);
            }
            
            // Intensity-based effects
            if (driftIntensity >= 3 && Rand.Chance(0.05f))
            {
                FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastPsychicEffect, 1f);
            }
        }

        private void ApplyInitialDriftEffects()
        {
            if (pawn.health?.hediffSet == null) return;
            
            // Apply Beach drift hediff if not already present
            Hediff existingDrift = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BeachDrift);
            if (existingDrift == null)
            {
                Hediff drift = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.BeachDrift, pawn);
                drift.Severity = 0.3f;
                pawn.health.AddHediff(drift);
            }
            else
            {
                existingDrift.Severity = Math.Min(1f, existingDrift.Severity + 0.2f);
            }
            
            // Temporary confusion
            Hediff confusion = HediffMaker.MakeHediff(
                DefDatabase<HediffDef>.GetNamedSilentFail("BeachConfusion") ?? HediffDefOf.PsychicHangover,
                pawn
            );
            confusion.Severity = 0.5f;
            pawn.health.AddHediff(confusion);
        }

        private void RemoveDriftEffects()
        {
            if (pawn.health?.hediffSet == null) return;
            
            // Remove temporary effects
            var tempEffects = pawn.health.hediffSet.hediffs
                .Where(h => h.def.defName.StartsWith("Beach") && 
                           h.def.defName != "BeachDrift") // Keep the main drift hediff
                .ToList();
            
            foreach (Hediff effect in tempEffects)
            {
                pawn.health.RemoveHediff(effect);
            }
        }

        private void ApplyRecoveryEffects()
        {
            // Add memory of the drift experience
            if (pawn.needs?.mood?.thoughts?.memories != null)
            {
                ThoughtDef driftMemory = DefDatabase<ThoughtDef>.GetNamedSilentFail("BeachDriftExperience");
                if (driftMemory != null)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(driftMemory);
                }
            }
            
            // Slight mood penalty from disorientation
            if (pawn.needs?.mood?.thoughts?.memories != null)
            {
                ThoughtDef disorientation = DefDatabase<ThoughtDef>.GetNamedSilentFail("BeachDisorientation");
                if (disorientation != null)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(disorientation);
                }
            }
        }

        private bool ShouldRecover()
        {
            // Recovery chance increases over time
            float baseRecoveryChance = 0.001f; // Very low base chance
            
            // Increase chance based on time in state
            if (age > 1800) baseRecoveryChance *= 2f; // After 30 minutes
            if (age > 3600) baseRecoveryChance *= 3f; // After 1 hour
            
            // Social interaction helps recovery
            if (pawn.interactions?.InteractedWithAnyoneRecently() == true)
            {
                baseRecoveryChance *= 3f;
            }
            
            // Being in a comfortable environment helps
            if (pawn.Position.GetRoom(pawn.Map)?.PsychologicallyOutdoors == false)
            {
                baseRecoveryChance *= 2f;
            }
            
            // DOOMS carriers have different recovery patterns
            if (DeathStrandingUtility.GetDOOMSLevel(pawn) > 0)
            {
                baseRecoveryChance *= 1.5f; // They're more resilient
            }
            
            // Reduce Beach drift hediff severity over time
            if (pawn.health?.hediffSet != null)
            {
                Hediff drift = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BeachDrift);
                if (drift != null)
                {
                    drift.Severity = Math.Max(0f, drift.Severity - 0.001f);
                    if (drift.Severity <= 0.1f)
                    {
                        baseRecoveryChance *= 5f; // Much higher chance when drift is low
                    }
                }
            }
            
            return Rand.Chance(baseRecoveryChance);
        }

        public override string GetInspectLine()
        {
            string inspectLine = "BeachDrift".Translate();
            
            if (driftIntensity > 1)
            {
                inspectLine += " (" + "Intensity".Translate() + " " + driftIntensity + ")";
            }
            
            if (isPhaseShifted)
            {
                inspectLine += " [" + "PhaseShifted".Translate() + "]";
            }
            
            return inspectLine;
        }
    }

    /// <summary>
    /// Mental state worker for Beach drift episodes
    /// </summary>
    public class MentalStateWorker_BeachDrift : MentalStateWorker
    {
        public override bool StateCanOccur(Pawn p)
        {
            if (!base.StateCanOccur(p)) return false;
            
            // Requires Beach-related conditions
            if (p.health?.hediffSet == null) return false;
            
            // Must have some Beach exposure
            bool hasBeachExposure = 
                p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BeachDrift) != null ||
                p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether) != null ||
                DeathStrandingUtility.GetDOOMSLevel(p) > 0;
            
            if (!hasBeachExposure) return false;
            
            // Environmental factors
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(p.Map);
            if (beachThreat < 0.2f) return false;
            
            // More likely during timefall
            if (DeathStrandingUtility.IsTimefallActive(p.Map))
                return true;
            
            return beachThreat > 0.4f;
        }

        public override float CommonalityFor(Pawn pawn, bool moodCaused = false)
        {
            float baseCommonality = base.CommonalityFor(pawn, moodCaused);
            
            // Increase based on Beach exposure
            if (pawn.health?.hediffSet != null)
            {
                Hediff drift = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BeachDrift);
                if (drift != null)
                {
                    baseCommonality *= (1f + drift.Severity);
                }
                
                Hediff tether = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
                if (tether != null)
                {
                    baseCommonality *= (1f + tether.Severity * 0.5f);
                }
            }
            
            // DOOMS carriers more susceptible
            int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(pawn);
            if (doomsLevel > 0)
            {
                baseCommonality *= (1f + doomsLevel * 0.3f);
            }
            
            // Environmental factors
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(pawn.Map);
            baseCommonality *= (1f + beachThreat);
            
            // Much more likely during timefall
            if (DeathStrandingUtility.IsTimefallActive(pawn.Map))
            {
                baseCommonality *= 3f;
            }
            
            return baseCommonality;
        }
    }
}