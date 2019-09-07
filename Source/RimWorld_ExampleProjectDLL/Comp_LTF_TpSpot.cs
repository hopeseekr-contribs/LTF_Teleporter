using RimWorld;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using UnityEngine;

using Verse;
using Verse.Sound;

namespace LTF_Teleport
{
    // Todo

    // if cooldown && non power  -> fire spawn
    // if link while cooldown -> fire spawn
    // if no power, reset

    // Main
    [StaticConstructorOnStartup]
    public class Comp_LTF_TpSpot : ThingComp
    {
        // Work base
        Building building = null;
        Vector3 buildingPos;
        String buildingName = string.Empty;
        String myDefName = string.Empty;
        Map myMap = null;

        string[] AutoLabel = { "If activated from workstation, ", "Automatically, " };
        string[] WayLabel = { "No way", "Tp out", "Tp in", "Swap" };
        string[] WayArrow = { "(x)", " =>", "<= ", "<=>" };
        string[] WayActionLabel = { "do nothing", "send away", "bring back", "exchange" };

        // definition
        public bool requiresPower = true;
        public bool requiresBench = true;

        // Production 
        public List<Thing> thingList = new List<Thing>();
        public Building facility = null;
        Pawn standingUser = null;
        bool benchManaged = false;

        public enum Way { No = 0, Out = 1, In = 2, Swap = 3 };
        Texture2D[] WayGizmo = { MyGizmo.WayNoGz, MyGizmo.WayOutGz, MyGizmo.WayInGz, MyGizmo.WaySwapGz };
        SimpleColor[] WayColor = { SimpleColor.White, SimpleColor.Red, SimpleColor.Cyan, SimpleColor.Magenta };

        public Building twin = null;
        public ToolsBuilding.Link MyLink = ToolsBuilding.Link.Orphan;

        bool AutomaticTeleportation = false;
        public Way MyWay = Way.No;

        //calculated
        float twinDistance = 0f;
        private bool teleportOrder = false;
        public float orderRange = 0f;
        //bool destination = false;
        //bool source = false;
        public bool slideShowOn = false;

        // Comp 
        public CompPowerTrader compPowerTrader = null;
        public CompQuality compQuality = null;
        // TpBench required
        public CompAffectedByFacilities compAffectedByFacilities = null;
        public CompPowerTrader compPowerFacility = null;
        public Comp_TpBench compBench = null;
        // Link
        public Comp_LTF_TpSpot compTwin = null;
        //        private CompFlickable flickComp;

        //weighted caracteristics weighted
        public float range = 0f;

        public int warmUpCalculated = 0;
        public int warmUpLeft = 0;
        public int warmUpBase = 0;

        //private int startFrame = 0;
        public float benchSynergy = 1;

        float weightCapacity = 0f;//will be set
        float currentWeight = 0f;//calculated

        float cooldownBase = 0f;
        float currentCooldown = 0f;

        float missChance = 0f;
        float missRange = 0f;

        float fumbleChance = 0f;
        float fumbleRange = 0f;

        bool FactionMajority = false;
        [Flags]
        public enum BuildingStatus
        {
            na = 0,

            noPower = 1,
            noFacility = 2,
            noItem = 4,
            overweight = 8,
            cooldown = 16,
            //x2
            noPowerNoFaci = noPower | noFacility,
            noPowerNoItem = noPower | noItem,
            noPowerOverweight = noPower | overweight,
            noPowerCooldown = noPower | cooldown,

            noFacilityNoitem = noFacility | noItem,
            noFacilityoverweight = noFacility | overweight,
            noFacilityCooldown = noFacility | cooldown,

            noItemOveW = noItem | overweight,
            noItemCooldown = noItem | cooldown,

            Overweight = overweight | cooldown,
            //x3
            noPowernoFacilityNoItem = noPower | noFacility | noItem,
            noPowerNoFacilityOverweight = noPower | noFacility | overweight,
            noPowernoFacilityCooldown = noPower | noFacility | cooldown,

            noFacilityNoitemOverweight = noFacility | noItem | overweight,
            noFacilityNoitemCooldown = noFacility | noItem | cooldown,

            noItemOverWCooldown = noItem | overweight | cooldown,
            //x4
            powerOk = noFacility | noItem | overweight | cooldown,
            facilityOk = noPower | noItem | overweight | cooldown,
            itemOk = noPower | noFacility | overweight | cooldown,
            weightOk = noPower | noFacility | noItem | cooldown,
            cooldownOk = noPower | noFacility | noItem | overweight,

            //x5
            allWrong = overweight | cooldown | noPower | noFacility | noItem,

            capable = 64,
        }

        public Gfx.AnimStep TeleportItemAnimStatus = Gfx.AnimStep.na;
        //public Gfx.AnimStep TpInStatus = Gfx.AnimStep.na;
        int BeginSequenceFrameLength = 120;
        int beginSequenceI = 0;
        int FrameSlowerMax = 3;
        int FrameSlower = 0;
        public bool drawUnderlay = true;
        public bool drawOverlay = true;
        private float myOpacity = 1f;

        // Debug 
        public bool gfxDebug = false;
        public bool prcDebug = false;
        private bool Hax = false;

        // Props
        public CompProperties_LTF_TpSpot Props
        {
            get
            {
                return (CompProperties_LTF_TpSpot)props;
            }
        }

        // Interface quality
        private void ChangeQuality(bool better = true)
        {
            ToolsQuality.ChangeQuality(building, compQuality, better);
            SetWeightedProperties();
        }
        public bool HasQuality
        {
            get
            {
                return (compQuality != null);
            }
        }
        public void SetQuality(bool debug=false)
        {
            if(building == null)
            {
                Tools.Warn("no building ?", debug);
                return;
            }

            compQuality = building?.TryGetComp<CompQuality>();
        }
        private void BetterQuality()
        {
            ChangeQuality(true);
        }
        private void WorseQuality()
        {
            ChangeQuality(false);
        }
        private string QualityLog()
        {
            string Answer = string.Empty;

            //Answer = "Warmup: " + Tools.Ticks2Str(warmUpBase);
            Answer = "Warmup: " + Tools.Ticks2Str(warmUpCalculated);
            Answer += " - Cooldown: " + Tools.Ticks2Str(cooldownBase);
            Answer += "\nRange: " + (int)range;
            Answer += " - Weight max: " + weightCapacity + "kg";

            /*
            Answer += "\nMiss chance: " + missChance.ToStringPercent("F0");
            Answer += " - offset: " + missRange;
            Answer += "\nFumble chance: " + fumbleChance.ToStringPercent("F0");
            Answer += " - offset: " + fumbleRange;
            */

            //Answer += "\nWorkstation synergy: " + benchSynergy;

            return Answer;
        }

        public void AutoToggle()
        {
            AutomaticTeleportation = !AutomaticTeleportation;

            if (IsLinked)
                compTwin.AutomaticTeleportation = AutomaticTeleportation;
        }

        public Way NextWay
        {
            get
            {
                Way Answer = Way.No;

                if (IsOrphan)
                    return Answer;

                Answer = ((MyWay == Way.Swap) ? (Way.No) : (MyWay.Next()));

                if ((int)Answer > (int)Way.Swap)
                    Answer = Way.No;

                return Answer;
            }
        }
        public void BrowseWay()
        {
            MyWay = NextWay;
            switch (MyWay)
            {
                case Way.Out:
                    compTwin.MyWay=Way.In;
                    break;
                case Way.In:
                    compTwin.MyWay = Way.Out;
                    break;
                case Way.Swap:
                    compTwin.MyWay = Way.Swap;
                    break;
                case Way.No:
                    compTwin.MyWay = Way.No;
                    break;
            }
        }
        bool InvalidWay
        {
            get
            {
                return (!ValidWay);
            }
        }
        public bool ValidWay
        {
            get
            {
                int cast = (int)MyWay;
                int min = (int)Way.No;
                int max = (int)Way.Swap;

                return ((cast >= min) && (cast <= max));
            }
        }

        public Texture2D IssueGizmoing
        {
            get
            {
                Texture2D Answer = null;

                if (requiresPower && !HasPower)
                    Answer = MyGizmo.IssuePowerGz;
                else if (requiresBench && StatusNoFacility)
                    Answer = MyGizmo.IssueNoFacilityGz;
                else if (requiresBench && !HasPoweredFacility)
                    Answer = MyGizmo.IssueNoPoweredFacilityGz;
                else if (StatusOverweight)
                    Answer = MyGizmo.IssueOverweightGz;
                else if (IsChilling)
                    Answer = MyGizmo.IssueCooldownGz;
                else if (IsOrphan)
                    Answer = MyGizmo.IssueOrphanGz;

                return Answer;
            }
        }
        public Texture2D WayGizmoing
        {
            get
            {
                if ((int)MyWay > (WayGizmo.Length - 1))
                    return null;

                return (WayGizmo[(int)MyWay]);
            }
        }

        public string WayNaming
        {
            get
            {
                if ((int)MyWay > (WayLabel.Length - 1))
                    return ("way outbound");

                return (WayLabel[(int)MyWay]);
            }
        }
        public string NextWayNaming
        {
            get
            {
                if ((int)NextWay > WayLabel.Length - 1)
                    return ("next way outbound");

                return (WayLabel[(int)NextWay]);
            }
        }
        public string WayActionLabeling
        {
            get
            {
                if ((int)MyWay > WayActionLabel.Length - 1)
                    return ("way action outbound");

                return (WayActionLabel[(int)MyWay]);
            }
        }
        public string AutoLabeling
        {
            get
            {

                int boolToInt = ((AutomaticTeleportation) ? (1) : (0));

                if ((int)boolToInt > AutoLabel.Length - 1)
                    return ("Auto outbound");

                return (AutoLabel[boolToInt]);
            }
        }
        public string WayArrowLabeling
        {
            get
            {
                if ((int)MyWay > WayArrow.Length - 1)
                    return ("Arrow outbound");

                return (WayArrow[(int)MyWay]);
            }
        }
        public SimpleColor WayColoring
        {
            get
            {
                if ((int)MyWay > WayColor.Length - 1)
                    return (SimpleColor.White);
                    //return ("Color outbound");

                return (WayColor[(int)MyWay]);
            }
        }

