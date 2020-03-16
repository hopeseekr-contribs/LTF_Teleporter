using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Verse;               // RimWorld universal objects are here (like 'Building')
using Verse.Sound;

using UnityEngine;

namespace LTF_Teleport
{

    public class ToolsQuality
    {


        //Quality
        public static bool BestQuality(CompQuality compQuality)
        {
            if (compQuality == null)
                return false;
            return (compQuality.Quality == QualityCategory.Legendary);
        }
        public static bool WorstQuality(CompQuality compQuality)
        {
            if (compQuality == null)
                return false;
            return (compQuality.Quality == QualityCategory.Awful);
        }
        public static string BetterQuality(CompQuality comp)
        {
            return (VirtualQuality(comp, 1));
        }
        public static string WorseQuality(CompQuality comp)
        {
            return (VirtualQuality(comp, -1));
        }
        public static string VirtualQuality(CompQuality comp, int relativeChange = 0)
        {
            string answer = "no quality comp";

            if (comp == null)
                return answer;

            QualityCategory qualityCategory = QualityCategory.Normal;
            answer = comp.Quality.ToString();

            if (relativeChange > 0)
            {
                if (comp.Quality != QualityCategory.Legendary)
                    qualityCategory = comp.Quality + 1;
            }
            else
            {
                if (comp.Quality != QualityCategory.Awful)
                    qualityCategory = comp.Quality - 1;
            }
            //answer = comp.Quality.AddLevels(relativeChange).GetLabelShort();

            answer = qualityCategory.ToString();

            return (answer);
        }

        //Quality - Building
        public static bool ChangeQuality(Building building, CompQuality comp = null, bool better = true)
        {
            if (comp == null)
                comp = building?.TryGetComp<CompQuality>();

            if (comp == null)
                return false;

            QualityCategory myQuality = comp.Quality;
            QualityCategory remember = myQuality;

            if (better)
            {
                if (myQuality != QualityCategory.Legendary)
                    myQuality = myQuality + 1;
            }
            else
            {
                if (myQuality != QualityCategory.Awful)
                    myQuality = myQuality - 1;
            }

            return (remember != myQuality);
        }
        public static CompQuality SetQuality(Building bench, bool debug = false)
        {
            CompQuality comp = null;
            if (bench == null)
            {
                if (debug)
                    Log.Warning("No becnh provided to retrieve comp");
                return null;
            }

            comp = bench.TryGetComp<CompQuality>();
            if (comp == null)
                if (debug)
                    Log.Warning("no comp found");

            return comp;
        }

        public static int Valid(int QualityValue, bool debug = false)
        {
            int Value = QualityValue;
            if ((Value < 0) || (Value > 8))
            {
                Tools.Warn("Stupid Quality:" + Value + ", correcting", debug);
                Value = Mathf.Max(0, Value);
                Value = Mathf.Min(8, Value);
            }
            return Value;
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

        public static float FactorCapacity(float capacityBase, float factor, CompQuality comp = null, bool pow2 = false, bool round = false, bool opposite=false, bool debug = false)
        {
            if (comp == null)
            {
                Tools.Warn("no quality comp found, ret="+capacityBase, debug);
                return (capacityBase);
            }
            if(opposite && round){
                Tools.Warn("chance with roundup", debug);
                return (capacityBase);
            }

            float answer = capacityBase;
            float quality = (float)comp.Quality;

            if (pow2)
            {
                //=1/PUISSANCE(2;(A7+$D$15)*$D$14)
                answer = Mathf.Pow(2, (quality + capacityBase) * factor);
            }
            else
            {
                //=ARRONDI.SUP($C$15+$C$14*A4)
                answer = (capacityBase + factor * quality);
            }

            if (opposite)
            {
                if (answer == 0)
                {
                    Tools.Warn("0 div", debug);
                    return (capacityBase);
                }
                answer = 1 / answer;
            }
            if (round)
            {
                answer = Mathf.RoundToInt(answer);
            }

            Tools.Warn("factor capacity, ret=" + capacityBase, debug);
            return (answer);
        }

    }

}
