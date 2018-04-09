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
            if (comp != null)
                answer = comp.Quality.AddLevels(relativeChange).GetLabelShort();

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
                comp?.SetQuality(myQuality.AddLevels(1), ArtGenerationContext.Colony);

            }
            else
            {
                comp.SetQuality(myQuality.AddLevels(-1), ArtGenerationContext.Colony);
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

    }

}
