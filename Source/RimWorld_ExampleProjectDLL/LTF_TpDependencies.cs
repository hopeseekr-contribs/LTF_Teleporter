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
            if (building == null || building.Map == null)
                return false;
            return true;
        }

        public static bool TickCheckFacility(Building facility, CompPowerTrader comp = null, bool debug=false)
        {
            bool Answer = true;

            if (debug)
                Log.Warning("tick check facility");

            Answer &= CheckBuilding(facility);

            if (comp == null) {
                Answer &= CheckPower(facility);
            }
            else
            {
                Answer &= CheckPower(comp);
            }

            if (!Answer)
            {
                if (debug)
                    Log.Warning("no powered facility");
            }

            return Answer;
        }

        //Facility required
        public static Building GetPoweredFacility(Building building, CompAffectedByFacilities compFacility=null, CompPowerTrader compFacilityPower = null, bool debug=false)
        {
            Building Answer = null;

            if (building == null)
            {
                if (debug)
                    Log.Warning("parent err");
                return null;
            }

            if(compFacility == null)
                compFacility = building?.TryGetComp<CompAffectedByFacilities>();

            //Log.Warning("Trying to set facility");
            if (compFacility == null)
            {
                if (debug)
                    Log.Warning("facility comp err");
                return null;
            }

            if (compFacility.LinkedFacilitiesListForReading.NullOrEmpty())
            {
                if (debug)
                    Log.Warning("no facility found");
                return null;
            }

            Thing thing = null;
            thing = compFacility.LinkedFacilitiesListForReading.RandomElement();

            if (thing == null)
            {
                // will happen when loading
                if(debug)
                    Log.Warning("no facility; ok on loading");

                return null;
            }

            Building facility = thing as Building;
            if (!CheckBuilding(facility))
            {
                if(debug)
                    Log.Warning("facility is not ok");

                return null;
            }

            if (compFacilityPower != null)
            {

            }
            else
            {
                if (!CheckPower(facility))
                {
                    if (debug)
                        Log.Warning("facility has no power");
                }
            }
            

            Answer = facility;
            return Answer;
        }

    }
}