using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using DeathStrandingMod.Core;

namespace DeathStrandingMod.Components
{
    /// <summary>
    /// Component for BT creatures that applies tether effects to nearby pawns
    /// </summary>
    public class CompBTTether : ThingComp
    {
        private CompProperties_BTTether Props => (CompProperties_BTTether)props;
        private List<Pawn> currentlyTetheredPawns = new List<Pawn>();
        private Dictionary<Pawn, float> tetherStrengths = new Dictionary<Pawn, float>();
        private int lastTetherUpdateTick = 0;
        
        public override void CompTick()
        {
            if (parent.IsHashIntervalTick(60)) // Check every 60 ticks (~1 second)
            {
                UpdateTetheredPawns();
                ApplyTetherEffects();
                CheckForVoidoutTriggers();
                lastTetherUpdateTick = Find.TickManager.TicksGame;
            }
        }

        private void UpdateTetheredPawns()
        {
            currentlyTetheredPawns.Clear();
            tetherStrengths.Clear();
            
            if (parent?.Map == null || parent.Destroyed) return;
            
            foreach (Pawn pawn in parent.Map.mapPawns.AllPawnsSpawned)
            {
                if (!IsValidTetherTarget(pawn)) continue;
                
                float distance = pawn.Position.DistanceTo(parent.Position);
                if (distance <= Props.tetherRange)
                {
                    if (HasLineOfEffect(pawn))
                    {
                        currentlyTetheredPawns.Add(pawn);
                        tetherStrengths[pawn] = CalculateTetherStrength(pawn, distance);
                    }
                }
            }
        }

        private bool IsValidTetherTarget(Pawn pawn)
        {
            return pawn != null &&
                   !pawn.Dead &&
                   pawn.RaceProps.Humanlike &&
                   pawn.Faction?.IsPlayer == true &&
                   !pawn.InMentalState;
        }

        private bool HasLineOfEffect(Pawn pawn)
        {
            // BTs can "see" through some obstacles but not heavy cover
            if (GenSight.LineOfSight(parent.Position, pawn.Position, parent.Map, true))
            {
                return true;
            }
            
            // Check for thin walls or partial cover
            return GenSight.LineOfSight(parent.Position, pawn.Position, parent.Map, false, null, 0, 3);
        }

        private float CalculateTetherStrength(Pawn pawn, float distance)
        {
            float baseStrength = Props.tetherStrength;
            
            // Distance falloff
            float distanceFactor = 1f - (distance / Props.tetherRange);
            distanceFactor = Mathf.Clamp01(distanceFactor);
            
            // DOOMS resistance/vulnerability
            float doomsModifier = GetDOOMSModifier(pawn);
            
            // Environmental factors
            float environmentalModifier = GetEnvironmentalModifier();
            
            // BT type modifier
            float btTypeModifier = GetBTTypeModifier();
            
            return baseStrength * distanceFactor * doomsModifier * environmentalModifier * btTypeModifier;
        }

        private float GetDOOMSModifier(Pawn pawn)
        {
            if (pawn.genes == null) return 1f;
            
            var doomsGene = pawn.genes.GenesListForReading
                .FirstOrDefault(g => g.def.defName.StartsWith("DOOMS_"));
            
            if (doomsGene?.def.GetModExtension<DOOMSProperties>() is DOOMSProperties props)
            {
                // Higher DOOMS levels have more resistance but also attract more attention
                float resistance = props.tetherResistance;
                float attraction = props.level * 0.1f; // Level 8 = 80% more attraction
                
                return (1f - resistance) * (1f + attraction);
            }
            
            return 1f; // Normal humans have no special resistance
        }

        private float GetEnvironmentalModifier()
        {
            // Stronger during timefall
            if (parent.Map.weatherManager.curWeather.defName.Contains("Timefall"))
            {
                return 1.5f;
            }
            
            // Weaker under chiral protection
            if (DeathStrandingUtility.IsUnderChiralProtection(parent.Position, parent.Map))
            {
                return 0.3f;
            }
            
            return 1f;
        }

        private float GetBTTypeModifier()
        {
            BTProperties btProps = parent.def.GetModExtension<BTProperties>();
            return btProps?.tetherStrength ?? 1f;
        }

