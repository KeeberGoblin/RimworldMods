using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using DeathStrandingMod.Core;
using DeathStrandingMod.Events;

namespace DeathStrandingMod.Storyteller
{
    /// <summary>
    /// Main storyteller component for Sam Porter Bridges storyteller
    /// Focuses on connection, isolation, and Beach-related events
    /// </summary>
    public class StorytellerComp_SamPorterBridges : StorytellerComp
    {
        private StorytellerCompProperties_SamPorterBridges Props => 
            (StorytellerCompProperties_SamPorterBridges)props;
        
        private int lastConnectionAssessmentTick = 0;
        private int lastBeachEventTick = 0;
        private int lastIsolationCheckTick = 0;
        private float currentConnectionLevel = 0f;
        private Dictionary<Map, BeachNarrativeState> mapNarrativeStates = new Dictionary<Map, BeachNarrativeState>();

        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            Map map = target as Map;
            if (map?.IsPlayerHome != true)
                yield break;
            
            UpdateNarrativeState(map);
            
            // Connection-based events
            foreach (var incident in GenerateConnectionEvents(map))
                yield return incident;
            
            // Beach threat events
            foreach (var incident in GenerateBeachThreatEvents(map))
                yield return incident;
            
            // Isolation/cooperation events
            foreach (var incident in GenerateIsolationEvents(map))
                yield return incident;
            
            // Porter delivery events
            foreach (var incident in GenerateDeliveryEvents(map))
                yield return incident;
        }

        private void UpdateNarrativeState(Map map)
        {
            if (!mapNarrativeStates.ContainsKey(map))
            {
                mapNarrativeStates[map] = new BeachNarrativeState();
            }
            
            BeachNarrativeState state = mapNarrativeStates[map];
            int currentTick = Find.TickManager.TicksGame;
            
            // Update connection level assessment
            if (currentTick - lastConnectionAssessmentTick > 15000) // Every ~4 hours
            {
                UpdateConnectionAssessment(map, state);
                lastConnectionAssessmentTick = currentTick;
            }
            
            // Update Beach threat progression
            if (currentTick - lastBeachEventTick > 30000) // Every ~8 hours
            {
                UpdateBeachThreatProgression(map, state);
                lastBeachEventTick = currentTick;
            }
            
            // Update isolation metrics
            if (currentTick - lastIsolationCheckTick > 45000) // Every ~12 hours
            {
                UpdateIsolationMetrics(map, state);
                lastIsolationCheckTick = currentTick;
            }
        }

        private void UpdateConnectionAssessment(Map map, BeachNarrativeState state)
        {
            // Calculate current connection level
            float chiralNetworkCoverage = DeathStrandingUtility.GetColonyConnectionLevel(map);
            int activePaths = CountActiveTradePaths(map);
            int connectedSettlements = CountConnectedSettlements(map);
            
            currentConnectionLevel = (chiralNetworkCoverage + activePaths * 0.1f + connectedSettlements * 0.05f) / 3f;
            state.connectionLevel = currentConnectionLevel;
            
            // Connection milestones
            if (currentConnectionLevel > 0.8f && !state.hasAchievedHighConnection)
            {
                state.hasAchievedHighConnection = true;
                TriggerConnectionMilestone(map, "HighConnectionAchieved");
            }
            else if (currentConnectionLevel > 0.5f && !state.hasAchievedMediumConnection)
            {
                state.hasAchievedMediumConnection = true;
                TriggerConnectionMilestone(map, "MediumConnectionAchieved");
            }
        }

        private int CountActiveTradePaths(Map map)
        {
            // Count active trade routes and caravans
            int activePaths = 0;
            
            // Count caravans from/to this map
            foreach (Caravan caravan in Find.WorldObjects.Caravans)
            {
                if (caravan.Faction?.IsPlayer == true)
                {
                    activePaths++;
                }
            }
            
            // Count established trade relationships
            foreach (Settlement settlement in Find.WorldObjects.Settlements)
            {
                if (settlement.Faction?.RelationKindWith(Faction.OfPlayer) == FactionRelationKind.Ally)
                {
                    activePaths++;
                }
            }
            
            return activePaths;
        }

        private int CountConnectedSettlements(Map map)
        {
            // In a full implementation, this would count settlements connected via chiral network
            // For now, approximate with faction relationships
            return Find.FactionManager.AllFactionsVisible
                .Count(f => f.RelationKindWith(Faction.OfPlayer) >= FactionRelationKind.Neutral);
        }

        private void TriggerConnectionMilestone(Map map, string milestoneType)
        {
            Find.LetterStack.ReceiveLetter(
                $"ConnectionMilestone{milestoneType}Title".Translate(),
                $"ConnectionMilestone{milestoneType}Desc".Translate(currentConnectionLevel.ToStringPercent()),
                LetterDefOf.PositiveEvent,
                map
            );
        }

