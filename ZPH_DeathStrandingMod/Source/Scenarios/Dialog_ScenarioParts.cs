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
    /// Dialog for editing Death Stranding specific scenario parts
    /// </summary>
    public class Dialog_EditDOOMSScenarioPart : Window
    {
        private ScenPart_StartingPawnDOOMS doomsScenPart;
        private Vector2 scrollPosition = Vector2.zero;
        
        public override Vector2 InitialSize => new Vector2(600f, 500f);

        public Dialog_EditDOOMSScenarioPart(ScenPart_StartingPawnDOOMS scenPart)
        {
            doomsScenPart = scenPart;
            forcePause = true;
            doCloseX = true;
            doCloseButton = true;
            closeOnAccept = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            Rect scrollRect = new Rect(0f, 0f, inRect.width - 20f, 700f);
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, scrollRect.height);
            
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            listing.Begin(viewRect);
            
            // Title
            Text.Font = GameFont.Medium;
            listing.Label("DOOMSScenarioPartTitle".Translate());
            Text.Font = GameFont.Small;
            listing.Gap();
            
            // Description
            listing.Label("DOOMSScenarioPartDescription".Translate());
            listing.Gap();
            
            // DOOMS Level Selection
            DrawDOOMSLevelSelection(listing);
            listing.Gap();
            
            // Probability Settings
            DrawProbabilitySettings(listing);
            listing.Gap();
            
            // Carrier Limits
            DrawCarrierLimits(listing);
            listing.Gap();
            
            // Advanced Options
            DrawAdvancedOptions(listing);
            listing.Gap();
            
            // Preview
            DrawPreview(listing);
            
            listing.End();
            Widgets.EndScrollView();
        }

        private void DrawDOOMSLevelSelection(Listing_Standard listing)
        {
            listing.Label("DOOMSLevelSelection".Translate());
            listing.Gap(6f);
            
            // Level descriptions with selection
            for (int level = 1; level <= 4; level++)
            {
                DOOMSLevel doomsLevel = (DOOMSLevel)level;
                bool isSelected = (int)doomsScenPart.doomsLevel == level;
                
                Rect levelRect = listing.GetRect(60f);
                Rect checkboxRect = new Rect(levelRect.x, levelRect.y, 24f, 24f);
                Rect labelRect = new Rect(checkboxRect.xMax + 8f, levelRect.y, levelRect.width - 32f, levelRect.height);
                
                bool wasSelected = isSelected;
                Widgets.Checkbox(checkboxRect.x, checkboxRect.y, ref isSelected);
                
                if (isSelected != wasSelected)
                {
                    if (isSelected)
                    {
                        doomsScenPart.doomsLevel = doomsLevel;
                    }
                }
                
                // Level description
                string levelTitle = GetDOOMSLevelTitle(doomsLevel);
                string levelDesc = GetDOOMSLevelDescription(doomsLevel);
                
                Text.Font = GameFont.Small;
                GUI.color = isSelected ? Color.white : Color.gray;
                Widgets.Label(labelRect, $"{levelTitle}\n{levelDesc}");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }
        }

        private void DrawProbabilitySettings(Listing_Standard listing)
        {
            listing.Label("ProbabilitySettings".Translate());
            listing.Gap(6f);
            
            // Guarantee at least one DOOMS carrier
            listing.CheckboxLabeled("GuaranteeOneDOOMSCarrier".Translate(), ref doomsScenPart.guaranteeAtLeastOne);
            
            // DOOMS chance slider (only if not guaranteeing)
            if (!doomsScenPart.guaranteeAtLeastOne)
            {
                string chanceLabel = "DOOMSChance".Translate() + ": " + (doomsScenPart.doomsChance * 100f).ToString("F0") + "%";
                
                listing.Label(chanceLabel);
                doomsScenPart.doomsChance = listing.Slider(doomsScenPart.doomsChance, 0f, 1f);
            }
            
            // Show probability explanation
            Rect explanationRect = listing.GetRect(40f);
            GUI.color = Color.gray;
            string explanation = doomsScenPart.guaranteeAtLeastOne ? 
                "DOOMSGuaranteeExplanation".Translate() : 
                "DOOMSChanceExplanation".Translate((doomsScenPart.doomsChance * 100f).ToString("F0"));
            Widgets.Label(explanationRect, explanation);
            GUI.color = Color.white;
        }

        private void DrawCarrierLimits(Listing_Standard listing)
        {
            listing.Label("CarrierLimits".Translate());
            listing.Gap(6f);
            
            // Allow multiple DOOMS carriers
            listing.CheckboxLabeled("AllowMultipleDOOMSCarriers".Translate(), ref doomsScenPart.allowMultipleDOOMS);
            
            // Maximum carriers (only if allowing multiple)
            if (doomsScenPart.allowMultipleDOOMS)
            {
                string maxLabel = "MaximumDOOMSCarriers".Translate() + ": " + doomsScenPart.maxDOOMSCarriers;
                
                listing.Label(maxLabel);
                doomsScenPart.maxDOOMSCarriers = Mathf.RoundToInt(listing.Slider(doomsScenPart.maxDOOMSCarriers, 1f, 8f));
                
                // Show warning for high numbers
                if (doomsScenPart.maxDOOMSCarriers > 4)
                {
                    Rect warningRect = listing.GetRect(30f);
                    GUI.color = Color.yellow;
                    Widgets.Label(warningRect, "HighDOOMSCarrierWarning".Translate());
                    GUI.color = Color.white;
                }
            }
        }

        private void DrawAdvancedOptions(Listing_Standard listing)
        {
            listing.Label("AdvancedOptions".Translate());
            listing.Gap(6f);
            
            // Include starting equipment
            bool includeEquipment = true; // Default value
            listing.CheckboxLabeled("IncludeDOOMSStartingEquipment".Translate(), ref includeEquipment);
            
            // Include trait modifications
            bool includeTraitMods = true; // Default value
            listing.CheckboxLabeled("IncludeDOOMSTraitModifications".Translate(), ref includeTraitMods);
            
            // Include skill adjustments
            bool includeSkillAdj = true; // Default value
            listing.CheckboxLabeled("IncludeDOOMSSkillAdjustments".Translate(), ref includeSkillAdj);
            
            // Starting abilities level
            AbilityStartLevel abilityLevel = AbilityStartLevel.Basic; // Default
            string abilityLabel = "StartingAbilityLevel".Translate() + ": " + GetAbilityLevelDescription(abilityLevel);
            
            if (listing.ButtonText(abilityLabel))
            {
                List<FloatMenuOption> abilityOptions = new List<FloatMenuOption>();
                
                foreach (AbilityStartLevel level in Enum.GetValues(typeof(AbilityStartLevel)))
                {
                    AbilityStartLevel localLevel = level;
                    abilityOptions.Add(new FloatMenuOption(
                        GetAbilityLevelDescription(level),
                        () => abilityLevel = localLevel
                    ));
                }
                
                Find.WindowStack.Add(new FloatMenu(abilityOptions));
            }
        }

        private void DrawPreview(Listing_Standard listing)
        {
            listing.Label("ScenarioPreview".Translate());
            listing.Gap(6f);
            
            Rect previewRect = listing.GetRect(120f);
            Widgets.DrawBoxSolid(previewRect, Color.gray * 0.2f);
            
            Rect textRect = previewRect.ContractedBy(8f);
            GUI.color = Color.cyan;
            
            string previewText = GeneratePreviewText();
            Widgets.Label(textRect, previewText);
            
            GUI.color = Color.white;
        }

        private string GeneratePreviewText()
        {
            string preview = "PreviewDOOMSScenario".Translate() + "\n\n";
            
            // DOOMS level
            preview += "Level".Translate() + ": " + GetDOOMSLevelTitle(doomsScenPart.doomsLevel) + "\n";
            
            // Probability
            if (doomsScenPart.guaranteeAtLeastOne)
            {
                preview += "Guarantee".Translate() + ": " + "AtLeastOneCarrier".Translate() + "\n";
            }
            else
            {
                preview += "Chance".Translate() + ": " + (doomsScenPart.doomsChance * 100f).ToString("F0") + "%\n";
            }
            
            // Multiple carriers
            if (doomsScenPart.allowMultipleDOOMS)
            {
                preview += "MaxCarriers".Translate() + ": " + doomsScenPart.maxDOOMSCarriers + "\n";
            }
            else
            {
                preview += "SingleCarrierOnly".Translate() + "\n";
            }
            
            return preview;
        }

        private string GetDOOMSLevelTitle(DOOMSLevel level)
        {
            return level switch
            {
                DOOMSLevel.Level1 => "DOOMSLevel1Title".Translate(),
                DOOMSLevel.Level2 => "DOOMSLevel2Title".Translate(),
                DOOMSLevel.Level3 => "DOOMSLevel3Title".Translate(),
                DOOMSLevel.Level4 => "DOOMSLevel4Title".Translate(),
                _ => "Unknown"
            };
        }

        private string GetDOOMSLevelDescription(DOOMSLevel level)
        {
            return level switch
            {
                DOOMSLevel.Level1 => "DOOMSLevel1Desc".Translate(),
                DOOMSLevel.Level2 => "DOOMSLevel2Desc".Translate(),
                DOOMSLevel.Level3 => "DOOMSLevel3Desc".Translate(),
                DOOMSLevel.Level4 => "DOOMSLevel4Desc".Translate(),
                _ => "Unknown"
            };
        }

        private string GetAbilityLevelDescription(AbilityStartLevel level)
        {
            return level switch
            {
                AbilityStartLevel.Basic => "AbilityLevelBasic".Translate(),
                AbilityStartLevel.Intermediate => "AbilityLevelIntermediate".Translate(),
                AbilityStartLevel.Advanced => "AbilityLevelAdvanced".Translate(),
                AbilityStartLevel.Master => "AbilityLevelMaster".Translate(),
                _ => "Unknown"
            };
        }
    }

    /// <summary>
    /// Dialog for editing Timefall start scenario parts
    /// </summary>
    public class Dialog_EditTimefallScenarioPart : Window
    {
        private ScenPart_TimefallStart timefallScenPart;
        private Vector2 scrollPosition = Vector2.zero;
        
        public override Vector2 InitialSize => new Vector2(650f, 600f);

        public Dialog_EditTimefallScenarioPart(ScenPart_TimefallStart scenPart)
        {
            timefallScenPart = scenPart;
            forcePause = true;
            doCloseX = true;
            doCloseButton = true;
            closeOnAccept = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            Rect scrollRect = new Rect(0f, 0f, inRect.width - 20f, 800f);
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, scrollRect.height);
            
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            listing.Begin(viewRect);
            
            // Title
            Text.Font = GameFont.Medium;
            listing.Label("TimefallScenarioPartTitle".Translate());
            Text.Font = GameFont.Small;
            listing.Gap();
            
            // Description
            listing.Label("TimefallScenarioPartDescription".Translate());
            listing.Gap();
            
            // Intensity Settings
            DrawIntensitySettings(listing);
            listing.Gap();
            
            // Duration Settings
            DrawDurationSettings(listing);
            listing.Gap();
            
            // Effect Options
            DrawEffectOptions(listing);
            listing.Gap();
            
            // Environmental Settings
            DrawEnvironmentalSettings(listing);
            listing.Gap();
            
            // Safety Features
            DrawSafetyFeatures(listing);
            listing.Gap();
            
            // Preview
            DrawTimefallPreview(listing);
            
            listing.End();
            Widgets.EndScrollView();
        }

        private void DrawIntensitySettings(Listing_Standard listing)
        {
            listing.Label("TimefallIntensitySettings".Translate());
            listing.Gap(6f);
            
            // Intensity selection with visual indicators
            TimefallIntensity currentIntensity = timefallScenPart.timefallIntensity;
            
            for (int i = 0; i < 4; i++)
            {
                TimefallIntensity intensity = (TimefallIntensity)i;
                bool isSelected = currentIntensity == intensity;
                
                Rect intensityRect = listing.GetRect(50f);
                Rect checkboxRect = new Rect(intensityRect.x, intensityRect.y, 24f, 24f);
                Rect labelRect = new Rect(checkboxRect.xMax + 8f, intensityRect.y, intensityRect.width - 32f, intensityRect.height);
                Rect dangerRect = new Rect(labelRect.xMax - 60f, intensityRect.y, 60f, 24f);
                
                bool wasSelected = isSelected;
                Widgets.Checkbox(checkboxRect.x, checkboxRect.y, ref isSelected);
                
                if (isSelected != wasSelected && isSelected)
                {
                    timefallScenPart.timefallIntensity = intensity;
                }
                
                // Intensity label and description
                string intensityTitle = GetTimefallIntensityTitle(intensity);
                string intensityDesc = GetTimefallIntensityDescription(intensity);
                
                GUI.color = isSelected ? Color.white : Color.gray;
                Widgets.Label(labelRect, $"{intensityTitle}\n{intensityDesc}");
                
                // Danger indicator
                Color dangerColor = GetIntensityDangerColor(intensity);
                GUI.color = dangerColor;
                Widgets.Label(dangerRect, GetIntensityDangerText(intensity));
                
                GUI.color = Color.white;
            }
        }

        private void DrawDurationSettings(Listing_Standard listing)
        {
            listing.Label("TimefallDurationSettings".Translate());
            listing.Gap(6f);
            
            // Duration slider
            string durationLabel = "TimefallDuration".Translate() + ": " + timefallScenPart.timefallDurationDays + " " + "DaysLower".Translate();
            
            listing.Label(durationLabel);
            timefallScenPart.timefallDurationDays = Mathf.RoundToInt(listing.Slider(timefallScenPart.timefallDurationDays, 1f, 15f));
            
            // Duration impact warning
            if (timefallScenPart.timefallDurationDays > 7)
            {
                Rect warningRect = listing.GetRect(30f);
                GUI.color = Color.yellow;
                Widgets.Label(warningRect, "LongTimefallWarning".Translate());
                GUI.color = Color.white;
            }
            
            // Real-time duration estimation
            float realTimeDays = timefallScenPart.timefallDurationDays * 24f / 60f;
            listing.Label("RealTimeDuration".Translate(realTimeDays.ToString("F1")));
        }

        private void DrawEffectOptions(Listing_Standard listing)
        {
            listing.Label("TimefallEffectOptions".Translate());
            listing.Gap(6f);
            
            // Include timefall effects on colonists
            listing.CheckboxLabeled("IncludeTimefallEffectsOnColonists".Translate(), ref timefallScenPart.includeTimefallEffects);
            
            if (timefallScenPart.includeTimefallEffects)
            {
                Rect effectsRect = listing.GetRect(40f);
                GUI.color = Color.gray;
                Widgets.Label(effectsRect, "TimefallEffectsDescription".Translate());
                GUI.color = Color.white;
            }
            
            // Add timefall knowledge
            listing.CheckboxLabeled("AddTimefallKnowledge".Translate(), ref timefallScenPart.addTimefallKnowledge);
            
            if (timefallScenPart.addTimefallKnowledge)
            {
                float researchPoints = CalculateKnowledgePoints(timefallScenPart.timefallIntensity);
                
                Rect knowledgeRect = listing.GetRect(30f);
                GUI.color = Color.cyan;
                Widgets.Label(knowledgeRect, "KnowledgeGainAmount".Translate(researchPoints.ToString("F0")));
                GUI.color = Color.white;
            }
        }

        private void DrawEnvironmentalSettings(Listing_Standard listing)
        {
            listing.Label("EnvironmentalSettings".Translate());
            listing.Gap(6f);
            
            // Spawn chiral crystals
            listing.CheckboxLabeled("SpawnChiralCrystals".Translate(), ref timefallScenPart.spawnChiralCrystals);
            
            if (timefallScenPart.spawnChiralCrystals)
            {
                int crystalFormations = CalculateCrystalFormations(timefallScenPart.timefallIntensity);
                
                Rect crystalRect = listing.GetRect(30f);
                GUI.color = Color.green;
                Widgets.Label(crystalRect, "ExpectedCrystalFormations".Translate(crystalFormations));
                GUI.color = Color.white;
            }
            
            // Beach threat level
            string threatLabel = "BeachThreatLevel".Translate() + ": " + (timefallScenPart.beachThreatLevel * 100f).ToString("F0") + "%";
            
            listing.Label(threatLabel);
            timefallScenPart.beachThreatLevel = listing.Slider(timefallScenPart.beachThreatLevel, 0f, 1f);
            
            // Threat level explanation
            Rect threatRect = listing.GetRect(40f);
            GUI.color = GetBeachThreatColor(timefallScenPart.beachThreatLevel);
            Widgets.Label(threatRect, GetBeachThreatDescription(timefallScenPart.beachThreatLevel));
            GUI.color = Color.white;
        }

        private void DrawSafetyFeatures(Listing_Standard listing)
        {
            listing.Label("SafetyFeatures".Translate());
            listing.Gap(6f);
            
            // Guarantee timefall shelter
            listing.CheckboxLabeled("GuaranteeTimefallShelter".Translate(), ref timefallScenPart.guaranteeTimefallShelter);
            
            if (timefallScenPart.guaranteeTimefallShelter)
            {
                Rect shelterRect = listing.GetRect(60f);
                GUI.color = Color.green;
                Widgets.Label(shelterRect, "TimefallShelterDescription".Translate());
                GUI.color = Color.white;
            }
            else
            {
                Rect noShelterRect = listing.GetRect(40f);
                GUI.color = Color.yellow;
                Widgets.Label(noShelterRect, "NoShelterWarning".Translate());
                GUI.color = Color.white;
            }
        }

        private void DrawTimefallPreview(Listing_Standard listing)
        {
            listing.Label("TimefallScenarioPreview".Translate());
            listing.Gap(6f);
            
            Rect previewRect = listing.GetRect(160f);
            Widgets.DrawBoxSolid(previewRect, Color.black * 0.3f);
            
            Rect textRect = previewRect.ContractedBy(8f);
            GUI.color = Color.cyan;
            
            string previewText = GenerateTimefallPreviewText();
            Widgets.Label(textRect, previewText);
            
            GUI.color = Color.white;
        }

        private string GenerateTimefallPreviewText()
        {
            string preview = "TimefallScenarioPreview".Translate() + "\n\n";
            
            // Intensity and duration
            preview += "Intensity".Translate() + ": " + GetTimefallIntensityTitle(timefallScenPart.timefallIntensity) + "\n";
            preview += "Duration".Translate() + ": " + timefallScenPart.timefallDurationDays + " " + "DaysLower".Translate() + "\n";
            
            // Effects
            if (timefallScenPart.includeTimefallEffects)
            {
                preview += "ColonistEffects".Translate() + ": " + "Enabled".Translate() + "\n";
            }
            
            // Crystals
            if (timefallScenPart.spawnChiralCrystals)
            {
                int crystals = CalculateCrystalFormations(timefallScenPart.timefallIntensity);
                preview += "ChiralCrystals".Translate() + ": " + crystals + " " + "Formations".Translate() + "\n";
            }
            
            // Beach threat
            preview += "BeachThreat".Translate() + ": " + (timefallScenPart.beachThreatLevel * 100f).ToString("F0") + "%\n";
            
            // Safety
            if (timefallScenPart.guaranteeTimefallShelter)
            {
                preview += "Shelter".Translate() + ": " + "Guaranteed".Translate() + "\n";
            }
            
            return preview;
        }

        private string GetTimefallIntensityTitle(TimefallIntensity intensity)
        {
            return intensity switch
            {
                TimefallIntensity.Light => "TimefallLight".Translate(),
                TimefallIntensity.Moderate => "TimefallModerate".Translate(),
                TimefallIntensity.Heavy => "TimefallHeavy".Translate(),
                TimefallIntensity.Extreme => "TimefallExtreme".Translate(),
                _ => "Unknown"
            };
        }

        private string GetTimefallIntensityDescription(TimefallIntensity intensity)
        {
            return intensity switch
            {
                TimefallIntensity.Light => "TimefallLightDesc".Translate(),
                TimefallIntensity.Moderate => "TimefallModerateDesc".Translate(),
                TimefallIntensity.Heavy => "TimefallHeavyDesc".Translate(),
                TimefallIntensity.Extreme => "TimefallExtremeDesc".Translate(),
                _ => "Unknown"
            };
        }

        private Color GetIntensityDangerColor(TimefallIntensity intensity)
        {
            return intensity switch
            {
                TimefallIntensity.Light => Color.green,
                TimefallIntensity.Moderate => Color.yellow,
                TimefallIntensity.Heavy => new Color(1f, 0.5f, 0f), // Orange
                TimefallIntensity.Extreme => Color.red,
                _ => Color.white
            };
        }

        private string GetIntensityDangerText(TimefallIntensity intensity)
        {
            return intensity switch
            {
                TimefallIntensity.Light => "Safe".Translate(),
                TimefallIntensity.Moderate => "Caution".Translate(),
                TimefallIntensity.Heavy => "Dangerous".Translate(),
                TimefallIntensity.Extreme => "Extreme".Translate(),
                _ => ""
            };
        }

        private float CalculateKnowledgePoints(TimefallIntensity intensity)
        {
            return intensity switch
            {
                TimefallIntensity.Light => 200f,
                TimefallIntensity.Moderate => 400f,
                TimefallIntensity.Heavy => 600f,
                TimefallIntensity.Extreme => 800f,
                _ => 300f
            };
        }

        private int CalculateCrystalFormations(TimefallIntensity intensity)
        {
            return intensity switch
            {
                TimefallIntensity.Light => 2,
                TimefallIntensity.Moderate => 3,
                TimefallIntensity.Heavy => 5,
                TimefallIntensity.Extreme => 6,
                _ => 2
            };
        }

        private Color GetBeachThreatColor(float threatLevel)
        {
            if (threatLevel < 0.3f) return Color.green;
            if (threatLevel < 0.6f) return Color.yellow;
            if (threatLevel < 0.8f) return new Color(1f, 0.5f, 0f); // Orange
            return Color.red;
        }

        private string GetBeachThreatDescription(float threatLevel)
        {
            if (threatLevel < 0.2f) return "BeachThreatLow".Translate();
            if (threatLevel < 0.4f) return "BeachThreatModerate".Translate();
            if (threatLevel < 0.7f) return "BeachThreatHigh".Translate();
            return "BeachThreatExtreme".Translate();
        }
    }

    /// <summary>
    /// Enhanced scenario part selector that includes Death Stranding parts
    /// </summary>
    public class Dialog_DeathStrandingScenarioPartSelector : Window
    {
        private Scenario scenario;
        private Vector2 scrollPosition = Vector2.zero;
        private string searchText = "";
        private ScenarioPartCategory selectedCategory = ScenarioPartCategory.All;
        
        public override Vector2 InitialSize => new Vector2(800f, 600f);

        public Dialog_DeathStrandingScenarioPartSelector(Scenario scen)
        {
            scenario = scen;
            forcePause = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Header
            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 60f);
            DrawHeader(headerRect);
            
            // Search and filter
            Rect filterRect = new Rect(inRect.x, headerRect.yMax + 5f, inRect.width, 35f);
            DrawSearchAndFilter(filterRect);
            
            // Scenario parts list
            Rect listRect = new Rect(inRect.x, filterRect.yMax + 5f, inRect.width, inRect.height - filterRect.yMax - 45f);
            DrawScenarioPartsList(listRect);
            
            // Close button
            Rect closeRect = new Rect(inRect.width - 100f, inRect.height - 35f, 90f, 30f);
            if (Widgets.ButtonText(closeRect, "Close".Translate()))
            {
                Close();
            }
        }

        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(rect, "AddDeathStrandingScenarioPart".Translate());
            Text.Font = GameFont.Small;
        }

        private void DrawSearchAndFilter(Rect rect)
        {
            // Search box
            Rect searchRect = new Rect(rect.x, rect.y, rect.width * 0.6f, rect.height);
            searchText = Widgets.TextField(searchRect, searchText);
            
            // Category filter
            Rect categoryRect = new Rect(searchRect.xMax + 10f, rect.y, rect.width * 0.35f, rect.height);
            if (Widgets.ButtonText(categoryRect, GetCategoryDisplayName(selectedCategory)))
            {
                List<FloatMenuOption> categoryOptions = new List<FloatMenuOption>();
                
                foreach (ScenarioPartCategory category in Enum.GetValues(typeof(ScenarioPartCategory)))
                {
                    ScenarioPartCategory localCategory = category;
                    categoryOptions.Add(new FloatMenuOption(
                        GetCategoryDisplayName(category),
                        () => selectedCategory = localCategory
                    ));
                }
                
                Find.WindowStack.Add(new FloatMenu(categoryOptions));
            }
        }

        private void DrawScenarioPartsList(Rect rect)
        {
            List<DeathStrandingScenarioPartInfo> scenarioParts = GetFilteredScenarioParts();
            
            Rect scrollRect = rect;
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 20f, scenarioParts.Count * 80f);
            
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);
            
            float y = 0f;
            foreach (DeathStrandingScenarioPartInfo partInfo in scenarioParts)
            {
                Rect partRect = new Rect(0f, y, viewRect.width, 75f);
                DrawScenarioPartOption(partRect, partInfo);
                y += 80f;
            }
            
            Widgets.EndScrollView();
        }

        private void DrawScenarioPartOption(Rect rect, DeathStrandingScenarioPartInfo partInfo)
        {
            // Background
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }
            
            // Icon area
            Rect iconRect = new Rect(rect.x + 5f, rect.y + 5f, 50f, 50f);
            DrawScenarioPartIcon(iconRect, partInfo);
            
            // Text area
            Rect textRect = new Rect(iconRect.xMax + 10f, rect.y + 5f, rect.width - iconRect.width - 20f, rect.height - 10f);
            
            // Title
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(textRect.x, textRect.y, textRect.width, 25f), partInfo.Title);
            
            // Description
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(textRect.x, textRect.y + 25f, textRect.width, 45f), partInfo.Description);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            
            // Click to add
            if (Widgets.ButtonInvisible(rect))
            {
                AddScenarioPartToScenario(partInfo);
                Close();
            }
        }

        private void DrawScenarioPartIcon(Rect rect, DeathStrandingScenarioPartInfo partInfo)
        {
            // Draw icon based on scenario part type
            GUI.color = partInfo.IconColor;
            Widgets.DrawBoxSolid(rect, partInfo.IconColor * 0.3f);
            
            // Draw simple icon representation
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, partInfo.IconText);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            
            GUI.color = Color.white;
        }

        private List<DeathStrandingScenarioPartInfo> GetFilteredScenarioParts()
        {
            List<DeathStrandingScenarioPartInfo> allParts = GetAllDeathStrandingScenarioParts();
            
            return allParts.Where(part => 
                (selectedCategory == ScenarioPartCategory.All || part.Category == selectedCategory) &&
                (string.IsNullOrEmpty(searchText) || 
                 part.Title.ToLower().Contains(searchText.ToLower()) ||
                 part.Description.ToLower().Contains(searchText.ToLower()))
            ).ToList();
        }

        private List<DeathStrandingScenarioPartInfo> GetAllDeathStrandingScenarioParts()
        {
            return new List<DeathStrandingScenarioPartInfo>
            {
                new DeathStrandingScenarioPartInfo
                {
                    Title = "DOOMS Starting Pawns",
                    Description = "Ensures some starting pawns have DOOMS abilities with customizable levels and probability.",
                    Category = ScenarioPartCategory.StartingPawns,
                    PartType = typeof(ScenPart_StartingPawnDOOMS),
                    IconText = "👁",
                    IconColor = Color.cyan
                },
                new DeathStrandingScenarioPartInfo
                {
                    Title = "Timefall Weather Start",
                    Description = "Begin the scenario during an active timefall event with configurable intensity and duration.",
                    Category = ScenarioPartCategory.Weather,
                    PartType = typeof(ScenPart_TimefallStart),
                    IconText = "🌧",
                    IconColor = Color.blue
                },
                new DeathStrandingScenarioPartInfo
                {
                    Title = "DOOMS Background",
                    Description = "Sets background story and starting equipment based on Death Stranding organizations.",
                    Category = ScenarioPartCategory.Background,
                    PartType = typeof(ScenPart_DOOMSBackground),
                    IconText = "📦",
                    IconColor = Color.yellow
                },
                new DeathStrandingScenarioPartInfo
                {
                    Title = "Chiral Crystal Cache",
                    Description = "Start with a cache of chiral crystals for early network building.",
                    Category = ScenarioPartCategory.StartingItems,
                    PartType = typeof(ScenPart_ChiralCrystalCache),
                    IconText = "💎",
                    IconColor = Color.green
                },
                new DeathStrandingScenarioPartInfo
                {
                    Title = "Beach Threat Level",
                    Description = "Set the initial Beach dimensional threat level for the colony.",
                    Category = ScenarioPartCategory.MapConditions,
                    PartType = typeof(ScenPart_BeachThreatLevel),
                    IconText = "🌊",
                    IconColor = Color.red
                },
                new DeathStrandingScenarioPartInfo
                {
                    Title = "Porter Equipment",
                    Description = "Start with specialized porter delivery equipment and gear.",
                    Category = ScenarioPartCategory.StartingItems,
                    PartType = typeof(ScenPart_PorterEquipment),
                    IconText = "🎒",
                    IconColor = Color.gray
                },
                new DeathStrandingScenarioPartInfo
                {
                    Title = "Network Node",
                    Description = "Begin with a functioning chiral network node already established.",
                    Category = ScenarioPartCategory.StartingBuildings,
                    PartType = typeof(ScenPart_NetworkNode),
                    IconText = "📡",
                    IconColor = Color.magenta
                },
                new DeathStrandingScenarioPartInfo
                {
                    Title = "BT Proximity",
                    Description = "Start near existing BT activity for immediate supernatural threat.",
                    Category = ScenarioPartCategory.MapConditions,
                    PartType = typeof(ScenPart_BTProximity),
                    IconText = "👻",
                    IconColor = Color.black
                }
            };
        }

        private void AddScenarioPartToScenario(DeathStrandingScenarioPartInfo partInfo)
        {
            try
            {
                ScenPart newPart = (ScenPart)Activator.CreateInstance(partInfo.PartType);
                scenario.parts.Add(newPart);
                
                Messages.Message(
                    "ScenarioPartAdded".Translate(partInfo.Title),
                    MessageTypeDefOf.PositiveEvent
                );
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create scenario part {partInfo.PartType}: {ex}");
                Messages.Message(
                    "ScenarioPartAddFailed".Translate(partInfo.Title),
                    MessageTypeDefOf.RejectInput
                );
            }
        }

        private string GetCategoryDisplayName(ScenarioPartCategory category)
        {
            return category switch
            {
                ScenarioPartCategory.All => "AllCategories".Translate(),
                ScenarioPartCategory.StartingPawns => "StartingPawns".Translate(),
                ScenarioPartCategory.StartingItems => "StartingItems".Translate(),
                ScenarioPartCategory.StartingBuildings => "StartingBuildings".Translate(),
                ScenarioPartCategory.Weather => "WeatherConditions".Translate(),
                ScenarioPartCategory.MapConditions => "MapConditions".Translate(),
                ScenarioPartCategory.Background => "BackgroundStory".Translate(),
                _ => category.ToString()
            };
        }
    }

    // ==================== ADDITIONAL SCENARIO PARTS ====================
    
    /// <summary>
    /// Scenario part that provides chiral crystal starting cache
    /// </summary>
    public class ScenPart_ChiralCrystalCache : ScenPart
    {
        private int crystalAmount = 20;
        private bool includeContainer = true;
        private bool includeMiningEquipment = false;
        
        public override string Summary(Scenario scen)
        {
            string summary = "ScenPart_ChiralCrystalCache".Translate(crystalAmount);
            if (includeContainer)
                summary += " " + "WithContainer".Translate();
            return summary;
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 4f);
            
            // Crystal amount slider
            Rect amountRect = new Rect(scenPartRect.x, scenPartRect.y, scenPartRect.width, ScenPart.RowHeight);
            string amountLabel = "ChiralCrystalAmount".Translate() + ": " + crystalAmount;
            Widgets.Label(amountRect, amountLabel);
            
            Rect sliderRect = new Rect(amountRect.x, amountRect.y + 25f, amountRect.width, 20f);
            crystalAmount = Mathf.RoundToInt(Widgets.HorizontalSlider(sliderRect, crystalAmount, 5f, 100f, true));
            
            // Include storage container
            Rect containerRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight * 2, scenPartRect.width, ScenPart.RowHeight);
            Widgets.CheckboxLabeled(containerRect, "IncludeStorageContainer".Translate(), ref includeContainer);
            
            // Include mining equipment
            Rect miningRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight * 3, scenPartRect.width, ScenPart.RowHeight);
            Widgets.CheckboxLabeled(miningRect, "IncludeMiningEquipment".Translate(), ref includeMiningEquipment);
        }

        public override void GenerateIntoMap(Map map)
        {
            // Spawn chiral crystals
            Thing crystals = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
            crystals.stackCount = crystalAmount;
            
            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);
            GenPlace.TryPlaceThing(crystals, dropSpot, map, ThingPlaceMode.Direct);
            
            // Add container if specified
            if (includeContainer)
            {
                Thing container = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralContainer);
                GenPlace.TryPlaceThing(container, dropSpot, map, ThingPlaceMode.Near);
            }
            
            // Add mining equipment if specified
            if (includeMiningEquipment)
            {
                Thing miningLaser = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralMiningLaser);
                GenPlace.TryPlaceThing(miningLaser, dropSpot, map, ThingPlaceMode.Near);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref crystalAmount, "crystalAmount", 20);
            Scribe_Values.Look(ref includeContainer, "includeContainer", true);
            Scribe_Values.Look(ref includeMiningEquipment, "includeMiningEquipment", false);
        }
    }

    /// <summary>
    /// Scenario part that sets initial Beach threat level
    /// </summary>
    public class ScenPart_BeachThreatLevel : ScenPart
    {
        private float initialThreatLevel = 0.3f;
        private bool allowFluctuation = true;
        
        public override string Summary(Scenario scen)
        {
            string threatLevel = (initialThreatLevel * 100f).ToString("F0") + "%";
            return "ScenPart_BeachThreatLevel".Translate(threatLevel);
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 3f);
            
            // Threat level slider
            Rect threatRect = new Rect(scenPartRect.x, scenPartRect.y, scenPartRect.width, ScenPart.RowHeight);
            string threatLabel = "InitialBeachThreat".Translate() + ": " + (initialThreatLevel * 100f).ToString("F0") + "%";
            Widgets.Label(threatRect, threatLabel);
            
            Rect sliderRect = new Rect(threatRect.x, threatRect.y + 25f, threatRect.width, 20f);
            initialThreatLevel = Widgets.HorizontalSlider(sliderRect, initialThreatLevel, 0f, 1f, true);
            
            // Allow fluctuation
            Rect fluctuationRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight * 2, scenPartRect.width, ScenPart.RowHeight);
            Widgets.CheckboxLabeled(fluctuationRect, "AllowThreatFluctuation".Translate(), ref allowFluctuation);
        }

        public override void GenerateIntoMap(Map map)
        {
            // Set initial Beach threat level
            DeathStrandingUtility.SetBeachThreatLevel(map, initialThreatLevel);
            
            // Configure fluctuation settings
            if (!allowFluctuation)
            {
                DeathStrandingUtility.LockBeachThreatLevel(map, true);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref initialThreatLevel, "initialThreatLevel", 0.3f);
            Scribe_Values.Look(ref allowFluctuation, "allowFluctuation", true);
        }
    }

    /// <summary>
    /// Scenario part that provides porter equipment
    /// </summary>
    public class ScenPart_PorterEquipment : ScenPart
    {
        private bool includePorterSuit = true;
        private bool includeDeliveryContainer = true;
        private bool includeScanner = false;
        private int extraCarryingCapacity = 50;
        
        public override string Summary(Scenario scen)
        {
            return "ScenPart_PorterEquipment".Translate();
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 5f);
            
            // Porter suit
            Rect suitRect = new Rect(scenPartRect.x, scenPartRect.y, scenPartRect.width, ScenPart.RowHeight);
            Widgets.CheckboxLabeled(suitRect, "IncludePorterSuit".Translate(), ref includePorterSuit);
            
            // Delivery container
            Rect containerRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight, scenPartRect.width, ScenPart.RowHeight);
            Widgets.CheckboxLabeled(containerRect, "IncludeDeliveryContainer".Translate(), ref includeDeliveryContainer);
            
            // Scanner equipment
            Rect scannerRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight * 2, scenPartRect.width, ScenPart.RowHeight);
            Widgets.CheckboxLabeled(scannerRect, "IncludeDOOMSScanner".Translate(), ref includeScanner);
            
            // Extra carrying capacity
            Rect capacityRect = new Rect(scenPartRect.x, scenPartRect.y + ScenPart.RowHeight * 3, scenPartRect.width, ScenPart.RowHeight);
            string capacityLabel = "ExtraCarryingCapacity".Translate() + ": " + extraCarryingCapacity + "kg";
            Widgets.Label(capacityRect, capacityLabel);
            
            Rect capacitySliderRect = new Rect(capacityRect.x, capacityRect.y + 25f, capacityRect.width, 20f);
            extraCarryingCapacity = Mathf.RoundToInt(Widgets.HorizontalSlider(capacitySliderRect, extraCarryingCapacity, 0f, 200f, true));
        }

        public override void GenerateIntoMap(Map map)
        {
            List<Thing> equipment = new List<Thing>();
            
            if (includePorterSuit)
            {
                Thing suit = ThingMaker.MakeThing(ThingDefOf_DeathStranding.PorterSuit);
                equipment.Add(suit);
            }
            
            if (includeDeliveryContainer)
            {
                Thing container = ThingMaker.MakeThing(ThingDefOf_DeathStranding.DeliveryContainer);
                equipment.Add(container);
            }
            
            if (includeScanner)
            {
                Thing scanner = ThingMaker.MakeThing(ThingDefOf_DeathStranding.DOOMSScanner);
                equipment.Add(scanner);
            }
            
            // Spawn equipment
            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);
            foreach (Thing item in equipment)
            {
                GenPlace.TryPlaceThing(item, dropSpot, map, ThingPlaceMode.Near);
            }
            
            // Apply carrying capacity bonus to colonists
            if (extraCarryingCapacity > 0)
            {
                foreach (Pawn colonist in map.mapPawns.FreeColonists)
                {
                    Hediff carryingBonus = HediffMaker.MakeHediff(
                        DefDatabase<HediffDef>.GetNamedSilentFail("PorterTraining") ?? HediffDefOf.Go,
                        colonist
                    );
                    carryingBonus.Severity = extraCarryingCapacity / 100f;
                    colonist.health.AddHediff(carryingBonus);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref includePorterSuit, "includePorterSuit", true);
            Scribe_Values.Look(ref includeDeliveryContainer, "includeDeliveryContainer", true);
            Scribe_Values.Look(ref includeScanner, "includeScanner", false);
            Scribe_Values.Look(ref extraCarryingCapacity, "extraCarryingCapacity", 50);
        }
    }

    // ==================== SUPPORTING ENUMS AND CLASSES ====================
    
    public enum AbilityStartLevel
    {
        Basic,
        Intermediate,
        Advanced,
        Master
    }

    public enum ScenarioPartCategory
    {
        All,
        StartingPawns,
        StartingItems,
        StartingBuildings,
        Weather,
        MapConditions,
        Background
    }

    public class DeathStrandingScenarioPartInfo
    {
        public string Title;
        public string Description;
        public ScenarioPartCategory Category;
        public Type PartType;
        public string IconText;
        public Color IconColor;
    }
}