using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using DeathStrandingMod.Core;

namespace DeathStrandingMod.Scenarios
{
    /// <summary>
    /// Scenario part that ensures starting pawns have DOOMS abilities
    /// </summary>
    public class ScenPart_StartingPawnDOOMS : ScenPart_PawnModifier
    {
        private DOOMSLevel doomsLevel = DOOMSLevel.Level1;
        private float doomsChance = 1f; // 100% chance by default for scenario
        private bool guaranteeAtLeastOne = true;
        private bool allowMultipleDOOMS = true;
        private int maxDOOMSCarriers = 3;
        
        public override string Summary(Scenario scen)
        {
            string levelText = GetDOOMSLevelDescription(doomsLevel);
            
            if (guaranteeAtLeastOne && allowMultipleDOOMS)
            {
                return "ScenPart_StartingDOOMSMultiple".Translate(levelText, maxDOOMSCarriers);
            }
            else if (guaranteeAtLeastOne)
            {
                return "ScenPart_StartingDOOMSSingle".Translate(levelText);
            }
            else
            {
                return "ScenPart_StartingDOOMSChance".Translate(levelText, (doomsChance * 100f).ToString("F0"));
            }
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 6f);
            
            // Title
            Rect titleRect = new Rect(scenPartRect.x, scenPartRect.y, scenPartRect.width, ScenPart.RowHeight);
            Widgets.Label(titleRect, "ScenPart_StartingDOOMSTitle".Translate());
            
            // DOOMS Level selection
            Rect levelRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight, 
                scenPartRect.width * 0.6f, ScenPart.RowHeight);
            
            string levelLabel = "ScenPart_DOOMSLevel".Translate() + ": " + GetDOOMSLevelDescription(doomsLevel);
            if (Widgets.ButtonText(levelRect, levelLabel))
            {
                List<FloatMenuOption> levelOptions = new List<FloatMenuOption>();
                
                foreach (DOOMSLevel level in Enum.GetValues(typeof(DOOMSLevel)))
                {
                    DOOMSLevel localLevel = level; // Capture for closure
                    levelOptions.Add(new FloatMenuOption(
                        GetDOOMSLevelDescription(level),
                        () => doomsLevel = localLevel
                    ));
                }
                
                Find.WindowStack.Add(new FloatMenu(levelOptions));
            }
            
            // Chance slider (only if not guaranteeing at least one)
            if (!guaranteeAtLeastOne)
            {
                Rect chanceRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight * 2, 
                    scenPartRect.width, ScenPart.RowHeight);
                
                string chanceLabel = "ScenPart_DOOMSChance".Translate() + ": " + (doomsChance * 100f).ToString("F0") + "%";
                Widgets.Label(chanceRect, chanceLabel);
                
