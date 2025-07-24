using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace DeathStrandingMod.Components
{
    // ==================== CHIRAL PROTECTION PROPERTIES ====================
    
    /// <summary>
    /// Properties for chiral protection components that create safe zones against timefall and BT effects
    /// </summary>
    public class CompProperties_ChiralProtection : CompProperties
    {
        // Core protection settings
        public float protectionRadius = 12f;
        public float tetherReduction = 0.02f;
        public float timefallProtectionFactor = 0.8f;
        
        // Resource consumption
        public float fuelConsumptionRate = 0.2f;
        public float timefallConsumptionMultiplier = 2.0f;
        public bool consumeFuelOnlyWhenUsed = true;
        
        // Enhanced protection effects
        public float corpseProtectionFactor = 0.5f;
        public float psychicEntropyReduction = 1.0f;
        public float beachDriftReduction = 0.01f;
        
        // Visual and interaction settings
        public bool showProtectionField = true;
        public bool allowManualToggle = true;
        public Color fieldColor = new Color(0.3f, 0.8f, 1f, 0.3f);
        public float fieldPulseSpeed = 2f;
        
        // Advanced settings
        public bool protectAgainstVoidouts = false;
        public float voidoutDamageReduction = 0.5f;
        public bool enhanceDOOMSAbilities = true;
        public float doomsAbilityBonus = 0.2f;

        public CompProperties_ChiralProtection()
        {
            compClass = typeof(CompChiralProtection);
        }
    }

    // ==================== BT TETHER PROPERTIES ====================
    
    /// <summary>
    /// Properties for BT tethering components that apply psychological influence to nearby pawns
    /// </summary>
    public class CompProperties_BTTether : CompProperties
    {
        // Basic tether mechanics
        public float tetherStrength = 0.1f;
        public float tetherRange = 8f;
        public float tetherBuildupRate = 0.001f; // Per tick
        public float tetherDecayRate = 0.0005f; // Natural decay when not in range
        
        // Voidout triggering
        public bool canTriggerVoidout = false;
        public float voidoutThreshold = 0.95f;
        public float voidoutChancePerTick = 0.0001f;
        
        // Environmental modifiers
        public float timefallMultiplier = 1.5f;
        public float chiralProtectionReduction = 0.3f;
        public bool affectedByWeather = true;
        
        // DOOMS interaction
        public bool attractedToDOOMS = true;
        public float doomsAttractionMultiplier = 1.2f;
        public float doomsResistanceReduction = 0.8f; // DOOMS carriers get partial resistance
        
        // Visual effects
        public bool showTetherEffects = true;
        public bool showTetherRangeWhenSelected = true;
        public bool showTetherLines = true;
        public Color tetherLineColor = new Color(0.2f, 0.2f, 0.4f, 0.6f);
        
        // Advanced behavior
        public bool requiresLineOfSight = false;
        public bool canTetherThroughWalls = true;
        public int maxSimultaneousTethers = 5;
        public bool preferIsolatedTargets = true;

        public CompProperties_BTTether()
        {
            compClass = typeof(CompBTTether);
        }
    }

    // ==================== CORPSE TIMER PROPERTIES ====================
    
    /// <summary>
    /// Properties for corpse timer components that handle conversion of dead pawns to BTs
    /// </summary>
    public class CompProperties_CorpseTimer : CompProperties
    {
        // Basic conversion timing
        public int baseConversionTime = 60000; // ~1 day (60,000 ticks)
        public int minimumConversionTime = 1000; // Absolute minimum
        public int maximumConversionTime = 300000; // ~5 days maximum
        public float conversionTimeVariance = 0.2f; // ±20% randomization
        
        // Environmental acceleration/deceleration
        public int timefallAcceleration = 3; // 3x faster during timefall
        public float chiralSlowdown = 0.5f; // Half speed under chiral protection
        public float coldSlowdown = 0.7f; // Cold temperatures slow conversion
        public float heatAcceleration = 1.3f; // Hot temperatures speed conversion
        
        // Proximity effects
        public bool massDeathAcceleration = true;
        public float nearbyCorpseMultiplier = 0.2f; // 20% faster per nearby corpse
        public float nearbyBTMultiplier = 2.0f; // Double speed near BTs
        public float proximityRange = 10f;
        
        // Player intervention options
        public bool allowEmergencyCremation = true;
        public bool allowChiralTreatment = true;
        public int chiralTreatmentCost = 3; // Chiral crystals needed for treatment
        public float chiralTreatmentEffectiveness = 0.2f; // Slows to 20% speed
        public int chiralTreatmentDuration = 30000; // Effect duration in ticks
        
        // Warning system
        public bool showWarnings = true;
        public bool showProgressBar = true;
        public bool playWarningSound = true;
        public List<float> warningThresholds = new List<float> { 0.3f, 0.6f, 0.8f, 0.95f };
        
        // Conversion resistance
        public bool allowDOOMSResistance = true;
        public bool allowTraitResistance = true;
        public bool allowFactionResistance = false;
        
        // Visual effects during decay
        public bool showDecayEffects = true;
        public float effectIntensityMultiplier = 1.0f;
        public bool createSmokeEffects = true;
        public bool createEnergyEffects = true;

        public CompProperties_CorpseTimer()
        {
            compClass = typeof(CompCorpseTimer);
        }
    }

    // ==================== BT VISUAL EFFECTS PROPERTIES ====================
    
    /// <summary>
    /// Properties for BT visual effects that make pawns appear as translucent otherworldly entities
    /// </summary>
    public class CompProperties_BTVisualEffects : CompProperties
    {
        // Basic appearance
        public float baseAlpha = 0.6f;
        public float minimumAlpha = 0.1f;
        public float maximumAlpha = 0.9f;
        public Color baseColor = new Color(0.1f, 0.1f, 0.15f, 1f);
        
        // Material properties for liquid effect
        public float metallicValue = 0.8f;
        public float smoothnessValue = 0.9f;
        public bool useLiquidShader = true;
        
        // Animation settings
        public float distortionSpeed = 1.0f;
        public float distortionAmplitude = 0.1f;
        public float glowPulseSpeed = 2.0f;
        public bool enableDistortion = true;
        public bool enableGlowPulse = true;
        
        // Visibility system
        public float baseVisibilityChance = 0.1f;
        public float doomsDetectionRange = 15f;
        public int corporealityLevel = 1; // 1-3, affects overall visibility
        public bool flickerWhenHidden = true;
        public float flickerFrequency = 0.3f;
        
        // Glow effects
        public Color glowColor = new Color(0.3f, 0.8f, 1f, 1f); // Cyan glow
        public float maxGlowSize = 1.5f;
        public bool enableGlowEffect = true;
        public float glowIntensity = 0.7f;
        
        // Interaction effects
        public bool showDOOMSConnections = true;
        public bool showDistortionField = true;
        public float distortionFieldRadius = 3f;
        public bool respondToProximity = true;
        
        // Environmental visibility
        public float timefallVisibilityBonus = 0.3f;
        public float chiralFieldVisibilityBonus = 0.2f;
        public bool moreVisibleAtNight = false;
        public bool lessVisibleInRain = false;
        
        // Performance settings
        public bool useVisibilityLOD = true;
        public float lodDistance = 30f;
        public bool cacheMaterials = true;
        public int maxSimultaneousEffects = 10;

        public CompProperties_BTVisualEffects()
        {
            compClass = typeof(CompBTVisualEffects);
        }
    }

    // ==================== CHIRAL RESOURCE PROPERTIES ====================
    
    /// <summary>
    /// Properties for things that contain or generate chiral energy
    /// </summary>
    public class CompProperties_ChiralResource : CompProperties
    {
        // Resource generation
        public float chiralEnergyValue = 1.0f;
        public bool generateChiralEnergy = false;
        public float generationRate = 0.1f; // Per day
        public int maxStoredEnergy = 100;
        
        // Environmental interactions
        public bool growDuringTimefall = true;
        public float timefallGrowthMultiplier = 3.0f;
        public bool degradeWithoutTimefall = false;
        public float degradationRate = 0.01f; // Per day
        
        // Quality and processing
        public bool hasQualityLevels = true;
        public float qualityEnergyMultiplier = 1.5f; // Excellent quality gives 50% more energy
        public bool canBeProcessed = true;
        public float processingEnergyBonus = 0.2f;
        
        // Network interaction
        public bool connectsToChiralNetwork = false;
        public float networkBonus = 0.1f;
        public bool enhancesNearbyNodes = false;
        public float enhancementRange = 5f;
        
        // Visual effects
        public bool showEnergyGlow = true;
        public Color energyGlowColor = new Color(0.4f, 0.9f, 1f, 0.8f);
        public float glowIntensity = 0.5f;
        public bool pulseWithEnergy = true;

        public CompProperties_ChiralResource()
        {
            compClass = typeof(CompChiralResource);
        }
    }

    // ==================== DOOMS ABILITY PROPERTIES ====================
    
    /// <summary>
    /// Properties for pawns with DOOMS abilities
    /// </summary>
    public class CompProperties_DOOMSAbility : CompProperties
    {
        // Ability management
        public List<string> availableAbilities = new List<string>();
        public float abilityCooldownMultiplier = 1.0f;
        public float abilityRangeMultiplier = 1.0f;
        public float abilityEffectivenessMultiplier = 1.0f;
        
        // Beach drift management
        public float beachDriftResistance = 0.1f;
        public float beachDriftRecoveryRate = 0.05f; // Per hour
        public bool canEnterBeachVoluntarily = false;
        public float voluntaryBeachDuration = 60000; // 1 day
        
        // Chiral sensitivity
        public float chiralSensitivityRange = 20f;
        public bool detectChiralAnomalies = true;
        public bool senseTimefallApproach = true;
        public int timefallPredictionHours = 6;
        
        // BT interaction
        public float btDetectionRange = 15f;
        public bool canSeeBTsAlways = false;
        public bool btDetectionThroughWalls = false;
        public float btInteractionBonus = 0.2f;
        
        // Psychic enhancement
        public bool enhancesPsychicAbilities = true;
        public float psychicAmplificationFactor = 1.3f;
        public bool reducePsychicEntropyCost = true;
        public float entropyCostReduction = 0.2f;
        
        // Voidout interaction
        public bool canTriggerVoidouts = false;
        public bool resistVoidoutDamage = true;
        public float voidoutDamageReduction = 0.5f;
        public bool canSurviveVoidouts = false;

        public CompProperties_DOOMSAbility()
        {
            compClass = typeof(CompDOOMSAbility);
        }
    }

    // ==================== TEMPORAL DISTORTION PROPERTIES ====================
    
    /// <summary>
    /// Properties for objects affected by temporal distortion from timefall or abilities
    /// </summary>
    public class CompProperties_TemporalDistortion : CompProperties
    {
        // Aging effects
        public float agingMultiplier = 1.0f;
        public bool affectedByTimefall = true;
        public float timefallAgingMultiplier = 2.5f;
        public bool canReverseAging = false;
        
        // Temporal stability
        public float temporalStability = 1.0f;
        public bool canBecomeUnstable = true;
        public float instabilityThreshold = 0.3f;
        public bool explodeWhenUnstable = false;
        
        // Time dilation effects
        public bool canSlowTime = false;
        public bool canSpeedTime = false;
        public float maxTimeDilation = 2.0f;
        public float timeDilationDuration = 30000; // 30 seconds
        
        // Interaction with abilities
        public bool enhancesTemporalAbilities = false;
        public float temporalAbilityBonus = 0.3f;
        public bool resistsTemporalManipulation = false;
        public float temporalResistance = 0.5f;
        
        // Visual effects
        public bool showTemporalEffects = true;
        public Color temporalFieldColor = new Color(0.8f, 0.6f, 1f, 0.4f);
        public bool showAgingEffects = true;
        public bool showTimeRipples = false;

        public CompProperties_TemporalDistortion()
        {
            compClass = typeof(CompTemporalDistortion);
        }
    }

    // ==================== BEACH DIMENSION PROPERTIES ====================
    
    /// <summary>
    /// Properties for objects or areas connected to the Beach dimension
    /// </summary>
    public class CompProperties_BeachDimension : CompProperties
    {
        // Beach connection
        public float beachConnectionStrength = 0.5f;
        public bool isBeachPortal = false;
        public bool allowsBeachTravel = false;
        public float portalStabilityTime = 60000; // 1 day
        
        // Dimensional effects
        public bool affectsNearbyPawns = true;
        public float dimensionalInfluenceRange = 10f;
        public bool causesDimensionalSickness = false;
        public float sicknessSeverity = 0.3f;
        
        // Entity spawning
        public bool canSpawnBTs = false;
        public float btSpawnChance = 0.01f; // Per day
        public List<string> spawnableBTTypes = new List<string> { "BT_Basic" };
        public bool spawnOnlyDuringTimefall = true;
        
        // Temporal anomalies
        public bool createTemporalAnomalies = false;
        public float anomalyStrength = 0.5f;
        public bool affectsTime = false;
        public float timeDistortionFactor = 1.2f;
        
        // Chiral resonance
        public bool resonatesWithChiralNetwork = true;
        public float resonanceStrength = 0.8f;
        public bool enhancesNearbyNodes = true;
        public float nodeEnhancementFactor = 1.3f;
        
        // Visual manifestation
        public bool showDimensionalEffects = true;
        public Color beachAuraColor = new Color(0.6f, 0.6f, 0.9f, 0.5f);
        public bool showBeachMist = true;
        public bool createBeachSounds = false;

        public CompProperties_BeachDimension()
        {
            compClass = typeof(CompBeachDimension);
        }
    }

    // ==================== VOIDOUT CATALYST PROPERTIES ====================
    
    /// <summary>
    /// Properties for objects that can trigger or amplify voidout events
    /// </summary>
    public class CompProperties_VoidoutCatalyst : CompProperties
    {
        // Catalyst behavior
        public float catalystStrength = 1.0f;
        public bool triggersVoidouts = false;
        public float triggerThreshold = 0.8f;
        public bool amplifiesVoidouts = true;
        
        // Range and damage
        public float voidoutRadiusMultiplier = 1.5f;
        public float voidoutDamageMultiplier = 1.2f;
        public bool createsLargerCrater = true;
        public float craterSizeMultiplier = 1.3f;
        
        // Chain reactions
        public bool canChainReact = true;
        public float chainReactionChance = 0.3f;
        public float chainReactionRange = 15f;
        public int maxChainReactions = 3;
        
        // Stability
        public float stability = 1.0f;
        public bool becomesUnstableOverTime = true;
        public float instabilityRate = 0.01f; // Per day
        public bool explodeWhenDestroyed = false;
        
        // Containment
        public bool canBeContained = true;
        public bool requiresSpecialContainment = false;
        public float containmentEffectiveness = 0.8f;
        public bool warnWhenUnstable = true;
        
        // Environmental interaction
        public bool affectedByTimefall = true;
        public float timefallInstabilityMultiplier = 2.0f;
        public bool stabilizedByChiralField = true;
        public float chiralStabilizationFactor = 0.5f;

        public CompProperties_VoidoutCatalyst()
        {
            compClass = typeof(CompVoidoutCatalyst);
        }
    }

    // ==================== PLACEHOLDER COMPONENT CLASSES ====================
    // These would need full implementation but are referenced by the properties above
    
    /// <summary>
    /// Placeholder for CompChiralResource - handles chiral energy generation and storage
    /// </summary>
    public class CompChiralResource : ThingComp
    {
        // Implementation would go here
    }

    /// <summary>
    /// Placeholder for CompDOOMSAbility - manages DOOMS-specific abilities and effects
    /// </summary>
    public class CompDOOMSAbility : ThingComp
    {
        // Implementation would go here
    }

    /// <summary>
    /// Placeholder for CompTemporalDistortion - handles time-related effects
    /// </summary>
    public class CompTemporalDistortion : ThingComp
    {
        // Implementation would go here
    }

    /// <summary>
    /// Placeholder for CompBeachDimension - manages Beach dimension connections
    /// </summary>
    public class CompBeachDimension : ThingComp
    {
        // Implementation would go here
    }

    /// <summary>
    /// Placeholder for CompVoidoutCatalyst - handles voidout triggering and amplification
    /// </summary>
    public class CompVoidoutCatalyst : ThingComp
    {
        // Implementation would go here
    }
}