        public ToolsBuilding.Link NextLink
        {
            get
            {
                ToolsBuilding.Link Answer = (MyLink == ToolsBuilding.Link.Linked) ? (ToolsBuilding.Link.Orphan) : (ToolsBuilding.Link.Linked);
                return Answer;
            }
        }
        public string LinkNaming
        {
            get
            {
                if (!ToolsBuilding.ValidLink(NextLink))
                    return ("link labeling outbound");

                return (ToolsBuilding.LinkLabel[(int)MyLink]);
            }
        }
        public string NextLinkNaming
        {
            get
            {
                if (!ToolsBuilding.ValidLink(NextLink))
                    return ("next link labeling outbound");

                return (ToolsBuilding.LinkLabel[(int)NextLink]);
            }
        }

        public string WayDescription
        {
            get
            {
                string Answer = string.Empty;

                Answer += 
                    AutoLabeling +
                    "will " + WayActionLabeling + ' ' +
                    "what stands on this " + myDefName;

                if (IsLinked)
                {
                    Answer += " and its twin.";
                }

                return Answer;
            }
        }

        //Dependency :Cooldown
        private bool IsChilling
        {
            get
            {
                return Tools.CapacityUsing(currentCooldown);
            }
        }
        void SetWeightedProperties(bool debug=false)
        {
            if (compQuality == null)
            {
                Tools.Warn("no quality provided", debug);
                return;
            }
            SetCooldownBase();
            SetWarmUpBase();

            SetWeight();
            SetMiss();
            SetFumble();
            SetRange();

            Tools.Warn(DumpProps + " / " + DumpSettings, debug);
        }

        private float StuffMultiplier(Thing thing)
        {
            if (thing == null)
                return 1;

            ThingDef myStuff = thing.Stuff;

            StatDef armor_sharp = StatDefOf.StuffPower_Armor_Sharp;
            StatDef armor_blunt = StatDefOf.StuffPower_Armor_Blunt;
            StatDef damage_blunt = StatDefOf.BluntDamageMultiplier;
            StatDef damage_sharp = StatDefOf.SharpDamageMultiplier;

            StatDef stuffMass = StatDefOf.Mass;

            float armSharp, armBlunt, dmgSharp, dmgBlunt, mass;

            armSharp = myStuff.GetStatValueAbstract(armor_sharp);
            armBlunt = myStuff.GetStatValueAbstract(armor_blunt);
            dmgSharp = myStuff.GetStatValueAbstract(damage_sharp);
            dmgBlunt = myStuff.GetStatValueAbstract(damage_blunt);

            mass = myStuff.GetStatValueAbstract(stuffMass);

            float answer = (0.5f + mass) * (armSharp + armBlunt + dmgSharp + dmgBlunt);

            /*
            Tools.Warn("stuff:" + thing.Stuff.defName +
                " armBlunt:" + armBlunt +
                " armSharp:" + armSharp +
                " dmgSharp:" + dmgSharp +
                " dmgBlunt:" + dmgBlunt +
                " answer:" + answer,
                true);
                */
            return answer;
        }

        private void SetRange(CompQuality comp = null)
        {
            if (comp == null) if((comp = compQuality)==null) return;
            // own quality factor
            range = ToolsQuality.FactorCapacity(Props.rangeBase, Props.rangeQualityFactor, comp, false, true, false, prcDebug);
            // facility quality factor
            //if (requiresBench)

            //Log.Warning(building.ThingID + "range base:" + range);

            range += 3 * TwinWorstFacilityRange;
            //Log.Warning(building.ThingID + "twin worst facility range:" + TwinWorstFacilityRange);
            range *= benchSynergy;
            //Log.Warning("bench synergy:" + benchSynergy);

            float mtp = StuffMultiplier(parent);
            range *= mtp;
            //Log.Warning(building.ThingID + " mtp:" + mtp);
            //Log.Warning(building.ThingID +" "+building.Label +" range final:" + range);

        }
        private void SetWarmUpBase(CompQuality comp = null)
        {
            if (comp == null) if ((comp = compQuality) == null) return;
            warmUpBase = (int)ToolsQuality.FactorCapacity(Props.warmUpBase, Props.warmUpQualityFactor, comp, false, false, false, prcDebug);
            /*
            CalculateWarmUp();
            SetWarmUpLeft();
            */
        }

        private void SetBeginSequenceFrameLength(CompQuality comp = null)
        {
            //int frameMin = (2 * 23 * FrameSlowerMax);
            int frameMin = (int)(1.5f * 23 * FrameSlowerMax);
            if (comp == null) if ((comp = compQuality) == null) return;
            //BeginSequenceFrameLength = (int)ToolsQuality.FactorCapacity(warmUpCalculated, Props.warmUpQualityFactor, comp, false, false, false, prcDebug);
            BeginSequenceFrameLength = warmUpCalculated - frameMin;
            BeginSequenceFrameLength = (BeginSequenceFrameLength < frameMin) ? (frameMin) : (BeginSequenceFrameLength);
        }

        private void SetCooldownBase(CompQuality comp = null)
        {
            if (comp == null) if ((comp = compQuality) == null) return;
            // 20 - 4 
            cooldownBase = ToolsQuality.FactorCapacity(Props.cooldownBase, Props.cooldownQualityFactor, comp, false, false, false, prcDebug);

            if (requiresBench && (benchSynergy != 0)) 
                cooldownBase /= benchSynergy;

            // correct current cooldown if too high for capacity
            currentCooldown = Mathf.Min(cooldownBase, currentCooldown);

            //MTP
        }
        private void SetBenchSynergy(CompQuality comp = null)
        {
            if (comp == null) if ((comp = compQuality) == null) return;
            benchSynergy = ToolsQuality.FactorCapacity(Props.benchSynergyBase, Props.benchSynergyQualityFactor, comp, false, false, false, prcDebug);
        }
        private void SetWeight(CompQuality comp = null)
        {
            if (comp == null) if ((comp = compQuality) == null) return;
            // 35 - 195
            weightCapacity = ToolsQuality.FactorCapacity(Props.weightBase, Props.weightQualityFactor, comp, false, false, false, prcDebug);
        }
        private void SetMiss(CompQuality comp = null)
        {
            if (comp == null) if ((comp = compQuality) == null) return;
            // 35 - 195
            missChance = ToolsQuality.FactorCapacity(Props.missChanceBase, Props.missChanceQualityFactor, comp, true, false, true, prcDebug);
            missRange = ToolsQuality.FactorCapacity(Props.missRangeBase, Props.missRangeQualityFactor, comp, false, true, false, prcDebug);
        }
        private void SetFumble(CompQuality comp = null)
        {
            if (comp == null) if ((comp = compQuality) == null) return;
            // 35 - 195
            fumbleChance = ToolsQuality.FactorCapacity(Props.fumbleChangeBase, Props.fumbleChanceQualityFactor, comp, true, false, true, prcDebug);
            fumbleRange = ToolsQuality.FactorCapacity(Props.fumbleRangeBase, Props.fumbleRangeQualityFactor, comp, false, true, false, prcDebug);
        }

        public void ResetOrder()
        {
            teleportOrder = false;

            TeleportItemAnimStatus = Gfx.AnimStep.na;
            if (IsLinked)
            {
                compTwin.TeleportItemAnimStatus = Gfx.AnimStep.na;
                compTwin.teleportOrder = false;
            }

            /*
            destination = false;
            source = false;
            */
        }

        private void ResetSettings()
        {
            MyWay = Way.No;
            AutomaticTeleportation = false;

            // TRying to remove spot from workstation it's registered
            if (requiresBench && HasRegisteredFacility && ToolsBuilding.CheckBuilding(building))
                compBench.RemoveSpot(building);

            UnlinkMe();

            StopCooldown();
            ResetWeight();

            if (requiresBench)
                ResetFacility();

            ResetItems();
            ResetPawn();
            ResetOrder();
        }

        public bool HasWarmUp
        {
            get
            {
                return(warmUpLeft > 0);
            }
        }
        public bool WarmUpJustStarted
        {
            get
            {
                return ((teleportOrder) && (warmUpCalculated == warmUpLeft));
            }
        }
        public float CooldownProgress
        {
            get
            {
                if (cooldownBase == 0)
                    return 0;

                return (currentCooldown / cooldownBase);
            }
        }
        public float WarmUpProgress
        {
            get
            {
                if(warmUpBase == 0) {
                    Tools.Warn("Unset warmUpBase", prcDebug);
                    return 0;
                }
                if (warmUpCalculated == 0)
                {
                    Tools.Warn("Unset warmUpCalculated", prcDebug);
                    return 0;
                }
                if (warmUpLeft == 0)
                {
                    // spammy
                    //Tools.Warn("Unset warmUpLeft", prcDebug);
                    return 1;
                }

                return ((float)warmUpLeft / (float)warmUpCalculated);
            }
        }

