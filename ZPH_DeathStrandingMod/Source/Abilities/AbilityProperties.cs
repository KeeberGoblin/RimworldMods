using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace DeathStrandingMod.Abilities
{
    /// <summary>
    /// Properties for DOOMS abilities and their costs/effects
    /// </summary>
    public class AbilityProperties_DOOMS : DefModExtension
    {
        // Resource costs
        public int chiralCostPerUse = 0;
        public float neuralHeatGain = 10f;
        public bool requiresChiralCrystals = true;
        public bool consumeCrystalsFromInventory = true;
        public bool consumeCrystalsFromStockpile = true;
        public float stockpileSearchRadius = 10f;
        
        // Beach drift risks
        public float beachDriftRisk = 0.0f;
        public float beachDriftSeverity = 0.5f;
        public bool guaranteeDriftAtMaxLevel = false;
        public float driftDurationMultiplier = 1.0f;
        
        // Voidout risks
        public bool canTriggerVoidout = false;
        public float voidoutChance = 0.0f;
        public float voidoutSeverity = 1.0f;
        public bool voidoutAffectsCaster = true;
        
        // Environmental requirements
        public bool requiresLineOfSight = true;
        public bool blockedByRoof = false;
        public bool enhancedDuringTimefall = false;
        public float timefallEffectivenessMultiplier = 1.5f;
        public bool reducedInChiralField = false;
        public float chiralFieldReduction = 0.8f;
        
        // Scaling with DOOMS level
        public bool scalesWithDOOMSLevel = true;
        public float doomsLevelMultiplier = 0.2f;
        public float maxDOOMSScaling = 2.0f;
        public bool requiresMinimumDOOMSLevel = true;
        public int minimumDOOMSLevel = 5;
        
        // Cooldown modifiers
        public bool doomsLevelReducesCooldown = true;
        public float cooldownReductionPerLevel = 0.1f;
        public float minimumCooldownMultiplier = 0.3f;
        
        // Range modifiers
        public bool doomsLevelIncreasesRange = true;
        public float rangeIncreasePerLevel = 0.15f;
        public float maximumRangeMultiplier = 2.0f;
        
        // Interaction with other systems
        public bool enhancedByChiralResonance = false;
        public float chiralResonanceBonus = 0.3f;
        public bool interferesWithTech = false;
        public float techInterferenceRadius = 5f;
        
        // Visual and audio effects
        public bool createVisualEffects = true;
        public Color primaryEffectColor = new Color(0.3f, 0.8f, 1f, 1f);
        public Color secondaryEffectColor = new Color(0.8f, 0.3f, 1f, 1f);
        public bool createSoundEffects = true;
        public float effectIntensity = 1.0f;
        
        // Performance and balance
        public bool hasGlobalCooldown = false;
        public int globalCooldownTicks = 600; // 10 seconds
        public bool limitsSimultaneousUse = false;
        public int maxSimultaneousUsers = 1;
        public bool requiresConcentration = false;
        public float concentrationBreakChance = 0.1f;
    }

    /// <summary>
    /// Extended properties for high-level reality manipulation abilities
    /// </summary>
    public class AbilityProperties_RealityManipulation : AbilityProperties_DOOMS
    {
        // Reality distortion effects
        public bool distortsLocalReality = true;
        public float realityDistortionRadius = 5f;
        public int realityDistortionDuration = 30000; // 30 seconds
        public bool affectsPhysicsLaws = false;
        
        // Temporal effects
        public bool affectsTime = false;
        public float timeDistortionFactor = 1.0f;
        public int timeEffectDuration = 15000; // 15 seconds
        public bool canSlowTime = false;
        public bool canSpeedTime = false;
        
        // Matter manipulation
        public bool canManipulateMatter = false;
        public float matterManipulationRange = 10f;
        public List<string> manipulatableThingDefs = new List<string>();
        public bool canCreateMatter = false;
        public bool canDestroyMatter = false;
        
        // Dimensional effects
        public bool opensDimensionalRifts = false;
        public float riftStabilityTime = 60000; // 1 minute
        public bool allowsBeachAccess = false;
        public float beachAccessDuration = 30000; // 30 seconds
        
        // Cascade effects
        public bool canCascade = false;
        public float cascadeChance = 0.1f;
        public int maxCascadeDepth = 3;
        public float cascadeDamageMultiplier = 0.8f;
        
        // Aftermath effects
        public bool leavesResidualEffects = true;
        public int residualEffectDuration = 120000; // 2 minutes
        public bool createsChiralResidue = false;
        public int chiralResidueAmount = 5;
        
        // Safety mechanisms
        public bool hasFailsafes = true;
        public float failsafeActivationChance = 0.95f;
        public bool protectsCasterFromBacklash = true;
        public float casterProtection = 0.7f;
    }

    /// <summary>
    /// Properties specific to gravity manipulation abilities
    /// </summary>
    public class AbilityProperties_GravityManipulation : AbilityProperties_DOOMS
    {
        // Gravity field properties
        public float gravityStrength = 1.0f;
        public float gravityRange = 8f;
        public bool canRepel = true;
        public bool canAttract = true;
        public bool canCrush = false;
        
        // Target restrictions
        public bool affectsPawns = true;
        public bool affectsItems = true;
        public bool affectsBuildings = false;
        public bool affectsProjectiles = true;
        public float maxTargetMass = 100f;
        
        // Duration and persistence
        public int gravityEffectDuration = 5000; // 5 seconds
        public bool isPersistent = false;
        public bool decaysOverTime = true;
        public float decayRate = 0.1f;
        
        // Damage and force
        public bool dealsDamage = false;
        public float crushDamage = 20f;
        public float launchForce = 5f;
        public bool canKnockDown = true;
        
        // Environmental interaction
        public bool affectedByGravity = true;
        public bool worksInSpace = false;
        public bool enhancedUnderground = false;
        public float undergroundBonus = 1.3f;
    }

    /// <summary>
    /// Properties for matter manipulation abilities
    /// </summary>
    public class AbilityProperties_MatterManipulation : AbilityProperties_DOOMS
    {
        // Manipulation scope
        public List<ThingCategory> allowedCategories = new List<ThingCategory> 
        { 
            ThingCategory.Item, 
            ThingCategory.Building 
        };
        public List<string> excludedThingDefs = new List<string>();
        public float manipulationRange = 15f;
        public float maxObjectMass = 50f;
        
        // Manipulation types
        public bool canMove = true;
        public bool canRotate = false;
        public bool canResize = false;
        public bool canReshape = false;
        public bool canDuplicate = false;
        
        // Material restrictions
        public bool affectsOrganic = false;
        public bool affectsMetals = true;
        public bool affectsStone = true;
        public bool affectsLiquids = false;
        public bool affectsEnergy = false;
        
        // Creation abilities
        public bool canCreateBarriers = true;
        public bool canCreateWeapons = false;
        public bool canCreateTools = false;
        public int maxCreatedObjects = 5;
        public int createdObjectDuration = 60000; // 1 minute
        
        // Precision and control
        public float manipulationPrecision = 1.0f;
        public bool requiresLineOfSight = true;
        public bool maintainMomentum = true;
        public float controlDifficulty = 1.0f;
    }

    /// <summary>
    /// Properties for Beach dimension interaction abilities
    /// </summary>
    public class AbilityProperties_BeachInteraction : AbilityProperties_DOOMS
    {
        // Beach access
        public bool allowsBeachEntry = false;
        public bool allowsBeachExit = true;
        public float beachAccessDuration = 30000; // 30 seconds
        public bool maintainsConsciousness = true;
        
        // Dimensional travel
        public bool allowsTeleportation = false;
        public float teleportationRange = 20f;
        public bool requiresBeachTraversal = true;
        public float travelTime = 3000; // 3 seconds
        
        // Beach perception
        public bool enhancesBeachVision = true;
        public float visionRange = 50f;
        public bool canSeeHiddenThings = true;
        public bool revealsSecrets = false;
        
        // Temporal effects
        public bool allowsTimePeering = false;
        public bool canSeeFuture = false;
        public bool canSeePast = false;
        public int temporalVisionRange = 24; // Hours
        
        // Entity interaction
        public bool canCommunicateWithBTs = false;
        public bool canControlBTs = false;
        public bool canBanishBTs = false;
        public float btInteractionRange = 15f;
        
        // Information gathering
        public bool providesInsights = true;
        public bool revealsThreats = true;
        public bool revealsOpportunities = true;
        public float insightAccuracy = 0.8f;
        
        // Risk factors
        public bool riskOfBeingLost = true;
        public float lostInBeachChance = 0.05f;
        public bool riskOfEntityAttention = true;
        public float entityAttentionChance = 0.1f;
    }

    /// <summary>
    /// Utility class for ability property calculations
    /// </summary>
    public static class AbilityPropertyUtility
    {
        /// <summary>
        /// Calculates the effective cost of an ability based on DOOMS level and other factors
        /// </summary>
        public static int CalculateEffectiveCost(AbilityProperties_DOOMS props, Pawn caster)
        {
            if (props == null || caster == null) return 0;
            
            float baseCost = props.chiralCostPerUse;
            
            // DOOMS level scaling
            if (props.scalesWithDOOMSLevel)
            {
                int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(caster);
                float scaling = 1f - (doomsLevel * 0.1f); // Higher DOOMS = lower cost
                baseCost *= Math.Max(0.1f, scaling);
            }
            
            // Environmental modifiers
            if (props.enhancedDuringTimefall && DeathStrandingUtility.IsTimefallActive(caster.Map))
            {
                baseCost *= 0.8f; // 20% cheaper during timefall
            }
            
            if (props.reducedInChiralField && 
                DeathStrandingUtility.IsUnderChiralProtection(caster.Position, caster.Map))
            {
                baseCost *= props.chiralFieldReduction;
            }
            
            return Mathf.RoundToInt(Math.Max(0, baseCost));
        }

        /// <summary>
        /// Calculates the effective range of an ability
        /// </summary>
        public static float CalculateEffectiveRange(float baseRange, AbilityProperties_DOOMS props, Pawn caster)
        {
            if (props == null || caster == null) return baseRange;
            
            float effectiveRange = baseRange;
            
            // DOOMS level scaling
            if (props.doomsLevelIncreasesRange)
            {
                int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(caster);
                float rangeMultiplier = 1f + (doomsLevel * props.rangeIncreasePerLevel);
                rangeMultiplier = Math.Min(rangeMultiplier, props.maximumRangeMultiplier);
                effectiveRange *= rangeMultiplier;
            }
            
            // Environmental modifiers
            if (props.enhancedDuringTimefall && DeathStrandingUtility.IsTimefallActive(caster.Map))
            {
                effectiveRange *= props.timefallEffectivenessMultiplier;
            }
            
            return effectiveRange;
        }

        /// <summary>
        /// Calculates the beach drift risk for an ability use
        /// </summary>
        public static float CalculateBeachDriftRisk(AbilityProperties_DOOMS props, Pawn caster)
        {
            if (props == null || caster == null) return 0f;
            
            float baseRisk = props.beachDriftRisk;
            
            // DOOMS level affects risk
            int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(caster);
            if (doomsLevel >= 8 && props.guaranteeDriftAtMaxLevel)
            {
                return 1f; // Guaranteed drift for Higgs-level abilities
            }
            
            // Higher DOOMS levels have slightly higher risk but better control
            float doomsModifier = 1f + (doomsLevel * 0.05f) - (doomsLevel * 0.03f);
            baseRisk *= Math.Max(0.1f, doomsModifier);
            
            // Environmental factors
            if (DeathStrandingUtility.IsTimefallActive(caster.Map))
            {
                baseRisk *= 1.3f; // Higher risk during timefall
            }
            
            return Math.Min(1f, baseRisk);
        }

        /// <summary>
        /// Determines if an ability can be used based on requirements
        /// </summary>
        public static bool CanUseAbility(AbilityProperties_DOOMS props, Pawn caster)
        {
            if (props == null || caster == null) return false;
            
            // Check minimum DOOMS level
            if (props.requiresMinimumDOOMSLevel)
            {
                int doomsLevel = DeathStrandingUtility.GetDOOMSLevel(caster);
                if (doomsLevel < props.minimumDOOMSLevel)
                {
                    return false;
                }
            }
            
            // Check chiral crystal availability
            if (props.requiresChiralCrystals && props.chiralCostPerUse > 0)
            {
                int effectiveCost = CalculateEffectiveCost(props, caster);
                int availableCrystals = GetAvailableChiralCrystals(caster);
                if (availableCrystals < effectiveCost)
                {
                    return false;
                }
            }
            
            // Check if caster is in valid state
            if (caster.Dead || caster.Downed || caster.InMentalState)
            {
                return false;
            }
            
            // Check for Beach drift (can't use abilities while drifting)
            if (caster.health.hediffSet.HasHediff(HediffDefOf_DeathStranding.BeachDrift))
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Gets available chiral crystals for a pawn
        /// </summary>
        private static int GetAvailableChiralCrystals(Pawn pawn)
        {
            int total = 0;
            
            // Check inventory
            total += pawn.inventory.innerContainer
                .Where(t => t.def == ThingDefOf_DeathStranding.ChiralCrystal)
                .Sum(t => t.stackCount);
            
            // Check nearby stockpiles if allowed
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(
                pawn.Position, pawn.Map, 10f, true))
            {
                if (thing.def == ThingDefOf_DeathStranding.ChiralCrystal)
                {
                    total += thing.stackCount;
                }
            }
            
            return total;
        }
    }
}