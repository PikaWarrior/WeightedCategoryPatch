using System;
using System.Collections.Generic;
using System.Linq;
using BrutalCompanyMinus;
using BrutalCompanyMinus.Minus;
using static BrutalCompanyMinus.Configuration;
using HarmonyLib;

namespace WeightedCategoryPatch
{
    [HarmonyPatch]
    internal static class EventManagerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EventManager), "ModifyLevel")]
        private static void ModifyLevel_Prefix()
        {
            if (!useCustomWeights.Value) return;

            float fix(float value) => value < 1f ? 1f : value;
            int eventTypeAmount = eventTypeScales.Length;

            float[] computedScales = new float[eventTypeAmount];
            for (int i = 0; i < eventTypeAmount; i++)
                computedScales[i] = MEvent.Scale.Compute(eventTypeScales[i]);

            float eventTypeWeightSum = 0f;
            for (int i = 0; i < eventTypeAmount; i++) eventTypeWeightSum += computedScales[i];
            eventTypeWeightSum = fix(eventTypeWeightSum);

            float[] eventTypeProbabilities = new float[eventTypeAmount];
            for (int i = 0; i < eventTypeAmount; i++)
                eventTypeProbabilities[i] = computedScales[i] / eventTypeWeightSum;

            EventManager.eventTypeRarities = eventTypeProbabilities;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EventManager), "ChooseEvents")]
        private static bool ChooseEvents_Prefix(out List<MEvent> additionalEvents, ref List<MEvent> __result)
        {
            if (!useCustomWeights.Value)
            {
                additionalEvents = new List<MEvent>();
                return true;
            }

            EventManager.currentEvents.Clear();
            EventManager.sideEvents.Clear();

            List<MEvent> chosenEvents = new List<MEvent>();
            List<MEvent> eventsToChooseForm = new List<MEvent>();
            foreach (MEvent e in EventManager.events) eventsToChooseForm.Add(e);

            System.Random rng = new System.Random(
                StartOfRound.Instance.randomMapSeed + 32345 + Environment.TickCount);

            int eventCount =
                (int)MEvent.Scale.Compute(eventsToSpawn, MEvent.EventType.Neutral)
                + RoundManager.Instance.GetRandomWeightedIndex(
                    weightsForExtraEvents.IntArray(), rng);

            if (scaleHeat.Value)
            {
                float currentHeat = EventManager.currentHeatDifficulty();
                if (currentHeat == heatMaxCap.Value && heatForceEventAtMax.Value)
                {
                    string[] eventNames = heatEventsToForce.Value
                        .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToArray();
                    EventManager.forcedEvents.AddRange(EventManager.GetEventsByName(eventNames));
                }
            }

            foreach (MEvent forcedEvent in EventManager.forcedEvents)
            {
                eventsToChooseForm.RemoveAll(x => x.Name() == forcedEvent.Name());
                foreach (string eventToRemove in forcedEvent.EventsToRemove)
                    eventsToChooseForm.RemoveAll(x => x.Name() == forcedEvent.Name());
            }

            for (int i = 0; i < eventCount; i++)
            {
                MEvent newEvent = PickEventByCategory(eventsToChooseForm, rng);

                if (!newEvent.AddEventIfOnly())
                {
                    i--;
                    eventsToChooseForm.RemoveAll(x => x.Name() == newEvent.Name());
                    continue;
                }

                bool eventValid = newEvent.MoonMode
                    ? EventManager.IsEventOnMoonWhitelist(newEvent)
                    : !EventManager.IsIgnoredEventByMoonBlacklist(newEvent);

                if (eventValid && (newEvent.isSpecialEvent || newEvent.isBetaEvent))
                {
                    bool specialFailed = newEvent.isSpecialEvent && !enableSpecialEvents.Value;
                    bool betaFailed    = newEvent.isBetaEvent    && !enableBetaEvents.Value;
                    if (specialFailed || betaFailed) eventValid = false;
                }

                if (!eventValid)
                {
                    i--;
                    eventsToChooseForm.RemoveAll(x => x.Name() == newEvent.Name());
                    continue;
                }

                chosenEvents.Add(newEvent);
                eventsToChooseForm.RemoveAll(x => x.Name() == newEvent.Name());

                int amountRemoved = 0;
                foreach (string eventToRemove in newEvent.EventsToRemove)
                {
                    eventsToChooseForm.RemoveAll(x => x.Name() == eventToRemove);
                    amountRemoved += chosenEvents.RemoveAll(x => x.Name() == eventToRemove);
                }
                foreach (string eventToSpawnWith in newEvent.EventsToSpawnWith)
                {
                    eventsToChooseForm.RemoveAll(x => x.Name() == eventToSpawnWith);
                    amountRemoved += chosenEvents.RemoveAll(x => x.Name() == eventToSpawnWith);
                }
                i -= amountRemoved;
            }

            List<MEvent> eventsToSpawnWith = new List<MEvent>();
            for (int i = 0; i < chosenEvents.Count; i++)
            {
                foreach (string eventToSpawnWith in chosenEvents[i].EventsToSpawnWith)
                {
                    int index = eventsToSpawnWith.FindIndex(x => x.Name() == eventToSpawnWith);
                    if (index == -1) eventsToSpawnWith.Add(MEvent.GetEvent(eventToSpawnWith));
                    index = EventManager.sideEvents.FindIndex(x => x.Name() == eventToSpawnWith);
                    if (index == -1) EventManager.sideEvents.Add(MEvent.GetEvent(eventToSpawnWith));
                }
            }

            foreach (MEvent e in EventManager.disabledEvents)
            {
                int index = eventsToSpawnWith.FindIndex(x => x.Name() == e.Name());
                if (index != -1) eventsToSpawnWith.RemoveAt(index);
            }

            additionalEvents           = eventsToSpawnWith;
            EventManager.currentEvents = chosenEvents;
            __result                   = chosenEvents;
            return false;
        }

        private static MEvent PickEventByCategory(List<MEvent> pool, System.Random rng)
        {
            MEvent.EventType chosenType = PickCategoryByRarities(rng);

            List<MEvent> categoryPool = new List<MEvent>();
            foreach (MEvent e in pool)
                if (e.Type == chosenType) categoryPool.Add(e);

            if (categoryPool.Count == 0)
                categoryPool.AddRange(pool);

            return EventManager.RandomWeightedEvent(categoryPool, rng);
        }

        private static MEvent.EventType PickCategoryByRarities(System.Random rng)
        {
            float[] rarities = EventManager.eventTypeRarities;
            float total = 0f;
            foreach (float r in rarities) total += r;

            if (total <= 0f) return MEvent.EventType.Neutral;

            float roll = (float)rng.NextDouble() * total;
            float cum  = 0f;

            Array enumValues = Enum.GetValues(typeof(MEvent.EventType));
            for (int i = 0; i < rarities.Length && i < enumValues.Length; i++)
            {
                cum += rarities[i];
                if (roll < cum) return (MEvent.EventType)enumValues.GetValue(i);
            }

            return MEvent.EventType.Neutral;
        }
    }
}