        private void UpdateDistance(bool debug=false)
        {
            if(!ToolsBuilding.CheckBuilding(building) || compTwin == null)
            {
                Tools.Warn("cant distance", debug);
            }
            twinDistance = building.Position.DistanceTo(twin.Position);
            compTwin.twinDistance = twinDistance;
            Tools.Warn("updated distance:"+twinDistance, debug);
        }
        private void WorkstationOrder(bool debug = false)
        {
            if (!StatusNoIssue)
            {
                Tools.Warn("cant accept an order: " + StatusLogNoUpdate, debug);
                return;
            }

            orderRange = building.Position.DistanceTo(twin.Position);
            int FacilityQuality = TwinWorstFacilityQuality;

            //BeginSequenceFrameLength = StartupTickCount = 240 / FacilityQuality + 10 * (int)twinDistance;
            FacilityQuality = (FacilityQuality == 0) ? 1 : FacilityQuality;
            /*
            BeginSequenceFrameLength = 240 / FacilityQuality + 10 * (int)twinDistance;
            warmUp = warmUpBase = BeginSequenceFrameLength + 2*23*FrameSlowerMax;
            */

            //SetWarmUpBase(); SetWarmUp();
            //CalculateWarmUp();
            SetWarmUpLeft();
            SetBeginSequenceFrameLength();
            //BeginSequenceFrameLength = 2 * 23 * FrameSlowerMax;

            //initseq
            teleportOrder = true;
        }

        // Gizmo command no debug pls
        public void OrderOut()
        {
            if (HasNothing)
                return;
            MyWay = Way.Out;
            compTwin.MyWay = Way.In;

            WorkstationOrder(prcDebug);
            BeginTeleportItemAnimSeq();
        }
        
        public void OrderIn()
        {
            compTwin.OrderOut();
        }
        
        public void OrderSwap()
        {
            MyWay = Way.Swap;
            compTwin.MyWay = Way.Swap;

            WorkstationOrder(prcDebug);
            BeginTeleportItemAnimSeq();
        }

        public IntVec3 SomeJitter(IntVec3 destination, bool debug=false) {
            IntVec3 answer = destination;
            IntVec3 offset = new IntVec3(0, 0, 0);

            if (Rand.Chance(missChance)) {
                offset = new IntVec3((int)Rand.Range(-missRange, missRange), 0, (int)Rand.Range(-missRange, missRange));
            }
            else if (Rand.Chance(fumbleChance)) {
                offset = new IntVec3((int)Rand.Range(-fumbleRange, fumbleRange), 0, (int)Rand.Range(-fumbleRange, fumbleRange));
            }

            IntVec3 newTile = (offset + destination);
            if (newTile.InBounds(myMap) && newTile.Standable(myMap))
                answer = destination + offset;

            return answer;
        }

        private void TeleportItem(Thing thing, IntVec3 destination, Map destinationMap, bool debug = false)
        {
            if (thing == null)
            {
                Tools.Warn("!!! thing == null", debug);
                return;
            }
            if (destination == null)
            {
                Tools.Warn("!!! destination == null", debug);
                return;
            }
            if(destinationMap == null)
            {
                Tools.Warn("!!! destinationMap == null", debug);
                return;
            }


            if (thing.Position == destination)
                Tools.Warn("!!! Trying to tp something where it already is", debug);

            if (thing is Pawn pawn)
            {
                //Tools.Warn("Pawn moving :" + pawn.Name.ToStringShort, debug);
                Tools.Warn("Pawn moving :" + pawn.ThingID, debug);

                if (pawn.RaceProps.Animal)
                {
                    pawn.DeSpawn();
                    GenSpawn.Spawn(pawn, destination, destinationMap);
                    Tools.Warn("Pawn moved :" + pawn.ThingID, debug);
                }
                else
                {
                    if (pawn.IsColonist)
                    {
                        bool PastDraft = pawn.Drafted;
                        pawn.drafter.Drafted = false;

                        pawn.DeSpawn();
                        GenSpawn.Spawn(pawn, destination, destinationMap);
                        pawn.drafter.Drafted = PastDraft;
                    }
                    else
                    {
                        pawn.DeSpawn();
                        GenSpawn.Spawn(pawn, destination, destinationMap);
                    }

                    Tools.Warn("Pawn moved :" + pawn.ThingID + " draft:" + pawn.Drafted, debug);
                }

            }
            else
            {
                //thing.Position = destination;
                //read only :( thing.Map = destinationMap;
                thing.DeSpawn();
                GenSpawn.Spawn(thing, destination, destinationMap);
                Tools.Warn("thing moved :" + thing.LabelShort, debug);
            }
        }

        private void TeleportItem(Thing thing, IntVec3 destination, bool debug=false)
        {
            if (thing == null)
            {
                Tools.Warn("!!! thing == null", debug);
                return;
            }
            if(destination == null)
            {
                Tools.Warn("!!! destination == null", debug);
                return;
            }
            

            if (thing.Position == destination)
                Tools.Warn("!!! Trying to tp something where it already is", debug);

            if (thing is Pawn pawn)
            {
                // pawn.Name.ToStringShort
                Tools.Warn("Pawn moving :" + pawn.ThingID, debug);

                if ( pawn.RaceProps.Animal )
                {
                    pawn.DeSpawn();
                    GenSpawn.Spawn(pawn, destination, myMap);
                    Tools.Warn("Pawn moved :" + pawn.ThingID, debug);
                }
                else
                {
                    if (pawn.IsColonist)
                    {
                        bool PastDraft = pawn.Drafted;
                        pawn.drafter.Drafted = false;

                        pawn.DeSpawn();
                        GenSpawn.Spawn(pawn, destination, myMap);
                        pawn.drafter.Drafted = PastDraft;
                    }
                    else
                    {
                        pawn.DeSpawn();
                        GenSpawn.Spawn(pawn, destination, myMap);
                    }

                    Tools.Warn("Pawn moved :" + pawn.ThingID + " draft:" + pawn.Drafted, debug);
                }
                
            }
            else
            {
                thing.Position = destination;
                Tools.Warn("thing moved :" + thing.LabelShort, debug);
            }
        }

        private bool TryTeleport(bool debug = false)
        {
            bool gotSomeJitter = false;

            if (!StatusReady)
            {
                Tools.Warn("unready - wont tp", debug);
                return false;
            }
            if (!IsLinked)
            {
                Tools.Warn("orphan - wont tp", debug);
                return false;
            }
            if (!teleportOrder)
            {
                Tools.Warn("no tp order - wont tp", debug);
                return false;
            }

            if (MyWay == Way.Out)
                if (!HasItems)
                {
                    Tools.Warn("WayOut no item - wont tp", debug);
                    return false;
                }

            Tools.Warn("TP MyWay => " + MyWay, debug);

            //IntVec3 destination = ((MyWay == Way.Out) ? (twin.Position) : (building.Position));
            IntVec3 twinPos = twin.Position;
            IntVec3 myPos = buildingPos.ToIntVec3();

            IntVec3 twinJittered = twinPos;
            IntVec3 myJittered = myPos;

            Tools.Warn("myPos : " + myPos + " ; twinPos : " + twinPos, debug);

            //List<Thing> thingToTeleport = ((MyWay == Way.Out) ? (thingList) : (compTwin.thingList));
            List<Thing> thingToTeleport = thingList;

            // swap items
            //if (teleportOrder && compTwin.teleportOrder)
            if ( MyWay == Way.Swap && compTwin.MyWay == Way.Swap )
            {
                foreach (Thing cur in thingList)
                {
                    Tools.Warn("looping " + cur.Label, debug);
                    // No jitter for now
                    /*
                    twinJittered = SomeJitter(twinJittered);
                    TeleportItem(cur, twinJittered, debug);
                    */

                    //TeleportItem(cur, twinPos, debug);
                    TeleportItem(cur, twinPos, twin.Map ,debug);

                    /*
                    if (twinJittered != twinPos)
                        gotSomeJitter = true;
                    */
                }
                foreach (Thing cur in compTwin.thingList)
                {
                    Tools.Warn("looping " + cur.Label, debug);
                    //myJittered = SomeJitter(myJittered);
                    // No jitter for now
                    //TeleportItem(cur, myJittered, debug);

                    //TeleportItem(cur, myPos, debug);
                    TeleportItem(cur, myPos, myMap, debug);

                    /*
                    if (myJittered != myPos)
                        gotSomeJitter = true;
                    */
                }
            }
            
            else if (MyWay == Way.Out)
            {
                foreach (Thing cur in thingToTeleport)
                {
                    Tools.Warn("looping " + cur.Label, debug);
                    //twinJittered = SomeJitter(twinJittered);
                    // No jitter for now
                    //TeleportItem(cur, twinJittered, debug);

                    //TeleportItem(cur, twinPos, debug);
                    TeleportItem(cur, twinPos, twin.Map, debug);

                    //if (twinJittered != twinPos) gotSomeJitter = true;
                }
            }


            if (gotSomeJitter)
                Messages.Message(buildingName + " got some jitter and missed its destination", this.parent, MessageTypeDefOf.TaskCompletion);

            return true;
        }

        private void StopCooldown()
        {
            currentCooldown = 0;
        }
        private void StartCooldown()
        {
            currentCooldown = cooldownBase;
        }
        private void HaxCooldown(float value)
        {
            currentCooldown = value;
        }
        private void SetCooldown()
        {
            currentCooldown = cooldownBase * (.5f + .5f * (.3f * currentWeight / weightCapacity + .7f * orderRange / TwinBestRange));

            /*
            float mtp = StuffMultiplier(parent) / 2;
            if (mtp < 1) mtp = 1;
            currentCooldown /= mtp;
            */
        }
        /*
        private void SetWarmUp()
        {
            warmUpCalculated = (int) ( warmUpBase * (.5f + .5f*(.7f * currentWeight / weightCapacity + .3f * orderRange / TwinBestRange)));
            warmUpLeft = warmUpCalculated;
        }
        */
        private void CalculateWarmUp()
        {
            //warmUpCalculated = (int)(((float)warmUpBase + (float)compTwin.warmUpBase)/2f * (.5f + .5f * (.7f * currentWeight / weightCapacity + .3f * orderRange / TwinBestRange)));
            //SetWarmUpLeft();
            if (IsLinked)
            {
                warmUpCalculated = (int)(((float)warmUpBase + (float)compTwin.warmUpBase) / 2f);
                compTwin.warmUpCalculated = warmUpCalculated;
            }
            else
                warmUpCalculated = warmUpBase;
            /*
            float mtp = StuffMultiplier(parent) / 3;
            warmUpCalculated =  warmUpCalculated - (int)mtp;

            if (warmUpCalculated < 2)
                warmUpCalculated = 2;
            */
        }

