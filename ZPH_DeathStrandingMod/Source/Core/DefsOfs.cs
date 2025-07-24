using RimWorld;
using Verse;

namespace DeathStrandingMod.Core
{
    /// <summary>
    /// DefOf class for Hediff definitions
    /// </summary>
    [DefOf]
    public static class HediffDefOf_DeathStranding
    {
        public static HediffDef BTTether;
        public static HediffDef BeachDrift;
        public static HediffDef DOOMS_Sensitivity;
        public static HediffDef ChiralOverdose;
        public static HediffDef VoidoutRadiation;
        
        static HediffDefOf_DeathStranding()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HediffDefOf_DeathStranding));
        }
    }

    /// <summary>
    /// DefOf class for Thing definitions
    /// </summary>
    [DefOf]
    public static class ThingDefOf_DeathStranding
    {
        public static ThingDef ChiralCrystal;
        public static ThingDef ChiralNetworkNode;
        public static ThingDef ChiralAlloy;
        public static ThingDef BT_Basic;
        public static ThingDef BT_Hunter;
        public static ThingDef BT_Catcher;
        public static ThingDef VoidoutCrater;
        
        static ThingDefOf_DeathStranding()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf_DeathStranding));
        }
    }

    /// <summary>
    /// DefOf class for PawnKind definitions
    /// </summary>
    [DefOf]
    public static class PawnKindDefOf_DeathStranding
    {
        public static PawnKindDef BT_Basic;
        public static PawnKindDef BT_Hunter;
        public static PawnKindDef BT_Catcher;
        public static PawnKindDef BT_Lion;
        
        static PawnKindDefOf_DeathStranding()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(PawnKindDefOf_DeathStranding));
        }
    }

    /// <summary>
    /// DefOf class for Incident definitions
    /// </summary>
    [DefOf]
    public static class IncidentDefOf_DeathStranding
    {
        public static IncidentDef Voidout;
        public static IncidentDef MajorVoidout;
        public static IncidentDef BTManifestation;
        public static IncidentDef BTSwarm;
        public static IncidentDef BTHunter;
        public static IncidentDef BTCatcher;
        public static IncidentDef BTLion;
        public static IncidentDef ChiralCrystalFormation;
        public static IncidentDef ChiralNetworkRequest;
        public static IncidentDef CorpseTransportQuest;
        public static IncidentDef NetworkExpansionReward;
        public static IncidentDef ChiralResonanceEvent;
        public static IncidentDef TimefallStorm;
        
        static IncidentDefOf_DeathStranding()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(IncidentDefOf_DeathStranding));
        }
    }

    /// <summary>
    /// DefOf class for Weather definitions
    /// </summary>
    [DefOf]
    public static class WeatherDefOf_DeathStranding
    {
        public static WeatherDef Timefall;
        public static WeatherDef TimefallStorm;
        
        static WeatherDefOf_DeathStranding()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WeatherDefOf_DeathStranding));
        }
    }

    /// <summary>
    /// DefOf class for Gene definitions
    /// </summary>
    [DefOf]
    public static class GeneDefOf_DeathStranding
    {
        public static GeneDef DOOMS_Level1;
        public static GeneDef DOOMS_Level3;
        public static GeneDef DOOMS_Level5;
        public static GeneDef DOOMS_Level7;
        public static GeneDef DOOMS_Level8_Higgs;
        
        static GeneDefOf_DeathStranding()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(GeneDefOf_DeathStranding));
        }
    }

    /// <summary>
    /// DefOf class for Ability definitions
    /// </summary>
    [DefOf]
    public static class AbilityDefOf_DeathStranding
    {
        // Level 5 abilities
        public static AbilityDef DOOMS_GravityNudge;
        public static AbilityDef DOOMS_MatterSense;
        public static AbilityDef DOOMS_BeachGlimpse;
        
        // Level 7 abilities
        public static AbilityDef DOOMS_GravityField;
        public static AbilityDef DOOMS_MatterControl;
        public static AbilityDef DOOMS_BeachStep;
        public static AbilityDef DOOMS_DimensionalAnchor;
        
        // Level 8 abilities
        public static AbilityDef DOOMS_RealityFracture;
        public static AbilityDef DOOMS_MassLevitation;
        public static AbilityDef DOOMS_TemporalDisplacement;
        public static AbilityDef DOOMS_PhaseShift;
        public static AbilityDef DOOMS_InstantArchitecture;
        
        static AbilityDefOf_DeathStranding()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AbilityDefOf_DeathStranding));
        }
    }

    /// <summary>
    /// DefOf class for MentalState definitions
    /// </summary>
    [DefOf]
    public static class MentalStateDefOf_DeathStranding
    {
        public static MentalStateDef BeachDrift;
        public static MentalStateDef RealityDistortion;
        
        static MentalStateDefOf_DeathStranding()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MentalStateDefOf_DeathStranding));
        }
    }

    /// <summary>
    /// DefOf class for Research definitions
    /// </summary>
    [DefOf]
    public static class ResearchProjectDefOf_DeathStranding
    {
        public static ResearchProjectDef ChiralTechnology;
        public static ResearchProjectDef BTContainment;
        public static ResearchProjectDef AdvancedChiralTechnology;
        public static ResearchProjectDef VoidoutPrevention;
        
        static ResearchProjectDefOf_DeathStranding()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ResearchProjectDefOf_DeathStranding));
        }
    }

    /// <summary>
    /// DefOf class for Damage definitions
    /// </summary>
    [DefOf]
    public static class DamageDefOf_DeathStranding
    {
        public static DamageDef VoidoutDamage;
        public static DamageDef ChiralBurn;
        public static DamageDef TemporalAging;
        
        static DamageDefOf_DeathStranding()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DamageDefOf_DeathStranding));
        }
    }
}