        private void UpdateBeachThreatProgression(Map map, BeachNarrativeState state)
        {
            float currentThreat = DeathStrandingUtility.CalculateBeachThreatLevel(map);
            state.beachThreatLevel = currentThreat;
            
            // Beach threat escalation
            if (currentThreat > 0.7f && !state.hasReachedCriticalThreat)
            {
                state.hasReachedCriticalThreat = true;
                TriggerBeachThreatEscalation(map, "Critical");
            }
            else if (currentThreat > 0.4f && !state.hasReachedHighThreat)
            {
                state.hasReachedHighThreat = true;
                TriggerBeachThreatEscalation(map, "High");
            }
            
            // Threat reduction rewards
            if (state.previousBeachThreat > 0.5f && currentThreat < 0.3f)
            {
                TriggerThreatReductionReward(map);
            }
            
            state.previousBeachThreat = currentThreat;
        }

        private void TriggerBeachThreatEscalation(Map map, string threatLevel)
        {
            Find.LetterStack.ReceiveLetter(
                $"BeachThreatEscalation{threatLevel}Title".Translate(),
                $"BeachThreatEscalation{threatLevel}Desc".Translate(),
                LetterDefOf.ThreatBig,
                map
            );
        }

        private void TriggerThreatReductionReward(Map map)
        {
            Find.LetterStack.ReceiveLetter(
                "BeachThreatReductionTitle".Translate(),
                "BeachThreatReductionDesc".Translate(),
                LetterDefOf.PositiveEvent,
                map
            );
            
            // Reward with bonus resources
            RewardThreatReduction(map);
        }

        private void RewardThreatReduction(Map map)
        {
            // Spawn bonus chiral crystals
            Thing bonus = ThingMaker.MakeThing(ThingDefOf_DeathStranding.ChiralCrystal);
            bonus.stackCount = Rand.Range(5, 15);
            
            IntVec3 dropLocation = DropCellFinder.RandomDropSpot(map);
            GenPlace.TryPlaceThing(bonus, dropLocation, map, ThingPlaceMode.Near);
            
            Messages.Message(
                "ThreatReductionBonusReceived".Translate(bonus.stackCount),
                new TargetInfo(dropLocation, map),
                MessageTypeDefOf.PositiveEvent
            );
        }

        private void UpdateIsolationMetrics(Map map, BeachNarrativeState state)
        {
            // Calculate isolation factors
            int daysWithoutVisitors = CalculateDaysWithoutVisitors(map);
            int daysWithoutCaravans = CalculateDaysWithoutCaravans(map);
            float socialIsolation = CalculateSocialIsolation(map);
            
            state.isolationLevel = (daysWithoutVisitors + daysWithoutCaravans + socialIsolation * 10f) / 30f;
            state.isolationLevel = Mathf.Clamp01(state.isolationLevel);
            
            // Isolation events
            if (state.isolationLevel > 0.7f && Rand.Chance(0.3f))
            {
                TriggerIsolationEvent(map, state);
            }
        }

        private int CalculateDaysWithoutVisitors(Map map)
        {
            // Approximate based on faction visit history
            return Mathf.Min(30, Rand.Range(0, 15)); // Placeholder implementation
        }

        private int CalculateDaysWithoutCaravans(Map map)
        {
            // Check when last caravan was sent out
            return Mathf.Min(30, Rand.Range(0, 10)); // Placeholder implementation
        }

        private float CalculateSocialIsolation(Map map)
        {
            // Calculate based on colonist social interactions and mood
            float totalMood = 0f;
            int colonistCount = 0;
            
            foreach (Pawn colonist in map.mapPawns.FreeColonists)
            {
                if (colonist.needs?.mood != null)
                {
                    totalMood += colonist.needs.mood.CurLevel;
                    colonistCount++;
                }
            }
            
            if (colonistCount == 0)
                return 1f;
            
            float averageMood = totalMood / colonistCount;
            return 1f - averageMood; // Lower mood = higher isolation
        }

        private void TriggerIsolationEvent(Map map, BeachNarrativeState state)
        {
            // Choose isolation event based on current state
            if (state.connectionLevel < 0.3f)
            {
                TriggerSevereIsolationEvent(map);
            }
            else
            {
                TriggerMildIsolationEvent(map);
            }
        }

        private void TriggerSevereIsolationEvent(Map map)
        {
            // Severe isolation can trigger psychological events
            var eligibleColonists = map.mapPawns.FreeColonists
                .Where(p => !p.InMentalState && p.needs?.mood?.CurLevel < 0.4f)
                .ToList();
            
            if (eligibleColonists.Any())
            {
                Pawn target = eligibleColonists.RandomElement();
                
                // Apply isolation-induced mental state
                MentalStateDef isolationBreak = DefDatabase<MentalStateDef>.GetNamedSilentFail("IsolationBreak") 
                    ?? MentalStateDefOf.Wander_Sad;
                
                target.mindState.mentalStateHandler.TryStartMentalState(isolationBreak);
                
                Messages.Message(
                    "SevereIsolationEvent".Translate(target.LabelShort),
                    target,
                    MessageTypeDefOf.NegativeEvent
                );
            }
        }