        private void SetWarmUpLeft()
        {
            warmUpLeft = warmUpCalculated;
        }

        //Dependency : facility
        public bool HasRegisteredFacility
        {
            get
            {
                return ( (facility != null) && (compBench!=null));
            }
        }
        public bool HasPoweredFacility
        {
            get
            {
                //return (Dependencies.TickCheckFacilityPower(facility, compPowerFacility, prcDebug));
                return (Dependencies.TickCheckFacilityPower(facility, compPowerFacility, false));
            }
        }
        public bool AreYouMyRegisteredFacility(Building daddy)
        {
            return (daddy == facility);
        }
        public int TwinWorstFacilityQuality
        {
            get{
                int answer = 0;

                //catcher
                if (!requiresBench)
                {
                    // spot facility
                    if (IsLinked && compTwin.HasQuality)
                        answer = (int)compTwin.compBench.compQuality.Quality;
                    else
                        answer = 0;
                }
                //spot
                else
                {
                    if (IsLinked)
                    {
                            int myFacilityQ = (int)(HasRegisteredFacility && compBench.HasQuality ? compBench.compQuality.Quality : 0);
                            int twinFacilityQ = (int)( compTwin.HasRegisteredFacility && compBench.HasQuality ? compTwin.compBench.compQuality.Quality : 0);
                            answer = Mathf.Min(myFacilityQ, twinFacilityQ);
                    }
                    else
                    {
                        answer = (int)(HasRegisteredFacility && compBench.HasQuality ? compBench.compQuality.Quality : 0);
                    }
                }
                //"Not enough squares to get to radius 64.72919.Max is 56.40036"
                answer = Tools.LimitToRange(answer, 0, 8);

                return answer;
            }
        }

        public float TwinBestRange
        {
            get
            {
                if (IsOrphan)
                    return range;

                return (Mathf.Max(range, compTwin.range));
            }
        }

        public float TwinWorstFacilityRange
        {
            get{
                float answer = 0;

                //catcher
                if (!requiresBench)
                {
                    // spot facility
                    if (IsLinked && compTwin.requiresBench && compTwin.HasRegisteredFacility)
                    {
                        answer = compTwin.compBench.moreRange;
                        //Log.Warning(building.ThingID + " catcher found linked spot facility more range =" + answer);
                    }
                    else
                    {
                        answer = 0;
                        //Log.Warning(building.ThingID + " catcher did not twin or facility");
                    }
                }
                //spot
                else
                {
                    if (IsLinked)
                    {
                        if (HasRegisteredFacility)
                        {
                            float myFacilityR = compBench.moreRange;
                            if ((!compTwin.requiresBench) || (!compTwin.HasRegisteredFacility))
                            {
                                answer = myFacilityR;
                            }
                            else
                            {
                                float twinFacilityR = (compTwin.requiresBench && compTwin.HasRegisteredFacility) ? compTwin.compBench.moreRange : 0;
                                answer = Mathf.Max(myFacilityR, twinFacilityR);
                            }
                            //Log.Warning(building.ThingID + " spot found linked spot facility more range =" + answer);
                        }
                        else
                        {
                            answer = 0;
                            //Log.Warning(building.ThingID + " spot did not find linked spot facility more range =" + answer);
                        }
                            
                    }
                    else
                    {
                        if (HasRegisteredFacility)
                        {
                            answer = compBench.moreRange;
                            //Log.Warning(building.ThingID + " spot found facility more range =" + answer);
                        }
                        else
                        {
                            answer = 0;
                            //Log.Warning(building.ThingID + " spot did not find facility more range =" + answer);
                        }

                    }
                }
                //"Not enough squares to get to radius 64.72919.Max is 56.40036"
                //answer = Tools.LimitToRange(answer, 0, 54);

                return answer;
            }
        }

        public bool CreateLink(Building newLinked, Comp_LTF_TpSpot newComp, bool debug = false)
        {
            if (newComp == null)
            {
                Tools.Warn("bad comp", debug);
                return false;
            }
            if (building == newLinked)
            {
                Tools.Warn("Wont register myself", debug);
                return false;
            }

            Tools.Warn(
                "Inc "+
                "Master - order:" + Tools.OkStr(teleportOrder) + "; link:"+ Tools.OkStr(IsLinked) +
                "Slave - order:" + Tools.OkStr(newComp.teleportOrder) + "; link:" + Tools.OkStr(newComp.IsLinked),
                debug);

            UnlinkMe();

            // This one
            twin = newLinked;
            compTwin = newComp;
            compTwin.ResetOrder();
            MyLink = ToolsBuilding.Link.Linked;

            // Remote
            newComp.twin = building;
            newComp.compTwin = this;
            newComp.MyLink = ToolsBuilding.Link.Linked;
            newComp.compTwin.ResetOrder();

            UpdateDistance();
            CalculateWarmUp();

            Tools.Warn(
                "Inc " +
                "Master - order:" + Tools.OkStr(teleportOrder) + "; link:" + Tools.OkStr(IsLinked) +
                "Slave - order:" + Tools.OkStr(newComp.teleportOrder) + "; link:" + Tools.OkStr(newComp.IsLinked),
                debug);

            //SoundDef.Named("LTF_TpSpotOut").PlayOneShotOnCamera(parent.Map);

            return true;
        }

        public string MyCoordinates {
            get {
                if (!ToolsBuilding.CheckBuilding(building))
                    return string.Empty;

                return (Tools.PosStr(building.Position));
            }
        }

        private static bool UnpoweredTp(Thing tpThing)
        {
            return ((tpThing.def.defName == "LTF_TpBed")
                || (tpThing.def.defName == "LTF_TpBox")
                || (tpThing.def.defName == "LTF_TpCatcher"));
        }

        private static bool IsTpSpot(Thing thing)
        {
            return (thing.def.defName == "LTF_TpSpot");
        }

        public static bool AtLeastOneTpSpot(Thing linkable1, Thing linkable2)
        {
            if (IsTpSpot(linkable1)
                && UnpoweredTp(linkable2))
                return true;

            if (IsTpSpot(linkable2)
                && UnpoweredTp(linkable1))
                return true;

            if (IsTpSpot(linkable1) && IsTpSpot(linkable2))
                return true;

            return false;
        }

        public static string ValidTpSpot(Thing thing)
        {
            string Answer = string.Empty;
            if ( !UnpoweredTp(thing) && !IsTpSpot(thing) )
                Answer = thing.Label + " is not a valid tp spot, it's a " + thing.def.label;

            return Answer;
        }

        public void UnlinkMe()
        {
            Unlink(this);
        }
        public void Unlink(Comp_LTF_TpSpot caller)
        {
            if (IsLinked && this==caller)
                compTwin.Unlink(this);

            compTwin = null;
            twin = null;
            MyLink = ToolsBuilding.Link.Orphan;
            twinDistance = 0;

            ResetWay();
        }
        public bool IsLinked
        {
            get
            {
                return (twin != null && compTwin != null);
            }
        }
        public bool IsOrphan
        {
            get
            {
                return (twin == null || compTwin == null);
            }
        }
        private void ResetWay()
        {
            MyWay = Way.No;
        }
        public void ResetFacility()
        {
            facility = null;
            compAffectedByFacilities = null;
            compPowerFacility = null;
            compBench = null;

            FacilityDependantCapacities();

            benchManaged = false;
        }
        public void SetFacility(bool debug=false)
        {
            if(building == null)
            {
                Tools.Warn("no building provided", debug);
                return;
            }
            compAffectedByFacilities = ToolsBuilding.GetAffectedComp(building, debug);

            if (compAffectedByFacilities == null)
            {
                Tools.Warn("no comp found", debug);
                return;
            }
            facility = ToolsBuilding.GetFacility(compAffectedByFacilities, debug);

            if (facility == null)
            {
                Tools.Warn("no facility found", debug);
                return;
            }
            compBench = facility?.TryGetComp<Comp_TpBench>();
            if (compBench==null)
            {
                Tools.Warn("no facility comp found", debug);
                return;
            }

            compPowerFacility = facility?.TryGetComp<CompPowerTrader>();
            if (compPowerFacility == null)
            {
                Tools.Warn("no facility power comp found", debug);
                return;
            }

            FacilityDependantCapacities();
            benchManaged = true;
        }

        public void FacilityDependantCapacities(bool debug=false)
        {
            if (!requiresBench)
            {
                Tools.Warn("asking for facility dep while def says no facility", debug);
                return;
            }

            SetBenchSynergy(compQuality);

            SetRange(compQuality);
            SetCooldownBase(compQuality);
        }

