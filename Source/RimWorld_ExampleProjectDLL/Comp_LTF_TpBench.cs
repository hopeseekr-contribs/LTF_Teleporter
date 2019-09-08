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
    // Main
    [StaticConstructorOnStartup]
    public class Comp_TpBench : ThingComp
    {
        Building building = null;
        Vector3 buildingPos;
        String buildingName = string.Empty;

        String TpSpotName = string.Empty;
        int FacilityCapacity = 0;
        int GizmoIndex = 0;
        /* Comp */
        /******************/
        private CompPowerTrader compPower;
        public CompQuality compQuality;
        private CompFacility compFacility;
        
        //private float benchRadius = 35.7f;

        private List<Building> Registry = new List<Building>();

        // reach spots
        public float range = 0f;
        public float moreRange = 0f;

        bool prcDebug = false;
        bool gfxDebug = false;
        bool Hax = false;
        // Props
        public CompProperties_TpBench Props
        {
            get
            {
                return (CompProperties_TpBench)props;
            }
        }

        // get power comp
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            //Building
            building = (Building)parent;
            buildingPos = building.DrawPos;
            buildingName = building?.LabelShort;
            TpSpotName = Tools.LabelByDefName("LTF_TpSpot", prcDebug);
            //Building comp
            compPower = building?.TryGetComp<CompPowerTrader>();
            compQuality = building?.TryGetComp<CompQuality>();
            compFacility = building?.TryGetComp<CompFacility>();
            range = compFacility?.Props.maxDistance ?? 0f;
            SetMoreRange();

            WeightFacilityCapacity(compQuality);
        }

        /*
        public void DeSpawn()
        {
            base.DeSpawn();
            this.ResetCurrentTarget();
        }
        */

        private bool IsMiniStation
        {
            get
            {
                return (building.def.defName == "LTF_MiniStation");
            }
        }

        private void SetMoreRange(CompQuality comp = null)
        {
            if (comp == null) if ((comp = compQuality) == null) return;

            moreRange = ToolsQuality.FactorCapacity(Props.moreRangeBase, Props.moreRange, comp, false, false, false, prcDebug);
            //moreRange = ToolsQuality.FactorCapacity(Props.moreRangeBase, Props.moreRange, comp, false, false, false, true);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look<Building>(ref Registry, "tpSpots", LookMode.Reference, new object[0]);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.Registry == null)
            {
                this.Registry= new List<Building>();
            }
            Scribe_Values.Look(ref GizmoIndex, "index");
        }

        public bool MoreThanOne
        {
            get
            {
                return (!IsEmpty && Registry.Count > 1);
            }
        }
        public bool IsFull
        {
            get
            {
                return ((!IsEmpty) && (Registry.Count >= FacilityCapacity));
            }
        }
        public bool IsEmpty
        {
            get
            {
                return (Registry.NullOrEmpty());
            }
        }
        public bool HasSpot
        {
            get
            {
                return (!IsEmpty);
            }
        }

        public Building CurrentSpot
        {
            get
            {
                if (GizmoIndex >= Registry.Count)
                    return null;

                return Registry[GizmoIndex];
            }
        }

        public void RemoveSpot(Building target)
        {
            if ((target == null) || (target.def.defName != "LTF_TpSpot"))
                Tools.Warn("Trying to remove a non tp spot", prcDebug);

            Registry.Remove(target);
            IndexCorrecter();
        }
        public void IndexCorrecter()
        {
            GizmoIndex = Tools.LimitToRange(GizmoIndex, 0, Registry.Count - 1);
        }
        
        public void AddSpot(Building target)
        {
            if ((target == null) || (target.def.defName != "LTF_TpSpot"))
                Tools.Warn("Trying to register a non tp spot", prcDebug);


            if (Registry.Contains(target))
            {
                return;
            }
            Registry.Add(target);

        }

        public bool GotThePower
        {
            get
            {
                return (ToolsBuilding.CheckPower(building));
            }
        }
        public bool HasQuality
        {
            get
            {
                return (compQuality != null);
            }
        }
        private void WeightFacilityCapacity(CompQuality comp, bool debug=false)
        {
            Tools.Warn(">Settin Quality>" + Props.FacilityCapacityBase + ';' + Props.FacilityCapacitySpectrum + ">FacilityCapacity>" + FacilityCapacity, debug);
            FacilityCapacity = (int)ToolsQuality.WeightedCapacity(Props.FacilityCapacityBase, Props.FacilityCapacitySpectrum, comp);
        }

        private void NextIndex()
        {
            GizmoIndex = Tools.NextIndexRoundBrowser(GizmoIndex, Registry.Count);
        }

        public void ShowReport()
        {
            StringBuilder stringBuilder = new StringBuilder();
            String buffer = string.Empty;

            stringBuilder.AppendLine("| Workstation registry |");
            stringBuilder.AppendLine("+-------------------------+");
            if (!Registry.NullOrEmpty())
                stringBuilder.AppendLine(">>> " + Registry.Count.ToString("D2") + " records."+"\n");
            else
                stringBuilder.AppendLine("Empty.");

            if (!Registry.NullOrEmpty())
            {
                int i = 1;
                foreach (Building cur in Registry)
                {
                    string report = string.Empty;

                    report = i.ToString("D2")+". " + cur.LabelShort + Tools.PosStr(cur.Position);
                    
                    Comp_LTF_TpSpot compSpot = cur?.TryGetComp<Comp_LTF_TpSpot>();
                    if ((compSpot != null) && compSpot.IsLinked)
                    {
                        // " <=> "
                        report += " " + compSpot.WayArrowLabeling  + " ";
                        report += compSpot.twin.Label + Tools.PosStr(compSpot.twin.Position);
                    }
                    else
                    {
                        report += " Orphan spot!";
                    }

                    stringBuilder.AppendLine(report);
                    i++;
                }
            }

            Dialog_MessageBox window = new Dialog_MessageBox(stringBuilder.ToString(), null, null, null, null, null, false);
            Find.WindowStack.Add(window);
        }
        // Interface quality
        private void ChangeQuality(bool better = true)
        {
            ToolsQuality.ChangeQuality(building, compQuality, better);
            WeightFacilityCapacity(compQuality);
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
            //Tools.WeightedCapacity(Props.facilityCapacityBase, Props.facilityCapacitySpectrum, compQuality);
            Answer = "Registry capacity: " + FacilityCapacity
            + "\nRange: " + range
            + "\nAssist range: " + moreRange;

            return Answer;
        }

        private float Bench2SpotDistance(Building spot)
        {
            float Answer = 999f;
            if (!ToolsBuilding.CheckBuilding(spot))
                return Answer;

            Answer = building.Position.DistanceTo(spot.Position);
            return Answer;
        }
        bool InRangeSpot(Building spot)
        {
            bool Answer = false;
            if (Bench2SpotDistance(spot) < range)
                return true;

            return Answer;
        }
        public override void CompTick()
        {
            base.CompTick();

            //Tools.Warn(" >>>TICK begin<<< ", prcDebug);
            Tools.Warn(" >>>TICK begin tp:"+ Registry.Count + "<<< ");
            if (HasSpot)
            {
                if (Tools.TwoTicksOneTrue())
                {
                    //foreach(Building cur in Registry)
                    for (int i = Registry.Count-1; i>=0; i--)
                    {
                        Building cur = Registry[i];
                        if( (!ToolsBuilding.CheckBuilding(cur)) || (!ToolsBuilding.CheckPower(cur)) || !InRangeSpot(cur)){
                            Comp_LTF_TpSpot comp_LTF_TpSpot  = cur?.TryGetComp<Comp_LTF_TpSpot>();
                            if(comp_LTF_TpSpot!=null)
                                comp_LTF_TpSpot.ResetFacility();
                            RemoveSpot(cur);
                        } 
                    }
                }
                IndexCorrecter();
            }


            // Tools.Warn(" >>>TICK end<<< ", prcDebug);
            Tools.Warn(" >>>TICK end "+ Registry.Count + "<<< ");
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

            if (GotThePower)
            {
                {
                    Texture2D myMat = MyGizmo.EmptyStatus2Gizmo(IsEmpty, IsFull);
                    String myLabel = "Registry";
                    String Grammar = ((MoreThanOne) ? ("s") : (""));
                    String myDesc = Tools.CapacityString(Registry.Count, FacilityCapacity) + ' ' + TpSpotName + ".";

                    if (IsEmpty || IsFull)
                    {
                        myDesc += ".Registry is " + ((IsEmpty) ? ("empty") : ("full"));
                        if (IsEmpty)
                            myDesc += "\nBuild a "+ TpSpotName + " in its range.";
                        if (IsFull)
                            myDesc += "\nNo additional " + TpSpotName + " will be managed.";
                    }
                    myDesc += "\nLists " + Registry.Count + " teleporter" + Grammar;
                    yield return new Command_Action
                    {
                        icon = myMat,
                        defaultLabel = myLabel,
                        defaultDesc = myDesc,
                        action = new Action(this.ShowReport),
                    };
                }

                if (HasSpot)
                {
                    Comp_LTF_TpSpot comp = CurrentSpot?.TryGetComp<Comp_LTF_TpSpot>();
                    if (comp != null)
                    {
                        if (comp.ValidWay)
                        {
                            String WayName = comp.WayNaming;
                            Texture2D myGizmo = null;

                            //if (comp.StatusReady && comp.IsLinked && !comp.compTwin.StatusChillin)
                            /*
                            if (comp.StatusReady && comp.IsLinked)
                                myGizmo = comp.WayGizmoing;
                            else
                                myGizmo = comp.IssueGizmoing;
                            */
                            myGizmo = comp.WayGizmoing;

                            if(comp.IsLinked)
                            if (comp.compTwin.StatusChillin)
                                myGizmo = comp.compTwin.IssueGizmoing;

                            String myLabel = "Cast "+WayName;
                            String myDesc = comp.WayDescription;
                            //+ "\n" + comp.StatusLogNoUpdate;
                            //Action todo = ShowReport;
                            Action todo = delegate
                            {
                                Tools.Warn("rip action on no way", prcDebug);
                            };

                            if (comp.IsOrphan || comp.StatusChillin || comp.compTwin.StatusChillin)
                            {
                                if (comp.IsOrphan)
                                    myDesc = "Selected spot is orphan. You need to link it to another.";
                                else if (comp.StatusChillin)
                                    myDesc = "Selected spot has some cooldown.";
                                else if (comp.compTwin.StatusChillin)
                                    myDesc = "Selected spot twin has some cooldown.";
                            }
                            else if (comp.MyWay == Comp_LTF_TpSpot.Way.Out)
                                todo = comp.OrderOut;
                            else if (comp.MyWay == Comp_LTF_TpSpot.Way.In)
                                todo = comp.OrderIn;
                            //todo = comp.compTwin.OrderOut;
                            //todo = comp.OrderIn;
                            else if (comp.MyWay == Comp_LTF_TpSpot.Way.Swap)
                                todo = comp.OrderSwap;

                            /*
                            else if (comp.MyWay == Comp_LTF_TpSpot.Way.No)
                                todo = ShowReport;
                            */

                            yield return new Command_Action
                            {
                                icon = myGizmo,
                                defaultLabel = myLabel,
                                defaultDesc = myDesc,
                                action = new Action(todo),
                            };

                        }
                        else Tools.Warn("gizmo should not be this way", prcDebug);
                    }
                    else Tools.Warn("gizmo should not be this way", prcDebug);
                }

                Tools.Warn("Gizmo browse records", prcDebug);
                if (MoreThanOne)
                {
                    Texture2D myMat = MyGizmo.NextTpGz;
                    String myLabel = Tools.CapacityString(GizmoIndex + 1, Registry.Count) + 
                            Tools.PosStr(CurrentSpot.Position);
                    //String Grammar = ((MoreThanOne) ? ("s") : (""));
                    String myDesc = "Browse " + Registry.Count + " records";
                    yield return new Command_Action
                    {
                        icon = myMat,
                        defaultLabel = myLabel,
                        defaultDesc = myDesc,
                        action = new Action(NextIndex),
                    };
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

        public override string CompInspectStringExtra()
        {
            if (!GotThePower)
                return null;

            if(Registry.NullOrEmpty())
                return "Empty registry.";

            return "Check registry(" + Registry.Count.ToString("D2") + " record" + ((MoreThanOne)?("s"):"") + ").";
        }

        public override void PostDrawExtraSelectionOverlays()
        {

            // Draw range circle
            if (range > 0f)
            {
                float CircleRadius = 0f;

                //CircleRadius = ((IsMiniStation)?():());
                if (IsMiniStation)
                    CircleRadius = 1;
                else
                    CircleRadius = (range / 2) + 1;

                GenDraw.DrawRadiusRing(this.parent.Position, CircleRadius);
            }

            // Not drawing if bench is empty or has no power
            if ((Registry.NullOrEmpty()) || (!GotThePower))
                return;

            // selected spot comp
            Comp_LTF_TpSpot comp = CurrentSpot?.TryGetComp<Comp_LTF_TpSpot>();
            if (comp == null)
                return;

            // not drawing lines with spot without power/facility
            if ((comp.requiresPower && !comp.HasPower))
                return;
            if ((comp.requiresBench && !comp.HasPoweredFacility))
                return;

            // Line from workstation to spot (in range)
            GenDraw.DrawLineBetween(this.parent.TrueCenter(), CurrentSpot.TrueCenter(), comp.WayColoring);
            if (comp.IsLinked)
            {
                // Line from spot to spot
                GenDraw.DrawLineBetween(CurrentSpot.TrueCenter(), comp.twin.TrueCenter(), comp.WayColoring);
                // Wish we could make it more noticeable
            }
        }
    }
}