        private void ApplyTetherEffects()
        {
            foreach (var kvp in tetherStrengths)
            {
                Pawn pawn = kvp.Key;
                float strength = kvp.Value;
                
                if (pawn.Dead || pawn.Map != parent.Map) continue;
                
                ApplyTetherToPawn(pawn, strength);
                CreateTetherVisualEffects(pawn, strength);
            }
        }

        private void ApplyTetherToPawn(Pawn pawn, float strength)
        {
            Hediff tetherHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
            
            if (tetherHediff == null)
            {
                tetherHediff = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.BTTether, pawn);
                pawn.health.AddHediff(tetherHediff);
                
                Messages.Message(
                    "BTTetherInitiated".Translate(pawn.LabelShort),
                    pawn,
                    MessageTypeDefOf.NegativeEvent
                );
            }
            
            // Increase tether severity
            float severityIncrease = strength * Props.tetherBuildupRate;
            tetherHediff.Severity += severityIncrease;
            
            // Cap at maximum severity
            tetherHediff.Severity = Mathf.Clamp01(tetherHediff.Severity);
            
            // Apply additional effects based on severity
            ApplyTetherSeverityEffects(pawn, tetherHediff);
        }

        private void ApplyTetherSeverityEffects(Pawn pawn, Hediff tetherHediff)
        {
            float severity = tetherHediff.Severity;
            
            // Moderate tether (0.3+): Occasional disturbing thoughts
            if (severity >= 0.3f && Rand.Chance(0.1f))
            {
                ThoughtDef disturbingThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("BTTetherDisturbance");
                if (disturbingThought != null)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(disturbingThought);
                }
            }
            
            // Strong tether (0.6+): Sleep disruption
            if (severity >= 0.6f && pawn.needs.rest != null)
            {
                pawn.needs.rest.CurLevel -= 0.001f; // Gradual sleep loss
            }
            
            // Critical tether (0.8+): Potential mental breaks
            if (severity >= 0.8f && Rand.Chance(0.05f))
            {
                TriggerTetherMentalState(pawn);
            }
            