        // Check local tile
        // Items set
        private void ResetItems()
        {
            thingList.Clear();
            ResetWeight();
        }
        private bool RemoveItemsIfAbsent()
        {

            if (HasNothing)
                return false;

            int neededAverageFaction = (int)(thingList.Count / 2);

            //Tools.Warn(building.Label + " checks history");
            //for (int i = 0; i < thingList.Count; i++)
            for (int i = thingList.Count - 1; i >= 0; i--)
            {
                Thing thing = null;
                thing = thingList[i];
                if (thing == null)
                {
                    Tools.Warn("lol what", prcDebug);
                    continue;
                }

                if (thing.Faction == Faction.OfPlayer)
                    neededAverageFaction -= 1;

                if ((thing is Pawn pawn) && (standingUser != null))
                {
                    //Tools.Warn(building.Label + " concerned about pawns");
                    if ((pawn != standingUser) || (pawn.Position != building.Position))
                    {
                        //Tools.Warn(" reseting bc he left  or someone" + standingUser.LabelShort);
                        ResetPawn();
                    }
                }

                if ((thing.Position != building.Position) ||
                    (thing == this.parent)
                    )
                {
                    RemoveItem(thing);
                    thingList.Remove(thing);
                }
            }

            FactionMajority = (neededAverageFaction <= 0);

            return (HasItems);
        }
        private void AddItem(Thing thing)
        {
            Tools.Warn("Adding " + thing.Label + " to " + building.Label, prcDebug);

            thingList.Add(thing);
        }
        private void RemoveItem(Thing thing)
        {
            Tools.Warn("Removing " + thing.Label + " from " + building.Label, prcDebug);

            thingList.Remove(thing);
        }
        private bool CheckNewItems()
        {
            return (AddSpotItems(building.Position.GetThingList(myMap)));
        }
        private bool CheckItems()
        {
            bool foundItem = false;

            foundItem |= RemoveItemsIfAbsent();
            foundItem |= CheckNewItems();

            return (foundItem);
        }
        private bool AddSpotItems(List<Thing> allThings, bool clearIfEmpty = true, bool debug = false)
        {
            Tools.Warn(building.Label + " checking items", debug);

            Thing thing = null;
            bool found = false;
            int pawnN = 0;

            Pawn passenger = null;
            Tools.Warn(building.Label + ":" + allThings.Count, debug);
            string tellMe = "Scanning: ";

            for (int i = 0; i < allThings.Count; i++)
            {
                thing = allThings[i];
                if (thing != null)
                {
                    //Projectile projectile = thing as Projectile;
                    // Can have 2 buildings, there if myself != null, myself=parent => idgaf
                    if ((thing.def.mote != null) || thing.def.IsFilth || thing.def.IsBuildingArtificial)
                    {
                        tellMe += "mote / filth / building skip";
                        Tools.Warn(tellMe, debug);
                        continue;
                    }
                    if (thing is Building myself)
                    {
                        tellMe += "Wont self register as an item";
                        Tools.Warn(tellMe, debug);
                        continue;
                    }
                    if (thing is Pawn pawn)
                    {
                        passenger = pawn;
                        pawnN += 1;
                    }

                    if (!thingList.Contains(thing))
                    {
                        AddItem(thing);
                        tellMe += thing.Label + " added;";
                    }
                    found = true;
                }
            }
            Tools.Warn(tellMe, debug);

            if (pawnN == 0)
            {
                ResetPawn();
            }
            else if (pawnN > 1)
            {
                ResetPawn();
                ResetItems();
                //thingList.Clear();
                //Tools.Warn("More than 1 pawn. Cant.");
            }
            else
            {
                SetPawn(passenger);

                Tools.Warn(passenger.LabelShort + " added", debug);
            }

            if (!found)
            {
                ResetItems();
            }

            return found;
        }

        // Items Status
        public int RegisteredCount
        {
            get
            {
                return thingList.Count;
            }
        }
        public bool HasItems
        {
            get
            {
                return !thingList.NullOrEmpty();
            }
        }
        public bool HasNothing
        {
            get
            {
                return (!HasItems);
            }
        }

        // Dependency : Weight 
        private void ResetWeight()
        {
            Tools.CapacityReset(currentWeight);
        }
        private void MoreWeight(Thing thing)
        {
            ChangeWeight(thing);
        }
        private void ChangeWeight(Thing thing, bool addWeight = true)
        {
            float newWeight = thing.GetStatValue(StatDefOf.Mass, true);
            float plusOrMinus = ((addWeight) ? (1) : (-1));

            currentWeight += plusOrMinus * newWeight;

            currentWeight = Tools.LimitToRange(currentWeight, 0, 3000);
            currentWeight = (float)Math.Round((Decimal)currentWeight, 2, MidpointRounding.AwayFromZero);

            Tools.Warn(thing.LabelShort + " adds(" + plusOrMinus + ")" + newWeight + " -> " + currentWeight, prcDebug);
        }


        // Special Item : pawn
        private void SetPawn(Pawn pawn = null)
        {
            standingUser = pawn;
        }
        private void ResetPawn()
        {
            SetPawn();
        }

        // Pawns status
        public bool HasRegisteredPawn
        {
            get
            {
                return (standingUser != null);
            }
        }
        public bool HasAnimal
        {
            get
            {
                return (HasRegisteredPawn && (!standingUser.RaceProps.Humanlike) && (!standingUser.RaceProps.IsMechanoid));
            }
        }
        public bool HasMechanoid
        {
            get
            {
                return (HasRegisteredPawn && (standingUser.RaceProps.IsMechanoid));
            }
        }
        public bool HasHumanoid
        {
            get
            {
                return (HasRegisteredPawn && (!HasAnimal));
            }
        }

        //public bool TeleportItemAnimEnd
        public bool TpSequenceEnd
        {
            get
            {
                return (TeleportItemAnimStatus == Gfx.AnimStep.end);
            }
        }
        //public bool TeleportItemAnimBegin
        public bool TpSequenceBegin
        {
            get
            {
                return (TeleportItemAnimStatus == Gfx.AnimStep.begin);
            }
        }
        public bool TeleportItemAnimActive
        {
            get
            {
                return (TeleportItemAnimStatus == Gfx.AnimStep.active);
            }
        }
        public bool TeleportItemAnimNa
        {
            get
            {
                return (TeleportItemAnimStatus == Gfx.AnimStep.na);
            }
        }
        private void SetBeginAnimLength(bool TpIn=false)
        {
            beginSequenceI = BeginSequenceFrameLength;
            //beginSequenceI                ((TpIn) ? (5) : (warmUpBase));
        }
        public void BeginTeleportItemAnimSeq()
        {
            TeleportItemAnimStatus = Gfx.AnimStep.begin;
            SetBeginAnimLength();
            if(IsLinked && MyWay == Way.Swap && compTwin.HasItems)
            {
                compTwin.TeleportItemAnimStatus = Gfx.AnimStep.begin;
                compTwin.SetBeginAnimLength();
            }
        }
        /*
        public void BeginTpInAnimSeq()
        {
            TeleportItemAnimStatus = Gfx.AnimStep.begin;
            SetBeginAnimLength(true);
        }
        */
        public bool IncBeginAnim(bool debug = false)
        {
            beginSequenceI--;
            if (beginSequenceI <= 0)
            {
                Tools.Warn("%%%%%%%%%%%%%%%Anim End", debug);
                return true;
            }
            return false;
        }
        public void AnimStatus(bool debug = false)
        {
            Tools.Warn("AnimStatus - " + TeleportItemAnimStatus + ": " + beginSequenceI + "/" + BeginSequenceFrameLength, debug);
        }
        public float AnimOpacity
        {
            get
            {
                return myOpacity;
            }
        }
        public void SetFrameSlower()
        {
            FrameSlower = FrameSlowerMax;
        }
        public int IncFrameSlower()
        {
            FrameSlower--;
            FrameSlower = Mathf.Max(0, FrameSlower);

            return FrameSlower;
        }

        public bool SetSlideShowOn(bool debug = false)
        {
            if(compTwin == null)
            {
                Tools.Warn("//bad twin comp", true);
                return false;
            }

            /*
            if(source)
                slideShowOn = true;

            if (destination)
                compTwin.slideShowOn = true;
                */
            slideShowOn = true;

            return true;
        }

        public bool NextAnim(bool debug = false)
        {
            bool Answer = false;
            if (compTwin == null)
            {
                Tools.Warn(buildingName + " //NextAnim bad twin comp", true);
                return false;
            }

            switch (TeleportItemAnimStatus)
            {
                case Gfx.AnimStep.begin:
                    TeleportItemAnimStatus = Gfx.AnimStep.active;
                    break;
                case Gfx.AnimStep.active:
                    TeleportItemAnimStatus = Gfx.AnimStep.end;
                    break;
                case Gfx.AnimStep.end:
                    TeleportItemAnimStatus = Gfx.AnimStep.na;
                    Answer = true;
                    break;
            }

            SetFrameSlower();

            return Answer;
        }

        /*
        public bool NextAnim(bool debug=false)
        {
            bool Answer = false;
            if (compTwin == null)
            {
                Tools.Warn(buildingName + " //NextAnim bad twin comp", true);
                return false;
            }
                
            if (source)
                switch (TeleportItemAnimStatus)
                {
                    case Gfx.AnimStep.begin:
                        TeleportItemAnimStatus = Gfx.AnimStep.active;
                        //BeginSequenceFrameLength
                        break;
                    case Gfx.AnimStep.active:
                        TeleportItemAnimStatus = Gfx.AnimStep.end;
                        //compTwin.BeginTpInAnimSeq();
                        break;
                    case Gfx.AnimStep.end:
                        TeleportItemAnimStatus = Gfx.AnimStep.na;
                        Answer = true;
                        break;
                }

            if (destination)
                switch (compTwin.TeleportItemAnimStatus)
                {
                    case Gfx.AnimStep.begin:
                        compTwin.TeleportItemAnimStatus = Gfx.AnimStep.active;
                        break;
                    case Gfx.AnimStep.active:
                        compTwin.TeleportItemAnimStatus = Gfx.AnimStep.end;
                        break;
                    case Gfx.AnimStep.end:
                        compTwin.TeleportItemAnimStatus = Gfx.AnimStep.na;
                        Answer = true;
                        break;
                }

            SetFrameSlower();

            return Answer;
        }
        */

        string DumpProps
        {
            get
            {
                return (
                    building?.Label +
                    Tools.PosStr(building.Position) +
                    "; synergy:" + benchSynergy +
                    " - range:" + range +
                    "; cooldown:" + Tools.CapacityString(currentCooldown, cooldownBase) +
                    "; wuBase:" + warmUpBase +
                    "; wuCal:" + warmUpCalculated +
                    "; wuLeft:" + warmUpLeft +
                    "; wuPrg:" + Tools.CapacityString(warmUpLeft, warmUpCalculated) +
                    "; weight:" + Tools.CapacityString(currentWeight, weightCapacity) +
                    "; miss:" + missChance + ";range:" + missRange +
                    "; fumble:" + fumbleChance + ";range:" + fumbleRange
                );
            }
        }

