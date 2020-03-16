using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace LTF_Teleport
{
    public class Tools
    {
        public static bool TwoTicksOneTrue(int period = 60, bool debug = false)
        {
            Tools.Warn("°°°°°°2t1true" + (int)((Time.realtimeSinceStartup * 100) % period), debug);
            return ((int)((Time.realtimeSinceStartup * 100) % period) == 1);
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
        public static string PosStr(IntVec3 position)
        {
            return (" [" + position.x + ";" + position.z + "];");
        }
        public static string LabelByDefName(string DefName, bool debug = false)
        {
            string Answer = string.Empty;
            Answer = ThingDef.Named(DefName)?.label;
            Tools.Warn("Answer: " + Answer, debug);

            return Answer;
        }
        public static string Ticks2Str(float ticks)
        {
            return (Math.Round(ticks / 60, 1) + "s");
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

        public static float LimitToRange(float val, float min, float max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }
        public static int LimitToRange(int val, int min, int max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }
        public static float LimitRadius(float value)
        {
            //"Not enough squares to get to radius 64.72919.Max is 56.40036"
            return LimitToRange(value, 0, 55);
        }
        public static int NextIndexRoundBrowser(int index, int count)
        {
            // only 1 item in registry, keep 0
            // protection
            if (count == 1)
                return 0;

            // upper limit
            if ((index + 1) >= count)
                return 0;

            return (index+1);
            //return ((index == count - 1) ? (0) : (index + 1));
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

    public static class Extensions
    {

        public static T Next<T>(this T src) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(String.Format("Argument {0} is not an Enum", typeof(T).FullName));

            T[] Arr = (T[])Enum.GetValues(src.GetType());
            int j = Array.IndexOf<T>(Arr, src) + 1;
            return (Arr.Length == j) ? Arr[0] : Arr[j];
        }

        
    }
}