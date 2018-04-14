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
        // parent shortcurts
        Building building = null;
        Vector3 buildingPos;
        String buildingName = string.Empty;
        String MyDefName = string.Empty;

        public bool RequiresPower = true;
        public bool RequiresBench = true;
        public enum Way { No = 0, Out = 1, In = 2, Swap = 3 };

        Texture2D[] WayGizmo = { MyGizmo.WayNoGz, MyGizmo.WayOutGz, MyGizmo.WayInGz, MyGizmo.WaySwapGz };

        public Building Twin = null;
        public ToolsBuilding.Link MyLink = ToolsBuilding.Link.Orphan;

        bool Auto = false;
        string[] AutoLabel = { ", if bench triggered,", "automatically" };
        string[] WayLabel = { "No way", "Tp out", "Tp in", "Swap" };
        string[] WayActionLabel = { "do nothing", "send away", "bring back", "exchange" };
        string TargetLabel = "what stands on";
        public Way MyWay = Way.No;

        public int WarmUp = 0;
        public int WarmUpBase = 0;

        float TwinDistance = 0f;
        private bool TeleportOrder = false;
        //int StartupTickCount = 1000;
        bool Destination = false;
        bool Source = false;
        public bool SlideShowOn = false;
        /* Comp */
        /******************/
        public CompPowerTrader compPowerTrader = null;
        public CompQuality compQuality = null;
        // TpBench required
        public CompAffectedByFacilities compAffectedByFacilities = null;
        public CompPowerTrader compPowerFacility = null;
        public Comp_TpBench comp_TpBench = null;
        // Linked tpspot maybe
        public Comp_LTF_TpSpot compTwin = null;
        //        private CompFlickable flickComp;

        /* Caracteristics */
        /******************/
        // Inherits from tpBench
        // 22 - 54 ; 32 / 8 = 4 

        //private float radius = 30f;

        float weightCapacity = 0f;//will be set
        float currentWeight = 0f;//calculated

        float cooldownBase = 60 * 60f;
        float currentCooldown = 0f;

        /* Production */
        /******************/
        List<Thing> ThingList = new List<Thing>();
        // Can be a dead animal
        Building facility = null;
        Pawn standingUser = null;
        bool BenchManaged = false;
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

        bool FactionMajority = false;

        public Gfx.AnimStep TpOutStatus = Gfx.AnimStep.na;
        public Gfx.AnimStep TpInStatus = Gfx.AnimStep.na;
        int BeginSequenceFrameLength = 120;
        int beginSequenceI = 0;

        int FrameSlowerMax = 3;
        int FrameSlower = 0;

        public bool drawUnderlay = true;
        public bool drawOverlay = true;
        float myOpacity = 1f;

        /* Debug */
        /**********/
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
            SetCooldownBase();
            SetWeightBase(compQuality);
        }
        public bool HasOrder
        {
            get
            {
                return TeleportOrder;
            }
        }
        public bool HasQuality
        {
            get
            {
                return (compQuality != null);
            }
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

            Answer = "Cooldown: " + Tools.Ticks2Str(cooldownBase);
            Answer += "\nWeight capacity: " + weightCapacity;

            return Answer;
        }

        public void AutoToggle()
        {
            Auto = !Auto;
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

                //if ((!IsLinked) && (Answer == Way.Swap))Answer = Way.No;

                return Answer;
            }
        }
        public void BrowseWay()
        {
            MyWay = NextWay;
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

                if (RequiresPower && !HasPower)
                    Answer = MyGizmo.IssuePowerGz;
                else if (RequiresBench && StatusNoFacility)
                    Answer = MyGizmo.IssueNoFacilityGz;
                else if (RequiresBench && !HasPoweredFacility)
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
                if ((int)MyWay > AutoLabel.Length - 1)
                    return ("Auto labeling outbound");

                return (AutoLabel[(int)MyWay]);
            }
        }
        public ToolsBuilding.Link NextLink
        {
            get
            {
                //Answer = ToolsBuilding.Link.Orphan;
                //Answer = ((MyLink ==ToolsBuilding.Link.Linked) ? (ToolsBuilding.Link.Orphan) : (MyLink.Next()));
                //if ((int)Answer > (int)ToolsBuilding.Link.Linked)
                //    Answer = ToolsBuilding.Link.Orphan;
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
                Answer += "will ";
                Answer += WayActionLabeling + ' ';
                Answer += AutoLabeling + ' ';
                Answer += TargetLabel + ' ';
                Answer += "this " + MyDefName;
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
        private void SetCooldownBase(CompQuality comp = null)
        {
            cooldownBase = Tools.WeightedCapacity(Props.cooldownBase, Props.cooldownSpectrum, comp);
            currentCooldown = Mathf.Min(cooldownBase, currentCooldown);
        }

        private void ResetSettings()
        {
            MyWay = Way.No;
            Auto = false;

            // TRying to remove spot from workstation it's registered
            if (RequiresBench && HasRegisteredFacility && ToolsBuilding.CheckBuilding(building))
                comp_TpBench.RemoveSpot(building);

            UnlinkMe();

            DisableCooldown();
            ResetWeight();

            if (RequiresBench)
                ResetFacility();

            ResetItems();
            ResetPawn();
            ResetOrder();
        }

        public bool HasWarmUp
        {
            get
            {
                return(WarmUp > 0);
            }
        }
        public bool WarmUpJustStarted
        {
            get
            {
                return ((TeleportOrder) && (WarmUpBase == WarmUp));
            }
        }
        public float WarmUpProgress
        {
            get
            {
                if(WarmUpBase == 0) {
                    Tools.Warn("Unset WarmUpBase", prcDebug);
                    return 0;
                }
                    
                return (WarmUp/WarmUpBase);
            }
        }
        public void ResetOrder()
        {
            TeleportOrder = false;

            Destination = false;
            Source = false;
            TpOutStatus = Gfx.AnimStep.na;
        }

        private void UpdateDistance(bool debug=false)
        {
            if(!ToolsBuilding.CheckBuilding(building) || compTwin == null)
            {
                Tools.Warn("cant distance", debug);
            }
            TwinDistance = building.Position.DistanceTo(Twin.Position);
            compTwin.TwinDistance = TwinDistance;
            Tools.Warn("updated distance:"+TwinDistance, debug);
        }

        private void WorkstationOrder(bool debug = false)
        {
            if (!StatusNoIssue)
            {
                Tools.Warn("cant accept an order: " + StatusLogNoUpdate, debug);
                return;
            }

            int FacilityQuality = TwinWorstFacilityQuality();

            //BeginSequenceFrameLength = StartupTickCount = 240 / FacilityQuality + 10 * (int)TwinDistance;
            BeginSequenceFrameLength = 240 / FacilityQuality + 10 * (int)TwinDistance;
            WarmUp = WarmUpBase = BeginSequenceFrameLength + 2*23*FrameSlowerMax;
            
            //initseq
            TeleportOrder = true;
        }

        // Gizmo command no debug pls
        public void OrderOut()
        {
           
            WorkstationOrder(prcDebug);
            BeginTpOutAnimSeq();

            Source = true;
            Destination = false;

            compTwin.Source = false;
            compTwin.Destination = true;
        }
        public void OrderIn()
        {
            WorkstationOrder(prcDebug);
            //BeginTpInAnimSeq();
            compTwin.OrderOut();
        }
        public void OrderSwap()
        {
            WorkstationOrder(prcDebug);

            SetBeginAnimLength();
            Source = true;
            Destination = false;
            compTwin.Source = true;
            compTwin.Destination = false;
        }

        private bool TryTeleport()
        {
            if (!StatusReady)
                return false;
            if (!IsLinked)
                return false;

            switch (MyWay)
            {
                case Way.Out:
                    if (HasItems)
                        foreach (Thing cur in ThingList)
                        {
                            cur.Position = Twin.Position;
                        }
                    break;
                case Way.In:
                    if( compTwin.HasItems)
                        foreach (Thing cur in compTwin.ThingList)
                        {
                            cur.Position = building.Position;
                        }
                    break;
                case Way.Swap:
                    if (HasItems)
                        foreach (Thing cur in ThingList)
                        {
                            cur.Position = Twin.Position;
                        }
                    if (compTwin.HasItems)
                        foreach (Thing cur in compTwin.ThingList)
                        {
                            cur.Position = building.Position;
                        }
                    break;
            }

            return true;
        }

        private void DisableCooldown()
        {
            currentCooldown = 0;
        }
        private void ResetCooldown()
        {
            currentCooldown = cooldownBase;
        }
        private void SetCooldown(float value)
        {
            currentCooldown = value;
        }

        //Dependency : facility
        public bool HasRegisteredFacility
        {
            get
            {
                return (facility != null);
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
        public int TwinWorstFacilityQuality(bool debug=false)
        {
            int Answer = 0;

            //if (IsCatcher)                return null;
            if (IsOrphan)
            {
                Answer = 999;
            }
            else
            {
                if (!RequiresBench)
                {
                    if (compTwin.HasPoweredFacility)
                    {
                        Answer = (int)compTwin.compQuality.Quality;
                    }
                    else
                    {
                        Tools.Warn("// non catcher from duo has not powered facility", debug);
                    }
                }
                else
                {
                    if (compTwin.HasPoweredFacility)
                    {
                        Answer = (int)(((compTwin.compQuality.Quality < compQuality.Quality)) ? (compTwin.compQuality.Quality) : (compQuality.Quality));
                    }
                    else
                    {
                        Answer = (int)compQuality.Quality;
                    }
                }
            }
            Answer = ToolsQuality.Valid(Answer, debug);

            return Answer;
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
                "Master - order:" + Tools.OkStr(TeleportOrder) + "; link:"+ Tools.OkStr(IsLinked) +
                "Slave - order:" + Tools.OkStr(newComp.HasOrder) + "; link:" + Tools.OkStr(newComp.IsLinked),
                debug);

            UnlinkMe();

            // This one
            Twin = newLinked;
            compTwin = newComp;
            compTwin.ResetOrder();
            MyLink = ToolsBuilding.Link.Linked;

            // Remote
            newComp.Twin = building;
            newComp.compTwin = this;
            newComp.MyLink = ToolsBuilding.Link.Linked;
            newComp.compTwin.ResetOrder();

            UpdateDistance();

            Tools.Warn(
                "Inc " +
                "Master - order:" + Tools.OkStr(TeleportOrder) + "; link:" + Tools.OkStr(IsLinked) +
                "Slave - order:" + Tools.OkStr(newComp.HasOrder) + "; link:" + Tools.OkStr(newComp.IsLinked),
                debug);

            //SoundDef.Named("LTF_TpSpotOut").PlayOneShotOnCamera(parent.Map);

            return true;
        }

        public static string ValidTpSpot(Thing thing)
        {
            string Answer = string.Empty;
            if (thing.def.defName != "LTF_TpSpot" && thing.def.defName != "LTF_TpCatcher")
                Answer = thing.Label + " is not a valid tp spot, it's a " + thing.def.label;

            return Answer;
        }

        public string MyCoordinates {
            get {
                if (!ToolsBuilding.CheckBuilding(building))
                    return string.Empty;

                return (Tools.PosStr(building.Position));
            }
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
            Twin = null;
            MyLink = ToolsBuilding.Link.Orphan;
            TwinDistance = 0;

            ResetWay();
        }
        public bool IsLinked
        {
            get
            {
                return (Twin != null && compTwin != null);
            }
        }
        public bool IsOrphan
        {
            get
            {
                return (Twin == null || compTwin == null);
            }
        }
        private void ResetWay()
        {
            MyWay = Way.No;
        }
        public void ResetFacility()
        {
            
            facility = null;
            compPowerFacility = null;
            comp_TpBench = null;

            BenchManaged = false;
        }

        // Check local tile
        // Items set
        private void ResetItems()
        {
            ThingList.Clear();
            ResetWeight();
        }
        private bool RemoveItemsIfAbsent()
        {

            if (HasNothing)
                return false;

            int neededAverageFaction = (int)(ThingList.Count / 2);

            //Tools.Warn(building.Label + " checks history");
            //for (int i = 0; i < ThingList.Count; i++)
            for (int i = ThingList.Count - 1; i >= 0; i--)
            {
                Thing thing = null;
                thing = ThingList[i];
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
                    ThingList.Remove(thing);
                }
            }

            FactionMajority = (neededAverageFaction <= 0);

            return (HasItems);
        }
        private void AddItem(Thing thing)
        {
            Tools.Warn("Adding " + thing.Label + " to " + building.Label, prcDebug);

            ThingList.Add(thing);
        }
        private void RemoveItem(Thing thing)
        {
            Tools.Warn("Removing " + thing.Label + " from " + building.Label, prcDebug);

            ThingList.Remove(thing);
        }
        private bool CheckNewItems()
        {
            return (AddSpotItems(building.Position.GetThingList(building.Map)));
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

                    if (!ThingList.Contains(thing))
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
                //ThingList.Clear();
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
                return ThingList.Count;
            }
        }
        public bool HasItems
        {
            get
            {
                return !ThingList.NullOrEmpty();
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
            int plusOrMinus = ((addWeight) ? (1) : (-1));

            currentWeight += plusOrMinus * newWeight;

            currentWeight = Tools.LimitToRange(currentWeight, 0, 3000);
            currentWeight = (float)Math.Round((Decimal)currentWeight, 2, MidpointRounding.AwayFromZero);

            Tools.Warn(thing.LabelShort + " adds(" + plusOrMinus + ")" + newWeight + " -> " + currentWeight, prcDebug);
        }
        private void SetWeightBase(CompQuality comp = null)
        {
            weightCapacity = Tools.WeightedCapacity(Props.weightBase, Props.weightSpectrum, comp);
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

        public bool TpOutEnd
        {
            get
            {
                return (TpOutStatus == Gfx.AnimStep.end);
            }
        }
        public bool TpOutBegin
        {
            get
            {
                return (TpOutStatus == Gfx.AnimStep.begin);
            }
        }
        public bool TpOutActive
        {
            get
            {
                return (TpOutStatus == Gfx.AnimStep.active);
            }
        }
        public bool TpOutNa
        {
            get
            {
                return (TpOutStatus == Gfx.AnimStep.na);
            }
        }
        private void SetBeginAnimLength(bool TpIn=false)
        {
            beginSequenceI = BeginSequenceFrameLength;
            //beginSequenceI                ((TpIn) ? (5) : (WarmUpBase));
        }
        public void BeginTpOutAnimSeq()
        {
            TpOutStatus = Gfx.AnimStep.begin;
            SetBeginAnimLength();
        }
        public void BeginTpInAnimSeq()
        {
            TpOutStatus = Gfx.AnimStep.begin;
            SetBeginAnimLength(true);
        }
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
            Tools.Warn("AnimStatus - " + TpOutStatus + ": " + beginSequenceI + "/" + BeginSequenceFrameLength, debug);
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

        public bool NextAnim(bool debug=false)
        {
            bool Answer = false;
            if (compTwin == null)
            {
                Tools.Warn("//bad twin comp", true);
                return false;
            }
                
            if (Source)
                switch (TpOutStatus)
                {
                    case Gfx.AnimStep.begin:
                        TpOutStatus = Gfx.AnimStep.active;
                        //BeginSequenceFrameLength
                        break;
                    case Gfx.AnimStep.active:
                        TpOutStatus = Gfx.AnimStep.end;
                        compTwin.BeginTpInAnimSeq();
                        break;
                    case Gfx.AnimStep.end:
                        TpOutStatus = Gfx.AnimStep.na;
                        Answer = true;
                        break;
                }

            if (Destination)
                switch (compTwin.TpInStatus)
                {
                    case Gfx.AnimStep.begin:
                        compTwin.TpInStatus = Gfx.AnimStep.active;
                        break;
                    case Gfx.AnimStep.active:
                        compTwin.TpInStatus = Gfx.AnimStep.end;
                        break;
                    case Gfx.AnimStep.end:
                        compTwin.TpInStatus = Gfx.AnimStep.na;
                        Answer = true;
                        break;
                }

            SetFrameSlower();

            return Answer;
        }

        //public void StartVanish(){drawVanish = true;}

        // Debug 
        private void DumpProps(bool debug=false)
        {
            Tools.Warn("settings: "+ MyWay + MyLink + Auto, debug);
        }

        private string DumpList()
        {
            string bla = String.Empty;
            foreach (Thing item in ThingList)
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

                if (!ToolsBuilding.CheckPower(building))
                    Answer ^= BuildingStatus.noPower;

                if (!HasRegisteredFacility)
                    Answer ^= BuildingStatus.noFacility;

                if (Tools.CapacityOverusing(currentWeight, weightCapacity))
                    Answer ^= BuildingStatus.overweight;

                if (Tools.CapacityUsing(currentCooldown))
                    Answer ^= BuildingStatus.cooldown;

                if (HasNothing)
                    Answer ^= BuildingStatus.noItem;

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
        public bool StatusChillin { get { return HasStatus(BuildingStatus.cooldown); } }
        public bool IsHot { get { return !StatusChillin; } }

        public bool StatusReady { get { return HasStatus(BuildingStatus.capable); } }
        public bool StatusNoIssue {
            get {
                bool Answer = true;

                if (RequiresPower)
                    Answer &= HasPower;

                if (RequiresBench)
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

                bla += Tools.OkStr(TeleportOrder) +
                    ((TeleportOrder) ? ("Roger;") : ("Say what?"));

                bla = (!HasItems)?
                    ("Nothing"):
                    (RegisteredCount + " item" + grammar + ' ' + Tools.CapacityString(currentWeight, weightCapacity) + " kg")+
                    ';';

                
                if (TeleportOrder && HasWarmUp)
                    bla += "\nWarm up: " + WarmUpProgress.ToStringPercent("F0") + "% (" + Tools.Ticks2Str(WarmUpBase - WarmUp) + " left)";

                return bla;
            }


            if (RequiresPower && StatusNoPower)
            {
                bla += " No power;";
            }

            if (RequiresBench){
                if( StatusNoFacility)
                    bla += " No facility;";
                else if (!HasPoweredFacility)
                    bla += " No powered facility;";
            }

            if (StatusOverweight)
                bla += ' ' + currentWeight + "kg. >" + weightCapacity + " kg;";

            if (StatusChillin) { 
                float coolPerc = currentCooldown / cooldownBase;
                bla += " Cooldown " + ((updateDisplay) ? (coolPerc.ToStringPercent("F0")) : ("undergoing")) + ";";
            }

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
        /*
        public bool IsCatcher
        {
            get
            {
                return (!RequiresPower);
            }
        }
        */

        // Overrides
        public override void PostDraw()
        {
            base.PostDraw();

            if ((buildingPos == null) || (building.Rotation != Rot4.North))
            {
                Tools.Warn("null pos draw", gfxDebug);
                return;
            }

            // nothing there standing
            if (StatusNoPower && RequiresPower)
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
            if (drawUnderlay && RequiresPower)
            {
                if ((HasItems) || ((HasNothing) && (HasPoweredFacility)) || (StatusReady))
                    underlay = MyGfx.Status2UnderlayMaterial(this, gfxDebug);

                underlay2 = MyGfx.UnderlayM;
                Tools.Warn("Underlay calculating - 1: " + (underlay != null) + "; 2: " + (underlay2 != null), gfxDebug);
            }

            // calculate Overlay
            if (drawOverlay)
            {
                //if( (!TpOutNa) && (TpOutBegin) )
                if (TpOutBegin && TeleportOrder && beginSequenceI>0)
                    overlay = MyGfx.Status2OverlayMaterial(this, FactionMajority, gfxDebug);

                if (RequiresPower && !StatusReady && HasItems)
                    warning = MyGfx.Status2WarningMaterial(this, gfxDebug);

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

            if (RequiresPower && warning != null)
                Gfx.PulseWarning(building, warning);

            if (TeleportOrder && TpOutBegin && (overlay != null))
            {
                float swirlSize = Gfx.VanillaPulse(parent) * 1.5f;
                //Gfx.DrawTickRotating(parent, overlay, 0, 0, swirlSize, Gfx.LoopFactorOne(parent) * 360, 1, Gfx.Layer.over, gfxDebug);
                Gfx.DrawTickRotating(parent, overlay, 0, 0, swirlSize, Gfx.LoopFactorOne(parent) * 360, 1, Gfx.Layer.over, false);

            }

            if (SlideShowOn)
                if (TpOutActive)
                {
                    MyGfx.ActiveAnim.Draw(parent.DrawPos, Rot4.North, this.parent, 0.027f * Rand.Range(-1, 1) * 360);
                }
                else if (TpOutEnd)
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
            MyDefName = building?.def?.label;
            buildingPos = building.DrawPos;
            buildingName = building?.LabelShort;
            RequiresPower = Props.PowerRequired;
            RequiresBench = Props.BenchRequired;

            if (RequiresPower)
            {
                //Building comp
                compPowerTrader = building?.TryGetComp<CompPowerTrader>();
                compAffectedByFacilities = ToolsBuilding.GetAffectedComp(building, prcDebug);
                //Facility
                //facility = ToolsBuilding.GetFacility(compAffectedByFacilities, prcDebug);
                facility = ToolsBuilding.GetFacility(compAffectedByFacilities, false);
                //Facility power comp
                compPowerFacility = facility?.TryGetComp<CompPowerTrader>();
                comp_TpBench = facility?.TryGetComp<Comp_TpBench>();
            }

            compQuality = building?.TryGetComp<CompQuality>();
            SetWeightBase(compQuality);
            SetCooldownBase(compQuality);

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

            if (Twin != null)
            {
                compTwin=Twin.TryGetComp<Comp_LTF_TpSpot>();
            }

            DumpProps();
        }
        public override void CompTick()
        {
            base.CompTick();

            Tools.Warn(" >>>TICK begin<<< ", prcDebug);

            Tools.Warn("Validated order:" +
    " warmup: " + Tools.CapacityString(WarmUp, WarmUpBase) +
    " begin seq: " + Tools.CapacityString(beginSequenceI, BeginSequenceFrameLength),
    prcDebug && TeleportOrder);

            if (!ToolsBuilding.CheckBuilding(building))
            {
                Tools.Warn("comp building not legit" + " - Exiting", prcDebug);
                return;
            }

            string tellMe = string.Empty;
            tellMe = Tools.OkStr(StatusReady) + "[" + TeleportCapable + "]" + buildingName + ": ";

            if (RequiresPower)
            {
                // Power - Will return if status
                tellMe += "Power: " + Tools.OkStr(HasPower) + "; ";
                if (StatusNoPower)
                {
                    if (RequiresBench && HasRegisteredFacility)
                    {
                        ResetSettings();
                    }
                    Tools.Warn(tellMe + " - Exiting", prcDebug);
                    return;
                }
            }

            if (RequiresBench)
            {
                // Facility - Will return if status
                tellMe += "Facility: " + Tools.OkStr(HasRegisteredFacility) + "; ";
                if (StatusNoFacility)
                {
                    //Facility
                    facility = ToolsBuilding.GetFacility(compAffectedByFacilities, false);
                    //Facility power comp
                    compPowerFacility = facility?.TryGetComp<CompPowerTrader>();
                    comp_TpBench = facility?.TryGetComp<Comp_TpBench>();
                    tellMe += "Found: " + Tools.OkStr(HasRegisteredFacility) + ((HasRegisteredFacility) ? (facility.LabelShort) : ("nothing")) + "; ";
                    Tools.Warn(tellMe + " - Exiting", prcDebug);
                    return;
                }

                //Okk
                tellMe += "FacilityPower: " + Tools.OkStr(HasPoweredFacility);
                if (!HasPoweredFacility)
                {
                    ResetFacility();
                    compPowerFacility = facility?.TryGetComp<CompPowerTrader>();
                    Tools.Warn(tellMe + " - Exiting", prcDebug);
                    return;
                }

                bool belongs = Dependencies.CheckBuildingBelongsFacility(compAffectedByFacilities, facility, prcDebug);
                tellMe += "Belongs to " + facility.Label + ':' + facility.ThingID + "?" + Tools.OkStr(belongs);
                if (!belongs)
                {
                    Tools.Warn(tellMe + " - Exiting", prcDebug);
                    return;
                }

                if (!BenchManaged)
                {
                    comp_TpBench = facility?.TryGetComp<Comp_TpBench>();
                    if (comp_TpBench != null)
                    {
                        comp_TpBench.AddSpot(building);
                    }
                }
                else BenchManaged = true;
            }

            Tools.Warn("TICK checking: " + tellMe, prcDebug);
            CheckItems();

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

            //if ((StatusChillin) || (StatusOverweight) || (StatusNoItem))
            if (StatusChillin || StatusOverweight)
            {
                Tools.Warn(tellMe + "TICK exit bc not ready: " + tellMe, prcDebug);
                return;
            }

            if (StatusReady)
            {
                tellMe += "ready to tp " + "N:" + RegisteredCount + ":" + DumpList();
                // Work here
                //
            }

            //(TpOutBegin) = hax
            if (TeleportOrder)
            {
                if (WarmUp > 0)
                {
                    WarmUp--;

                    tellMe += Tools.CapacityString(WarmUp, WarmUpBase) + ":" + WarmUpProgress;
                    myOpacity = ((TpOutActive) ? (.6f + .4f * Rand.Range(0, 1)) : (1));

                    if (TpOutBegin && beginSequenceI>0)
                        if (IncBeginAnim(prcDebug))
                        {
                            NextAnim();
                            SlideShowOn = true;
                        }

                    //if (WarmUp <= (int).5f * (FrameSlowerMax * 23))
                    //if(WarmUp == (int).9f * FrameSlowerMax * 23) 
                    /*
                    BeginSequenceFrameLength = 240 / FacilityQuality + 10 * (int)TwinDistance;
                    WarmUp = WarmUpBase = BeginSequenceFrameLength + 2 * 23 * FrameSlowerMax;
                    */
                    if (WarmUp == (int)1.5f * FrameSlowerMax * 23)
                    {
                        tellMe += "Trying to teleport";
                        bool didIt = false;
                        if (didIt = TryTeleport())
                        {
                            SoundDef.Named("LTF_TpSpotOut").PlayOneShotOnCamera(parent.Map);
                            ResetOrder();
                            ResetCooldown();
                        }
                        tellMe += ">>>didit: " + Tools.OkStr(didIt) + "<<<";
                    }
                }
                else
                {
                    SlideShowOn = false;
                }
            }
            AnimStatus(prcDebug);
            tellMe += StatusLogUpdated;
            Tools.Warn("TICK End: " + tellMe, prcDebug);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentCooldown, "cooldown");
            Scribe_Values.Look(ref currentWeight, "weight");
            Scribe_References.Look(ref standingUser, "user");
            Scribe_Collections.Look(ref ThingList, "things", LookMode.Reference, new object[0]);

            Scribe_References.Look(ref Twin, "linkedspot");

            Scribe_Values.Look(ref MyLink, "LinkStatus");
            Scribe_Values.Look(ref MyWay, "way");
            Scribe_Values.Look(ref Auto, "auto");

            Scribe_Values.Look(ref TeleportOrder, "order");
            Scribe_Values.Look(ref WarmUp, "warmup");
            Scribe_Values.Look(ref WarmUpBase, "warmupbase");

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
            if (ToolsBuilding.ValidLink(MyLink))
            {

                String LinkName = LinkNaming;
                //int NextIndex = (int)((IsLinked) ? (ToolsBuilding.Link.Orphan) : (ToolsBuilding.Link.Linked));
                String NextLinkName = NextLinkNaming;

                Texture2D myGizmo = ((IsLinked) ? (MyGizmo.LinkedGz) : (MyGizmo.OrphanGz));
                String myLabel = "Unlink";

                //String myLabel = ((IsLinked) ? ("Unlink") : ("Right click with a colonist to link"));
                String myDesc = (
                    (IsLinked) ?
                    (' ' + LinkName + "->" + NextLinkName + "\nCurrent: " + Tools.PosStr(Twin.Position)) :
                    ("Right click with a colonist to link to another " + MyDefName)
               );

                yield return new Command_Action
                {
                    icon = myGizmo,
                    defaultLabel = myLabel,
                    defaultDesc = myDesc,
                    action = new Action(UnlinkMe)
                };
            }

            if (RequiresPower && HasPower)// && !StatusNoFacility)
            {
                // Way to teleport
                if (ValidWay)
                {
                    String WayName = WayNaming;
                    String NextName = NextWayNaming;

                    Texture2D myGizmo = WayGizmoing;

                    //((Auto) ? (MyGizmo.AutoOnGz) : (MyGizmo.AutoOffGz));
                    String myLabel = "browse ways";
                    String myDesc = WayName + " -> " + NextWayNaming +
                        "\ncurrent action: " + WayDescription;
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
                if ((Auto == false) || (Auto == true))
                {
                    String AutoName = ((Auto) ? ("Automatic") : ("Manual"));
                    String ComplementaryAutoName = ((!Auto) ? ("Automatic") : ("Manual"));

                    Texture2D myGizmo = ((Auto) ? (MyGizmo.AutoOnGz) : (MyGizmo.AutoOffGz));
                    String myLabel = "toggle";
                    String myDesc = AutoName + " -> " + ComplementaryAutoName;
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
                    defaultDesc = "process debug",
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
                        defaultLabel = "tpOut " + TpOutStatus + "->" + TpOutBegin,
                        action = delegate
                        {
                            BeginTpInAnimSeq();
                        }
                    };

                    if (currentCooldown != 0)
                        yield return new Command_Action
                        {
                            icon = MyGizmo.HaxEmptyGz,
                            defaultLabel = currentCooldown + "->" + cooldownBase,
                            defaultDesc = "force cooldown",
                            action = delegate
                            {
                                ResetCooldown();
                            }
                        };
                    yield return new Command_Action
                    {
                        icon = MyGizmo.HaxFullGz,
                        defaultLabel = currentCooldown + "->0",
                        defaultDesc = "reset cooldown",
                        action = delegate
                        {
                            DisableCooldown();
                        }
                    };

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
                            SetCooldown(plus10perc);
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
                            SetCooldown(minus10perc);
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
            if (IsLinked)
            {
                GenDraw.DrawLineBetween(this.parent.TrueCenter(), Twin.TrueCenter(), SimpleColor.Cyan);
            }
        }
    }
}