        string DumpSettings
        {
            get
            {
                return (
                    "; way: "+MyWay+"; Link:"+IsLinked+"; Auto:" + AutomaticTeleportation
                );
            }
        }

        private string DumpList()
        {
            string bla = String.Empty;
            foreach (Thing item in thingList)
            {
                bla += item.Label + ";";
            }
            return bla;
        }

        // Status management
        public BuildingStatus TeleportCapable
        {
            get
            {
                BuildingStatus Answer = BuildingStatus.na;

                //if (!ToolsBuilding.CheckPower(building))
                if (requiresPower && (!ToolsBuilding.CheckPower(building)))
                    Answer ^= BuildingStatus.noPower;

                //if (!HasRegisteredFacility)
                if (requiresBench && (!HasRegisteredFacility))
                    Answer ^= BuildingStatus.noFacility;

                if (Tools.CapacityOverusing(currentWeight, weightCapacity))
                    Answer ^= BuildingStatus.overweight;

                if (Tools.CapacityUsing(currentCooldown))
                    Answer ^= BuildingStatus.cooldown;

                switch (MyWay)
                {
                    case Way.Out:
                        if (HasNothing)
                            Answer ^= BuildingStatus.noItem;
                        break;
                    case Way.In:
                        break;
                    case Way.Swap:
                        if (HasNothing)
                            Answer ^= BuildingStatus.noItem;
                        break;
                    default:
                        if (HasNothing)
                            Answer ^= BuildingStatus.noItem;
                        break;
                }
                        

                if (Answer == BuildingStatus.na)
                    Answer = BuildingStatus.capable;

                return Answer;
            }
        }
        private bool HasStatus(BuildingStatus buildingStatus) { return ((TeleportCapable & buildingStatus) != 0); }
        public bool StatusNoPower { get { return HasStatus(BuildingStatus.noPower); } }
        public bool HasPower { get { return (!StatusNoPower); } }
        public bool StatusNoFacility { get { return HasStatus(BuildingStatus.noFacility); } }
        private bool StatusNoItem { get { return HasStatus(BuildingStatus.noItem); } }
        private bool StatusHasItem { get { return !StatusNoItem; } }
        public bool StatusOverweight { get { return HasStatus(BuildingStatus.overweight); } }
        public bool IsLightBoned { get { return !StatusOverweight; } }
        public bool StatusChillin {
            get {
                return ( 
                    // my cooldown status
                    HasStatus(BuildingStatus.cooldown) || 
                    // my link cooldown status
                    ( IsLinked && compTwin.HasStatus(BuildingStatus.cooldown) ) 
                    );
            }
        }
        public bool IsHot { get { return !StatusChillin; } }

        public bool StatusReady { get { return HasStatus(BuildingStatus.capable); } }
        public bool StatusNoIssue {
            get {
                bool Answer = true;

                if (requiresPower)
                    Answer &= HasPower;

                if (requiresBench)
                    Answer &= HasPoweredFacility;

                Answer &= IsLinked;
                Answer &= IsHot;
                Answer &= IsLightBoned;

                return Answer;
            }
        }
        public string StatusLog(bool updateDisplay = true)
        {
            string bla = string.Empty;

            if (StatusNoIssue)
            {
                int itemCount = RegisteredCount;
                string grammar = ((RegisteredCount > 1) ? ("s") : (""));

                bla += Tools.OkStr(teleportOrder) +
                    ((teleportOrder) ? ("Roger;") : ("Say what?"));

                bla = (HasNothing)?
                    ("Nothing"):
                    (RegisteredCount + " item" + grammar + ' ' + Tools.CapacityString(currentWeight, weightCapacity) + " kg")+
                    ';';

                
                if (teleportOrder && HasWarmUp)
                    bla += "Warming up: " + WarmUpProgress.ToStringPercent("F0") + " (" + Tools.Ticks2Str(warmUpLeft) + " left)";

                return bla;
            }


            if (requiresPower && StatusNoPower)
            {
                bla += " No power;";
            }

            if (requiresBench){
                if( StatusNoFacility)
                    bla += " No facility;";
                else if (!HasPoweredFacility)
                    bla += " No powered facility;";
            }

            if (StatusOverweight)
                bla += ' ' + currentWeight + "kg. >" + weightCapacity + " kg;";

            if (StatusChillin)
                if (IsLinked && CooldownProgress == 0) 
                    bla += " Linked spot cooldown " + compTwin.CooldownProgress.ToStringPercent("F0");
                else
                    bla += " Cooldown " + ((updateDisplay) ? (CooldownProgress.ToStringPercent("F0")) : ("undergoing")) + ";";

            if (IsOrphan)
                bla += " Orphan.";
  
            return bla;
        }
        public string StatusLogUpdated
        {
            get
            {
                return StatusLog();
            }
        }

        public string StatusLogNoUpdate
        {
            get
            {
                return StatusLog(false);
            }
        }

