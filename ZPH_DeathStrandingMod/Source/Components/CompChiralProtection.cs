using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using DeathStrandingMod.Core;

namespace DeathStrandingMod.Components
{
    /// <summary>
    /// Component that provides chiral protection against timefall and BT effects
    /// </summary>
    public class CompChiralProtection : ThingComp
    {
        private CompProperties_ChiralProtection Props => (CompProperties_ChiralProtection)props;
        private CompRefuelable refuelableComp;
        private CompPowerTrader powerComp;
        private int lastProtectionTick = 0;
        
        public bool IsActive => 
            (powerComp?.PowerOn ?? true) && 
            (refuelableComp?.HasFuel ?? true) &&
            parent.Spawned;
        
        public float ProtectionRadius => Props.protectionRadius;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            refuelableComp = parent.GetComp<CompRefuelable>();
            powerComp = parent.GetComp<CompPowerTrader>();
        }

        public override void CompTick()
        {
            if (parent.IsHashIntervalTick(250)) // Check every 250 ticks (~4 seconds)
            {
                if (IsActive)
                {
                    ApplyProtectionInRadius();
                    HandleFuelConsumption();
                    lastProtectionTick = Find.TickManager.TicksGame;
                }
            }
        }

        private void ApplyProtectionInRadius()
        {
            bool isTimefallActive = IsTimefallActive();
            
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(parent.Position, Props.protectionRadius, true))
            {
                if (!cell.InBounds(parent.Map)) continue;

                List<Thing> things = cell.GetThingList(parent.Map);
                foreach (Thing thing in things)
                {
                    if (thing is Pawn pawn)
                    {
                        ApplyProtectionToPawn(pawn, isTimefallActive);
                    }
                    else if (thing.def.useHitPoints && isTimefallActive)
                    {
                        ApplyProtectionToStructure(thing);
                    }
                    else if (thing is Corpse corpse)
                    {
                        ApplyProtectionToCorpse(corpse);
                    }
                }
            }
        }

        private bool IsTimefallActive()
        {
            return parent.Map.weatherManager.curWeather.defName == "Timefall" ||
                   parent.Map.weatherManager.curWeather.defName == "TimefallStorm";
        }

        private void ApplyProtectionToPawn(Pawn pawn, bool duringTimefall)
        {
            // Reduce BT tether buildup
            Hediff tetherHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BTTether);
            if (tetherHediff != null)
            {
                tetherHediff.Severity -= Props.tetherReduction;
                if (tetherHediff.Severity <= 0)
                {
                    pawn.health.RemoveHediff(tetherHediff);
                    
                    Messages.Message(
                        "ChiralProtectionTetherSevered".Translate(pawn.LabelShort),
                        pawn,
                        MessageTypeDefOf.PositiveEvent
                    );
                }
            }

            // Provide timefall protection
            if (duringTimefall)
            {
                // Add temporary protection hediff
                Hediff protection = pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamedSilentFail("ChiralProtection"));
                if (protection == null)
                {
                    protection = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamedSilentFail("ChiralProtection"), pawn);
                    if (protection != null)
                    {
                        pawn.health.AddHediff(protection);
                    }
                }
                else
                {
                    protection.Severity = 1.0f; // Refresh protection
                }
            }

            // Enhance DOOMS abilities recovery
            if (HasDOOMSGene(pawn))
            {
                ApplyDOOMSEnhancement(pawn);
            }
        }

        private void ApplyProtectionToStructure(Thing structure)
        {
            // Reduce timefall deterioration
            if (structure.def.useHitPoints && structure.HitPoints < structure.MaxHitPoints)
            {
                // Slow down or prevent deterioration
                // This would need to be implemented via Harmony patches on deterioration methods
            }
        }

        private void ApplyProtectionToCorpse(Corpse corpse)
        {
            // Slow down BT conversion
            CompCorpseTimer timer = corpse.GetComp<CompCorpseTimer>();
            if (timer != null)
            {
                // Slow conversion rate while under protection
                timer.SlowConversion(Props.corpseProtectionFactor);
            }
        }

        private bool HasDOOMSGene(Pawn pawn)
        {
            return DeathStrandingUtility.HasDOOMSGene(pawn);
        }

        private void ApplyDOOMSEnhancement(Pawn pawn)
        {
            // Accelerate psychic entropy recovery
            if (pawn.psychicEntropy != null)
            {
                pawn.psychicEntropy.OffsetEntropy(-Props.psychicEntropyReduction);
            }

            // Reduce beach drift severity
            Hediff beachDrift = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf_DeathStranding.BeachDrift);
            if (beachDrift != null)
            {
                beachDrift.Severity -= Props.beachDriftReduction;
                if (beachDrift.Severity <= 0)
                {
                    pawn.health.RemoveHediff(beachDrift);
                }
            }
        }

        private void HandleFuelConsumption()
        {
            if (refuelableComp == null || !refuelableComp.HasFuel) return;

            float consumptionRate = Props.fuelConsumptionRate;
            
            // Increase consumption during timefall
            if (IsTimefallActive())
            {
                consumptionRate *= Props.timefallConsumptionMultiplier;
            }

            // Increase consumption based on protection load
            int protectedEntities = CountProtectedEntities();
            float loadMultiplier = 1f + (protectedEntities * 0.1f); // 10% per entity
            consumptionRate *= loadMultiplier;

            refuelableComp.ConsumeFuel(consumptionRate / 250f); // Per tick rate
        }

        private int CountProtectedEntities()
        {
            int count = 0;
            
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(parent.Position, Props.protectionRadius, true))
            {
                if (!cell.InBounds(parent.Map)) continue;
                
                count += cell.GetThingList(parent.Map)
                    .Count(t => t is Pawn || (t.def.useHitPoints && t.def.category == ThingCategory.Building));
            }
            
            return count;
        }

        public override void PostDraw()
        {
            if (IsActive && Props.showProtectionField)
            {
                DrawProtectionField();
            }
        }

        private void DrawProtectionField()
        {
            Vector3 drawPos = parent.DrawPos;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            
            // Create pulsing effect
            float pulseAlpha = (Mathf.Sin(Time.time * 2f) + 1f) * 0.15f + 0.1f;
            
            Material fieldMaterial = FadedMaterialPool.FadedVersionOf(
                MaterialPool.MatFrom("UI/Overlays/ChiralField", ShaderDatabase.Transparent), 
                pulseAlpha
            );
            
            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(
                    drawPos, 
                    Quaternion.identity, 
                    Vector3.one * Props.protectionRadius * 2f
                ),
                fieldMaterial,
                0
            );
            
            // Draw node indicator
            if (Find.Selector.IsSelected(parent))
            {
                GenDraw.DrawFieldEdges(
                    GenRadial.RadialCellsAround(parent.Position, Props.protectionRadius, true).ToList(),
                    Color.cyan
                );
            }
        }

        public override string CompInspectStringExtra()
        {
            var inspectStrings = new List<string>();
            
            if (IsActive)
            {
                inspectStrings.Add("ChiralProtectionActive".Translate(Props.protectionRadius.ToString("F1")));
                
                int protectedCount = CountProtectedEntities();
                if (protectedCount > 0)
                {
                    inspectStrings.Add("ChiralProtectionCovering".Translate(protectedCount));
                }
                
                if (IsTimefallActive())
                {
                    inspectStrings.Add("ChiralProtectionTimefallActive".Translate());
                }
            }
            else
            {
                if (powerComp != null && !powerComp.PowerOn)
                {
                    inspectStrings.Add("ChiralProtectionNoPower".Translate());
                }
                else if (refuelableComp != null && !refuelableComp.HasFuel)
                {
                    inspectStrings.Add("ChiralProtectionNoFuel".Translate());
                }
            }
            
            return string.Join("\n", inspectStrings);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastProtectionTick, "lastProtectionTick");
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Props.allowManualToggle && parent.Faction == Faction.OfPlayer)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "ChiralProtectionToggle".Translate(),
                    defaultDesc = "ChiralProtectionToggleDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/ChiralProtectionToggle"),
                    isActive = () => IsActive,
                    toggleAction = ToggleProtection,
                    hotKey = KeyBindingDefOf.Command_TogglePower
                };
            }
            
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Force timefall test",
                    action = () => {
                        parent.Map.weatherManager.TransitionTo(WeatherDefOf_DeathStranding.Timefall);
                    }
                };
            }
        }

        private void ToggleProtection()
        {
            if (powerComp != null)
            {
                if (powerComp.PowerOn)
                {
                    powerComp.PowerOutput = 0f;
                }
                else
                {
                    powerComp.PowerOutput = -powerComp.Props.basePowerConsumption;
                }
            }
        }
    }

    /// <summary>
    /// Properties for chiral protection components
    /// </summary>
    public class CompProperties_ChiralProtection : CompProperties
    {
        public float protectionRadius = 12f;
        public float tetherReduction = 0.02f;
        public float fuelConsumptionRate = 0.2f;
        public float timefallProtectionFactor = 0.8f;
        public float timefallConsumptionMultiplier = 2.0f;
        public float corpseProtectionFactor = 0.5f;
        public float psychicEntropyReduction = 1.0f;
        public float beachDriftReduction = 0.01f;
        public bool showProtectionField = true;
        public bool allowManualToggle = true;

        public CompProperties_ChiralProtection()
        {
            compClass = typeof(CompChiralProtection);
        }
    }
}