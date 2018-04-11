using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace LTF_Teleport
{
    public class Tools
    {
        public static bool TwoTicksOneTrue(int period=60, bool debug=false)
        {
            Tools.Warn("°°°°°°2t1true" + (int)((Time.realtimeSinceStartup * 100) % period), debug);
            return ( (int)((Time.realtimeSinceStartup * 100) % period) == 1);
        }

//        public static bool 

        //String
        public static string OkStr(bool boolean = false)
        {
            return "[" + ((boolean) ? ("OK") : ("KO")) + "]";
        }
        public static string IfStr(bool boolean, String myString, bool AddCr = true)
        {
            string Answer = string.Empty;

            if (AddCr)
                Answer = "\n";

            if (boolean)
                Answer += myString;

            return Answer;
        }

        // Debug
        public static void Warn(string warning, bool debug = false)
        {
            if (debug)
                Log.Warning(warning);
        }
        public static void WarnRare(string warning, int period = 300, bool debug = false)
        {
            if (debug)
            {
                bool display = ((Find.TickManager.TicksGame % period) == 0);
                if (display)
                    Log.Warning(warning);
            }
        }

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

        public static string PawnResumeString(Pawn pawn)
        {
            return (pawn?.LabelShort.CapitalizeFirst() +
                    ", " +
                    (int)pawn?.ageTracker?.AgeBiologicalYears + " y/o" +
                    " " + pawn?.gender.ToString()?.Translate()?.ToLower() +
                    ", " + pawn?.def?.label + "(" + pawn.kindDef + ")"
                    );
        }

        //debug Toggle kinda pointless
        public static string DebugStatus(bool debug)
        {
            return (debug + "->" + !debug);
        }

        //PauseOnError for debug purpose
        public static void PauseOnErrorToggle()
        {
            Prefs.PauseOnError = !Prefs.PauseOnError;
        }

        public static bool WarnBoolToggle(bool boolean, string PropertyName = "Debug")
        {
            Log.Warning(((boolean) ? ("<<<") : (">>>")) + " " + PropertyName + " " + Tools.DebugStatus(boolean));
            boolean = !boolean;
            return boolean;
        }
    }
}