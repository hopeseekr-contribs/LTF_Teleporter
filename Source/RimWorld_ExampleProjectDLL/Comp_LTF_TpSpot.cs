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

        /* Comp */
        /******************/
        public CompPowerTrader compPowerTrader;
        
        public CompQuality compQuality;
        // TpBench required
        public CompAffectedByFacilities compAffectedByFacilities;
        public CompPowerTrader compPowerFacility;
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
        [Flags]
        public enum BuildingStatus
        {
            na          =0,

            noPower     =1,
            noFacility  =2,
            noItem      =4,
            overweight  =8,
            cooldown    =16,
            //x2
            noPowerNoFaci= noPower | noFacility,
            noPowerNoItem= noPower | noItem,
            noPowerOverweight= noPower | overweight,
            noPowerCooldown= noPower | cooldown,

            noFacilityNoitem = noFacility|noItem,
            noFacilityoverweight = noFacility|overweight,
            noFacilityCooldown = noFacility | cooldown,

            noItemOveW = noItem | overweight,
            noItemCooldown= noItem | cooldown,

            Overweight= overweight | cooldown,
            //x3
            noPowernoFacilityNoItem = noPower | noFacility | noItem,
            noPowerNoFacilityOverweight= noPower | noFacility | overweight,
            noPowernoFacilityCooldown= noPower | noFacility | cooldown,

            noFacilityNoitemOverweight= noFacility | noItem |overweight,
            noFacilityNoitemCooldown= noFacility | noItem|cooldown,

            noItemOverWCooldown= noItem | overweight |cooldown,
            //x4
            powerOk = noFacility | noItem|overweight | cooldown,
            facilityOk=noPower | noItem|overweight | cooldown,
            itemOk=noPower | noFacility|overweight | cooldown,
            weightOk=noPower | noFacility | noItem| cooldown,
            cooldownOk=noPower | noFacility | noItem|overweight,

            //x5
            allWrong = overweight | cooldown | noPower | noFacility | noItem,

            capable=64,
        };

        bool FactionMajority = false;

        public bool drawVanish = false;
        public bool drawUnderlay = true;
        public bool drawOverlay = true;

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
            currentCooldown = Mathf.Min(cooldownBase, currentCooldown);
        }
        private void BetterQuality()
        {
            ChangeQuality(true);
        }
        private void WorseQuality()
        {
            ChangeQuality(false);
        }

        //Dependency :Cooldown
        private bool IsChilling()
        {
            //return (currentCooldown != 0);
            return Tools.CapacityUsing(currentCooldown);
        }
        private void SetCooldownBase(CompQuality comp = null)
        {
            cooldownBase = Tools.WeightedCapacity(Props.cooldownBase, Props.cooldownSpectrum, comp);
        }
        private void ResetCooldown()
        {
            Tools.CapacityReset(currentCooldown);
        }
        private void ForceCooldown()
        {
            currentCooldown=cooldownBase;
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
                return (Dependencies.TickCheckFacilityPower(facility, compPowerFacility, prcDebug));
            }
        }
        private void ResetFacility()
        {
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

                if (thing.Position != building.Position)
                {
                    RemoveItem(thing);
                    ThingList.Remove(thing);
                }
            }

            FactionMajority=(neededAverageFaction <= 0) ;

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
            Tools.Warn(building.Label+" checking items", prcDebug);

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
                    if ((thing.def.mote != null )|| thing.def.IsFilth)
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
                return (standingUser != null) ;
            }
        }
        public bool HasAnimal
        {
            get
            {
                return (HasRegisteredPawn && (!standingUser.RaceProps.Humanlike) && (!standingUser.RaceProps.IsMechanoid) );
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
        

        //Gfx interface
        public void StopVanish()
        {
            drawVanish = false;
        }
        //public void StartVanish(){drawVanish = true;}

        // Debug 
        private void DumpProps(float val1, float val2, string myString = "bla: ")
        {
            Tools.Warn(myString + val1 + " / " + val2, prcDebug);
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
        public bool StatusNoFacility { get { return HasStatus(BuildingStatus.noFacility); } }
        private bool StatusNoItem { get { return HasStatus(BuildingStatus.noItem); } }
        private bool StatusHasItem { get { return !StatusNoItem; } }
        public bool StatusOverweight { get { return HasStatus(BuildingStatus.overweight); } }
        public bool StatusChillin { get { return HasStatus(BuildingStatus.cooldown); } }
        private bool StatusReady { get { return HasStatus(BuildingStatus.capable); } }
        string StatusExplanation()
        {
            string bla = string.Empty;

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

            if (StatusOverweight)
            {
                bla += ' '+currentWeight + "kg. >" + weightCapacity + " kg.";
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
                bla += ' '+RegisteredCount + " item" + ((RegisteredCount > 1) ? ("s") : ("")) + ". " + currentWeight + " kg. Max: " + weightCapacity + " kg.";
            }

            bla=bla.Trim();

            return bla;
        }

        // Overrides
        public override void PostDraw()
        {
            base.PostDraw();

            if (buildingPos == null)
            {
                Tools.Warn("null pos draw", gfxDebug);
                return;
            }
            if (building.Rotation != Rot4.North)
            {
                Tools.Warn("Rotation", gfxDebug);
                return;
            }
            // nothing there standing
            if (StatusNoPower)
            {
                Tools.Warn(buildingName + " Nothing to draw: " + TeleportCapable, gfxDebug);
                return;
            }

            Tools.Warn(
                " pulse: " + Gfx.PulseFactorOne(parent) * 360 +
                "; Loop: " + Gfx.LoopFactorOne(parent) * 360 +
                "; %real:" + (Gfx.RealLinear(parent, currentWeight, 1, gfxDebug) * 360)
                , gfxDebug);
            

            Material overlay = null;
            Material underlay = null;
            Material warning = null;

            if(drawUnderlay)
                underlay = MyGfx.Status2UnderlayMaterial(this, gfxDebug);

            if (drawOverlay)
            {
                if(StatusReady)
                {
                    //Tools.Warn("gfx : : :::::::: Ready", gfxDebug);
                    overlay = MyGfx.Status2OverlayMaterial(this, FactionMajority, gfxDebug);
                }
                else
                {
                    //Tools.Warn("gfx: : ::::: :::::: not readdyyyyy ffs", gfxDebug);
                    warning = MyGfx.Status2WarningMaterial(this, gfxDebug);
                }
            }

            //MyGfx.OpacityWay opacityWay = MyGfx.OpacityWay.no;

            // Underlay
            float underlayAngle = ((!HasItems) ? Gfx.RealLinear(parent, 1, 10+Rand.Range(0,3), gfxDebug) : (Gfx.PulseFactorOne(parent) ));
            if (drawUnderlay && underlay != null)
                Gfx.DrawTickRotating(parent, underlay, 0, 0, underlayAngle * 360, 1, Gfx.Layer.under, gfxDebug);
            //Gfx.DrawTickRotating(parent, underlay, 0, 0, underlayAngle*360, Gfx.VanillaPulse(parent),Gfx.Layer.under, gfxDebug);
            //    Gfx.DrawPulse(parent, underlay, MeshPool.plane10, Gfx.Layer.under, Gfx.OpacityWay.loop, gfxDebug);


            //Gfx.DrawRandRotating(parent, underlay, 0, 0, Gfx.Layer.under, gfxDebug);

            //Overlay
            if (drawOverlay)
            {
                if (!StatusReady && warning != null)
                    Gfx.PulseWarning(building, warning);

                if (StatusHasItem && overlay != null)
                    Gfx.DrawTickRotating(parent, overlay, 0, 0, Gfx.LoopFactorOne(parent)*360, 1, Gfx.Layer.over, gfxDebug);
            }

            if (drawVanish)
            {
                Vector3 drawPos = this.parent.DrawPos;
                MyGfx.Vanish.Draw(drawPos, Rot4.North, this.parent, 0f);
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            //Building
            building = (Building)parent;
            buildingPos = building.DrawPos;
            buildingName = building?.LabelShort;
            //Building comp
            compPowerTrader = building?.TryGetComp<CompPowerTrader>();
            compQuality = building?.TryGetComp<CompQuality>();
            compAffectedByFacilities = ToolsBuilding.GetAffectedComp(building, prcDebug);

            //Facility
            facility = ToolsBuilding.GetFacility( compAffectedByFacilities,  prcDebug);
            //Facility power comp
            compPowerFacility = facility?.TryGetComp<CompPowerTrader>();

            SetWeightBase(compQuality);
            SetCooldownBase(compQuality);
            //DumpProps();
        }
        public override void CompTick()
        {
            base.CompTick();

            if (!ToolsBuilding.CheckBuilding(building))
            {
                Tools.Warn("comp building not legit", prcDebug);
                return;
            }

            string tellMe = string.Empty;
            tellMe = Tools.OkStr(StatusReady) + "[" + TeleportCapable + "]"+ buildingName + ": ";
            
            // Power - Will return if status
            tellMe += "Power: " + Tools.OkStr(StatusNoPower)+"; ";
            if (StatusNoPower) {
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
                tellMe += "Found: " + Tools.OkStr(HasRegisteredFacility) + ((HasRegisteredFacility)?(facility.LabelShort) :("nothing"))+ "; ";
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
                compPowerFacility = facility?.TryGetComp<CompPowerTrader>();
                Tools.Warn(tellMe, prcDebug);
                return;
            }

            bool belongs = Dependencies.CheckBuildingBelongsFacility(compAffectedByFacilities, facility, prcDebug);
            tellMe += "Belongs to " + facility.Label + "?" + Tools.OkStr(belongs);
            if (!belongs)
                return;

            

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

            if ((StatusChillin) || (StatusOverweight) || (StatusNoItem)) {
                Tools.Warn(tellMe, prcDebug);
                return;
            }
            
            

            if (StatusReady)
            {
                tellMe += "ready to tp " + "N:" + RegisteredCount + ":" + DumpList();
                // Work here
                //
            }

            Tools.Warn(tellMe, prcDebug);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentCooldown, "cooldown");
            Scribe_Values.Look(ref currentWeight, "weight");
            Scribe_References.Look(ref standingUser, "user");
            Scribe_Collections.Look(ref ThingList, "things", LookMode.Reference, new object[0]);
        }
        public override string CompInspectStringExtra()
        {
            string text = base.CompInspectStringExtra();
            string result = string.Empty;

            result += ((StatusReady) ? ("[Ok]") : ("[Ko]")) + " ";
            result += StatusExplanation();

            if (!text.NullOrEmpty())
            {
                result = "\n" + text;
            }

            return result;
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if ( !StatusNoPower && !StatusNoFacility)
            {
                if (HasItems)
                {
                    // TP command there
                    // Status cooldown
                    // Status weight
                }

                if (Prefs.DevMode)
                {
                    // Debug process
                    yield return new Command_Action
                    {
                        icon = ((prcDebug) ? (MyGfx.DebugOnGz) : (MyGfx.DebugOffGz)),
                        defaultLabel = "prc: "+Tools.DebugStatus(prcDebug),
                        defaultDesc = "process debug",
                        action = delegate
                        {
                            prcDebug = Tools.WarnBoolToggle(prcDebug, "debug " + building.Label);
                        }
                    };
                    // Debug gfx
                    yield return new Command_Action
                    {
                        icon = ((gfxDebug) ? (MyGfx.DebugOnGz) : (MyGfx.DebugOffGz)),
                        defaultLabel = "gfx: "+Tools.DebugStatus(gfxDebug),
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
                            defaultLabel = "vanish " + drawVanish + "->" + !drawVanish,
                            action = delegate
                            {
                                drawVanish = !drawVanish;
                            }
                        };
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
                    if(prcDebug)
                    yield return new Command_Action
                    {
                        //icon = ContentFinder<Texture2D>.Get("UI/Commands/HaxReady", true),
                        icon = MyGfx.DebugLogGz,
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
                        if (currentCooldown != 0)
                            yield return new Command_Action
                            {
                                icon = MyGfx.HaxEmptyGz,
                                defaultLabel = currentCooldown + "->" + cooldownBase,
                                defaultDesc = "force cooldown",
                                action = delegate
                                {
                                    ForceCooldown();
                                }
                            };
                        yield return new Command_Action
                        {
                            icon = MyGfx.HaxFullGz,
                            defaultLabel = currentCooldown + "->0" ,
                            defaultDesc = "reset cooldown",
                            action = delegate
                            {
                                ResetCooldown();
                            }
                        };

                        int minus10perc = (int)Mathf.Max(0, (currentCooldown - cooldownBase / 10));
                        int plus10perc = (int)Mathf.Min(cooldownBase, (currentCooldown + cooldownBase / 10));

                        yield return new Command_Action
                        {
                            icon = MyGfx.HaxSubGz,
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
                            icon = MyGfx.HaxAddGz,
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
                    if (prcDebug &&Hax && (compQuality != null))
                    {
                        if (!ToolsQuality.BestQuality(compQuality))
                            yield return new Command_Action
                            {
                                defaultLabel = compQuality.Quality.GetLabelShort() + "->" + ToolsQuality.BetterQuality(compQuality),
                                defaultDesc = "Better quality",
                                //icon = ContentFinder<Texture2D>.Get("UI/Commands/HaxReady", true),
                                icon = MyGfx.HaxBetterGz,
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
                                icon = MyGfx.HaxWorseGz,
                                action = delegate
                                {
                                    WorseQuality();
                                }
                            };
                    }
                }
            }
        }
    }
}
