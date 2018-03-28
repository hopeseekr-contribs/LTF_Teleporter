using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace LTF_Teleport
{
    public class Tools
    {
        // Max capacity quality weighted Init
        public static float WeightedCapacity(float capacityBase, float capacitySpectrum, CompQuality comp = null, bool debug = false)
        {
            if (comp == null)
            {
                if (debug)
                    Log.Warning("no qualit comp found");
                return (capacityBase);
            }
            // 0..8
            return (capacityBase + (float)comp.Quality * (capacitySpectrum / 8));
        }

        public static float LimitToRange(float val, float min, float max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        public static string CapacityString(float capacity, float capacityMax)
        {
            string buffer = string.Empty;
            buffer = capacity + " / " + capacityMax;
            return (buffer);
        }
        public static bool CapacityOverusing(float capacity, float capacityMax)
        {
            return (capacity > capacityMax);
        }
        public static bool CapacityUsing(float capacity, float noActivityValue=0)
        {
            return (capacity != noActivityValue);
        }
        private static void CapacitySet(float capacity, float value) {
            capacity = value;
        }
        public static void CapacityReset(float capacity, float noActivityValue = 0)
        {
            CapacitySet(capacity, noActivityValue);
        }

        public static void Toggle(bool mySwitch)
        {
            mySwitch = !mySwitch;
        }
    }


    //void static SideEffect(Pawn pawn)
            //random tp back ?
        //            AddThought(pawn);
    
    /*
    private void AddThought(Pawn pawn)
    {
        if (CorpseGrinding())
        {
            if (HumanCorpseGrinding())
            {
                standingUser.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.ButcheredHumanlikeCorpse, null);
                foreach (Pawn current in standingUser.Map.mapPawns.SpawnedPawnsInFaction(standingUser.Faction))
                {
                    if (current != standingUser && current.needs != null && current.needs.mood != null && current.needs.mood.thoughts != null)
                    {
                        current.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.KnowButcheredHumanlikeCorpse, null);
                    }
                }
                TaleRecorder.RecordTale(TaleDefOf.ButcheredHumanlikeCorpse, new object[]
                {
                standingUser
                });
            }
            else
            {
                standingUser.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("LTF_AnimalGrind"), null);
            }
        }
    }
    */
}