        private void TriggerMildIsolationEvent(Map map)
        {
            // Mild isolation affects mood and motivation
            ThoughtDef isolationThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("IsolationLoneliness");
            
            foreach (Pawn colonist in map.mapPawns.FreeColonists.Take(3))
            {
                if (isolationThought != null)
                {
                    colonist.needs.mood.thoughts.memories.TryGainMemory(isolationThought);
                }
            }
            
            Messages.Message(
                "MildIsolationEvent".Translate(),
                MessageTypeDefOf.CautionInput
            );
        }

        private IEnumerable<FiringIncident> GenerateConnectionEvents(Map map)
        {
            BeachNarrativeState state = mapNarrativeStates[map];
            
            // Connection building opportunities
            if (state.connectionLevel < 0.5f && Rand.Chance(0.2f))
            {
                yield return CreateConnectionOpportunityIncident(map);
            }
            
            // Network expansion events
            if (state.connectionLevel > 0.3f && Rand.Chance(0.15f))
            {
                yield return CreateNetworkExpansionIncident(map);
            }
        }

        private FiringIncident CreateConnectionOpportunityIncident(Map map)
        {
            IncidentDef connectionIncident = IncidentDefOf_DeathStranding.ConnectionOpportunity 
                ?? IncidentDefOf.TraderCaravanArrival;
            
            IncidentParms parms = new IncidentParms
            {
                target = map,
                points = StorytellerUtility.DefaultThreatPointsNow(map),
                faction = Find.FactionManager.RandomAlliedFaction()
            };
            
            return new FiringIncident(connectionIncident, this, parms);
        }

        private FiringIncident CreateNetworkExpansionIncident(Map map)
        {
            IncidentDef expansionIncident = IncidentDefOf_DeathStranding.NetworkExpansion 
                ?? IncidentDefOf.ResourcePodCrash;
            
            IncidentParms parms = new IncidentParms
            {
                target = map,
                points = 100f
            };
            
            return new FiringIncident(expansionIncident, this, parms);
        }

        private IEnumerable<FiringIncident> GenerateBeachThreatEvents(Map map)
        {
            BeachNarrativeState state = mapNarrativeStates[map];
            float threatLevel = state.beachThreatLevel;
            
            // Escalating Beach threats
            if (threatLevel > 0.6f && Rand.Chance(0.25f))
            {
                yield return CreateMajorBeachIncident(map, threatLevel);
            }
            else if (threatLevel > 0.3f && Rand.Chance(0.15f))
            {
                yield return CreateMinorBeachIncident(map, threatLevel);
            }
            
            // Timefall events (more frequent with this storyteller)
            if (Rand.Chance(0.3f))
            {
                yield return CreateTimefallIncident(map);
            }
        }

        private FiringIncident CreateMajorBeachIncident(Map map, float threatLevel)
        {
            // Choose major incident based on threat level
            IncidentDef incidentType;
            
            if (threatLevel > 0.8f && Rand.Chance(0.3f))
            {
                incidentType = IncidentDefOf_DeathStranding.Voidout ?? IncidentDefOf.RaidEnemyBeacon;
            }
            else
            {
                incidentType = IncidentDefOf_DeathStranding.BTSwarm ?? IncidentDefOf.RaidEnemy;
            }
            
            IncidentParms parms = new IncidentParms
            {
                target = map,
                points = StorytellerUtility.DefaultThreatPointsNow(map) * (1f + threatLevel),
                faction = null // Beach events don't have traditional factions
            };
            
            return new FiringIncident(incidentType, this, parms);
        }

        private FiringIncident CreateMinorBeachIncident(Map map, float threatLevel)
        {
            IncidentDef incidentType = IncidentDefOf_DeathStranding.BTManifestation 
                ?? IncidentDefOf.WildManWandersIn;
            
            IncidentParms parms = new IncidentParms
            {
                target = map,
                points = 150f * threatLevel
            };
            
            return new FiringIncident(incidentType, this, parms);
        }

        private FiringIncident CreateTimefallIncident(Map map)
        {
            // Force timefall weather
            IncidentDef timefallIncident = IncidentDefOf_DeathStranding.Timefall 
                ?? IncidentDefOf.WeatherChange;
            
            IncidentParms parms = new IncidentParms
            {
                target = map
            };
            
            return new FiringIncident(timefallIncident, this, parms);
        }

