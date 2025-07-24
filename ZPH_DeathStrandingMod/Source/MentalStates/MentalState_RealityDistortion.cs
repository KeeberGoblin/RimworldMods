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
    /// Mental state where pawn experiences reality distortion episodes, seeing things that aren't there
    /// </summary>
    public class MentalState_RealityDistortion : MentalState
    {
        private int distortionLevel = 1;
        private int episodeCounter = 0;
        private int lastEpisodeTick = 0;
        private bool isHavingEpisode = false;
        private int episodeDuration = 0;
        private Thing perceivedThreat = null;
        private List<DistortionMemory> recentDistortions = new List<DistortionMemory>();
        private float realityAnchor = 1f; // How connected to reality they are
        
        private const int EPISODE_CHECK_INTERVAL = 300; // Every 5 seconds
        private const int MAX_EPISODE_DURATION = 900;   // 15 seconds max per episode
        private const int DISTORTION_DECAY_INTERVAL = 1800; // 30 seconds

        public override RandomSocialMode SocialModeMax()
        {
            return isHavingEpisode ? RandomSocialMode.Off : RandomSocialMode.Normal;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref distortionLevel, "distortionLevel", 1);
            Scribe_Values.Look(ref episodeCounter, "episodeCounter", 0);
            Scribe_Values.Look(ref lastEpisodeTick, "lastEpisodeTick", 0);
            Scribe_Values.Look(ref isHavingEpisode, "isHavingEpisode", false);
            Scribe_Values.Look(ref episodeDuration, "episodeDuration", 0);
            Scribe_References.Look(ref perceivedThreat, "perceivedThreat");
            Scribe_Collections.Look(ref recentDistortions, "recentDistortions", LookMode.Deep);
            Scribe_Values.Look(ref realityAnchor, "realityAnchor", 1f);
        }

        public override void MentalStateTick()
        {
            base.MentalStateTick();
            
            int currentTick = Find.TickManager.TicksGame;
            
            // Handle ongoing episode
            if (isHavingEpisode)
            {
                HandleOngoingEpisode();
            }
            else
            {
                // Check for new episodes
                if (currentTick - lastEpisodeTick > EPISODE_CHECK_INTERVAL)
                {
                    CheckForNewEpisode();
                    lastEpisodeTick = currentTick;
                }
            }
            
            // Update distortion level and reality anchor
            UpdateDistortionParameters();
            
            // Decay old distortion memories
            DecayDistortionMemories();
            
            // Create ambient effects
            CreateDistortionEffects();
            
            // Check recovery conditions
            if (ShouldRecover())
            {
                RecoverFromState = true;
            }
        }

        public override void PreStart()
        {
            base.PreStart();
            
            // Calculate initial distortion level
            CalculateDistortionLevel();
            
            // Initialize reality anchor
            CalculateRealityAnchor();
            
            // Apply initial effects
            ApplyInitialDistortionEffects();
            
            // Notification
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                Messages.Message(
                    "RealityDistortionStarted".Translate(pawn.LabelShort),
                    pawn,
                    MessageTypeDefOf.NegativeEvent
                );
            }
        }

        public override void PostEnd()
        {
            base.PostEnd();
            
            // End any ongoing episode
            if (isHavingEpisode)
            {
                EndCurrentEpisode();
            }
            
            // Remove distortion effects
            RemoveDistortionEffects();
            
            // Apply recovery memory
            ApplyRecoveryMemory();
            
            // Notification
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                Messages.Message(
                    "RealityDistortionEnded".Translate(pawn.LabelShort),
                    pawn,
                    MessageTypeDefOf.PositiveEvent
                );
            }
        }

        private void CheckForNewEpisode()
        {
            float episodeChance = CalculateEpisodeChance();
            
            if (Rand.Chance(episodeChance))
            {
                StartNewEpisode();
            }
        }

        private float CalculateEpisodeChance()
        {
            float baseChance = 0.05f; // 5% base chance per check
            
            // Increase with distortion level
            baseChance *= distortionLevel;
            
            // Decrease with reality anchor (stronger anchor = fewer episodes)
            baseChance *= (2f - realityAnchor);
            
            // Environmental factors
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(pawn.Map);
            baseChance *= (1f + beachThreat);
            
            // Time of day (more likely at night/dawn/dusk)
            float hourOfDay = GenLocalDate.HourFloat(pawn.Map);
            if (hourOfDay < 6f || hourOfDay > 20f) // Night time
            {
                baseChance *= 1.5f;
            }
            else if (hourOfDay < 8f || hourOfDay > 18f) // Dawn/dusk
            {
                baseChance *= 1.2f;
            }
            
            // Stress factors
            if (pawn.needs?.mood?.CurLevel < 0.4f)
            {
                baseChance *= 1.3f;
            }
            
            // Recent episodes reduce chance (fatigue factor)
            if (episodeCounter > 3 && age < 3600) // More than 3 episodes in last hour
            {
                baseChance *= 0.5f;
            }
            
            return Mathf.Clamp(baseChance, 0f, 0.4f); // Max 40% chance
        }

        private void StartNewEpisode()
        {
            isHavingEpisode = true;
            episodeDuration = Rand.Range(300, MAX_EPISODE_DURATION); // 5-15 seconds
            episodeCounter++;
            
            // Determine episode type
            DistortionEpisodeType episodeType = ChooseEpisodeType();
            
            // Execute episode
            ExecuteDistortionEpisode(episodeType);
            
            // Record the episode
            RecordDistortionEpisode(episodeType);
            
            // Visual effects
            CreateEpisodeStartEffects();
            
            // Notification for severe episodes
            if (distortionLevel >= 3)
            {
                Messages.Message(
                    "RealityDistortionEpisode".Translate(pawn.LabelShort, episodeType.ToString()),
                    pawn,
                    MessageTypeDefOf.CautionInput
                );
            }
        }

        private DistortionEpisodeType ChooseEpisodeType()
        {
            List<DistortionEpisodeType> availableTypes = new List<DistortionEpisodeType>();
            
            // Basic episodes always available
            availableTypes.Add(DistortionEpisodeType.VisualHallucination);
            availableTypes.Add(DistortionEpisodeType.AudioHallucination);
            availableTypes.Add(DistortionEpisodeType.PerceptualShift);
            
            // More severe episodes at higher distortion levels
            if (distortionLevel >= 2)
            {
                availableTypes.Add(DistortionEpisodeType.FalseMemory);
                availableTypes.Add(DistortionEpisodeType.IdentityConfusion);
            }
            
            if (distortionLevel >= 3)
            {
                availableTypes.Add(DistortionEpisodeType.PhantomInteraction);
                availableTypes.Add(DistortionEpisodeType.TemporalDisplacement);
            }
            
            if (distortionLevel >= 4)
            {
                availableTypes.Add(DistortionEpisodeType.RealityBreak);
                availableTypes.Add(DistortionEpisodeType.DimensionalBleed);
            }
            
            // Weight recent episodes less likely
            var weightedTypes = availableTypes.Where(type => 
                !recentDistortions.Any(mem => mem.EpisodeType == type && 
                Find.TickManager.TicksGame - mem.OccurrenceTick < 1800)).ToList(); // Not in last 30 seconds
            
            if (weightedTypes.Any())
                return weightedTypes.RandomElement();
            else
                return availableTypes.RandomElement();
        }

        private void ExecuteDistortionEpisode(DistortionEpisodeType episodeType)
        {
            switch (episodeType)
            {
                case DistortionEpisodeType.VisualHallucination:
                    ExecuteVisualHallucination();
                    break;
                case DistortionEpisodeType.AudioHallucination:
                    ExecuteAudioHallucination();
                    break;
                case DistortionEpisodeType.PerceptualShift:
                    ExecutePerceptualShift();
                    break;
                case DistortionEpisodeType.FalseMemory:
                    ExecuteFalseMemory();
                    break;
                case DistortionEpisodeType.IdentityConfusion:
                    ExecuteIdentityConfusion();
                    break;
                case DistortionEpisodeType.PhantomInteraction:
                    ExecutePhantomInteraction();
                    break;
                case DistortionEpisodeType.TemporalDisplacement:
                    ExecuteTemporalDisplacement();
                    break;
                case DistortionEpisodeType.RealityBreak:
                    ExecuteRealityBreak();
                    break;
                case DistortionEpisodeType.DimensionalBleed:
                    ExecuteDimensionalBleed();
                    break;
            }
        }

        private void ExecuteVisualHallucination()
        {
            // Pawn sees things that aren't there
            Vector3 hallucinationPos = pawn.Position.ToVector3Shifted() + new Vector3(
                Rand.Range(-5f, 5f), 0f, Rand.Range(-5f, 5f));
            
            // Intermittent visual effects during episode
            FleckMaker.Static(hallucinationPos.ToIntVec3(), pawn.Map, FleckDefOf.Mote_ItemSparkle, 1f);
            
            // Pawn might react to the hallucination
            if (Rand.Chance(0.7f))
            {
                // Look toward the hallucination
                pawn.rotationTracker.FaceTarget(hallucinationPos.ToIntVec3());
                
                // Maybe try to move away if it's threatening
                if (Rand.Chance(0.3f))
                {
                    IntVec3 fleeTarget = pawn.Position + (pawn.Position - hallucinationPos.ToIntVec3());
                    if (fleeTarget.InBounds(pawn.Map) && fleeTarget.Walkable(pawn.Map))
                    {
                        Job fleeJob = JobMaker.MakeJob(JobDefOf.Goto, fleeTarget);
                        fleeJob.locomotionUrgency = LocomotionUrgency.Jog;
                        pawn.jobs.TryTakeOrderedJob(fleeJob);
                    }
                }
            }
        }

        private void ExecuteAudioHallucination()
        {
            // Pawn hears sounds that aren't there
            SoundDefOf.PsychicPulseGlobal.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            
            // Pawn reacts to the sound
            if (Rand.Chance(0.8f))
            {
                // Look around for the source
                IntVec3 soundDirection = pawn.Position + IntVec3Utility.RandomHorizontalOffset(8);
                pawn.rotationTracker.FaceTarget(soundDirection);
                
                // Might investigate
                if (Rand.Chance(0.4f) && soundDirection.InBounds(pawn.Map) && soundDirection.Walkable(pawn.Map))
                {
                    Job investigateJob = JobMaker.MakeJob(JobDefOf.Goto, soundDirection);
                    investigateJob.locomotionUrgency = LocomotionUrgency.Walk;
                    pawn.jobs.TryTakeOrderedJob(investigateJob);
                }
            }
        }

        private void ExecutePerceptualShift()
        {
            // Pawn's perception of surroundings becomes distorted
            if (pawn.health?.hediffSet != null)
            {
                Hediff perceptualShift = HediffMaker.MakeHediff(
                    DefDatabase<HediffDef>.GetNamedSilentFail("PerceptualDistortion") ?? HediffDefOf.PsychicHangover,
                    pawn
                );
                perceptualShift.Severity = 0.6f;
                pawn.health.AddHediff(perceptualShift);
                
                // Schedule removal
                Find.TickManager.later.ScheduleCallback(() => {
                    if (pawn.health?.hediffSet != null)
                    {
                        pawn.health.RemoveHediff(perceptualShift);
                    }
                }, episodeDuration);
            }
            
            // Visual distortion effects
            for (int i = 0; i < 5; i++)
            {
                Vector3 distortionPos = pawn.DrawPos + new Vector3(
                    Rand.Range(-3f, 3f), 0f, Rand.Range(-3f, 3f));
                FleckMaker.ThrowAirPuffUp(distortionPos, pawn.Map);
            }
        }

        private void ExecuteFalseMemory()
        {
            // Pawn remembers something that didn't happen
            if (pawn.needs?.mood?.thoughts?.memories != null)
            {
                List<ThoughtDef> falseMemories = new List<ThoughtDef>
                {
                    DefDatabase<ThoughtDef>.GetNamedSilentFail("FalseMemoryAttack"),
                    DefDatabase<ThoughtDef>.GetNamedSilentFail("FalseMemoryLoss"),
                    DefDatabase<ThoughtDef>.GetNamedSilentFail("FalseMemoryBetrayal")
                }.Where(t => t != null).ToList();
                
                if (falseMemories.Any())
                {
                    ThoughtDef falseMemory = falseMemories.RandomElement();
                    pawn.needs.mood.thoughts.memories.TryGainMemory(falseMemory);
                }
            }
            
            // Pawn acts confused
            if (Rand.Chance(0.6f))
            {
                Job confusedJob = JobMaker.MakeJob(JobDefOf.Wait_Combat);
                confusedJob.expiryInterval = Rand.Range(120, 300);
                pawn.jobs.TryTakeOrderedJob(confusedJob);
            }
        }

        private void ExecuteIdentityConfusion()
        {
            // Pawn becomes confused about their identity or role
            if (pawn.needs?.mood?.thoughts?.memories != null)
            {
                ThoughtDef identityConf = DefDatabase<ThoughtDef>.GetNamedSilentFail("IdentityConfusion");
                if (identityConf != null)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(identityConf);
                }
            }
            
            // Pawn might stop their current work
            if (pawn.CurJob != null && Rand.Chance(0.7f))
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                
                // Wander aimlessly
                Job wanderJob = JobMaker.MakeJob(JobDefOf.GotoWander, 
                    RCellFinder.RandomWanderDestFor(pawn, pawn.Position, 10f, null, Danger.None));
                pawn.jobs.TryTakeOrderedJob(wanderJob);
            }
        }

        private void ExecutePhantomInteraction()
        {
            // Pawn interacts with something that isn't there
            IntVec3 phantomTarget = pawn.Position + IntVec3Utility.RandomHorizontalOffset(5);
            
            if (phantomTarget.InBounds(pawn.Map) && phantomTarget.Walkable(pawn.Map))
            {
                // Move to phantom target
                Job phantomJob = JobMaker.MakeJob(JobDefOf.Goto, phantomTarget);
                phantomJob.locomotionUrgency = LocomotionUrgency.Walk;
                pawn.jobs.TryTakeOrderedJob(phantomJob);
                
                // Schedule phantom interaction
                Find.TickManager.later.ScheduleCallback(() => {
                    if (pawn.Position.DistanceTo(phantomTarget) <= 3f)
                    {
                        // Phantom interaction effects
                        FleckMaker.Static(phantomTarget, pawn.Map, FleckDefOf.Mote_ItemSparkle, 2f);
                        pawn.rotationTracker.FaceTarget(phantomTarget);
                        
                        // Brief pause as if interacting
                        Job pauseJob = JobMaker.MakeJob(JobDefOf.Wait);
                        pauseJob.expiryInterval = Rand.Range(60, 180);
                        pawn.jobs.TryTakeOrderedJob(pauseJob);
                    }
                }, Rand.Range(150, 300));
            }
        }

        private void ExecuteTemporalDisplacement()
        {
            // Pawn feels displaced in time
            if (pawn.health?.hediffSet != null)
            {
                Hediff temporalShift = HediffMaker.MakeHediff(
                    DefDatabase<HediffDef>.GetNamedSilentFail("TemporalDisplacement") ?? HediffDefOf.PsychicHangover,
                    pawn
                );
                temporalShift.Severity = 0.8f;
                pawn.health.AddHediff(temporalShift);
                
                // Schedule removal
                Find.TickManager.later.ScheduleCallback(() => {
                    if (pawn.health?.hediffSet != null)
                    {
                        pawn.health.RemoveHediff(temporalShift);
                    }
                }, episodeDuration);
            }
            
            // Time distortion visual effects
            for (int i = 0; i < 8; i++)
            {
                Find.TickManager.later.ScheduleCallback(() => {
                    FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastPsychicEffect, 1f);
                }, i * 100);
            }
            
            // Pawn becomes disoriented
            if (Rand.Chance(0.8f))
            {
                Job disorientedJob = JobMaker.MakeJob(JobDefOf.Wait_Combat);
                disorientedJob.expiryInterval = episodeDuration;
                pawn.jobs.TryTakeOrderedJob(disorientedJob);
            }
        }

        private void ExecuteRealityBreak()
        {
            // Severe episode where reality seems to break down
            episodeDuration = Mathf.Max(episodeDuration, 600); // Minimum 10 seconds for severe episodes
            
            // Multiple simultaneous effects
            CreateRealityBreakEffects();
            
            // Severe disorientation
            if (pawn.health?.hediffSet != null)
            {
                Hediff realityBreak = HediffMaker.MakeHediff(
                    DefDatabase<HediffDef>.GetNamedSilentFail("RealityBreak") ?? HediffDefOf.PsychicShock,
                    pawn
                );
                realityBreak.Severity = 1f;
                pawn.health.AddHediff(realityBreak);
                
                // Schedule removal
                Find.TickManager.later.ScheduleCallback(() => {
                    if (pawn.health?.hediffSet != null)
                    {
                        pawn.health.RemoveHediff(realityBreak);
                    }
                }, episodeDuration);
            }
            
            // Pawn might collapse or flee
            if (Rand.Chance(0.5f))
            {
                // Flee in panic
                IntVec3 fleeTarget = RCellFinder.RandomWanderDestFor(pawn, pawn.Position, 15f, null, Danger.None);
                Job fleeJob = JobMaker.MakeJob(JobDefOf.Goto, fleeTarget);
                fleeJob.locomotionUrgency = LocomotionUrgency.Sprint;
                pawn.jobs.TryTakeOrderedJob(fleeJob);
            }
            else
            {
                // Collapse in place
                Job collapseJob = JobMaker.MakeJob(JobDefOf.LayDown, pawn.Position);
                collapseJob.expiryInterval = episodeDuration / 2;
                pawn.jobs.TryTakeOrderedJob(collapseJob);
            }
        }

        private void ExecuteDimensionalBleed()
        {
            // Reality bleeds through from the Beach dimension
            CreateDimensionalBleedEffects();
            
            // Temporary connection to Beach dimension
            float oldRealityAnchor = realityAnchor;
            realityAnchor *= 0.3f; // Severely reduced connection to reality
            
            // Schedule restoration
            Find.TickManager.later.ScheduleCallback(() => {
                realityAnchor = oldRealityAnchor;
            }, episodeDuration);
            
            // Apply dimensional exposure
            if (pawn.health?.hediffSet != null)
            {
                Hediff dimensionalExp = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.BeachDrift, pawn);
                dimensionalExp.Severity = Math.Min(1f, dimensionalExp.Severity + 0.3f);
                pawn.health.AddHediff(dimensionalExp);
            }
            
            // Pawn experiences otherworldly phenomena
            ExecuteOtherworldlyPhenomena();
        }

        private void CreateRealityBreakEffects()
        {
            // Intense visual disturbances
            for (int i = 0; i < 12; i++)
            {
                Find.TickManager.later.ScheduleCallback(() => {
                    Vector3 effectPos = pawn.DrawPos + new Vector3(
                        Rand.Range(-4f, 4f), 0f, Rand.Range(-4f, 4f));
                    FleckMaker.Static(effectPos.ToIntVec3(), pawn.Map, FleckDefOf.ExplosionFlash, 0.5f);
                }, i * 50);
            }
            
            // Reality distortion waves
            for (int wave = 1; wave <= 3; wave++)
            {
                Find.TickManager.later.ScheduleCallback(() => {
                    for (int point = 0; point < wave * 8; point++)
                    {
                        float angle = (float)point / (wave * 8) * 2f * Mathf.PI;
                        Vector3 wavePos = pawn.DrawPos + new Vector3(
                            Mathf.Cos(angle) * wave * 3f, 0f, Mathf.Sin(angle) * wave * 3f);
                        FleckMaker.Static(wavePos.ToIntVec3(), pawn.Map, FleckDefOf.PsycastPsychicEffect, 1f);
                    }
                }, wave * 200);
            }
        }

        private void CreateDimensionalBleedEffects()
        {
            // Dimensional portal effects
            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.ExplosionFlash, 3f);
            
            // Beach dimension "bleeding through"
            for (int i = 0; i < 20; i++)
            {
                Find.TickManager.later.ScheduleCallback(() => {
                    Vector3 bleedPos = pawn.DrawPos + new Vector3(
                        Rand.Range(-6f, 6f), Rand.Range(0f, 2f), Rand.Range(-6f, 6f));
                    FleckMaker.ThrowSmoke(bleedPos, pawn.Map, 2f);
                }, i * 30);
            }
            
            // Sound of dimensional disturbance
            SoundDefOf.PsycastPsychicPulse.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
        }

        private void ExecuteOtherworldlyPhenomena()
        {
            // Pawn might see "ghosts" or other supernatural entities
            for (int i = 0; i < 3; i++)
            {
                IntVec3 phantomPos = pawn.Position + IntVec3Utility.RandomHorizontalOffset(8);
                if (phantomPos.InBounds(pawn.Map))
                {
                    Find.TickManager.later.ScheduleCallback(() => {
                        FleckMaker.Static(phantomPos, pawn.Map, FleckDefOf.Mote_ItemSparkle, 2f);
                        
                        // Pawn might react to phantom
                        if (pawn.Position.DistanceTo(phantomPos) <= 12f && Rand.Chance(0.6f))
                        {
                            pawn.rotationTracker.FaceTarget(phantomPos);
                        }
                    }, i * 150);
                }
            }
        }

        private void HandleOngoingEpisode()
        {
            episodeDuration--;
            
            if (episodeDuration <= 0)
            {
                EndCurrentEpisode();
            }
            else
            {
                // Ongoing episode effects
                CreateOngoingEpisodeEffects();
            }
        }

        private void CreateOngoingEpisodeEffects()
        {
            // Periodic effects during episode
            if (Rand.Chance(0.1f))
            {
                FleckMaker.ThrowAirPuffUp(pawn.DrawPos, pawn.Map);
            }
            
            // Intensity-based effects
            if (distortionLevel >= 3 && Rand.Chance(0.05f))
            {
                FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastPsychicEffect, 0.8f);
            }
        }

        private void EndCurrentEpisode()
        {
            isHavingEpisode = false;
            episodeDuration = 0;
            perceivedThreat = null;
            
            // Episode end effects
            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.Mote_ItemSparkle, 1f);
            
            // Slight recovery of reality anchor
            realityAnchor = Math.Min(1f, realityAnchor + 0.1f);
        }

        private void RecordDistortionEpisode(DistortionEpisodeType episodeType)
        {
            recentDistortions.Add(new DistortionMemory
            {
                EpisodeType = episodeType,
                OccurrenceTick = Find.TickManager.TicksGame,
                Severity = distortionLevel
            });
            
            // Limit memory list
            if (recentDistortions.Count > 10)
            {
                recentDistortions.RemoveAt(0);
            }
        }

        private void CreateEpisodeStartEffects()
        {
            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastPsychicEffect, 2f);
            
            // Sound effect
            if (distortionLevel >= 2)
            {
                SoundDefOf.PsychicPulseGlobal.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            }
        }

        private void UpdateDistortionParameters()
        {
            // Recalculate distortion level periodically
            if (age % 1800 == 0) // Every 30 seconds
            {
                CalculateDistortionLevel();
                CalculateRealityAnchor();
            }
        }

        private void CalculateDistortionLevel()
        {
            distortionLevel = 1; // Base level
            
            // Increase with Beach-related conditions
            if (pawn.health?.hediffSet != null)
            {
                Hediff beachDrift = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BeachDrift);
                if (beachDrift != null)
                {
                    distortionLevel += Mathf.RoundToInt(beachDrift.Severity * 2f);
                }
                
                Hediff btTether = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
                if (btTether != null)
                {
                    distortionLevel += Mathf.RoundToInt(btTether.Severity * 1.5f);
                }
                
                Hediff timefallExp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.TimefallExposure);
                if (timefallExp != null)
                {
                    distortionLevel += Mathf.RoundToInt(timefallExp.Severity);
                }
            }
            
            // DOOMS carriers have more intense episodes
            int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(pawn);
            if (doomsLevel > 0)
            {
                distortionLevel += doomsLevel;
            }
            
            // Environmental factors
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(pawn.Map);
            if (beachThreat > 0.6f)
            {
                distortionLevel += 1;
            }
            
            // Time in state
            if (age > 3600) // After 1 hour
            {
                distortionLevel += 1;
            }
            
            distortionLevel = Mathf.Clamp(distortionLevel, 1, 5);
        }

        private void CalculateRealityAnchor()
        {
            realityAnchor = 1f; // Start with full connection
            
            // Reduce with various factors
            
            // Recent episodes weaken anchor
            int recentEpisodeCount = recentDistortions.Count(d => 
                Find.TickManager.TicksGame - d.OccurrenceTick < 3600); // Last hour
            realityAnchor -= recentEpisodeCount * 0.1f;
            
            // Social connections strengthen anchor
            if (pawn.relations?.RelatedPawns?.Any() == true)
            {
                realityAnchor += 0.2f;
            }
            
            // Recent social interaction helps
            if (pawn.interactions?.InteractedWithAnyoneRecently() == true)
            {
                realityAnchor += 0.15f;
            }
            
            // Comfortable environment helps
            Room room = pawn.Position.GetRoom(pawn.Map);
            if (room != null && !room.PsychologicallyOutdoors)
            {
                realityAnchor += 0.1f;
                
                // Well-decorated rooms help more
                if (room.GetStat(RoomStatDefOf.Beauty) > 0)
                {
                    realityAnchor += 0.1f;
                }
            }
            
            // Medical treatment helps
            if (pawn.health?.HasHediffsNeedingTend() == false)
            {
                realityAnchor += 0.1f;
            }
            
            realityAnchor = Mathf.Clamp(realityAnchor, 0.1f, 1.5f);
        }

        private void DecayDistortionMemories()
        {
            if (age % DISTORTION_DECAY_INTERVAL == 0)
            {
                // Remove old distortion memories
                recentDistortions.RemoveAll(d => 
                    Find.TickManager.TicksGame - d.OccurrenceTick > 7200); // Older than 2 hours
            }
        }

        private void CreateDistortionEffects()
        {
            // Ambient reality distortion effects
            if (Rand.Chance(0.01f))
            {
                Vector3 effectPos = pawn.DrawPos + new Vector3(
                    Rand.Range(-2f, 2f), 0f, Rand.Range(-2f, 2f));
                FleckMaker.ThrowAirPuffUp(effectPos, pawn.Map);
            }
            
            // More intense effects during episodes
            if (isHavingEpisode && Rand.Chance(0.05f))
            {
                FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastPsychicEffect, 0.5f);
            }
        }

        private void ApplyInitialDistortionEffects()
        {
            if (pawn.health?.hediffSet == null) return;
            
            // Apply reality distortion hediff
            Hediff distortion = HediffMaker.MakeHediff(
                DefDatabase<HediffDef>.GetNamedSilentFail("RealityDistortion") ?? HediffDefOf.PsychicHangover,
                pawn
            );
            distortion.Severity = 0.4f;
            pawn.health.AddHediff(distortion);
        }

        private void RemoveDistortionEffects()
        {
            if (pawn.health?.hediffSet == null) return;
            
            // Remove temporary distortion effects
            var distortionEffects = pawn.health.hediffSet.hediffs
                .Where(h => h.def.defName.Contains("Distortion") || 
                           h.def.defName.Contains("Reality") ||
                           h.def.defName.Contains("Temporal"))
                .ToList();
            
            foreach (Hediff effect in distortionEffects)
            {
                pawn.health.RemoveHediff(effect);
            }
        }

        private void ApplyRecoveryMemory()
        {
            if (pawn.needs?.mood?.thoughts?.memories == null) return;
            
            // Add memory of the distortion experience
            ThoughtDef recoveryMemory = DefDatabase<ThoughtDef>.GetNamedSilentFail("RealityDistortionRecovery");
            if (recoveryMemory != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(recoveryMemory);
            }
            
            // Different memory based on severity
            if (episodeCounter >= 5)
            {
                ThoughtDef severeMemory = DefDatabase<ThoughtDef>.GetNamedSilentFail("SevereRealityDistortion");
                if (severeMemory != null)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(severeMemory);
                }
            }
        }

        private bool ShouldRecover()
        {
            // Base recovery chance
            float recoveryChance = 0.001f;
            
            // Strong reality anchor improves recovery
            recoveryChance *= realityAnchor;
            
            // Time in state increases recovery chance
            if (age > 1800) recoveryChance *= 2f; // After 30 minutes
            if (age > 3600) recoveryChance *= 3f; // After 1 hour
            
            // Recent social interaction helps
            if (pawn.interactions?.InteractedWithAnyoneRecently() == true)
            {
                recoveryChance *= 3f;
            }
            
            // Medical treatment helps
            if (pawn.health?.HasHediffsNeedingTend() == false)
            {
                recoveryChance *= 1.5f;
            }
            
            // Being in a safe, comfortable place helps
            Room room = pawn.Position.GetRoom(pawn.Map);
            if (room != null && !room.PsychologicallyOutdoors)
            {
                recoveryChance *= 2f;
                
                if (room.GetStat(RoomStatDefOf.Beauty) > 0)
                {
                    recoveryChance *= 1.5f;
                }
            }
            
            // DOOMS carriers recover differently
            if (DeathStrandingUtility.GetDOOMSLevel(pawn) > 0)
            {
                recoveryChance *= 1.3f; // They're more resilient
            }
            
            // If too many episodes, force recovery
            if (episodeCounter > 8)
            {
                recoveryChance *= 10f;
            }
            
            return Rand.Chance(recoveryChance);
        }

        public override string GetInspectLine()
        {
            string inspectLine = "RealityDistortion".Translate();
            
            if (distortionLevel > 1)
            {
                inspectLine += " (" + "Level".Translate() + " " + distortionLevel + ")";
            }
            
            if (isHavingEpisode)
            {
                inspectLine += " [" + "Episode".Translate() + "]";
            }
            
            if (episodeCounter > 0)
            {
                inspectLine += " (" + episodeCounter + " " + "Episodes".Translate() + ")";
            }
            
            return inspectLine;
        }
    }

    /// <summary>
    /// Mental state worker for reality distortion episodes
    /// </summary>
    public class MentalStateWorker_RealityDistortion : MentalStateWorker
    {
        public override bool StateCanOccur(Pawn p)
        {
            if (!base.StateCanOccur(p)) return false;
            
            // Requires some form of supernatural exposure
            if (p.health?.hediffSet == null) return false;
            
            // Must have Beach-related conditions or DOOMS
            bool hasSupernaturalExposure = 
                p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BeachDrift) != null ||
                p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether) != null ||
                p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.TimefallExposure) != null ||
                DeathStrandingUtility.GetDOOMSLevel(p) > 0;
            
            if (!hasSupernaturalExposure) return false;
            
            // Environmental requirements
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(p.Map);
            if (beachThreat < 0.3f) return false;
            
            // Mental state requirements
            if (p.needs?.mood?.CurLevel > 0.7f) return false; // Too happy/stable
            
            return true;
        }

        public override float CommonalityFor(Pawn pawn, bool moodCaused = false)
        {
            float baseCommonality = base.CommonalityFor(pawn, moodCaused);
            
            // Increase based on supernatural exposure severity
            if (pawn.health?.hediffSet != null)
            {
                // Beach drift greatly increases chance
                Hediff beachDrift = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BeachDrift);
                if (beachDrift != null)
                {
                    baseCommonality *= (1f + beachDrift.Severity * 2f);
                }
                
                // BT tether increases chance
                Hediff btTether = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
                if (btTether != null)
                {
                    baseCommonality *= (1f + btTether.Severity * 1.5f);
                }
                
                // Timefall exposure contributes
                Hediff timefallExp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.TimefallExposure);
                if (timefallExp != null)
                {
                    baseCommonality *= (1f + timefallExp.Severity);
                }
            }
            
            // DOOMS carriers more susceptible
            int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(pawn);
            if (doomsLevel > 0)
            {
                baseCommonality *= (1f + doomsLevel * 0.5f);
            }
            
            // Environmental factors
            float beachThreat = DeathStrandingUtility.CalculateBeachThreatLevel(pawn.Map);
            baseCommonality *= (1f + beachThreat * 1.5f);
            
            // Time factors
            float hourOfDay = GenLocalDate.HourFloat(pawn.Map);
            if (hourOfDay < 6f || hourOfDay > 20f) // Night time
            {
                baseCommonality *= 1.8f;
            }
            else if (hourOfDay < 8f || hourOfDay > 18f) // Dawn/dusk
            {
                baseCommonality *= 1.3f;
            }
            
            // Isolation increases susceptibility
            if (pawn.Position.GetRoom(pawn.Map)?.PsychologicallyOutdoors == true)
            {
                baseCommonality *= 1.4f;
            }
            
            // Recent social interaction reduces chance
            if (pawn.interactions?.InteractedWithAnyoneRecently() == true)
            {
                baseCommonality *= 0.7f;
            }
            
            // Mood state affects susceptibility
            float moodLevel = pawn.needs?.mood?.CurLevel ?? 0.5f;
            if (moodLevel < 0.3f)
            {
                baseCommonality *= 2f; // Very unhappy = more susceptible
            }
            else if (moodLevel < 0.5f)
            {
                baseCommonality *= 1.5f; // Somewhat unhappy = more susceptible
            }
            
            // During timefall events
            if (DeathStrandingUtility.IsTimefallActive(pawn.Map))
            {
                baseCommonality *= 2.5f;
            }
            
            return baseCommonality;
        }
    }

    // ==================== SUPPORTING CLASSES AND ENUMS ====================
    
    /// <summary>
    /// Types of reality distortion episodes
    /// </summary>
    public enum DistortionEpisodeType
    {
        VisualHallucination,    // Seeing things that aren't there
        AudioHallucination,     // Hearing things that aren't there
        PerceptualShift,        // Distorted perception of reality
        FalseMemory,           // Remembering things that didn't happen
        IdentityConfusion,      // Confusion about self or role
        PhantomInteraction,     // Interacting with things that aren't there
        TemporalDisplacement,   // Feeling displaced in time
        RealityBreak,          // Severe breakdown of reality perception
        DimensionalBleed       // Beach dimension bleeding through
    }

    /// <summary>
    /// Memory of a distortion episode
    /// </summary>
    public class DistortionMemory : IExposable
    {
        public DistortionEpisodeType EpisodeType;
        public int OccurrenceTick;
        public int Severity;
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref EpisodeType, "episodeType");
            Scribe_Values.Look(ref OccurrenceTick, "occurrenceTick");
            Scribe_Values.Look(ref Severity, "severity");
        }
    }

    /// <summary>
    /// Utility class for managing reality distortion effects
    /// </summary>
    public static class RealityDistortionUtility
    {
        /// <summary>
        /// Triggers a temporary reality distortion episode in a pawn
        /// </summary>
        public static void TriggerDistortionEpisode(Pawn pawn, DistortionEpisodeType episodeType, int duration = 300)
        {
            if (pawn.InMentalState && pawn.MentalState is MentalState_RealityDistortion distortionState)
            {
                // Force a specific episode if already in distortion state
                distortionState.isHavingEpisode = true;
                distortionState.episodeDuration = duration;
            }
            else
            {
                // Try to trigger the mental state
                MentalStateDef distortionStateDef = DefDatabase<MentalStateDef>.GetNamedSilentFail("RealityDistortion");
                if (distortionStateDef != null)
                {
                    pawn.mindState.mentalStateHandler.TryStartMentalState(distortionStateDef, "Reality distortion triggered");
                }
            }
        }

        /// <summary>
        /// Calculates how prone a pawn is to reality distortion
        /// </summary>
        public static float CalculateDistortionSusceptibility(Pawn pawn)
        {
            float susceptibility = 0f;
            
            if (pawn.health?.hediffSet != null)
            {
                // Beach-related hediffs increase susceptibility
                Hediff beachDrift = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BeachDrift);
                if (beachDrift != null)
                {
                    susceptibility += beachDrift.Severity * 0.3f;
                }
                
                Hediff btTether = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
                if (btTether != null)
                {
                    susceptibility += btTether.Severity * 0.2f;
                }
                
                Hediff timefallExp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.TimefallExposure);
                if (timefallExp != null)
                {
                    susceptibility += timefallExp.Severity * 0.15f;
                }
            }
            
            // DOOMS level affects susceptibility
            int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(pawn);
            susceptibility += doomsLevel * 0.1f;
            
            // Traits affect susceptibility
            if (pawn.story?.traits != null)
            {
                if (pawn.story.traits.HasTrait(TraitDefOf.Neurotic))
                {
                    susceptibility += 0.15f;
                }
                
                if (pawn.story.traits.HasTrait(TraitDefOf.Psychopath))
                {
                    susceptibility -= 0.1f; // Less empathetic = less susceptible to some effects
                }
                
                if (pawn.story.traits.HasTrait(TraitDefOf.Tough))
                {
                    susceptibility -= 0.1f;
                }
            }
            
            // Social connections provide stability
            if (pawn.relations?.RelatedPawns?.Any() == true)
            {
                susceptibility -= 0.1f;
            }
            
            return Mathf.Clamp(susceptibility, 0f, 1f);
        }

        /// <summary>
        /// Applies protective effects against reality distortion
        /// </summary>
        public static void ApplyRealityStabilization(Pawn pawn, float strength = 0.5f, int duration = 3600)
        {
            if (pawn.health?.hediffSet == null) return;
            
            Hediff stabilization = HediffMaker.MakeHediff(
                DefDatabase<HediffDef>.GetNamedSilentFail("RealityStabilization") ?? HediffDefOf.Go,
                pawn
            );
            stabilization.Severity = strength;
            pawn.health.AddHediff(stabilization);
            
            // Schedule removal
            Find.TickManager.later.ScheduleCallback(() => {
                if (pawn.health?.hediffSet != null)
                {
                    pawn.health.RemoveHediff(stabilization);
                }
            }, duration);
        }

        /// <summary>
        /// Creates area-wide reality distortion effects
        /// </summary>
        public static void CreateAreaDistortionField(Map map, IntVec3 center, float radius, int duration = 1800)
        {
            // Find pawns in the area
            var affectedPawns = GenRadial.RadialDistinctThingsAround(center, map, radius, true)
                .OfType<Pawn>()
                .Where(p => p.RaceProps.Humanlike)
                .ToList();
            
            // Apply distortion effects to each pawn
            foreach (Pawn pawn in affectedPawns)
            {
                float distance = pawn.Position.DistanceTo(center);
                float effectStrength = 1f - (distance / radius);
                
                if (effectStrength > 0.2f && Rand.Chance(effectStrength))
                {
                    TriggerDistortionEpisode(pawn, DistortionEpisodeType.PerceptualShift);
                }
            }
            
            // Visual effects for the field
            CreateDistortionFieldEffects(map, center, radius, duration);
        }

        private static void CreateDistortionFieldEffects(Map map, IntVec3 center, float radius, int duration)
        {
            int effectCount = Mathf.RoundToInt(radius * 2f);
            
            for (int i = 0; i < effectCount; i++)
            {
                Find.TickManager.later.ScheduleCallback(() => {
                    if (Rand.Chance(0.3f))
                    {
                        Vector3 effectPos = center.ToVector3Shifted() + new Vector3(
                            Rand.Range(-radius, radius),
                            0f,
                            Rand.Range(-radius, radius)
                        );
                        
                        if (effectPos.ToIntVec3().InBounds(map))
                        {
                            FleckMaker.Static(effectPos.ToIntVec3(), map, FleckDefOf.PsycastPsychicEffect, 1f);
                        }
                    }
                }, Rand.Range(0, duration));
            }
        }
    }
}