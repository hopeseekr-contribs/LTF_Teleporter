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
        List<Thing> tpThingList = new List<Thing>();
        // Can be a dead animal
        Building facility = null;
        Pawn standingUser = null;
        [Flags]
        enum BuildingStatus
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


        public bool drawVanish = false;
        public bool drawUnderlay = true;
        public bool drawOverlay = true;

        /* Debug */
        /**********/
        public bool gfxDebug = false;
        public bool prcDebug = false;


        // Props
        public CompProperties_LTF_TpSpot Props
        {
            get
            {
                return (CompProperties_LTF_TpSpot)props;
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

            if(prcDebug)
                Log.Warning(thing.LabelShort + " adds(" + plusOrMinus + ")" + newWeight + " -> " + currentWeight);
        }


        private void SetWeightBase(CompQuality comp = null)
        {
            weightCapacity = Tools.WeightedCapacity(Props.weightBase, Props.weightSpectrum, comp);
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

        //Dependency : facility
        private bool HasRegisteredFacility
        {
            get
            {
                return (facility != null);
            }
        }
        private bool HasPoweredFacility
        {
            get
            {
                return (Dependencies.TickCheckFacility(facility, compPowerFacility, prcDebug));
            }
        }
        private void ResetFacility()
        {
            facility = null;
        }

        // Check local tile
        // Items
        public int RegisteredCount
        {
            get
            {
                return tpThingList.Count;
            }
        }
        protected bool HasRegisteredItems
        {
            get
            {
                return !tpThingList.NullOrEmpty();
            }
        }
        private void ResetItems()
        {
            tpThingList.Clear();
            ResetWeight();
        }
        private bool RemoveItemsIfAbsent()
        {

            if (!HasRegisteredItems)
                return false;


            //Log.Warning(building.Label + " checks history");
            //for (int i = 0; i < tpThingList.Count; i++)
            for (int i = tpThingList.Count - 1; i >= 0; i--)
            {
                Thing thing = null;
                thing = tpThingList[i];
                if (thing == null)
                {
                    Log.Warning("lol what");
                    continue;
                }

                Pawn pawn = thing as Pawn;
                if ((pawn != null) && (standingUser != null))
                {
                    //Log.Warning(building.Label + " concerned about pawns");
                    if ((pawn != standingUser) || (pawn.Position != building.Position))
                    {
                        //Log.Warning(" reseting bc he left  or someone" + standingUser.LabelShort);
                        ResetPawn();
                    }
                }

                if (thing.Position != building.Position)
                {
                    RemoveItem(thing);
                    tpThingList.Remove(thing);
                }
            }

            return (HasRegisteredItems);
        }
        private void AddItem(Thing thing)
        {
            if (prcDebug)
                Log.Warning("Adding " + thing.Label + " to " + building.Label);

            tpThingList.Add(thing);
        }
        private void RemoveItem(Thing thing)
        {
            if (prcDebug)
                Log.Warning("Removing " + thing.Label + " from " + building.Label);

            tpThingList.Remove(thing);
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
            //Log.Warning(building.Label+" checking items");
            //if (CheckNothing(building.Position.GetThingList(building.Map)))
            if (CheckNothing(allThings))
            {
                if (clearIfEmpty)
                    ResetItems();

                return false;
            }

            Thing thing = null;
            bool found = false;
            int pawnN = 0;

            Pawn passenger = null;

            if (prcDebug)
                Log.Warning(building.Label + ":" + allThings.Count);

            for (int i = 0; i < allThings.Count; i++)
            {

                thing = allThings[i];
                if (thing != null)
                {
                    //Projectile projectile = thing as Projectile;
                    Pawn pawn = thing as Pawn;
                    // Can have 2 buildings, there if myself != null, myself=parent => idgaf
                    Building myself = thing as Building;

                    //if ((thing.def.mote != null || thing.def.IsFilth || projectile != null || myself != null))
                    if ((thing.def.mote != null || thing.def.IsFilth || myself != null))
                        continue;

                    if (pawn != null)
                    {
                        passenger = pawn;
                        pawnN += 1;
                    }

                    if (!tpThingList.Contains(thing))
                    {
                        AddItem(thing);
                        if (prcDebug)
                            Log.Warning(thing.Label + " added");
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
                //tpThingList.Clear();
                //Log.Warning("More than 1 pawn. Cant.");
            }
            else
            {
                SetPawn(passenger);
                if (prcDebug)
                    Log.Warning(passenger.LabelShort + " added");
            }

            if (!found)
            {
                ResetItems();
            }

            return found;
        }
        private bool CheckThingPresent(List<Thing> all)
        {
            return (!CheckNothing(all));
        }
        private bool CheckNothing(List<Thing> all)
        {
            return (all.NullOrEmpty());
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
        private bool HasRegisteredPawn
        {
            get
            {
                return (standingUser != null);
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
            Log.Warning(myString + val1 + " / " + val2);
        }
        private string DumpList()
        {
            string bla = String.Empty;
            foreach (Thing item in tpThingList)
            {
                bla += item.Label + ";";
            }
            return bla;
        }

        // Status management
        private BuildingStatus TeleportCapable
        {
            get
            {
                BuildingStatus Answer = BuildingStatus.na;

                if (!Dependencies.CheckPower(building))
                    Answer ^= BuildingStatus.noPower;

                if (!HasRegisteredFacility)
                    Answer ^= BuildingStatus.noFacility;

                if (!HasRegisteredItems)
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
        private bool StatusNoPower { get { return HasStatus(BuildingStatus.noPower); } }
        private bool StatusNoFacility { get { return HasStatus(BuildingStatus.noFacility); } }
        private bool StatusNoItem { get { return HasStatus(BuildingStatus.noItem); } }
        private bool StatusHasItem { get { return !StatusNoItem; } }
        private bool StatusOverweight { get { return HasStatus(BuildingStatus.overweight); } }
        private bool StatusChillin { get { return HasStatus(BuildingStatus.cooldown); } }
        private bool StatusReady { get { return HasStatus(BuildingStatus.capable); } }
        string StatusExplanation()
        {
            string bla = string.Empty;

            if (StatusNoPower)
            {
                bla += "No power; ";
                return bla;
            }
            if (StatusNoFacility)
            {
                bla += "No facility; ";
                return bla;
            }

            if (StatusOverweight)
            {
                bla += currentWeight + "kg. >" + weightCapacity + " kg. ";
            }
            if (StatusChillin)
            {
                float coolPerc = currentCooldown / cooldownBase;
                bla += "Cooldown: " + coolPerc.ToStringPercent("F0")+" ";
            }
            if (StatusNoItem)
            {
                bla += "Nothing. ";
            }

            if (StatusReady)
            {
                int itemCount = RegisteredCount;
                bla += RegisteredCount + " item" + ((RegisteredCount > 1) ? ("s") : ("")) + ". " + currentWeight + " kg. Max: " + weightCapacity + " kg. ";
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
                Log.Warning("null pos draw");
                return;
            }

            if (building.Rotation != Rot4.North)
            {
                Log.Warning("Rotation");
                return;
            }

            // nothing there standing
            if (StatusNoFacility || StatusNoPower)
            {
                if (gfxDebug)
                    Log.Warning(buildingName + " Nothing to draw: " + TeleportCapable);
                return;
            }

            Color overlayColor = Color.white;
            /*
            Material overlay = Gfx.MagentaPixel;
            Material underlay = Gfx.MagentaPixel;
            */
            Material overlay = Gfx.EmptyTile;
            //Material overlay = Gfx.aze;
            Gfx.OpacityWay opacityWay = Gfx.OpacityWay.no;

           // Material overlay = Gfx.MagentaPixel;
            string checkIf = string.Empty;

            // Nothing on tile
            if (StatusNoItem)
            {
             //   overlay = Gfx.EmptyTile;
                //                overlay = PoweredGfx;
                checkIf = "nothing gfx";
                opacityWay = Gfx.OpacityWay.loop;
            }
            // something over building
            else
            {
                opacityWay = Gfx.OpacityWay.pulse;

                if (HasRegisteredPawn)
                {
                    overlay = Gfx.PawnTile;
                    //overlayColor = Color.yellow;
                    /*
                    underlay = Gfx.YellowPixel;
                    overlay = Gfx.PawnOverTile;
                    */
                    checkIf = "pawn gfx";
                    
                }
                else
                {
                    overlay = Gfx.ItemTile;
                    //overlayColor = Color.magenta;
                    /*
                    underlay = Gfx.CyanPixel;
                    overlay = Gfx.ItemOverTile;
                    */
                    checkIf = "object gfx";
                }
            }

            if (gfxDebug) Log.Warning(checkIf);
            //Gfx.ChangeColor(overlay, overlayColor, -1);
            // Underlay if item
            /*
            if (StatusHasItem)
                if (drawUnderlay)
                    Gfx.DrawPulse(parent, underlay, MeshPool.plane10, Gfx.Layer.under, Gfx.OpacityWay.loop, gfxDebug);
            */

            // Overlay
            if (drawOverlay)
                Gfx.DrawPulse(parent, overlay, MeshPool.plane10, Gfx.Layer.over, opacityWay, gfxDebug);

            //DrawColorPulse(parent, overlay, buildingPos, mesh1x1, overlayColor);
            //Gfx.DrawPulse(parent, overlay, MeshPool.plane10, gfxDebug);

            //ChangeColor(overlay, overlayColor, PulseOpacity(parent));
            // Gfx.Draw1x1Overlay(buildingPos, overlay, mesh1x1, 1);
            //Draw1x1Overlay( buildingPos, overlay, mesh1x1, 0, 0, myColor, randRot, flick, oscil, noFlickChance, minOpacity, maxOpacity);

            if (drawVanish)
            {
                Vector3 drawPos = this.parent.DrawPos;
                Gfx.Vanish.Draw(drawPos, Rot4.North, this.parent, 0f);
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            building = (Building)parent;

            buildingPos = building.DrawPos;
            buildingName = building?.LabelShort;

            compPowerTrader = building?.TryGetComp<CompPowerTrader>();

            compQuality = building?.TryGetComp<CompQuality>();
            SetWeightBase(compQuality);
            SetCooldownBase(compQuality);

            compAffectedByFacilities = building?.TryGetComp<CompAffectedByFacilities>();

            if ((building != null) && (compAffectedByFacilities != null))
                facility = Dependencies.GetPoweredFacility(building, compAffectedByFacilities, compPowerFacility, prcDebug);

            compPowerFacility = facility?.TryGetComp<CompPowerTrader>();

            //DumpProps();
        }
        public override void CompTick()
        {
            base.CompTick();

            if (!Dependencies.CheckBuilding(building))
            {
                Log.Warning("Impossibru");
                return;
            }

            string tellMe = string.Empty;

            tellMe = buildingName + "(" + TeleportCapable + "): ";

            // Will return if status
            //Kooo
            if (StatusNoPower || StatusNoFacility)
            {
                if (StatusNoPower)
                {
                    tellMe += "no power; ";
                }

                if (StatusNoFacility)
                {
                    tellMe += "no facility; ";
                    facility = Dependencies.GetPoweredFacility(building, compAffectedByFacilities, compPowerFacility, prcDebug);
                    if (HasRegisteredFacility)
                    {
                        tellMe += "but found" + facility.LabelShort;
                    }
                    else
                    {
                        tellMe += "but cant find any";
                    }
                    
                }
                if (prcDebug)
                    Log.Warning(tellMe);

                return;
            }

            //Okk
            if (!HasPoweredFacility)
            {
                compPowerTrader = building?.TryGetComp<CompPowerTrader>();
                if (compPowerTrader == null)
                {
                    ResetFacility();
                    return;
                }
            }

            CheckItems();


                

            if (StatusReady)
            {
                tellMe += "ready to tp " + "N:" + RegisteredCount + ":" + DumpList();

                // Work here
                //
            }

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

            if (prcDebug)
                Log.Warning(tellMe);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentCooldown, "cooldown");
            Scribe_Values.Look(ref currentWeight, "weight");
            Scribe_References.Look(ref standingUser, "user");
            Scribe_Collections.Look(ref tpThingList, "things", LookMode.Reference, new object[0]);
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
                if (HasRegisteredItems)
                {
                    // TP command there
                    //ForceCooldown();
                }

                if (Prefs.DevMode)
                {
                    /*
                    yield return new Command_Action
                    {
                        defaultLabel = "bad capa" + weightCapacity * .5f,
                        defaultDesc = "wc:"+weightCapacity,
                        action = delegate
                        {
                            FullHax(weightCapacity, weightCapacity * .5f);
                        }
                    };

                    if(weightCapacity<40)
                    yield return new Command_Action
                    {
                        defaultLabel = "reset weight",
                        action = delegate
                        {
                            SetWeightCapacity();
                        }
                    };

                    yield return new Command_Action
                    {
                        defaultLabel = "bad cooldown" + cooldownBase * 2f,
                        defaultDesc = "cb:" + cooldownBase,
                        action = delegate
                        {
                            FullHax(cooldownBase, cooldownBase * 2f);
                        }
                    };

                    if(cooldownBase>10000)
                    yield return new Command_Action
                    {
                        defaultLabel = "reset cool base",
                        action = delegate
                        {
                            SetCooldownBase();
                        }
                    };

    */
                    if (StatusReady)
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = "cool "+currentCooldown+"->"+currentCooldown,
                            defaultDesc = "c:" + currentCooldown,
                            action = delegate
                            {
                                ForceCooldown();
                            }
                        };
                    }
                    else
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = "reset cooldown",
                            defaultDesc = "c:" + currentCooldown,
                            action = delegate
                            {
                                ResetCooldown();
                            }
                        };
                    }

                    if (gfxDebug)
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = "vanish "+ drawVanish + "->" + !drawVanish,
                            action = delegate
                            {
                                drawVanish = !drawVanish;
                            }
                        };
                        yield return new Command_Action
                        {
                            defaultLabel = "under "+drawUnderlay+"->"+!drawUnderlay,
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
                    

                    yield return new Command_Action
                    {
                        defaultLabel = "gfx Debug"+gfxDebug+"->"+!gfxDebug,
                        action = delegate
                        {
                            gfxDebug = !gfxDebug;
                        }
                    };
                    yield return new Command_Action
                    {
                        defaultLabel = "prc Debug" + prcDebug + "->" + !prcDebug,
                        action = delegate
                        {
                            prcDebug =! prcDebug;
                        }
                    };

                }
            }
        }
    }
}
