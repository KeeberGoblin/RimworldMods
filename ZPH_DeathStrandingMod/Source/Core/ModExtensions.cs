using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeathStrandingMod.Core
{
    /// <summary>
    /// Properties for DOOMS gene levels and abilities
    /// </summary>
    public class DOOMSProperties : DefModExtension
    {
        public int level = 1;
        public float btDetectionRange = 15f;
        public float tetherResistance = 0.1f;
        public float chiralSensitivity = 1.0f;
        public bool timefallPrediction = false;
        public bool voidoutResistance = false;
        public List<AbilityDef> unlockedAbilities = new List<AbilityDef>();
    }

    /// <summary>
    /// Properties for BT creatures and their behaviors
    /// </summary>
    public class BTProperties : DefModExtension
    {
        public float tetherStrength = 0.1f;
        public float tetherRange = 8f;
        public bool voidoutTrigger = false;
        public int corporealityLevel = 1; // 1-3, affects visibility and interaction
        public float spawnWeight = 1.0f;
        public bool canHunt = true;
        public bool attractedToCorpses = true;
    }

    /// <summary>
    /// Properties for chiral crystals and related materials
    /// </summary>
    public class ChiralProperties : DefModExtension
    {
        public bool timefallProtection = false;
        public float protectionRadius = 3.5f;
        public float degradationRate = 0.05f;
        public bool enhancesDOOMS = false;
        public float chiralEnergyValue = 1.0f;
    }

    /// <summary>
    /// Properties for DOOMS abilities and their costs/effects
    /// </summary>
    public class AbilityProperties_DOOMS : DefModExtension
    {
        public int chiralCostPerUse = 0;
        public float neuralHeatGain = 10f;
        public float beachDriftRisk = 0.0f;
        public bool requiresChiralCrystals = true;
        public bool canTriggerVoidout = false;
        public float voidoutChance = 0.0f;
    }

    /// <summary>
    /// Properties for storyteller configurations
    /// </summary>
    public class StorytellerProperties_BridgesTheme : DefModExtension
    {
        public float timefallFrequencyMultiplier = 2.5f;
        public bool btCorpseConversionEnabled = true;
        public bool voidoutEventEnabled = true;
        public bool chiralNetworkFocus = true;
        public bool connectionThemeFocus = true;
        public bool reduceTraditionalRaids = true;
        public bool emphasizeTransportQuests = true;
        public float doomsCarrierSpawnChance = 0.15f;
    }

    /// <summary>
    /// Properties for voidout events and their effects
    /// </summary>
    public class VoidoutProperties : DefModExtension
    {
        public float baseRadius = 15f;
        public float maxRadius = 35f;
        public bool leaveCrater = true;
        public bool spawnChiralResidue = true;
        public bool gameEndingRisk = false;
        public float structureDamageMultiplier = 2.0f;
    }

    /// <summary>
    /// Properties for timefall weather effects
    /// </summary>
    public class TimefallProperties : DefModExtension
    {
        public float agingMultiplier = 2.5f;
        public float chiralSpawnChance = 0.08f;
        public float btSpawnChance = 0.15f;
        public bool accelerateCorpseConversion = true;
        public float conversionSpeedMultiplier = 3.0f;
    }
}