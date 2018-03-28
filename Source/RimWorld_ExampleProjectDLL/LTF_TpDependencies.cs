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
        public static bool CheckPower(Building building, CompPowerTrader comp=null)
        {
            if (comp==null)
                comp = building?.TryGetComp<CompPowerTrader>();

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

            if (comp == null) {
                comp = facility?.TryGetComp<CompPowerTrader>();
                if (debug)
                    Log.Warning("tried to find facility power");
            }

            Answer &= CheckBuilding(facility);
            Answer &= CheckPower(facility, comp);

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

            if(comp == null)
                comp = building?.TryGetComp<CompAffectedByFacilities>();

            //Log.Warning("Trying to set facility");
            if (comp == null)
            {
                if (debug)
                    Log.Warning("facility comp err");
                return null;
            }

            if (comp.LinkedFacilitiesListForReading.NullOrEmpty())
            {
                if (debug)
                    Log.Warning("no facility found");
                return null;
            }

            Thing thing = null;
            thing = comp.LinkedFacilitiesListForReading.RandomElement();

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

            if (!CheckPower(facility))
            {
                if (debug)
                    Log.Warning("facility has no power");
            }

            Answer = facility;
            return Answer;
        }

    }
}