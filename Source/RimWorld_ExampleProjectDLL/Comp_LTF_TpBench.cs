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

        /* Comp */
        /******************/
        private CompPowerTrader powerComp;
        public CompQuality compQuality;

        //private float benchRadius = 35.7f;

        private List<Building> tpSpotRegistry = new List<Building>();

        private enum MindVector { Ascen, Manip, Empat, Na };
        string[] vectorName = { "Ascendancy", "Manipulation", "Empathy", "Impossibru" };

        private const float defaultWorkAmount = 3600f; // 120sec = 60  * 120 = 7200 
        private float workGoal = defaultWorkAmount;
        private float workProgress = 0;

        private bool mindcontrolEnabled = false;
        //bool mindReachable = false;

        bool prcDebug = false;
        bool gfxDebug = false;
        bool Hax = false;

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

            //Building comp
            powerComp = building?.TryGetComp<CompPowerTrader>();
            compQuality = building?.TryGetComp<CompQuality>();

            if (powerComp == null)
            {
                Tools.Warn("power comp Null");
            }
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
            Scribe_Collections.Look<Building>(ref tpSpotRegistry, "tpSpots", LookMode.Reference, new object[0]);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.tpSpotRegistry == null)
            {
                this.tpSpotRegistry= new List<Building>();
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
        public bool HasSpot
        {
            get
            {
                return (!tpSpotRegistry.NullOrEmpty());
            }
        }
        public void RemoveSpot(Building target)
        {
            if ((target == null) || (target.def.defName != "LTF_TpSpot"))
                Tools.Warn("Trying to remove a non tp spot", prcDebug);

            tpSpotRegistry.Remove(target);
        }
        public void AddSpot(Building target)
        {
            if ((target == null) || (target.def.defName != "LTF_TpSpot"))
                Tools.Warn("Trying to register a non tp spot", prcDebug);


            if (tpSpotRegistry.Contains(target))
            {
                return;
            }
            tpSpotRegistry.Add(target);

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

        public void ShowReport()
        {
            StringBuilder stringBuilder = new StringBuilder();
            String buffer = string.Empty;

            stringBuilder.AppendLine("| Worktation logs |");
            stringBuilder.AppendLine("+----------------+");

            if (!tpSpotRegistry.NullOrEmpty())
            {
                foreach (Building cur in tpSpotRegistry)
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
            /*
            SetCooldownBase();
            currentCooldown = Mathf.Min(cooldownBase, currentCooldown);
            */
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

            // No spectrum, dumb math 8/7/6 5/4/3 2/1/0
            // No spectrum, dumb math 4/3/3 2/2/2 1/1/1
            //int nerfLevelNum = (int)XpLossPivot;
            //int RNG = Rand.RangeInclusive(-(int)XpRand, (int)XpRand);
            Answer = "nice";

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
                    foreach(Building cur in tpSpotRegistry)
                    {
                        if( (!ToolsBuilding.CheckBuilding(cur)) || (!ToolsBuilding.CheckPower(cur)) ){
                            Comp_LTF_TpSpot comp_LTF_TpSpot  = cur?.TryGetComp<Comp_LTF_TpSpot>();
                            if(comp_LTF_TpSpot!=null)
                                comp_LTF_TpSpot.ResetFacility();
                            tpSpotRegistry.Remove(cur);
                        } 
                    }

                }
            }

            Tools.Warn(" >>>TICK end<<< ", prcDebug);
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (GotThePower)
            {
                if (Prefs.DevMode)
                {
                    // Debug process
                    yield return new Command_Action
                    {
                        icon = ((prcDebug) ? (MyGfx.DebugOnGz) : (MyGfx.DebugOffGz)),
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
                        icon = ((gfxDebug) ? (MyGfx.DebugOnGz) : (MyGfx.DebugOffGz)),
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
                        /*
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
                            defaultLabel = currentCooldown + "->0",
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
                        */
                    }

                    // Hax quality
                    if (prcDebug && Hax && (compQuality != null))
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

                if (compQuality != null) {
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

                // Registration/Registry Log
                if (HasSpot)
                {
                    Texture2D LogMat = MyGfx.TpLogGz;
                    String LogString = " tp registry";
                    yield return new Command_Action
                    {
                        icon = LogMat,
                        defaultLabel = LogString,
                        defaultDesc = "Lists remote space manipulator. ok teleporters.",
                        action = new Action(this.ShowReport),
                    };
                }

            }
        }

        public override string CompInspectStringExtra()
        {
            if (!GotThePower)
                return null;

            if(tpSpotRegistry.NullOrEmpty())
                return "Empty registry.";

            string report = string.Empty;
            string spotName = tpSpotRegistry.RandomElement().def.label;

            report = ">"+tpSpotRegistry.Count+"<" + ' ' + spotName + ": ";
            foreach (Building cur in tpSpotRegistry)
            {
                report += "[" + cur.Position.x + ";" + cur.Position.z + "];";
            }
            return report;
            /*
            string report = string.Empty;
            foreach (Building cur in tpSpotRegistry)
            {
                report += ' '+cur.Label + "[" + cur.Position.x + ";" + cur.Position.z + "];";
            }*/
        }
        public override void PostDrawExtraSelectionOverlays()
        {
            if (!tpSpotRegistry.NullOrEmpty())
            {
                foreach (Building cur in tpSpotRegistry)
                    GenDraw.DrawLineBetween(this.parent.TrueCenter(), cur.TrueCenter());
            }
        }
    }
}