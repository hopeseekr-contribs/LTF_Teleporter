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

    public class Dependencies
    {
        
        public static bool TickCheckFacilityPower(Building facility, CompPowerTrader powerComp = null, bool debug = false)
        {
            bool Answer = true;
            //ToolsBuilding.CheckBuildingBelongsFacility(buildingFacilityComp, facility, debug);
            Tools.Warn("tick check facility",debug);

            //Valid
            Answer &= ToolsBuilding.CheckBuilding(facility);

            //power
            Answer &= ( (powerComp == null) ? (ToolsBuilding.CheckPower(facility)):(ToolsBuilding.CheckPower(powerComp)) );

            Tools.WarnRare("no powered facility", 300, debug);

            return Answer;
        }
        public static bool CheckBuildingBelongsFacility(CompAffectedByFacilities buildingFacilityComp, Building facility, bool debug = false)
        {
            if (!ToolsBuilding.CheckBuilding(facility))
            {
                Tools.Warn("null facility", debug);
                return false;
            }
            if (buildingFacilityComp == null)
            {
                Tools.Warn("null facility comp", debug);
                return false;
            }

            //Building.CompFacility legit
            if (buildingFacilityComp.LinkedFacilitiesListForReading.NullOrEmpty())
            {
                Tools.Warn("no linked facility found", debug);
                return false;
            }
            Tools.Warn("Found: " + buildingFacilityComp.LinkedFacilitiesListForReading.Count + " facilities", debug);

            Thing thing = null;
            thing = buildingFacilityComp.LinkedFacilitiesListForReading.RandomElement();
            if (thing == null)
            {
                // will happen on load
                Tools.Warn("no facility found; ok on load", debug);
                return false;
            }

            Building maybeFacility = thing as Building;
            if (maybeFacility != facility)
            {
                Tools.Warn("Found " + maybeFacility.ThingID + ", but it's not " + facility.ThingID, debug);
                return false;
            }

            return true;
        }


    }
}