            // Maximum tether (0.95+): Voidout risk
            if (severity >= 0.95f && Props.canTriggerVoidout)
            {
                ConsiderVoidoutTrigger(pawn, severity);
            }
        }

        private void TriggerTetherMentalState(Pawn pawn)
        {
            List<MentalStateDef> possibleStates = new List<MentalStateDef>
            {
                MentalStateDefOf.Wander_Sad,
                MentalStateDefOf.Wander_Psychotic,
                MentalStateDefOf_DeathStranding.BeachDrift
            };
            
            MentalStateDef chosenState = possibleStates.Where(s => s != null).RandomElementWithFallback();
            if (chosenState != null && pawn.mindState.mentalStateHandler.TryStartMentalState(chosenState))
            {
                Messages.Message(
                    "BTTetherMentalBreak".Translate(pawn.LabelShort),
                    pawn,
                    MessageTypeDefOf.NegativeEvent
                );
            }
        }

        private void CreateTetherVisualEffects(Pawn pawn, float strength)
        {
            if (!Props.showTetherEffects) return;
            
            // Draw tether line
            if (strength > 0.3f && Rand.Chance(0.3f))
            {
                Vector3 btPos = parent.DrawPos;
                Vector3 pawnPos = pawn.DrawPos;
                
                // Create a subtle line effect
                FleckMaker.ConnectingLine(btPos, pawnPos, FleckDefOf.AirPuff, parent.Map);
            }
            
            // Pawn disturbance effects
            if (strength > 0.5f && Rand.Chance(0.2f))
            {
                FleckMaker.ThrowAirPuffUp(pawn.DrawPos, pawn.Map);
            }
            
            // Critical tether warning
            if (strength > 0.8f && Find.TickManager.TicksGame % 120 == 0)
            {
                FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1f);
            }
        }

        private void CheckForVoidoutTriggers()
        {
            if (!Props.canTriggerVoidout) return;
            
            foreach (Pawn pawn in currentlyTetheredPawns)
            {
                Hediff tether = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
                if (tether?.Severity >= 0.95f)
                {
                    ConsiderVoidoutTrigger(pawn, tether.Severity);
                }
            }
        }

        private void ConsiderVoidoutTrigger(Pawn pawn, float tetherSeverity)
        {
            float voidoutChance = (tetherSeverity - 0.9f) * 2f; // 0-20% chance at max tether
            
            // Increase chance for high DOOMS levels
            int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(pawn);
            if (doomsLevel >= 6)
            {
                voidoutChance *= (1f + doomsLevel * 0.2f);
            }
            
            // Environmental factors
            if (parent.Map.weatherManager.curWeather.defName.Contains("Timefall"))
            {
                voidoutChance *= 1.5f;
            }
            
            if (Rand.Chance(voidoutChance / 1000f)) // Per-tick chance
            {
                TriggerVoidoutEvent(pawn);
            }
        }

        private void TriggerVoidoutEvent(Pawn triggerPawn)
        {
            Messages.Message(
                "VoidoutTriggeredByTether".Translate(triggerPawn.LabelShort),
                triggerPawn,
                MessageTypeDefOf.ThreatBig
            );
            
            IncidentParms voidoutParms = new IncidentParms
            {
                target = parent.Map,
                spawnCenter = triggerPawn.Position,
                points = 1000f, // High threat level
                forced = true
            };
            
            IncidentDefOf_DeathStranding.Voidout.Worker.TryExecute(voidoutParms);
        }

        public override void PostDraw()
        {
            if (Props.showTetherRangeWhenSelected && Find.Selector.IsSelected(parent))
            {
                GenDraw.DrawFieldEdges(
                    GenRadial.RadialCellsAround(parent.Position, Props.tetherRange, false).ToList(),
                    Color.red,
                    null
                );
            }
        }

        public override string CompInspectStringExtra()
        {
            var inspectStrings = new List<string>();
            
            if (currentlyTetheredPawns.Any())
            {
                inspectStrings.Add("BTTetherActive".Translate(currentlyTetheredPawns.Count));
                
                // Show most critically tethered pawn
                var mostTethered = currentlyTetheredPawns
                    .Select(p => new { Pawn = p, Severity = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether)?.Severity ?? 0f })
                    .OrderByDescending(x => x.Severity)
                    .FirstOrDefault();
                    
                if (mostTethered != null && mostTethered.Severity > 0.5f)
                {
                    inspectStrings.Add("BTTetherStrongest".Translate(
                        mostTethered.Pawn.LabelShort, 
                        (mostTethered.Severity * 100f).ToString("F0")
                    ));
                }
            }
            else
            {
                inspectStrings.Add("BTTetherNoTargets".Translate(Props.tetherRange.ToString("F1")));
            }
            
            if (parent.Map.weatherManager.curWeather.defName.Contains("Timefall"))
            {
                inspectStrings.Add("BTTetherTimefallBoost".Translate());
            }
            
            return string.Join("\n", inspectStrings);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref currentlyTetheredPawns, "currentlyTetheredPawns", LookMode.Reference);
            Scribe_Values.Look(ref lastTetherUpdateTick, "lastTetherUpdateTick");
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                currentlyTetheredPawns?.RemoveAll(p => p == null);
                tetherStrengths?.Clear();
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Max tether nearby",
                    action = () => {
                        foreach (Pawn pawn in parent.Map.mapPawns.FreeColonistsSpawned)
                        {
                            if (pawn.Position.DistanceTo(parent.Position) <= Props.tetherRange)
                            {
                                Hediff tether = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.BTTether, pawn);
                                tether.Severity = 0.95f;
                                pawn.health.AddHediff(tether);
                            }
                        }
                    }
                };
                
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Clear all tethers",
                    action = () => {
                        foreach (Pawn pawn in parent.Map.mapPawns.AllPawnsSpawned)
                        {
                            Hediff tether = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
                            if (tether != null)
                            {
                                pawn.health.RemoveHediff(tether);
                            }
                        }
                    }
                };
            }
        }
    }

    /// <summary>
    /// Properties for BT tethering components
    /// </summary>
    public class CompProperties_BTTether : CompProperties
    {
        public float tetherStrength = 0.1f;
        public float tetherRange = 8f;
        public float tetherBuildupRate = 0.001f; // Per tick
        public bool canTriggerVoidout = false;
        public bool showTetherEffects = true;
        public bool showTetherRangeWhenSelected = true;

        public CompProperties_BTTether()
        {
            compClass = typeof(CompBTTether);
        }
    }
}