        // Overrides
        public override void PostDraw()
        {
            base.PostDraw();

            //if ((buildingPos == null) || (building.Rotation != Rot4.North))
            if (buildingPos == null)
            {
                Tools.Warn(buildingName + ": null pos draw", gfxDebug);
                return;
            }

            // nothing there standing
            if ( 
                    (requiresPower && StatusNoPower) || 
                    IsOrphan
                )
            {
                Tools.Warn(buildingName + " Nothing to draw: " + TeleportCapable, gfxDebug);
                return;
            }

            /*
            Tools.Warn(
                " pulse: " + Gfx.PulseFactorOne(parent) * 360 +
                "; Loop: " + Gfx.LoopFactorOne(parent) * 360 +
                "; %real:" + (Gfx.RealLinear(parent, 15 + currentWeight * 5, gfxDebug) * 360)
                , gfxDebug);
                */

            Material overlay = null;
            Material underlay = null;
            Material underlay2 = null;
            Material warning = null;

            // >>>> Calculate mat
            // calculate underlay
            if (requiresPower && drawUnderlay)
            {
                //if ((HasItems) || ((HasNothing) && (HasPoweredFacility)) || (StatusReady))
                underlay = MyGfx.Status2UnderlayMaterial(this, gfxDebug);
                underlay2 = MyGfx.UnderlayM;
                Tools.Warn("Underlay calculating - 1: " + (underlay != null) + "; 2: " + (underlay2 != null), gfxDebug);
            }

            // calculate Overlay
            if (drawOverlay)
            {
                //if( (!TeleportItemAnimNa) && (TeleportItemAnimBegin) )
                //if (TpSequenceBegin) && teleportOrder && beginSequenceI>0)
                if (TpSequenceBegin)
                    overlay = MyGfx.Status2OverlayMaterial(this, FactionMajority, gfxDebug);

                //if (requiresPower && !StatusReady && HasItems)
                if (requiresPower)
                {
                    //if (!StatusReady)
                    if(!StatusNoIssue)
                        warning = MyGfx.Status2WarningMaterial(this, gfxDebug);
                    else if (!compTwin.StatusNoIssue)
                        warning = MyGfx.Status2WarningMaterial(compTwin, gfxDebug);
                }

                Tools.Warn("Overlay calculating - warning: " + (warning != null) + "; anim: " + (overlay != null), gfxDebug);
            }

            // >>>> Draw mat
            // draw Underlay
            if (underlay != null)
            {
                float underlayOpacity = 1f;

                float underlayAngle = Gfx.PulseFactorOne(parent);

                if (StatusReady)
                    underlayOpacity = .8f + .2f * (Rand.Range(0, 1f));
                else if ((HasNothing && HasPoweredFacility))
                    underlayOpacity = .6f + .3f * (Rand.Range(0, 1f));
                if (HasItems)
                    underlayOpacity = .5f + .2f * (Rand.Range(0, 1f));

                //Gfx.DrawTickRotating(parent, underlay, 0, 0, 1, underlayAngle * 360, underlayOpacity, Gfx.Layer.under, gfxDebug);
                Gfx.DrawTickRotating(parent, underlay, 0, 0, 1, underlayAngle * 360, underlayOpacity, Gfx.Layer.under, false);
            }

            if (underlay2 != null)
            {
                float underlay2Opacity = Gfx.VanillaPulse(parent);
                //float underlay2Angle = Gfx.RealLinear(parent, 15 + currentWeight * 5, gfxDebug);
                float underlay2Angle = Gfx.RealLinear(parent, 15 + currentWeight * 5, false);

                //Gfx.DrawTickRotating(parent, underlay2, 0, 0, 1, underlay2Angle * 360, underlay2Opacity, Gfx.Layer.under, gfxDebug);
                Gfx.DrawTickRotating(parent, underlay2, 0, 0, 1, underlay2Angle * 360, underlay2Opacity, Gfx.Layer.under, false);
            }
            Tools.Warn("Underlay drew - 1: " + (underlay != null) + "; 2: " + (underlay2 != null), gfxDebug);

            //draw Overlay
            if (!drawOverlay)
            {
                Tools.Warn("no overlay asked", gfxDebug);
                return;
            }

            if (requiresPower && (warning != null))
                Gfx.PulseWarning(building, warning);

            if (TpSequenceBegin && (overlay != null))
            //if (TpSequenceBegin)
            //if (teleportOrder && TpSequenceBegin && (overlay != null))
            {
                float swirlSize = Gfx.VanillaPulse(parent) * 1.5f;
                //Gfx.DrawTickRotating(parent, overlay, 0, 0, swirlSize, Gfx.LoopFactorOne(parent) * 360, 1, Gfx.Layer.over, gfxDebug);
                Gfx.DrawTickRotating(parent, overlay, 0, 0, swirlSize, Gfx.LoopFactorOne(parent) * 360, 1, Gfx.Layer.over, false);

            }

            if (slideShowOn)
                if (TeleportItemAnimActive)
                {
                    MyGfx.ActiveAnim.Draw(parent.DrawPos, Rot4.North, this.parent, 0.027f * Rand.Range(-1, 1) * 360);
                }
                else if (TpSequenceEnd)
                {
                    Vector3 vec = parent.DrawPos;
                    vec.z += .3f;
                    MyGfx.EndAnim.Draw(vec, Rot4.North, this.parent, 0f);
                }
            Tools.Warn("Overlay drew - warning: " + (warning != null) + "; anim: " + (overlay != null), gfxDebug);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            //Building
            building = (Building)parent;
            myDefName = building?.def?.label;
            buildingPos = building.DrawPos;
            buildingName = building?.LabelShort;

            requiresPower = Props.PowerRequired;
            requiresBench = Props.BenchRequired;
            myMap = building?.Map;

            SetQuality();

            if (requiresPower)
                compPowerTrader = building?.TryGetComp<CompPowerTrader>();

            if (requiresBench)
                SetFacility();

            SetWeightedProperties();

            if (InvalidWay)
            {
                Tools.Warn("reset bc bad way", prcDebug);
                ResetSettings();
            }
            if (!ToolsBuilding.ValidLink(MyLink))
            {
                Tools.Warn("reset bc bad link", prcDebug);
                ResetSettings();
            }

            if (twin != null)
            {
                compTwin=twin.TryGetComp<Comp_LTF_TpSpot>();
            }
        }
        public override void CompTick()
        {
            base.CompTick();

            //Tools.Warn(">>>TICK begin<<< " + buildingName, prcDebug);
            Tools.Warn(">>>TICK begin<<< " + building.ThingID, prcDebug);

            Tools.Warn("Validated order:" +
            //" warmUp: " + Tools.CapacityString(warmUp, warmUpBase) +
            " warmUp: " + WarmUpProgress.ToStringPercent("F0") +
            " begin seq: " + Tools.CapacityString(beginSequenceI, BeginSequenceFrameLength),
            prcDebug && teleportOrder);

            if (!ToolsBuilding.CheckBuilding(building))
            {
                Tools.Warn("comp building not legit" + " - Exiting", prcDebug);
                return;
            }

            /*
            if (IsOrphan)
            {
                Tools.Warn("no need to comptick if not linked", prcDebug);
                return;
            }
            */

            string tellMe = string.Empty;
            tellMe = Tools.OkStr(StatusReady) + "[" + TeleportCapable + "]" + buildingName + ": ";

            //Tools.Warn("power checking: " + requiresPower, prcDebug);
            if (requiresPower)
            {
                // Power - Will return if status
                tellMe += "Power: " + Tools.OkStr(HasPower) + "; ";
                if (StatusNoPower)
                {
                    if (requiresBench && HasRegisteredFacility)
                    {
                        ResetSettings();
                    }
                    Tools.Warn(tellMe + "No power - Exiting", prcDebug);
                    return;
                }
            }

            //Tools.Warn("bench checking: " + requiresBench, prcDebug);
            if (requiresBench)
            {
                if (!benchManaged)
                {
                    SetFacility(prcDebug);
                    if (benchManaged)
                        compBench.AddSpot(building);
                }

                // Facility - Will return if status
                tellMe += "Facility: " + Tools.OkStr(HasRegisteredFacility) + "; ";
                if (StatusNoFacility)
                {
                    tellMe += "Found: " + Tools.OkStr(HasRegisteredFacility) + ((HasRegisteredFacility) ? (facility.LabelShort) : ("nothing")) + "; ";
                    Tools.Warn(tellMe + "no facility - Exiting", prcDebug);
                    ResetFacility();
                    return;
                }

                //Okk
                tellMe += "FacilityPower: " + Tools.OkStr(HasPoweredFacility);
                if (!HasPoweredFacility)
                {
                    compPowerFacility = facility?.TryGetComp<CompPowerTrader>();
                    Tools.Warn(tellMe + "no powered facility - Exiting", prcDebug);
                    ResetFacility();
                    return;
                }

                bool belongs = Dependencies.CheckBuildingBelongsFacility(compAffectedByFacilities, facility, prcDebug);
                tellMe += "Belongs to " + facility.Label + ':' + facility.ThingID + "?" + Tools.OkStr(belongs);
                if (!belongs)
                {
                    Tools.Warn(tellMe + " - Exiting", prcDebug);
                    ResetFacility();
                    return;
                }
            }

            if (IsOrphan)
            {
                Tools.Warn("no need to comptick if not linked", prcDebug);
                return;
            }

            //Tools.Warn("dependencies checked: " + tellMe, prcDebug);
            //if(WarmUpProgress>10)
            CheckItems();

            //Tools.Warn("Items checked", prcDebug);
            if (StatusChillin)
            {
                tellMe += " Chillin;";
                currentCooldown -= 1;
                currentCooldown = ((currentCooldown < 0) ? (0) : (currentCooldown));
            }
            if (StatusOverweight)
            {
                tellMe += " overweight;";
            }
            if (StatusNoItem)
            {
                tellMe += " nothing2do;";
            }

            if (StatusChillin || StatusOverweight || compTwin.StatusChillin || compTwin.StatusOverweight)
            {
                Tools.Warn(tellMe + " - TICK exit bc not ready: ", prcDebug);
                return;
            }

            //Tools.Warn("stating we ready", prcDebug);
            if (IsLinked && StatusReady && compTwin.StatusReady)
            {
                tellMe += "ready to tp " + "N:" + RegisteredCount + ":" + DumpList();
            }

            //Tools.Warn("checking if automatic", prcDebug);

            // automatic ; no order yet ; ready
            if (
                    (IsLinked && AutomaticTeleportation ) && 
                    (!teleportOrder && !compTwin.teleportOrder) &&
                    (StatusReady && compTwin.StatusReady)
                )
            {
                Tools.Warn(tellMe + " - Starting automatic order", prcDebug);
                switch (MyWay)
                {
                    case Way.Out:
                        if(HasItems)
                            OrderOut();
                        break;
                    case Way.In:
                        if(compTwin.HasItems)
                            OrderIn();
                        break;
                    case Way.Swap:
                        // one or another has something to tp
                        if (HasItems || compTwin.HasItems)
                            OrderSwap();
                        break;
                    default:break;
                }
            }
            else
            {
                //Tools.Warn("no automatic order needed", prcDebug);
            }

            Tools.Warn("Got TP order", teleportOrder && prcDebug);
            if (teleportOrder)
            {
                if (HasWarmUp)
                {
                    tellMe += "old warmup:"+warmUpLeft;
                    warmUpLeft--;
                    tellMe += "new warmup:" + warmUpLeft;

                    tellMe += Tools.CapacityString(warmUpLeft, warmUpCalculated) + ":" + WarmUpProgress;
                    myOpacity = ((TeleportItemAnimActive) ? (.6f + .4f * Rand.Range(0, 1)) : (1));

                    //if (TeleportItemAnimBegin && beginSequenceI>0)
                    //if (TpSequenceBegin && beginSequenceI > 0)
                    if(beginSequenceI > 0)
                        if (IncBeginAnim(prcDebug))
                        {
                            NextAnim();
                            SetSlideShowOn();
                        }

                    //if (warmUp <= (int).5f * (FrameSlowerMax * 23))
                    //if(warmUp == (int).9f * FrameSlowerMax * 23) 
                    /*
                    BeginSequenceFrameLength = 240 / FacilityQuality + 10 * (int)twinDistance;
                    warmUp = warmUpBase = BeginSequenceFrameLength + 2 * 23 * FrameSlowerMax;
                    */
                    //if (warmUpLeft == startFrame)
                    if (warmUpLeft == 0)
                    {
                        tellMe += "Trying to TP";
                        bool didIt = false;
                        //if (didIt = TryTeleport(prcDebug))
                        if (didIt = TryTeleport(prcDebug))
                        {
                            SoundDef.Named("LTF_TpSpotOut").PlayOneShotOnCamera(parent.Map);
                            //if(MyWay == Way.Out && compTwin.MyWay == Way.Out)
                            compTwin.ResetOrder();
                            compTwin.StartCooldown();
                            compTwin.orderRange = 0;

                            ResetOrder();
                            StartCooldown();
                            orderRange = 0;
                        }
                        tellMe += "\n>>>Did teleport: " + Tools.OkStr(didIt) + "<<<";
                    }
                }
                else
                {
                    slideShowOn = false;
                }
            }
            else
            {
                Tools.Warn("no teleporter order", prcDebug);
            }

            AnimStatus(prcDebug);
            tellMe += "\n" + StatusLogUpdated;
            Tools.Warn(buildingName + "=>" + tellMe, prcDebug);
            Tools.Warn(">>>TICK End<<< ", prcDebug);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentCooldown, "cooldown");
            Scribe_Values.Look(ref cooldownBase, "cooldo");

            Scribe_Values.Look(ref currentWeight, "weight");
            Scribe_Values.Look(ref weightCapacity, "wCapa");

            Scribe_References.Look(ref standingUser, "user");
            Scribe_Collections.Look(ref thingList, "things", LookMode.Reference, new object[0]);

            Scribe_References.Look(ref twin, "linkedspot");
            Scribe_Values.Look(ref MyLink, "LinkStatus");

            Scribe_Values.Look(ref MyWay, "way");
            Scribe_Values.Look(ref AutomaticTeleportation, "AutomaticTeleportation");

            Scribe_Values.Look(ref teleportOrder, "order");
            Scribe_Values.Look(ref warmUpLeft, "warmUpLeft");
            Scribe_Values.Look(ref warmUpCalculated, "warmUpCalculated");
        }

