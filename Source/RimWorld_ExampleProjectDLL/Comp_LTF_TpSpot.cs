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

    // Main
    [StaticConstructorOnStartup]
    public class Comp_LTF_TpSpot : ThingComp
    {
        // parent shortcurts
        Building building = null;
        Vector3 buildingPos;
        String buildingName = string.Empty;
        String MyDefName = string.Empty;

        bool RequiresPower = true;
        public enum Way { No = 0, Out = 1, In = 2, Swap = 3 };

        Texture2D[] WayGizmo = { MyGizmo.WayNoGz, MyGizmo.WayOutGz, MyGizmo.WayInGz, MyGizmo.WaySwapGz };

        public Building LinkedTpSpot = null;
        public ToolsBuilding.Link MyLink = ToolsBuilding.Link.Orphan;

        bool Auto = false;
        string[] AutoLabel = { ", if bench triggered,", "automatically" };
        string[] WayLabel = { "No way", "Tp out", "Tp in", "Swap" };
        string[] WayActionLabel = { "do nothing", "send away", "bring back", "exchange" };
        string TargetLabel = "what stands on";
        public Way MyWay = Way.No;

        /* Comp */
        /******************/
        public CompPowerTrader compPowerTrader = null;
        public CompQuality compQuality = null;
        // TpBench required
        public CompAffectedByFacilities compAffectedByFacilities = null;
        public CompPowerTrader compPowerFacility = null;
        public Comp_TpBench comp_TpBench = null;
        // Linked tpspot maybe
        public Comp_LTF_TpSpot linkedComp_LTF_TpSpot = null;
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
        };

        bool FactionMajority = false;

        public Gfx.AnimStep TpOutStatus = Gfx.AnimStep.na;
        int beginSequenceFrameLength = 120;
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
                else if (!IsCatcher && StatusNoFacility)
                    Answer = MyGizmo.IssueNoFacilityGz;
                else if (!IsCatcher && !HasPoweredFacility)
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
            Unlink();
            DisableCooldown();
            ResetWeight();

            if (!IsCatcher)
                ResetFacility();

            ResetItems();
            ResetPawn();
        }
        public void WorkstationOrder()
        {
            bool didSomething = false;
            Tools.Warn("Recieved order", prcDebug);
            didSomething = TryTeleport();
            Tools.Warn("didIt: " + Tools.OkStr(didSomething));
        }
        private bool TryTeleport()
        {
            bool didSomething = false;
            if (!StatusReady)
                return didSomething;
            if (!IsLinked)
                return didSomething;

            if (MyWay == Way.Out)
            {
                if (HasItems)
                    foreach (Thing cur in ThingList)
                    {
                        cur.Position = LinkedTpSpot.Position;
                    }
            }

            return didSomething;
        }

        private void DisableCooldown()
        {
            currentCooldown = cooldownBase;
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

            LinkedTpSpot = newLinked;
            linkedComp_LTF_TpSpot = newComp;
            this.MyLink = ToolsBuilding.Link.Linked;

            newComp.LinkedTpSpot = building;
            linkedComp_LTF_TpSpot = this;
            newComp.MyLink = ToolsBuilding.Link.Linked;

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

        public void Unlink()
        {
            if (linkedComp_LTF_TpSpot != null)
                linkedComp_LTF_TpSpot.Unlink();

            linkedComp_LTF_TpSpot = null;
            LinkedTpSpot = null;
            MyLink = ToolsBuilding.Link.Orphan;

            ResetWay();
        }
        public bool IsLinked
        {
            get
            {
                return (linkedComp_LTF_TpSpot != null);
            }
        }
        public bool IsOrphan
        {
            get
            {
                return (!IsLinked);
            }
        }
        private void ResetWay()
        {
            MyWay = Way.No;
        }
        public void ResetFacility()
        {
            BenchManaged = false;
            compPowerFacility = null;
            comp_TpBench = null;
            facility = null;
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

                Pawn pawn = thing as Pawn;
                if ((pawn != null) && (standingUser != null))
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
        private bool AddSpotItems(List<Thing> allThings, bool clearIfEmpty = true)
        {
            Tools.Warn(building.Label + " checking items", prcDebug);

            Thing thing = null;
            bool found = false;
            int pawnN = 0;

            Pawn passenger = null;
            Tools.Warn(building.Label + ":" + allThings.Count, prcDebug);

            for (int i = 0; i < allThings.Count; i++)
            {
                thing = allThings[i];
                if (thing != null)
                {
                    //Projectile projectile = thing as Projectile;
                    // Can have 2 buildings, there if myself != null, myself=parent => idgaf
                    if ((thing.def.mote != null) || thing.def.IsFilth)
                    {
                        Tools.Warn("mote or filth skip", prcDebug);
                        continue;
                    }
                    Building myself = thing as Building;
                    if ((myself != null) && (myself == building))
                    {
                        Tools.Warn("Wont self register", prcDebug);
                        continue;
                    }
                    Pawn pawn = thing as Pawn;
                    if (pawn != null)
                    {
                        passenger = pawn;
                        pawnN += 1;
                    }

                    if (!ThingList.Contains(thing))
                    {
                        AddItem(thing);

                        Tools.Warn(thing.Label + " added", prcDebug);
                    }

                    found = true;
                }
            }

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

                Tools.Warn(passenger.LabelShort + " added", prcDebug);
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
        private void SetBeginAnimLength()
        {
            beginSequenceI = beginSequenceFrameLength;
        }
        public void BeginAnimSeq()
        {
            TpOutStatus = Gfx.AnimStep.begin;
            SetBeginAnimLength();
        }
        public void IncBeginAnim(bool debug = false)
        {
            //AnimStatus(debug);
            beginSequenceI--;
            if (beginSequenceI <= 0)
            {
                TpOutNextAnim();
                SoundDef.Named("LTF_TpSpotOut").PlayOneShotOnCamera(parent.Map);
            }
            //AnimStatus(debug);
        }
        public void AnimStatus(bool debug = false)
        {
            Tools.Warn("AnimStatus=>" + TpOutStatus + ":" + beginSequenceI + "/" + beginSequenceFrameLength, debug);
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

        public void TpOutNextAnim()
        {
            //if (TpOutStatus == Gfx.AnimStep.na)TpOutStatus = Gfx.AnimStep.begin;
            if (TpOutStatus == Gfx.AnimStep.begin) TpOutStatus = Gfx.AnimStep.active;
            else if (TpOutStatus == Gfx.AnimStep.active) TpOutStatus = Gfx.AnimStep.end;
            else if (TpOutStatus == Gfx.AnimStep.end) TpOutStatus = Gfx.AnimStep.na;
            SetFrameSlower();
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

                if (HasNothing)
                    Answer ^= BuildingStatus.noItem;

                if (Tools.CapacityOverusing(currentWeight, weightCapacity))
                    Answer ^= BuildingStatus.overweight;

                if (Tools.CapacityUsing(currentCooldown))
                    Answer ^= BuildingStatus.cooldown;

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
        public bool StatusChillin { get { return HasStatus(BuildingStatus.cooldown); } }
        public bool StatusReady { get { return HasStatus(BuildingStatus.capable); } }
        public string StatusExplanation
        {
            get
            {
                string bla = string.Empty;

                if (!IsCatcher)
                {
                    if (StatusNoPower)
                    {
                        bla += " No power;";
                        return bla;
                    }
                    if (StatusNoFacility)
                    {
                        bla += " No facility;";
                        return bla;
                    }
                }
                if (StatusOverweight)
                {
                    bla += ' ' + currentWeight + "kg. >" + weightCapacity + " kg.";
                }
                if (StatusChillin)
                {
                    float coolPerc = currentCooldown / cooldownBase;
                    bla += " Cooldown: " + coolPerc.ToStringPercent("F0");
                }
                if (StatusNoItem)
                {
                    bla += " Nothing.";
                }

                if (StatusReady)
                {
                    int itemCount = RegisteredCount;
                    string grammar = ((RegisteredCount > 1) ? ("s") : (""));
                    bla = '[' + RegisteredCount + "] item" + grammar + ". " + Tools.CapacityString(currentWeight, weightCapacity) + " kg.";

                }
                bla += (IsOrphan) ? (" Orphan.") : (" Linked.");

                return bla;
            }
        }

        public bool IsCatcher
        {
            get
            {
                return (!RequiresPower);
            }
        }

        // Overrides
        public override void PostDraw()
        {
            base.PostDraw();

            if ( (buildingPos == null) || (building.Rotation != Rot4.North))
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
            if (drawUnderlay && !IsCatcher)
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
                if (TpOutBegin)
                    overlay = MyGfx.Status2OverlayMaterial(this, FactionMajority, gfxDebug);

                if (!IsCatcher && (!StatusReady) && (HasItems))
                    warning = MyGfx.Status2WarningMaterial(this, gfxDebug);

                Tools.Warn("Overlay calculating - warning: " + (warning != null) + "; anim: " + (overlay != null), gfxDebug);
            }

            // >>>> Draw mat
            // draw Underlay
            if (drawUnderlay && !IsCatcher)
            {
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

                    Gfx.DrawTickRotating(parent, underlay, 0, 0, 1, underlayAngle * 360, underlayOpacity, Gfx.Layer.under, gfxDebug);
                }

                if (underlay2 != null)
                {
                    float underlay2Opacity = Gfx.VanillaPulse(parent);
                    float underlay2Angle = Gfx.RealLinear(parent, 15 + currentWeight * 5, gfxDebug);

                    Gfx.DrawTickRotating(parent, underlay2, 0, 0, 1, underlay2Angle * 360, underlay2Opacity, Gfx.Layer.under, gfxDebug);
                }
                Tools.Warn("Underlay drew - 1: " + (underlay != null) + "; 2: " + (underlay2 != null), gfxDebug);
            }
            //draw Overlay
            if (drawOverlay)
            {
                if (!IsCatcher && !StatusReady && warning != null)
                    Gfx.PulseWarning(building, warning);

                 if ((TpOutBegin) && (overlay != null))
                {
                    float swirlSize = Gfx.VanillaPulse(parent) * 1.5f;
                    Gfx.DrawTickRotating(parent, overlay, 0, 0, swirlSize, Gfx.LoopFactorOne(parent) * 360, 1, Gfx.Layer.over, gfxDebug);
                }

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
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            //Building
            building = (Building)parent;
            MyDefName = building?.def?.label;
            buildingPos = building.DrawPos;
            buildingName = building?.LabelShort;
            RequiresPower = Props.PowerRequired;

            if (!IsCatcher)
            {
                //Building comp
                compPowerTrader = building?.TryGetComp<CompPowerTrader>();
                compAffectedByFacilities = ToolsBuilding.GetAffectedComp(building, prcDebug);
                //Facility
                facility = ToolsBuilding.GetFacility(compAffectedByFacilities, prcDebug);
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
                
            DumpProps();
        }
        public override void CompTick()
        {
            base.CompTick();

            Tools.Warn(" >>>TICK begin<<< ", prcDebug);

            if (!ToolsBuilding.CheckBuilding(building))
            {
                Tools.Warn("comp building not legit", prcDebug);
                return;
            }

            string tellMe = string.Empty;
            tellMe = Tools.OkStr(StatusReady) + "[" + TeleportCapable + "]" + buildingName + ": ";

            if (!IsCatcher)
            {
                // Power - Will return if status
                tellMe += "Power: " + Tools.OkStr(StatusNoPower) + "; ";
                if (StatusNoPower)
                {
                    if (HasRegisteredFacility)
                    {
                        if (comp_TpBench != null && building != null)
                            comp_TpBench.RemoveSpot(building);
                        ResetFacility();
                    }
                    Tools.Warn(tellMe, prcDebug);
                    return;
                }

                // Facility - Will return if status
                tellMe += "Facility: " + Tools.OkStr(StatusNoFacility) + "; ";
                if (StatusNoFacility)
                {
                    //Facility
                    facility = ToolsBuilding.GetFacility(compAffectedByFacilities, prcDebug);
                    //Facility power comp
                    compPowerFacility = facility?.TryGetComp<CompPowerTrader>();
                    comp_TpBench = facility?.TryGetComp<Comp_TpBench>();
                    tellMe += "Found: " + Tools.OkStr(HasRegisteredFacility) + ((HasRegisteredFacility) ? (facility.LabelShort) : ("nothing")) + "; ";
                }
                if (StatusNoFacility)
                {
                    Tools.Warn(tellMe, prcDebug);
                    return;
                }


                //Okk
                tellMe += "FacilityPower: " + Tools.OkStr(HasPoweredFacility);
                if (!HasPoweredFacility)
                {
                    ResetFacility();
                    compPowerFacility = facility?.TryGetComp<CompPowerTrader>();
                    Tools.Warn(tellMe, prcDebug);
                    return;
                }

                bool belongs = Dependencies.CheckBuildingBelongsFacility(compAffectedByFacilities, facility, prcDebug);
                tellMe += "Belongs to " + facility.Label + ':' + facility.ThingID + "?" + Tools.OkStr(belongs);
                if (!belongs)
                    return;

                if (!BenchManaged)
                {
                    comp_TpBench = facility?.TryGetComp<Comp_TpBench>();
                    if (comp_TpBench != null)
                    {
                        comp_TpBench.AddSpot(building);
                    }
                }
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

            if ((StatusChillin) || (StatusOverweight) || (StatusNoItem))
            {
                Tools.Warn("TICK exit bc not ready: " + tellMe, prcDebug);
                return;
            }

            if (StatusReady)
            {
                tellMe += "ready to tp " + "N:" + RegisteredCount + ":" + DumpList();
                // Work here
                //
            }

            tellMe += StatusExplanation;
            AnimStatus(prcDebug);

            if (TpOutBegin)
            {
                IncBeginAnim(prcDebug);
            }

            if (TpOutActive)
            {
                myOpacity = .6f + .4f * Rand.Range(0, 1);
            }
            else
            {
                myOpacity = 1;
            }

            Tools.Warn("TICK End: " + tellMe, prcDebug);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentCooldown, "cooldown");
            Scribe_Values.Look(ref currentWeight, "weight");
            Scribe_References.Look(ref standingUser, "user");
            Scribe_Collections.Look(ref ThingList, "things", LookMode.Reference, new object[0]);

            Scribe_References.Look(ref LinkedTpSpot, "linkedspot");
            Scribe_Values.Look(ref MyLink, "LinkStatus");
            Scribe_Values.Look(ref MyWay, "way");
            Scribe_Values.Look(ref Auto, "auto");

        }
        public override string CompInspectStringExtra()
        {
            string text = base.CompInspectStringExtra();
            string result = string.Empty;

            result += Tools.OkStr(StatusReady) + " ";
            result += StatusExplanation;

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
                    (' ' + LinkName + "->" + NextLinkName + "\nCurrent: " + Tools.PosStr(LinkedTpSpot.Position)) :
                    ("Right click with a colonist to link to another " + MyDefName)
               );

                yield return new Command_Action
                {
                    icon = myGizmo,
                    defaultLabel = myLabel,
                    defaultDesc = myDesc,
                    action = new Action(this.Unlink)
                };
            }

            if (HasPower && !IsCatcher)// && !StatusNoFacility)
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
                            BeginAnimSeq();
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
                GenDraw.DrawLineBetween(this.parent.TrueCenter(), LinkedTpSpot.TrueCenter(), SimpleColor.Cyan);
            }
        }
    }
}
