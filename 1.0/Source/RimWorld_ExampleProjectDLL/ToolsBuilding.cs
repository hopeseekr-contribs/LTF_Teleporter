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

    public class ToolsBuilding
    {

        public enum Link { Orphan=0, Linked=1 };
        public static string[] LinkLabel = { "Orphan", "Linked" };

        public static bool ValidLink(Link val)
        {

            int cast = (int)val;
            int min = (int)Link.Orphan;
            int max = (int)Link.Linked;

            return ((cast >= min) && (cast <= max));

        }

        //Building dependencies
        public static bool CheckPower(Building building)
        {
            CompPowerTrader comp = null;
            comp = building?.TryGetComp<CompPowerTrader>();

            if (comp == null || !comp.PowerOn)
                return false;

            return true;
        }
        public static bool CheckPower(CompPowerTrader comp)
        {
            if (comp == null || !comp.PowerOn)
                return false;

            return true;
        }
        public static bool CheckBuilding(Building building)
        {
            if (building == null || building.Map == null || building.Position == null)
                return false;
            return true;
        }

        public static CompAffectedByFacilities GetAffectedComp(Building building, bool debug = false)
        {
            if (!CheckBuilding(building)) {
                Tools.Warn("//bad building, wont check facility", debug);
                return null;
            }

            CompAffectedByFacilities affectedComp;
            affectedComp = building.TryGetComp<CompAffectedByFacilities>();

            if (affectedComp == null)
            {
                Tools.Warn("//no affected by facility comp found", debug);
                return null;
            }

            return affectedComp;
        }
        public static Building GetFacility(CompAffectedByFacilities buildingFacilityComp, bool debug=false)
        {
            if(buildingFacilityComp == null)
            {
                Tools.Warn("//no comp", debug);
                return null;
            }

            //Building.CompFacility legit
            if (buildingFacilityComp.LinkedFacilitiesListForReading.NullOrEmpty())
            {
                Tools.Warn("//no linked facility found", debug);
                return null;
            }
            Tools.Warn("Found: " + buildingFacilityComp.LinkedFacilitiesListForReading.Count + " facilities", debug);

            Thing thing = null;
            thing = buildingFacilityComp.LinkedFacilitiesListForReading.RandomElement();
            if (thing == null)
            {
                // will happen on load
                Tools.Warn("no facility found; ok on load", debug);
                return null;
            }
            Building newFacility = thing as Building;

            return newFacility;
        }

        public static float TheoricBestRange(Comp_LTF_TpSpot comp1, Comp_LTF_TpSpot comp2)
        {
            return (Mathf.Max(comp1?.range ?? 0, comp2?.range ?? 0));
        }
    }
}