                Rect sliderRect = new Rect(chanceRect.x + chanceRect.width * 0.4f, chanceRect.y, 
                    chanceRect.width * 0.6f, chanceRect.height);
                doomsChance = Widgets.HorizontalSlider(sliderRect, doomsChance, 0f, 1f, true);
            }
            
            // Guarantee at least one checkbox
            Rect guaranteeRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight * 3, 
                scenPartRect.width, ScenPart.RowHeight);
            Widgets.CheckboxLabeled(guaranteeRect, "ScenPart_GuaranteeOneDOOMS".Translate(), ref guaranteeAtLeastOne);
            
            // Allow multiple DOOMS carriers checkbox
            Rect multipleRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight * 4, 
                scenPartRect.width, ScenPart.RowHeight);
            Widgets.CheckboxLabeled(multipleRect, "ScenPart_AllowMultipleDOOMS".Translate(), ref allowMultipleDOOMS);
            
            // Max DOOMS carriers (only if allowing multiple)
            if (allowMultipleDOOMS)
            {
                Rect maxRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight * 5, 
                    scenPartRect.width * 0.6f, ScenPart.RowHeight);
                
                string maxLabel = "ScenPart_MaxDOOMS".Translate() + ": " + maxDOOMSCarriers;
                Widgets.Label(maxRect, maxLabel);
                
                Rect maxSliderRect = new Rect(maxRect.x + maxRect.width, maxRect.y, 
                    scenPartRect.width * 0.4f, maxRect.height);
                maxDOOMSCarriers = Mathf.RoundToInt(Widgets.HorizontalSlider(maxSliderRect, maxDOOMSCarriers, 1f, 5f, true));
            }
        }

        public override void Randomize()
        {
            base.Randomize();
            
            // Randomize DOOMS parameters
            doomsLevel = (DOOMSLevel)Rand.Range(1, 4); // Level 1-3
            doomsChance = Rand.Range(0.3f, 1f);
            guaranteeAtLeastOne = Rand.Bool;
            allowMultipleDOOMS = Rand.Bool;
            maxDOOMSCarriers = Rand.Range(1, 4);
        }

        public override void ModifyNewPawn(Pawn pawn)
        {
            if (!pawn.RaceProps.Humanlike)
                return;
            
            // Check if we should apply DOOMS to this pawn
            bool shouldApplyDOOMS = DetermineIfShouldApplyDOOMS(pawn);
            
            if (shouldApplyDOOMS)
            {
                ApplyDOOMSToPawn(pawn, doomsLevel);
                
                // Apply additional modifications for DOOMS carriers
                ApplyDOOMSCarrierModifications(pawn);
                
                // Add DOOMS-related starting equipment
                AddDOOMSStartingEquipment(pawn);
            }
        }

        private bool DetermineIfShouldApplyDOOMS(Pawn pawn)
        {
            // Count existing DOOMS carriers in the scenario
            int existingDOOMSCarriers = CountExistingDOOMSCarriers();
            
            if (guaranteeAtLeastOne && existingDOOMSCarriers == 0)
            {
                return true; // Guarantee at least one
            }
            
            if (!allowMultipleDOOMS && existingDOOMSCarriers > 0)
            {
                return false; // Only allow one
            }
            
            if (existingDOOMSCarriers >= maxDOOMSCarriers)
            {
                return false; // Hit maximum
            }
            
            return Rand.Chance(doomsChance);
        }

        private int CountExistingDOOMSCarriers()
        {
            // This would need to check the current scenario's pawns
            // For now, we'll use a simple approach
            return 0; // Simplified for scenario generation
        }

        private void ApplyDOOMSToPawn(Pawn pawn, DOOMSLevel level)
        {
            // Add DOOMS trait
            Trait doomsTrait = new Trait(TraitDefOf_DeathStranding.DOOMS, (int)level);
            pawn.story.traits.GainTrait(doomsTrait);
            
            // Add starting DOOMS abilities based on level
            GrantStartingDOOMSAbilities(pawn, level);
            
            // Apply DOOMS-related hediffs
            ApplyDOOMSHediffs(pawn, level);
        }

        private void GrantStartingDOOMSAbilities(Pawn pawn, DOOMSLevel level)
        {
            if (pawn.abilities == null)
                pawn.abilities = new Pawn_AbilityTracker(pawn);
            
            // Level 1: Basic BT detection
            if (level >= DOOMSLevel.Level1)
            {
                pawn.abilities.GainAbility(AbilityDefOf_DeathStranding.DOOMS_BTDetection);
            }
            
            // Level 2: Enhanced detection and basic manipulation
            if (level >= DOOMSLevel.Level2)
            {
                pawn.abilities.GainAbility(AbilityDefOf_DeathStranding.DOOMS_EnhancedDetection);
                pawn.abilities.GainAbility(AbilityDefOf_DeathStranding.DOOMS_GravityNudge);
            }
            
            // Level 3: Advanced abilities
            if (level >= DOOMSLevel.Level3)
            {
                pawn.abilities.GainAbility(AbilityDefOf_DeathStranding.DOOMS_BeachVision);
                pawn.abilities.GainAbility(AbilityDefOf_DeathStranding.DOOMS_TimeAcceleration);
            }
            
            // Level 4: Master level abilities (very rare)
            if (level >= DOOMSLevel.Level4)
            {
                pawn.abilities.GainAbility(AbilityDefOf_DeathStranding.DOOMS_VoidManipulation);
            }
        }

        private void ApplyDOOMSHediffs(Pawn pawn, DOOMSLevel level)
        {
            // DOOMS sensitivity - makes pawn more aware but also more vulnerable
            Hediff sensitivity = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.DOOMSSensitivity, pawn);
            sensitivity.Severity = 0.1f + ((int)level * 0.1f); // 0.2 to 0.5 severity
            pawn.health.AddHediff(sensitivity);
            
            // Aphenphosmphobia (fear of being touched) for higher levels
            if (level >= DOOMSLevel.Level3)
            {
                Hediff phobia = HediffMaker.MakeHediff(HediffDefOf_DeathStranding.Aphenphosmphobia, pawn);
                phobia.Severity = 0.3f;
                pawn.health.AddHediff(phobia);
            }
        }

        private void ApplyDOOMSCarrierModifications(Pawn pawn)
        {
            // Adjust backstory to reflect DOOMS nature
            if (pawn.story != null)
            {
                // Boost intellectual and artistic skills (DOOMS carriers are often sensitive/creative)
                if (pawn.skills != null)
                {
                    pawn.skills.GetSkill(SkillDefOf.Intellectual).Level += Rand.Range(1, 3);
                    pawn.skills.GetSkill(SkillDefOf.Artistic).Level += Rand.Range(1, 2);
                    
                    // Slight penalty to social due to isolation tendencies
                    pawn.skills.GetSkill(SkillDefOf.Social).Level = Math.Max(0, 
                        pawn.skills.GetSkill(SkillDefOf.Social).Level - 1);
                }
                
                // Add complementary traits that fit DOOMS carriers
                if (Rand.Chance(0.3f) && !pawn.story.traits.HasTrait(TraitDefOf.Neurotic))
                {
                    pawn.story.traits.GainTrait(new Trait(TraitDefOf.Neurotic));
                }
                
                if (Rand.Chance(0.2f) && !pawn.story.traits.HasTrait(TraitDefOf.CreativelyInspired))
                {
                    pawn.story.traits.GainTrait(new Trait(TraitDefOf.CreativelyInspired));
                }
            }
        }

        private void AddDOOMSStartingEquipment(Pawn pawn)
        {
            // Add DOOMS-specific starting equipment
            
            // Chiral crystals (small amount)
            Thing crystals = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
            crystals.stackCount = Rand.Range(2, 5);
            
            if (pawn.inventory != null)
            {
                pawn.inventory.TryAddItemNotForSale(crystals);
            }
            
            // Special DOOMS scanner device (if available)
            if (Rand.Chance(0.4f))
            {
                Thing scanner = ThingMaker.MakeThing(ThingDefOf_DeathStranding.DOOMSScanner);
                if (pawn.inventory != null)
                {
                    pawn.inventory.TryAddItemNotForSale(scanner);
                }
            }
            
            // Memory about their DOOMS awakening
            if (pawn.needs?.mood?.thoughts?.memories != null)
            {
                ThoughtDef awakeningMemory = DefDatabase<ThoughtDef>.GetNamedSilentFail("DOOMSAwakeningMemory");
                if (awakeningMemory != null)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(awakeningMemory);
                }
            }
        }

        private string GetDOOMSLevelDescription(DOOMSLevel level)
        {
            return level switch
            {
                DOOMSLevel.Level1 => "DOOMSLevel1Description".Translate(),
                DOOMSLevel.Level2 => "DOOMSLevel2Description".Translate(),
                DOOMSLevel.Level3 => "DOOMSLevel3Description".Translate(),
                DOOMSLevel.Level4 => "DOOMSLevel4Description".Translate(),
                _ => "Unknown"
            };
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref doomsLevel, "doomsLevel", DOOMSLevel.Level1);
            Scribe_Values.Look(ref doomsChance, "doomsChance", 1f);
            Scribe_Values.Look(ref guaranteeAtLeastOne, "guaranteeAtLeastOne", true);
            Scribe_Values.Look(ref allowMultipleDOOMS, "allowMultipleDOOMS", true);
            Scribe_Values.Look(ref maxDOOMSCarriers, "maxDOOMSCarriers", 3);
        }

        public override bool TryMerge(ScenPart other)
        {
            if (other is ScenPart_StartingPawnDOOMS otherDOOMS)
            {
                // Can merge if they have compatible settings
                if (doomsLevel == otherDOOMS.doomsLevel)
                {
                    doomsChance = Math.Max(doomsChance, otherDOOMS.doomsChance);
                    maxDOOMSCarriers = Math.Max(maxDOOMSCarriers, otherDOOMS.maxDOOMSCarriers);
                    guaranteeAtLeastOne = guaranteeAtLeastOne || otherDOOMS.guaranteeAtLeastOne;
                    allowMultipleDOOMS = allowMultipleDOOMS || otherDOOMS.allowMultipleDOOMS;
                    return true;
                }
            }
            
            return false;
        }

        public override int GetHashCode()
        {
            return Gen.HashCombine(base.GetHashCode(), doomsLevel.GetHashCode());
        }
    }

    /// <summary>
    /// Scenario part that adds DOOMS-related starting conditions
    /// </summary>
    public class ScenPart_DOOMSBackground : ScenPart
    {
        private DOOMSBackgroundType backgroundType = DOOMSBackgroundType.PorterExpedition;
        private bool includeChiralEquipment = true;
        private bool includeNetworkKnowledge = true;
        
        public override string Summary(Scenario scen)
        {
            return backgroundType switch
            {
                DOOMSBackgroundType.PorterExpedition => "ScenPart_PorterExpeditionBG".Translate(),
                DOOMSBackgroundType.BridgesSurvivors => "ScenPart_BridgesSurvivorsBG".Translate(),
                DOOMSBackgroundType.DOOMSResearchers => "ScenPart_DOOMSResearchersBG".Translate(),
                DOOMSBackgroundType.ChiralMiners => "ScenPart_ChiralMinersBG".Translate(),
                _ => "Unknown DOOMS Background"
            };
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 4f);
            
            // Background type selection
            Rect bgTypeRect = new Rect(scenPartRect.x, scenPartRect.y, scenPartRect.width, ScenPart.RowHeight);
            string bgLabel = "ScenPart_DOOMSBackground".Translate() + ": " + GetBackgroundDescription(backgroundType);
            
            if (Widgets.ButtonText(bgTypeRect, bgLabel))
            {
                List<FloatMenuOption> bgOptions = new List<FloatMenuOption>();
                
                foreach (DOOMSBackgroundType bgType in Enum.GetValues(typeof(DOOMSBackgroundType)))
                {
                    DOOMSBackgroundType localBgType = bgType;
                    bgOptions.Add(new FloatMenuOption(
                        GetBackgroundDescription(bgType),
                        () => backgroundType = localBgType
                    ));
                }
                
                Find.WindowStack.Add(new FloatMenu(bgOptions));
            }
            
            // Include chiral equipment checkbox
            Rect equipmentRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight, 
                scenPartRect.width, ScenPart.RowHeight);
            Widgets.CheckboxLabeled(equipmentRect, "ScenPart_IncludeChiralEquipment".Translate(), ref includeChiralEquipment);
            
            // Include network knowledge checkbox
            Rect knowledgeRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight * 2, 
                scenPartRect.width, ScenPart.RowHeight);
            Widgets.CheckboxLabeled(knowledgeRect, "ScenPart_IncludeNetworkKnowledge".Translate(), ref includeNetworkKnowledge);
        }

        public override void GenerateIntoMap(Map map)
        {
            base.GenerateIntoMap(map);
            
            // Apply background-specific starting conditions
            ApplyBackgroundEffects(map);
            
            // Add background-specific starting items
            if (includeChiralEquipment)
            {
                AddChiralStartingEquipment(map);
            }
            
            // Grant background-specific research
            if (includeNetworkKnowledge)
            {
                GrantNetworkKnowledge();
            }
        }

        private void ApplyBackgroundEffects(Map map)
        {
            switch (backgroundType)
            {
                case DOOMSBackgroundType.PorterExpedition:
                    ApplyPorterExpeditionEffects(map);
                    break;
                case DOOMSBackgroundType.BridgesSurvivors:
                    ApplyBridgesSurvivorEffects(map);
                    break;
                case DOOMSBackgroundType.DOOMSResearchers:
                    ApplyDOOMSResearcherEffects(map);
                    break;
                case DOOMSBackgroundType.ChiralMiners:
                    ApplyChiralMinerEffects(map);
                    break;
            }
        }

        private void ApplyPorterExpeditionEffects(Map map)
        {
            // Porter expeditions start with delivery experience
            foreach (Pawn colonist in map.mapPawns.FreeColonists)
            {
                if (colonist.skills != null)
                {
                    colonist.skills.GetSkill(SkillDefOf.Animals).Level += 2; // Pack animal handling
                    colonist.skills.GetSkill(SkillDefOf.Melee).Level += 1; // Self-defense
                }
                
                // Add porter mindset
                ThoughtDef porterPride = DefDatabase<ThoughtDef>.GetNamedSilentFail("PorterExpeditionPride");
                if (porterPride != null && colonist.needs?.mood?.thoughts?.memories != null)
                {
                    colonist.needs.mood.thoughts.memories.TryGainMemory(porterPride);
                }
            }
        }

        private void ApplyBridgesSurvivorEffects(Map map)
        {
            // Bridges survivors have organizational knowledge
            foreach (Pawn colonist in map.mapPawns.FreeColonists)
            {
                if (colonist.skills != null)
                {
                    colonist.skills.GetSkill(SkillDefOf.Intellectual).Level += 2;
                    colonist.skills.GetSkill(SkillDefOf.Social).Level += 1;
                }
                
                // Strong connection ideology
                ThoughtDef bridgesLegacy = DefDatabase<ThoughtDef>.GetNamedSilentFail("BridgesLegacyMemory");
                if (bridgesLegacy != null && colonist.needs?.mood?.thoughts?.memories != null)
                {
                    colonist.needs.mood.thoughts.memories.TryGainMemory(bridgesLegacy);
                }
            }
        }

        private void ApplyDOOMSResearcherEffects(Map map)
        {
            // DOOMS researchers have scientific background
            foreach (Pawn colonist in map.mapPawns.FreeColonists)
            {
                if (colonist.skills != null)
                {
                    colonist.skills.GetSkill(SkillDefOf.Intellectual).Level += 3;
                    colonist.skills.GetSkill(SkillDefOf.Medicine).Level += 2;
                }
            }
        }

        private void ApplyChiralMinerEffects(Map map)
        {
            // Chiral miners have industrial experience
            foreach (Pawn colonist in map.mapPawns.FreeColonists)
            {
                if (colonist.skills != null)
                {
                    colonist.skills.GetSkill(SkillDefOf.Mining).Level += 3;
                    colonist.skills.GetSkill(SkillDefOf.Crafting).Level += 2;
                }
            }
        }

        private void AddChiralStartingEquipment(Map map)
        {
            List<Thing> startingEquipment = new List<Thing>();
            
            // Base chiral equipment for all backgrounds
            Thing crystals = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
            crystals.stackCount = 10;
            startingEquipment.Add(crystals);
            
            // Background-specific equipment
            switch (backgroundType)
            {
                case DOOMSBackgroundType.PorterExpedition:
                    // Porter equipment
                    Thing porterSuit = ThingMaker.MakeThing(ThingDefOf_DeathStranding.PorterSuit);
                    startingEquipment.Add(porterSuit);
                    
                    Thing deliveryContainer = ThingMaker.MakeThing(ThingDefOf_DeathStranding.DeliveryContainer);
                    startingEquipment.Add(deliveryContainer);
                    break;
                    
                case DOOMSBackgroundType.BridgesSurvivors:
                    // Bridges communication equipment
                    Thing commDevice = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCommunicator);
                    startingEquipment.Add(commDevice);
                    break;
                    
                case DOOMSBackgroundType.DOOMSResearchers:
                    // Research equipment
                    Thing scanner = ThingMaker.MakeThing(ThingDefOf_DeathStranding.DOOMSScanner);
                    startingEquipment.Add(scanner);
                    
                    Thing researchNotes = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ResearchData);
                    startingEquipment.Add(researchNotes);
                    break;
                    
                case DOOMSBackgroundType.ChiralMiners:
                    // Mining equipment
                    Thing miningLaser = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralMiningLaser);
                    startingEquipment.Add(miningLaser);
                    
                    Thing extraCrystals = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
                    extraCrystals.stackCount = 15;
                    startingEquipment.Add(extraCrystals);
                    break;
            }
            
            // Drop equipment near colony center
            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);
            foreach (Thing item in startingEquipment)
            {
                GenPlace.TryPlaceThing(item, dropSpot, map, ThingPlaceMode.Near);
            }
        }

        private void GrantNetworkKnowledge()
        {
            // Grant relevant research based on background
            float researchPoints = backgroundType switch
            {
                DOOMSBackgroundType.PorterExpedition => 500f,
                DOOMSBackgroundType.BridgesSurvivors => 800f,
                DOOMSBackgroundType.DOOMSResearchers => 1200f,
                DOOMSBackgroundType.ChiralMiners => 600f,
                _ => 300f
            };
            
            Find.ResearchManager.ResearchPerformed(researchPoints, null);
        }

        private string GetBackgroundDescription(DOOMSBackgroundType bgType)
        {
            return bgType switch
            {
                DOOMSBackgroundType.PorterExpedition => "DOOMSBGPorterExpedition".Translate(),
                DOOMSBackgroundType.BridgesSurvivors => "DOOMSBGBridgesSurvivors".Translate(),
                DOOMSBackgroundType.DOOMSResearchers => "DOOMSBGDOOMSResearchers".Translate(),
                DOOMSBackgroundType.ChiralMiners => "DOOMSBGChiralMiners".Translate(),
                _ => "Unknown"
            };
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref backgroundType, "backgroundType", DOOMSBackgroundType.PorterExpedition);
            Scribe_Values.Look(ref includeChiralEquipment, "includeChiralEquipment", true);
            Scribe_Values.Look(ref includeNetworkKnowledge, "includeNetworkKnowledge", true);
        }
    }

    // ==================== ENUMS AND SUPPORTING TYPES ====================
    
    public enum DOOMSLevel
    {
        Level1 = 1,
        Level2 = 2,
        Level3 = 3,
        Level4 = 4
    }

    public enum DOOMSBackgroundType
    {
        PorterExpedition,
        BridgesSurvivors,
        DOOMSResearchers,
        ChiralMiners
    }
}