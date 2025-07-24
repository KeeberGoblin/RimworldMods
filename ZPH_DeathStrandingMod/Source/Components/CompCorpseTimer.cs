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
    /// Component that handles the conversion of corpses into BTs over time
    /// </summary>
    public class CompCorpseTimer : ThingComp
    {
        private CompProperties_CorpseTimer Props => (CompProperties_CorpseTimer)props;
        public int ticksUntilConversion;
        private bool hasStartedDecay = false;
        private bool conversionSlowed = false;
        private int slowdownEndTick = 0;
        private int lastWarningStage = 0;
        
        public float ConversionProgress => 1f - ((float)ticksUntilConversion / Props.baseConversionTime);
        public bool IsNearConversion => ticksUntilConversion <= Props.baseConversionTime * 0.2f;
        public bool IsCriticalStage => ticksUntilConversion <= Props.baseConversionTime * 0.1f;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                ticksUntilConversion = Props.baseConversionTime;
                // Add some randomness to prevent simultaneous conversions
                ticksUntilConversion += Rand.Range(-10000, 10000);
                ticksUntilConversion = Math.Max(1000, ticksUntilConversion); // Minimum 1000 ticks
            }
        }

        public override void CompTick()
        {
            if (!(parent is Corpse corpse) || !ShouldConvert(corpse))
                return;

            if (!hasStartedDecay)
            {
                InitiateDecayProcess(corpse);
            }

            ProcessConversion(corpse);
            UpdateVisualEffects(corpse);
        }

        private bool ShouldConvert(Corpse corpse)
        {
            // Only humanlike corpses convert
            if (!corpse.InnerPawn.RaceProps.Humanlike)
                return false;
                
            // Don't convert if already processed (cremated, buried, etc.)
            if (corpse.Destroyed || corpse.Map == null)
                return false;
                
            // Don't convert corpses of certain factions or special pawns
            if (ShouldBeExemptFromConversion(corpse.InnerPawn))
                return false;
                
            return true;
        }

        private bool ShouldBeExemptFromConversion(Pawn pawn)
        {
            // High-level DOOMS carriers might resist conversion
            int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(pawn);
            if (doomsLevel >= 7 && Rand.Chance(0.3f))
            {
                Messages.Message(
                    "HighDOOMSCorpseResistance".Translate(pawn.LabelShort),
                    MessageTypeDefOf.PositiveEvent
                );
                return true;
            }
            
            // Certain backstories or traits might provide resistance
            if (pawn.story?.traits?.HasTrait(TraitDefOf.Psychopath) == true)
            {
                if (Rand.Chance(0.2f))
                {
                    Messages.Message(
                        "PsychopathCorpseResistance".Translate(pawn.LabelShort),
                        MessageTypeDefOf.PositiveEvent
                    );
                    return true;
                }
            }
            
            // Mechanoids and other non-organic entities
            if (!pawn.RaceProps.IsFlesh)
                return true;
            
            return false;
        }

        private void InitiateDecayProcess(Corpse corpse)
        {
            hasStartedDecay = true;
            
            Messages.Message(
                "CorpseDecayStarted".Translate(corpse.InnerPawn.LabelShort),
                corpse,
                MessageTypeDefOf.NegativeEvent
            );
            
            // Add decay hediff to corpse for tracking
            if (corpse.GetComp<CompRottable>() is CompRottable rottable)
            {
                // Accelerate normal rotting process
                rottable.RotProgress += 50000; // Skip ahead in rot
            }
            
            // Alert nearby DOOMS carriers
            AlertNearbyDOOMSCarriers(corpse);
        }

        private void AlertNearbyDOOMSCarriers(Corpse corpse)
        {
            foreach (Pawn pawn in corpse.Map.mapPawns.FreeColonists)
            {
                if (DeathStrandingUtility.HasDOOMSGene(pawn) && 
                    pawn.Position.DistanceTo(corpse.Position) <= 25f)
                {
                    int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(pawn);
                    if (doomsLevel >= 3)
                    {
                        Messages.Message(
                            "DOOMSCarrierSensesDecay".Translate(pawn.LabelShort, corpse.InnerPawn.LabelShort),
                            pawn,
                            MessageTypeDefOf.CautionInput
                        );
                    }
                }
            }
        }

        private void ProcessConversion(Corpse corpse)
        {
            int conversionRate = CalculateConversionRate(corpse);
            ticksUntilConversion -= conversionRate;
            
            // Check for conversion completion
            if (ticksUntilConversion <= 0)
            {
                ExecuteConversion(corpse);
                return;
            }
            
            // Warning stages
            CheckConversionWarnings(corpse);
        }

        private int CalculateConversionRate(Corpse corpse)
        {
            int baseRate = 1;
            
            // Timefall acceleration
            if (IsUnderTimefall())
            {
                baseRate *= Props.timefallAcceleration;
            }
            
            // Chiral protection slowdown
            if (IsUnderChiralProtection() || conversionSlowed)
            {
                baseRate = Mathf.RoundToInt(baseRate * Props.chiralSlowdown);
                
                if (conversionSlowed && Find.TickManager.TicksGame > slowdownEndTick)
                {
                    conversionSlowed = false;
                }
            }
            
            // Temperature effects (cold slows, heat speeds)
            float temperature = corpse.AmbientTemperature;
            if (temperature < 0f)
            {
                baseRate = Mathf.RoundToInt(baseRate * 0.7f); // Cold slows conversion
            }
            else if (temperature > 35f)
            {
                baseRate = Mathf.RoundToInt(baseRate * 1.3f); // Heat speeds conversion
            }
            
            // Proximity to other corpses (mass death accelerates conversion)
            int nearbyCorpses = CountNearbyCorpses(corpse);
            if (nearbyCorpses > 0)
            {
                float massDeathMultiplier = 1f + nearbyCorpses * 0.2f;
                baseRate = Mathf.RoundToInt(baseRate * massDeathMultiplier);
            }
            
            // Proximity to BTs (existing BTs accelerate conversion)
            if (HasNearbyBTs(corpse))
            {
                baseRate *= 2;
            }
            
            // DOOMS level of the corpse (higher levels convert differently)
            int corpseDooms = DeathStrandingUtility.GetDOOMSLevel(corpse.InnerPawn);
            if (corpseDooms >= 5)
            {
                baseRate = Mathf.RoundToInt(baseRate * 1.5f); // High DOOMS converts faster but to stronger BTs
            }
            
            return Math.Max(1, baseRate);
        }

        private bool IsUnderTimefall()
        {
            return parent.Map.weatherManager.curWeather.defName.Contains("Timefall");
        }

        private bool IsUnderChiralProtection()
        {
            return DeathStrandingUtility.IsUnderChiralProtection(parent.Position, parent.Map);
        }

        private int CountNearbyCorpses(Corpse corpse)
        {
            int count = 0;
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(corpse.Position, corpse.Map, 10f, true))
            {
                if (thing is Corpse otherCorpse && otherCorpse != corpse && 
                    otherCorpse.InnerPawn.RaceProps.Humanlike)
                {
                    CompCorpseTimer otherTimer = otherCorpse.GetComp<CompCorpseTimer>();
                    if (otherTimer?.hasStartedDecay == true)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private bool HasNearbyBTs(Corpse corpse)
        {
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(corpse.Position, corpse.Map, 15f, true))
            {
                if (thing.def.defName.StartsWith("BT_"))
                {
                    return true;
                }
            }
            return false;
        }

        private void CheckConversionWarnings(Corpse corpse)
        {
            float progress = ConversionProgress;
            int currentStage = GetWarningStage(progress);
            
            if (currentStage > lastWarningStage)
            {
                lastWarningStage = currentStage;
                SendWarningMessage(corpse, currentStage, progress);
            }
        }

        private int GetWarningStage(float progress)
        {
            if (progress >= 0.95f) return 4; // Imminent
            if (progress >= 0.8f) return 3;  // Critical
            if (progress >= 0.6f) return 2;  // Advanced
            if (progress >= 0.3f) return 1;  // Moderate
            return 0; // Early
        }

        private void SendWarningMessage(Corpse corpse, int stage, float progress)
        {
            string messageKey = stage switch
            {
                1 => "CorpseConversionModerate",
                2 => "CorpseConversionAdvanced", 
                3 => "CorpseConversionCritical",
                4 => "CorpseConversionImminent",
                _ => "CorpseConversionUnknown"
            };
            
            MessageTypeDef messageType = stage >= 3 ? MessageTypeDefOf.ThreatBig : MessageTypeDefOf.CautionInput;
            
            Messages.Message(
                messageKey.Translate(corpse.InnerPawn.LabelShort, (progress * 100f).ToString("F0")),
                corpse,
                messageType
            );
            
            // Play urgency sound for critical stages
            if (stage >= 3)
            {
                SoundDefOf.MessageAlert.PlayOneShotOnCamera();
            }
        }

        private void UpdateVisualEffects(Corpse corpse)
        {
            float progress = ConversionProgress;
            
            // Early decay effects (30%+)
            if (progress >= 0.3f && parent.IsHashIntervalTick(120) && Rand.Chance(0.3f))
            {
                FleckMaker.ThrowSmoke(corpse.DrawPos, corpse.Map, 0.5f);
            }
            
            // Advanced decay effects (60%+)
            if (progress >= 0.6f && parent.IsHashIntervalTick(90) && Rand.Chance(0.4f))
            {
                FleckMaker.ThrowAirPuffUp(corpse.DrawPos, corpse.Map);
            }
            
            // Critical stage effects (80%+)
            if (progress >= 0.8f && parent.IsHashIntervalTick(60) && Rand.Chance(0.5f))
            {
                FleckMaker.ThrowLightningGlow(corpse.DrawPos, corpse.Map, 0.8f);
            }
            
            // Imminent conversion effects (95%+)
            if (progress >= 0.95f && parent.IsHashIntervalTick(30))
            {
                FleckMaker.Static(corpse.Position, corpse.Map, FleckDefOf.PsycastPsychicEffect, 1f);
                
                // Intensifying effects as conversion approaches
                if (Rand.Chance(0.7f))
                {
                    Vector3 effectPos = corpse.DrawPos + new Vector3(
                        Rand.Range(-1f, 1f), 
                        0f, 
                        Rand.Range(-1f, 1f)
                    );
                    FleckMaker.ThrowSmoke(effectPos, corpse.Map, 1f);
                }
            }
        }

        private void ExecuteConversion(Corpse corpse)
        {
            IntVec3 spawnPos = corpse.Position;
            Map map = corpse.Map;
            Pawn originalPawn = corpse.InnerPawn;
            
            // Pre-conversion effects
            CreateConversionEffects(spawnPos, map);
            
            // Select BT type based on original pawn
            PawnKindDef btKind = SelectBTType(originalPawn);
            
            // Generate the BT
            Pawn bt = CreateBTFromCorpse(btKind, originalPawn, map);
            
            if (bt != null)
            {
                // Spawn the BT
                GenSpawn.Spawn(bt, spawnPos, map);
                
                // Configure BT properties
                ConfigureBTFromOriginal(bt, originalPawn);
                
                // Remove the corpse
                corpse.Destroy(DestroyMode.Vanish);
                
                // Send conversion message
                SendConversionMessage(originalPawn, bt, spawnPos, map);
                
                // Trigger any follow-up events
                TriggerConversionEvents(bt, originalPawn, map);
            }
            else
            {
                Log.Error("DeathStranding: Failed to create BT from corpse, destroying corpse anyway to prevent issues");
                corpse.Destroy(DestroyMode.Vanish);
            }
        }

        private void CreateConversionEffects(IntVec3 position, Map map)
        {
            // Dramatic conversion visuals
            FleckMaker.Static(position, map, FleckDefOf.ExplosionFlash, 3f);
            FleckMaker.Static(position, map, FleckDefOf.PsycastPsychicEffect, 2f);
            
            // Sound effect
            SoundDefOf.Hive_Spawn.PlayOneShot(new TargetInfo(position, map));
            
            // Expanding dark energy
            for (int i = 0; i < 8; i++)
            {
                IntVec3 effectCell = position + GenAdj.AdjacentCells[i % 8];
                if (effectCell.InBounds(map))
                {
                    FleckMaker.ThrowSmoke(effectCell.ToVector3Shifted(), map, 1f);
                }
            }
            
            // Shockwave effect
            for (int radius = 1; radius <= 3; radius++)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(position, radius, false))
                {
                    if (cell.InBounds(map) && Rand.Chance(0.3f))
                    {
                        FleckMaker.ThrowAirPuffUp(cell.ToVector3Shifted(), map);
                    }
                }
            }
        }

        private PawnKindDef SelectBTType(Pawn originalPawn)
        {
            // Base BT type selection
            PawnKindDef btType = PawnKindDefOf_DeathStranding.BT_Basic;
            
            // Special cases based on original pawn
            int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(originalPawn);
            
            if (doomsLevel >= 7)
            {
                // Highest DOOMS levels become legendary BTs
                btType = PawnKindDefOf_DeathStranding.BT_Lion ?? btType;
            }
            else if (doomsLevel >= 5)
            {
                // High DOOMS levels become special BTs
                btType = PawnKindDefOf_DeathStranding.BT_Catcher ?? btType;
            }
            else if (originalPawn.skills?.GetSkill(SkillDefOf.Melee)?.Level >= 15)
            {
                // Melee specialists become hunters
                btType = PawnKindDefOf_DeathStranding.BT_Hunter ?? btType;
            }
            else if (originalPawn.skills?.GetSkill(SkillDefOf.Shooting)?.Level >= 15)
            {
                // Ranged specialists become catchers
                btType = PawnKindDefOf_DeathStranding.BT_Catcher ?? btType;
            }
            else if (originalPawn.story?.traits?.HasTrait(TraitDefOf.Psychopath) == true)
            {
                // Psychopaths have a chance to become hunters
                if (Rand.Chance(0.7f))
                {
                    btType = PawnKindDefOf_DeathStranding.BT_Hunter ?? btType;
                }
            }
            
            return btType;
        }

        private Pawn CreateBTFromCorpse(PawnKindDef btKind, Pawn originalPawn, Map map)
        {
            try
            {
                Pawn bt = PawnGenerator.GeneratePawn(btKind);
                
                // Preserve some characteristics
                if (bt.Name == null && originalPawn.Name != null)
                {
                    bt.Name = originalPawn.Name;
                }
                
                // Set appropriate faction (hostile to players)
                bt.SetFaction(null); // Neutral/hostile
                
                return bt;
            }
            catch (Exception ex)
            {
                Log.Error($"DeathStranding: Failed to create BT from corpse {originalPawn?.LabelShort}: {ex}");
                return null;
            }
        }

        private void ConfigureBTFromOriginal(Pawn bt, Pawn originalPawn)
        {
            // Configure BT tethering component
            CompBTTether tetherComp = bt.GetComp<CompBTTether>();
            if (tetherComp != null)
            {
                // BTs converted from high-DOOMS pawns are more dangerous
                int originalDOOMS = DeathStrandingUtility.GetDOOMSLevel(originalPawn);
                if (originalDOOMS >= 5)
                {
                    // Increase tether strength based on original DOOMS level
                    float doomsMultiplier = 1f + originalDOOMS * 0.2f;
                    
                    // Access and modify the properties (this might need adjustment based on your CompBTTether implementation)
                    var newProps = new CompProperties_BTTether
                    {
                        tetherStrength = tetherComp.Props.tetherStrength * doomsMultiplier,
                        tetherRange = tetherComp.Props.tetherRange,
                        canTriggerVoidout = originalDOOMS >= 7
                    };
                    
                    // Note: This is a simplified approach. You might need to implement a way to modify existing comp properties
                }
            }
            
            // Configure visual effects
            CompBTVisualEffects visualComp = bt.GetComp<CompBTVisualEffects>();
            if (visualComp != null)
            {
                int originalDOOMS = DeathStrandingUtility.GetDOOMSLevel(originalPawn);
                if (originalDOOMS >= 5)
                {
                    // Higher DOOMS = more corporeal/visible BT
                    visualComp.Props.corporealityLevel = Math.Min(3, 1 + originalDOOMS / 3);
                    visualComp.Props.baseAlpha = Math.Min(0.9f, 0.4f + originalDOOMS * 0.1f);
                }
            }
            
            // Scale health based on original pawn's capabilities
            if (originalPawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) > 1.2f)
            {
                // Boost BT movement if original was fast
                bt.health.capacities.GetLevel(PawnCapacityDefOf.Moving);
            }
        }

        private void SendConversionMessage(Pawn originalPawn, Pawn bt, IntVec3 position, Map map)
        {
            string messageKey = bt.kindDef == PawnKindDefOf_DeathStranding.BT_Basic ? 
                "CorpseConvertedToBT" : "CorpseConvertedToSpecialBT";
                
            Messages.Message(
                messageKey.Translate(originalPawn.LabelShort, bt.kindDef.LabelCap),
                new TargetInfo(position, map),
                MessageTypeDefOf.ThreatBig
            );
            
            // Play dramatic sound
            SoundDefOf.Explosion_Bomb.PlayOneShot(new TargetInfo(position, map));
        }

        private void TriggerConversionEvents(Pawn bt, Pawn originalPawn, Map map)
        {
            // Nearby corpses might convert faster due to "resonance"
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(bt.Position, map, 12f, true))
            {
                if (thing is Corpse nearbyCorpse && nearbyCorpse.InnerPawn.RaceProps.Humanlike)
                {
                    CompCorpseTimer nearbyTimer = nearbyCorpse.GetComp<CompCorpseTimer>();
                    if (nearbyTimer != null && nearbyTimer.hasStartedDecay)
                    {
                        nearbyTimer.ticksUntilConversion = Math.Max(1000, nearbyTimer.ticksUntilConversion / 2);
                        
                        Messages.Message(
                            "CorpseResonanceAcceleration".Translate(nearbyCorpse.InnerPawn.LabelShort),
                            nearbyCorpse,
                            MessageTypeDefOf.NegativeEvent
                        );
                    }
                }
            }
            
            // Alert DOOMS carriers in the area
            foreach (Pawn pawn in map.mapPawns.FreeColonists)
            {
                if (DeathStrandingUtility.HasDOOMSGene(pawn) && 
                    pawn.Position.DistanceTo(bt.Position) <= 30f)
                {
                    int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(pawn);
                    if (doomsLevel >= 3)
                    {
                        Messages.Message(
                            "DOOMSCarrierSensesBTConversion".Translate(pawn.LabelShort, originalPawn.LabelShort),
                            pawn,
                            MessageTypeDefOf.CautionInput
                        );
                    }
                }
            }
            
            // Possible chain reaction for mass casualty events
            int recentConversions = CountRecentConversions(map);
            if (recentConversions >= 3)
            {
                TriggerMassConversionEvent(map, recentConversions);
            }
        }

        private int CountRecentConversions(Map map)
        {
            int count = 0;
            int recentTicks = 60000; // Last day
            
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing.def.defName.StartsWith("BT_") && thing is Pawn btPawn)
                {
                    if (btPawn.ageTracker.TicksTotal <= recentTicks)
                    {
                        count++;
                    }
                }
            }
            
            return count;
        }

        private void TriggerMassConversionEvent(Map map, int conversionCount)
        {
            Messages.Message(
                "MassConversionEventTriggered".Translate(conversionCount),
                MessageTypeDefOf.ThreatBig
            );
            
            // Schedule additional BT manifestations
            IncidentParms btParms = new IncidentParms
            {
                target = map,
                points = 300f + conversionCount * 50f // Scale with conversion count
            };
            
            // Multiple waves of BTs
            for (int i = 0; i < Math.Min(3, conversionCount / 2); i++)
            {
                Find.Storyteller.incidentQueue.Add(
                    IncidentDefOf_DeathStranding.BTSwarm,
                    Find.TickManager.TicksGame + Rand.Range(1000 + i * 2000, 5000 + i * 3000),
                    btParms
                );
            }
        }

        public void SlowConversion(float factor)
        {
            conversionSlowed = true;
            slowdownEndTick = Find.TickManager.TicksGame + 30000; // 30 second effect
            
            Messages.Message(
                "CorpseConversionSlowedByChiral".Translate(parent.LabelShort),
                parent,
                MessageTypeDefOf.PositiveEvent
            );
        }

        public void AccelerateConversion(float factor)
        {
            ticksUntilConversion = Mathf.RoundToInt(ticksUntilConversion / factor);
            ticksUntilConversion = Math.Max(100, ticksUntilConversion); // Minimum 100 ticks
            
            Messages.Message(
                "CorpseConversionAccelerated".Translate(parent.LabelShort),
                parent,
                MessageTypeDefOf.NegativeEvent
            );
        }

        public override string CompInspectStringExtra()
        {
            if (!(parent is Corpse corpse) || !corpse.InnerPawn.RaceProps.Humanlike)
                return null;
                
            var inspectStrings = new List<string>();
            
            float progress = ConversionProgress;
            string progressPercent = (progress * 100f).ToString("F0");
            
            // Progress description
            if (progress < 0.3f)
            {
                inspectStrings.Add("CorpseDecayEarly".Translate(progressPercent));
            }
            else if (progress < 0.6f)
            {
                inspectStrings.Add("CorpseDecayModerate".Translate(progressPercent));
            }
            else if (progress < 0.8f)
            {
                inspectStrings.Add("CorpseDecayAdvanced".Translate(progressPercent));
            }
            else if (progress < 0.95f)
            {
                inspectStrings.Add("CorpseDecayCritical".Translate(progressPercent));
            }
            else
            {
                inspectStrings.Add("CorpseDecayImminent".Translate(progressPercent));
            }
            
            // Time remaining
            float hoursRemaining = ticksUntilConversion / 2500f; // ~2500 ticks per hour
            if (hoursRemaining > 48f)
            {
                inspectStrings.Add("TimeUntilBTConversionDays".Translate((hoursRemaining / 24f).ToString("F1")));
            }
            else if (hoursRemaining > 2f)
            {
                inspectStrings.Add("TimeUntilBTConversionHours".Translate(hoursRemaining.ToString("F1")));
            }
            else
            {
                inspectStrings.Add("TimeUntilBTConversionMinutes".Translate((hoursRemaining * 60f).ToString("F0")));
            }
            
            // Environmental factors
            if (IsUnderTimefall())
            {
                inspectStrings.Add("CorpseTimefallAcceleration".Translate());
            }
            
            if (IsUnderChiralProtection())
            {
                inspectStrings.Add("CorpseChiralProtection".Translate());
            }
            
            if (conversionSlowed)
            {
                int remainingSlowTicks = slowdownEndTick - Find.TickManager.TicksGame;
                if (remainingSlowTicks > 0)
                {
                    inspectStrings.Add("CorpseConversionSlowed".Translate((remainingSlowTicks / 60f).ToString("F0")));
                }
            }
            
            // Predicted BT type
            PawnKindDef predictedBT = SelectBTType(corpse.InnerPawn);
            if (predictedBT != PawnKindDefOf_DeathStranding.BT_Basic)
            {
                inspectStrings.Add("PredictedBTType".Translate(predictedBT.LabelCap));
            }
            
            return string.Join("\n", inspectStrings);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksUntilConversion, "ticksUntilConversion");
            Scribe_Values.Look(ref hasStartedDecay, "hasStartedDecay");
            Scribe_Values.Look(ref conversionSlowed, "conversionSlowed");
            Scribe_Values.Look(ref slowdownEndTick, "slowdownEndTick");
            Scribe_Values.Look(ref lastWarningStage, "lastWarningStage");
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction == Faction.OfPlayer)
            {
                // Emergency cremation option
                yield return new Command_Action
                {
                    defaultLabel = "EmergencyCremation".Translate(),
                    defaultDesc = "EmergencyCremationDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Burn", false) ?? BaseContent.BadTex,
                    action = () => {
                        if (parent is Corpse corpse)
                        {
                            PerformEmergencyCremation(corpse);
                        }
                    },
                    hotKey = KeyBindingDefOf.Misc1
                };
                
                // Chiral treatment option (if player has chiral crystals)
                if (HasChiralCrystalsNearby())
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "ChiralTreatment".Translate(),
                        defaultDesc = "ChiralTreatmentDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/ChiralTreatment", false) ?? BaseContent.BadTex,
                        action = () => {
                            if (parent is Corpse corpse)
                            {
                                PerformChiralTreatment(corpse);
                            }
                        },
                        hotKey = KeyBindingDefOf.Misc2
                    };
                }
            }
            
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Force conversion now",
                    action = () => {
                        ticksUntilConversion = 1;
                    }
                };
                
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Reset timer",
                    action = () => {
                        ticksUntilConversion = Props.baseConversionTime;
                        hasStartedDecay = false;
                        lastWarningStage = 0;
                    }
                };
                
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Slow conversion (chiral)",
                    action = () => {
                        SlowConversion(0.1f);
                    }
                };
                
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Accelerate conversion",
                    action = () => {
                        AccelerateConversion(3f);
                    }
                };
                
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Test mass conversion",
                    action = () => {
                        TriggerMassConversionEvent(parent.Map, 5);
                    }
                };
            }
        }

        private bool HasChiralCrystalsNearby()
        {
            // Check for chiral crystals in nearby stockpiles or carried by pawns
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(parent.Position, parent.Map, 10f, true))
            {
                if (thing.def == ThingDefOf_DeathStranding.ChiralCrystal && thing.stackCount >= 3)
                {
                    return true;
                }
            }
            
            // Check pawns carrying crystals
            foreach (Pawn pawn in parent.Map.mapPawns.FreeColonists)
            {
                if (pawn.Position.DistanceTo(parent.Position) <= 5f)
                {
                    var crystals = pawn.inventory.innerContainer
                        .Where(t => t.def == ThingDefOf_DeathStranding.ChiralCrystal)
                        .Sum(t => t.stackCount);
                    if (crystals >= 3)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        private void PerformChiralTreatment(Corpse corpse)
        {
            // Find and consume chiral crystals
            bool foundCrystals = false;
            int crystalsNeeded = 3;
            
            // Try nearby things first
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(corpse.Position, corpse.Map, 10f, true))
            {
                if (thing.def == ThingDefOf_DeathStranding.ChiralCrystal && thing.stackCount >= crystalsNeeded)
                {
                    thing.stackCount -= crystalsNeeded;
                    if (thing.stackCount <= 0)
                    {
                        thing.Destroy();
                    }
                    foundCrystals = true;
                    break;
                }
            }
            
            // Try pawns carrying crystals
            if (!foundCrystals)
            {
                foreach (Pawn pawn in corpse.Map.mapPawns.FreeColonists)
                {
                    if (pawn.Position.DistanceTo(corpse.Position) <= 5f)
                    {
                        var crystalThings = pawn.inventory.innerContainer
                            .Where(t => t.def == ThingDefOf_DeathStranding.ChiralCrystal)
                            .ToList();
                        
                        int availableCrystals = crystalThings.Sum(t => t.stackCount);
                        if (availableCrystals >= crystalsNeeded)
                        {
                            int remaining = crystalsNeeded;
                            foreach (Thing crystal in crystalThings)
                            {
                                if (remaining <= 0) break;
                                
                                int toConsume = Math.Min(remaining, crystal.stackCount);
                                crystal.stackCount -= toConsume;
                                remaining -= toConsume;
                                
                                if (crystal.stackCount <= 0)
                                {
                                    crystal.Destroy();
                                }
                            }
                            foundCrystals = true;
                            break;
                        }
                    }
                }
            }
            
            if (!foundCrystals)
            {
                Messages.Message(
                    "ChiralTreatmentNoCrystals".Translate(crystalsNeeded),
                    MessageTypeDefOf.RejectInput
                );
                return;
            }
            
            // Apply treatment effect
            SlowConversion(0.2f); // Significant slowdown
            
            // Visual effects
            FleckMaker.Static(corpse.Position, corpse.Map, FleckDefOf.PsycastPsychicEffect, 2f);
            for (int i = 0; i < 5; i++)
            {
                Vector3 effectPos = corpse.DrawPos + new Vector3(
                    Rand.Range(-1f, 1f), 
                    0f, 
                    Rand.Range(-1f, 1f)
                );
                FleckMaker.ThrowLightningGlow(effectPos, corpse.Map, 0.5f);
            }
            
            Messages.Message(
                "ChiralTreatmentApplied".Translate(corpse.InnerPawn.LabelShort),
                corpse,
                MessageTypeDefOf.PositiveEvent
            );
        }

        private void PerformEmergencyCremation(Corpse corpse)
        {
            // Check for fire source or fuel nearby
            bool hasFireSource = false;
            Thing fuelSource = null;
            
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(corpse.Position, corpse.Map, 5f, true))
            {
                if (thing.def.defName.Contains("Campfire") || 
                    thing.def.defName.Contains("Torch") ||
                    thing.def.defName.Contains("Fire"))
                {
                    hasFireSource = true;
                    break;
                }
                else if (thing.def == ThingDefOf.WoodLog && thing.stackCount >= 5)
                {
                    fuelSource = thing;
                }
            }
            
            // If no fire source, check if we can create one with fuel
            if (!hasFireSource && fuelSource != null)
            {
                // Consume wood to create fire
                fuelSource.stackCount -= 5;
                if (fuelSource.stackCount <= 0)
                {
                    fuelSource.Destroy();
                }
                hasFireSource = true;
            }
            
            if (!hasFireSource)
            {
                Messages.Message(
                    "EmergencyCremationNoFuel".Translate(),
                    MessageTypeDefOf.RejectInput
                );
                return;
            }
            
            // Perform cremation with dramatic effects
            FleckMaker.Static(corpse.Position, corpse.Map, FleckDefOf.FireGlow, 3f);
            FleckMaker.Static(corpse.Position, corpse.Map, FleckDefOf.Smoke, 4f);
            
            // Create expanding fire effect
            for (int i = 0; i < 8; i++)
            {
                IntVec3 fireCell = corpse.Position + GenAdj.AdjacentCells[i];
                if (fireCell.InBounds(corpse.Map))
                {
                    FleckMaker.ThrowFireGlow(fireCell, corpse.Map, 1f);
                }
            }
            
            // Sound effect
            SoundDefOf.Recipe_Surgery.PlayOneShot(new TargetInfo(corpse.Position, corpse.Map));
            
            // Destroy the corpse
            corpse.Destroy(DestroyMode.KillFinalize);
            
            Messages.Message(
                "EmergencyCremationComplete".Translate(corpse.InnerPawn.LabelShort),
                MessageTypeDefOf.PositiveEvent
            );
            
            // Small chance to spawn some ash or residue
            if (Rand.Chance(0.3f))
            {
                Thing ash = ThingMaker.MakeThing(ThingDefOf.Chemfuel); // Using chemfuel as ash substitute
                ash.stackCount = 1;
                GenPlace.TryPlaceThing(ash, corpse.Position, corpse.Map, ThingPlaceMode.Near);
            }
        }

        /// <summary>
        /// Called by external systems to check if this corpse is in decay process
        /// </summary>
        public bool IsDecaying => hasStartedDecay;
        
        /// <summary>
        /// Called by external systems to get remaining time in hours
        /// </summary>
        public float GetRemainingHours => ticksUntilConversion / 2500f;
        
        /// <summary>
        /// Called by external systems to force immediate conversion (for special events)
        /// </summary>
        public void ForceConversion()
        {
            if (parent is Corpse corpse && ShouldConvert(corpse))
            {
                ticksUntilConversion = 1;
            }
        }
        
        /// <summary>
        /// Called by external systems to completely prevent conversion (for special preservation)
        /// </summary>
        public void PreventConversion()
        {
            hasStartedDecay = false;
            ticksUntilConversion = int.MaxValue;
            
            Messages.Message(
                "CorpseConversionPrevented".Translate(parent.LabelShort),
                parent,
                MessageTypeDefOf.PositiveEvent
            );
        }
    }

    /// <summary>
    /// Properties for corpse timer components
    /// </summary>
    public class CompProperties_CorpseTimer : CompProperties
    {
        public int baseConversionTime = 60000; // ~1 day (60,000 ticks)
        public int timefallAcceleration = 3; // 3x faster during timefall
        public float chiralSlowdown = 0.5f; // Half speed under chiral protection
        public bool showWarnings = true;
        public bool allowEmergencyCremation = true;
        public bool allowChiralTreatment = true;
        public int chiralTreatmentCost = 3; // Chiral crystals needed for treatment

        public CompProperties_CorpseTimer()
        {
            compClass = typeof(CompCorpseTimer);
        }
    }
}