        private IEnumerable<FiringIncident> GenerateIsolationEvents(Map map)
        {
            BeachNarrativeState state = mapNarrativeStates[map];
            
            // Combat isolation with connection opportunities
            if (state.isolationLevel > 0.5f && Rand.Chance(0.2f))
            {
                yield return CreateIsolationReliefIncident(map);
            }
        }

        private FiringIncident CreateIsolationReliefIncident(Map map)
        {
            // Send friendly visitors or trade opportunities
            IncidentDef reliefIncident = IncidentDefOf.VisitorGroup;
            
            IncidentParms parms = new IncidentParms
            {
                target = map,
                faction = Find.FactionManager.RandomAlliedFaction(),
                points = 200f
            };
            
            return new FiringIncident(reliefIncident, this, parms);
        }

        private IEnumerable<FiringIncident> GenerateDeliveryEvents(Map map)
        {
            // Porter-style delivery missions and requests
            if (Rand.Chance(0.1f))
            {
                yield return CreateDeliveryMissionIncident(map);
            }
            
            // Resource delivery requests from other settlements
            if (Rand.Chance(0.08f))
            {
                yield return CreateDeliveryRequestIncident(map);
            }
        }

        private FiringIncident CreateDeliveryMissionIncident(Map map)
        {
            IncidentDef deliveryIncident = IncidentDefOf_DeathStranding.DeliveryMission 
                ?? IncidentDefOf.Quest_ItemStash;
            
            IncidentParms parms = new IncidentParms
            {
                target = map,
                points = 300f,
                questScriptDef = QuestScriptDefOf.LongRangeMineralScannerLump
            };
            
            return new FiringIncident(deliveryIncident, this, parms);
        }

        private FiringIncident CreateDeliveryRequestIncident(Map map)
        {
            IncidentDef requestIncident = IncidentDefOf.RequestUrgent;
            
            IncidentParms parms = new IncidentParms
            {
                target = map,
                faction = Find.FactionManager.RandomAlliedFaction(),
                points = 200f
            };
            
            return new FiringIncident(requestIncident, this, parms);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastConnectionAssessmentTick, "lastConnectionAssessmentTick");
            Scribe_Values.Look(ref lastBeachEventTick, "lastBeachEventTick");
            Scribe_Values.Look(ref lastIsolationCheckTick, "lastIsolationCheckTick");
            Scribe_Values.Look(ref currentConnectionLevel, "currentConnectionLevel");
            Scribe_Collections.Look(ref mapNarrativeStates, "mapNarrativeStates", LookMode.Reference, LookMode.Deep);
        }
    }

    /// <summary>
    /// Properties class for Sam Porter Bridges storyteller component
    /// </summary>
    public class StorytellerCompProperties_SamPorterBridges : StorytellerCompProperties
    {
        public float baseConnectionEventChance = 0.2f;
        public float baseBeachThreatChance = 0.15f;
        public float baseIsolationEventChance = 0.1f;
        public float timefallFrequencyMultiplier = 2.5f;
        public float connectionRewardMultiplier = 1.5f;
        
        public StorytellerCompProperties_SamPorterBridges()
        {
            compClass = typeof(StorytellerComp_SamPorterBridges);
        }
    }

    /// <summary>
    /// Tracks narrative state for Beach-related storytelling
    /// </summary>
    public class BeachNarrativeState : IExposable
    {
        public float connectionLevel = 0f;
        public float beachThreatLevel = 0f;
        public float isolationLevel = 0f;
        public float previousBeachThreat = 0f;
        
        // Milestone tracking
        public bool hasAchievedMediumConnection = false;
        public bool hasAchievedHighConnection = false;
        public bool hasReachedHighThreat = false;
        public bool hasReachedCriticalThreat = false;
        
        // Event history
        public int lastMajorEventTick = 0;
        public int consecutiveBeachEvents = 0;
        public int totalConnectionEvents = 0;
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref connectionLevel, "connectionLevel");
            Scribe_Values.Look(ref beachThreatLevel, "beachThreatLevel");
            Scribe_Values.Look(ref isolationLevel, "isolationLevel");
            Scribe_Values.Look(ref previousBeachThreat, "previousBeachThreat");
            
            Scribe_Values.Look(ref hasAchievedMediumConnection, "hasAchievedMediumConnection");
            Scribe_Values.Look(ref hasAchievedHighConnection, "hasAchievedHighConnection");
            Scribe_Values.Look(ref hasReachedHighThreat, "hasReachedHighThreat");
            Scribe_Values.Look(ref hasReachedCriticalThreat, "hasReachedCriticalThreat");
            
            Scribe_Values.Look(ref lastMajorEventTick, "lastMajorEventTick");
            Scribe_Values.Look(ref consecutiveBeachEvents, "consecutiveBeachEvents");
            Scribe_Values.Look(ref totalConnectionEvents, "totalConnectionEvents");
        }
    }
}