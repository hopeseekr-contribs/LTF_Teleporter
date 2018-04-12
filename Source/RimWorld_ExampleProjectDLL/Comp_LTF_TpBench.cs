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
        private CompPowerTrader powerComp;
        public CompQuality compQuality;

        //private float benchRadius = 35.7f;

        private List<Building> Registry = new List<Building>();

        private const float defaultWorkAmount = 3600f; // 120sec = 60  * 120 = 7200 
        private float workGoal = defaultWorkAmount;
        private float workProgress = 0;

        private bool mindcontrolEnabled = false;
        //bool mindReachable = false;

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
        public override void PostDraw()
        {
            base.PostDraw();

            if (!GotThePower)
            {
                Tools.Warn("no power no overlay", false);
                return;
            }

            Vector3 benchPos = this.parent.DrawPos;
            if (benchPos == null)
            {
                Tools.Warn("null bench pos draw", prcDebug);
                return;
            }
            // higher than ground to be visible ; maybe too high ?
            benchPos.y += 4;

            float progressSize = (ProgressToRegister > 1f) ? (1f) : (ProgressToRegister * 1.5f);

            //if (mindcontrolEnabled)
            //DrawPulse((Thing)parent, Gfx.readyMat, benchPos);

            /*
            if (!IsWorkDone)
            {

            }
            */

            // work progress bars
            /*
            int neededWorkBarN = Mathf.RoundToInt(ProgressToRegister * Gfx.workBarNum);
            if (neededWorkBarN > 1)
            {
                if (neededWorkBarN > Gfx.workBarNum) neededWorkBarN = Gfx.workBarNum;

                for (int i = 1; i < neededWorkBarN + 1; i++)
                {
                    Gfx.DrawBar(benchPos, -1.5f, .12f, i);
                }
            }
            */
        }
        // progress in work
        public float ProgressToRegister
        {
            get
            {
                return workProgress / workGoal;
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
            powerComp = building?.TryGetComp<CompPowerTrader>();
            compQuality = building?.TryGetComp<CompQuality>();

            SetFacilityCapacity(compQuality);
        }

        /*
        public void DeSpawn()
        {
            base.DeSpawn();
            this.ResetCurrentTarget();
        }
        */

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look<Building>(ref Registry, "tpSpots", LookMode.Reference, new object[0]);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.Registry == null)
            {
                this.Registry= new List<Building>();
            }
        }
        public void MindMineTick(Pawn masterMind)
        {
            workProgress += 1;

            if (this.workProgress > workGoal)
            {
                mindcontrolEnabled = true;
            }
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

        public void RemoveSpot(Building target)
        {
            if ((target == null) || (target.def.defName != "LTF_TpSpot"))
                Tools.Warn("Trying to remove a non tp spot", prcDebug);

            Registry.Remove(target);
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

        public void InitWork()
        {
            workProgress = 0;
        }

        public void ResetProgress()
        {
            workProgress = 0;
            mindcontrolEnabled = false;
        }
        public bool IsWorkDone
        {
            get
            {
                return (mindcontrolEnabled);
            }
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
        private void SetFacilityCapacity(CompQuality comp, bool debug=false)
        {
            Tools.Warn(">Settin Quality>" + Props.FacilityCapacityBase + ';' + Props.FacilityCapacitySpectrum + ">FacilityCapacity>" + FacilityCapacity, debug);
            FacilityCapacity = (int)Tools.WeightedCapacity(Props.FacilityCapacityBase, Props.FacilityCapacitySpectrum, comp);
        }

        private void NextIndex()
        {
            GizmoIndex = Tools.NextIndexRoundBrowser(GizmoIndex, Registry.Count);
        }

        public void ShowReport()
        {
            StringBuilder stringBuilder = new StringBuilder();
            String buffer = string.Empty;

            stringBuilder.AppendLine("| Worktation logs |");
            stringBuilder.AppendLine("+----------------+");

            if (!Registry.NullOrEmpty())
            {
                foreach (Building cur in Registry)
                {
                    stringBuilder.AppendLine(cur.Label);
                }
            }
            Dialog_MessageBox window = new Dialog_MessageBox(stringBuilder.ToString(), null, null, null, null, null, false);
            Find.WindowStack.Add(window);
        }
        // Interface quality
        private void ChangeQuality(bool better = true)
        {
            ToolsQuality.ChangeQuality(building, compQuality, better);
            SetFacilityCapacity(compQuality);
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
            Answer = "Registry capacity: " + FacilityCapacity;

            return Answer;
        }

        public override void CompTick()
        {
            base.CompTick();

            Tools.Warn(" >>>TICK begin<<< ", prcDebug);
            if (HasSpot)
            {

                if (Tools.TwoTicksOneTrue())
                {
                    foreach(Building cur in Registry)
                    {
                        if( (!ToolsBuilding.CheckBuilding(cur)) || (!ToolsBuilding.CheckPower(cur)) ){
                            Comp_LTF_TpSpot comp_LTF_TpSpot  = cur?.TryGetComp<Comp_LTF_TpSpot>();
                            if(comp_LTF_TpSpot!=null)
                                comp_LTF_TpSpot.ResetFacility();
                            Registry.Remove(cur);
                        } 
                    }

                }
            }

            Tools.Warn(" >>>TICK end<<< ", prcDebug);
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
                        myDesc += " Registry is " + ((IsEmpty) ? ("empty") : ("full"));
                        if (IsEmpty)
                            myDesc += " It wont help managing any.";
                        if (IsFull)
                            myDesc += " No additional " + TpSpotName + " will be managed.";
                    }
                    myDesc += "\nLists " + Registry.Count + " remote space manipulator" + Grammar + ". ok teleporter" + Grammar + ".";
                    yield return new Command_Action
                    {
                        icon = myMat,
                        defaultLabel = myLabel,
                        defaultDesc = myDesc,
                        action = new Action(this.ShowReport),
                    };
                }
                
                if (MoreThanOne)
                {
                    Texture2D myMat = MyGizmo.NextTpGz;
                    String myLabel = Tools.CapacityString(GizmoIndex+1, Registry.Count)+" - "+Tools.PosStr(Registry[GizmoIndex].Position);
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

            string report = string.Empty;
            string spotName = Registry.RandomElement().def.label;

            report = ">"+Registry.Count+"<" + ' ' + spotName + ": ";
            foreach (Building cur in Registry)
            {
                report += Tools.PosStr(cur.Position);
            }
            return report;
            /*
            string report = string.Empty;
            foreach (Building cur in Registry)
            {
                report += ' '+cur.Label + "[" + cur.Position.x + ";" + cur.Position.z + "];";
            }*/
        }
        public override void PostDrawExtraSelectionOverlays()
        {
            if (!Registry.NullOrEmpty())
            {
                foreach (Building cur in Registry)
                    GenDraw.DrawLineBetween(this.parent.TrueCenter(), cur.TrueCenter());
            }
        }
    }
}