        public override string CompInspectStringExtra()
        {
            string text = base.CompInspectStringExtra();
            string result = string.Empty;

            result += Tools.OkStr(StatusNoIssue) + " ";
            result += StatusLogUpdated;

            if (!text.NullOrEmpty())
            {
                result = "\n" + text;
            }

            return result;
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (HasQuality)
            {
                Texture2D qualityMat = ToolsGizmo.Quality2Mat(compQuality);
                float qualitySize = ToolsGizmo.Quality2Size(compQuality);

                yield return new Command_Action
                {
                    //icon = ContentFinder<Texture2D>.Get("UI/Commands/CancelRegistry", true),
                    icon = qualityMat,
                    iconDrawScale = qualitySize,
                    defaultLabel = "Quality matters",
                    defaultDesc = QualityLog(),
                    action = delegate
                    {
                        Tools.Warn("rip quality button", prcDebug);
                    }
                };
            }

            // Link or not
            //if (ToolsBuilding.ValidLink(MyLink))
            ////("Right click with a colonist and target another tp spot to link it with.")
            /////' ' + LinkName + "->" + NextLinkName + "\n" +
            //if (IsLinked)

            if (ToolsBuilding.ValidLink(MyLink))
            {
                String LinkName = LinkNaming;
                //int NextIndex = (int)((IsLinked) ? (ToolsBuilding.Link.Orphan) : (ToolsBuilding.Link.Linked));
                String NextLinkName = NextLinkNaming;

                Texture2D myGizmo = ((IsLinked) ? (MyGizmo.LinkedGz) : (MyGizmo.OrphanGz));
                String myLabel = "Unlink";
                String myDesc = "Right click with a colonist to link to another tp spot.";
                Action todo = delegate { Tools.Warn("nothing link gizmo", prcDebug);  };

                //String myLabel = ((IsLinked) ? ("Unlink") : ("Right click with a colonist to link"));
                if (IsLinked)
                {
                    myLabel = "Unlink";
                    myDesc = (
                        "Linked with " + twin.def.label + "\n" +
                        Tools.PosStr(building.Position) + " " + WayArrowLabeling + " " + Tools.PosStr(twin.Position) +
                        "\nClick to unlink."
                    );
                    todo = new Action(UnlinkMe);
                }

                yield return new Command_Action
                {
                    icon = myGizmo,
                    defaultLabel = myLabel,
                    defaultDesc = myDesc,
                    action = todo
                };
            }

            if (requiresPower && HasPower && IsLinked)// && !StatusNoFacility)
            {
                // Way to teleport
                if (ValidWay)
                {
                    String WayName = WayNaming;
                    String NextName = NextWayNaming;

                    Texture2D myGizmo = WayGizmoing;

                    //((Auto) ? (MyGizmo.AutoOnGz) : (MyGizmo.AutoOffGz));
                    String myLabel = "browse ways";
                    String myDesc = "Current : " + WayName;
                    myDesc += "\nClick to change to " + NextWayNaming;
                        
                    yield return new Command_Action
                    {
                        icon = myGizmo,
                        defaultLabel = myLabel,
                        defaultDesc = myDesc,
                        action = new Action(this.BrowseWay)
                    };
                }
                else
                {
                    Tools.Warn("Invalid way:" + (int)MyWay);
                }

                // Auto or not
                if ((AutomaticTeleportation == false) || (AutomaticTeleportation == true))
                {
                    String AutoName = ((AutomaticTeleportation) ? ("Automatic") : ("Manual"));
                    String ComplementaryAutoName = ((!AutomaticTeleportation) ? ("Automatic") : ("Manual"));

                    Texture2D myGizmo = ((AutomaticTeleportation) ? (MyGizmo.AutoOnGz) : (MyGizmo.AutoOffGz));
                    String myLabel = "set automatic";
                    String myDesc = "Current : " + AutoName;
                    myDesc += "\nClick to change to "+ ComplementaryAutoName;

                    yield return new Command_Action
                    {
                        icon = myGizmo,
                        defaultLabel = myLabel,
                        defaultDesc = myDesc,
                        action = new Action(this.AutoToggle)
                    };
                }

                if (HasItems)
                {
                    // TP command there
                    // Status cooldown
                    // Status weight
                }


            }
            if (Prefs.DevMode)
            {
                // Debug process
                yield return new Command_Action
                {
                    icon = ((prcDebug) ? (MyGizmo.DebugOnGz) : (MyGizmo.DebugOffGz)),
                    defaultLabel = "prc: " + Tools.DebugStatus(prcDebug),
                    defaultDesc = "process debug\n"+DumpProps+DumpSettings,
                    action = delegate
                    {
                        prcDebug = Tools.WarnBoolToggle(prcDebug, "debug " + building.Label);
                    }
                };
                // Debug gfx
                yield return new Command_Action
                {
                    icon = ((gfxDebug) ? (MyGizmo.DebugOnGz) : (MyGizmo.DebugOffGz)),
                    defaultLabel = "gfx: " + Tools.DebugStatus(gfxDebug),
                    defaultDesc = "gfx debug",
                    action = delegate
                    {
                        gfxDebug = Tools.WarnBoolToggle(gfxDebug, "debug " + building.Label);
                    }
                };

                if (gfxDebug)
                {

                    yield return new Command_Action
                    {
                        defaultLabel = "under " + drawUnderlay + "->" + !drawUnderlay,
                        action = delegate
                        {
                            drawUnderlay = !drawUnderlay;
                        }
                    };
                    yield return new Command_Action
                    {
                        defaultLabel = "over " + drawOverlay + "->" + !drawOverlay,
                        action = delegate
                        {
                            drawOverlay = !drawOverlay;
                        }
                    };
                }

                //debug log + hax activate
                if (prcDebug)
                    yield return new Command_Action
                    {
                        //icon = ContentFinder<Texture2D>.Get("UI/Commands/HaxReady", true),
                        icon = MyGizmo.DebugLogGz,
                        defaultLabel = "hax " + Tools.DebugStatus(Hax),
                        defaultDesc = "$5,000 for you advert here.",
                        action = delegate
                        {
                            Hax = Tools.WarnBoolToggle(Hax, "hax " + building.Label);
                        }
                    };

                // Hax Progress
                if (prcDebug && Hax)
                {
                    yield return new Command_Action
                    {
                        icon = null,
                        defaultLabel = "raz",
                        defaultDesc = "reset();",
                        action = delegate
                        {
                            ResetSettings();
                        }
                    };

                    yield return new Command_Action
                    {
                        defaultLabel = "tpOut " + TeleportItemAnimStatus + "->" + TpSequenceBegin,
                        action = delegate
                        {
                            //BeginTpInAnimSeq();
                            BeginTeleportItemAnimSeq();
                        }
                    };

                    if (currentCooldown == 0)
                    {
                        yield return new Command_Action
                        {
                            icon = MyGizmo.HaxEmptyGz,
                            defaultLabel = currentCooldown + "->" + cooldownBase,
                            defaultDesc = "force cooldown",
                            action = delegate
                            {
                                StartCooldown();
                            }
                        };
                    }
                    else
                    {
                        yield return new Command_Action
                        {
                            icon = MyGizmo.HaxFullGz,
                            defaultLabel = currentCooldown + "->0",
                            defaultDesc = "reset cooldown",
                            action = delegate
                            {
                                StopCooldown();
                            }
                        };
                    }
                    int minus10perc = (int)Mathf.Max(0, (currentCooldown - cooldownBase / 10));
                    int plus10perc = (int)Mathf.Min(cooldownBase, (currentCooldown + cooldownBase / 10));

                    yield return new Command_Action
                    {
                        icon = MyGizmo.HaxSubGz,
                        //defaultLabel = currentCooldown + "->" + minus10perc,
                        defaultLabel = currentCooldown + "->" + plus10perc,
                        defaultDesc = "-10%",
                        action = delegate
                        {
                            HaxCooldown(plus10perc);
                        }
                    };

                    yield return new Command_Action
                    {
                        icon = MyGizmo.HaxAddGz,
                        defaultLabel = currentCooldown + "->" + minus10perc,
                        //defaultLabel = currentCooldown + "->" + plus10perc,
                        defaultDesc = "+10%",
                        action = delegate
                        {
                            HaxCooldown(minus10perc);
                        }
                    };

                }

                // Hax quality
                if (prcDebug && Hax && HasQuality)
                {
                    if (!ToolsQuality.BestQuality(compQuality))
                        yield return new Command_Action
                        {
                            defaultLabel = compQuality.Quality.GetLabelShort() + "->" + ToolsQuality.BetterQuality(compQuality),
                            defaultDesc = "Better quality",
                            //icon = ContentFinder<Texture2D>.Get("UI/Commands/HaxReady", true),
                            icon = MyGizmo.HaxBetterGz,
                            action = delegate
                            {
                                BetterQuality();
                            }
                        };

                    if (!ToolsQuality.WorstQuality(compQuality))
                        yield return new Command_Action
                        {
                            defaultDesc = "Worse quality",
                            defaultLabel = compQuality.Quality.GetLabelShort() + "->" + ToolsQuality.WorseQuality(compQuality),
                            icon = MyGizmo.HaxWorseGz,
                            action = delegate
                            {
                                WorseQuality();
                            }
                        };
                }
            }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            // Flickering line between spot and twin
            if (IsLinked)
            {
                GenDraw.DrawLineBetween(this.parent.TrueCenter(), twin.TrueCenter(), WayColoring);
            }

            if (range > 0f)
            {
                // if tpspot range drawable, then draw it
                // Cannot draw radius ring of radius 140.7: not enough squares in the precalculated list.
                if (range < GenRadial.MaxRadialPatternRadius)
                    GenDraw.DrawRadiusRing(this.parent.Position, range);
            }
        }
    }
}
