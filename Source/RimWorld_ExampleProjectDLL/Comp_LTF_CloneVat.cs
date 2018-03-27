using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Verse;               // RimWorld universal objects are here (like 'Building')
using Verse.Sound;

using UnityEngine;


namespace LighterThanFast
{
    //TODO
    //Faction impact // Cloning should only happen with people from faction, but with mind control
    //        private CompFlickable flickComp;
    // buildWorkAmount depends on meat + quality
    // graceMax depends on quality

    // Main
    [StaticConstructorOnStartup]
    public class Comp_LTF_CloneVat : ThingComp
    {
        // bench Setup
        Building bench = null;
        private CompPowerTrader powerComp;
        private CompQuality qualityComp;

        private float setupRadius = 10f;

        private float graceProgress = 0;
        private float graceMax = 5000;

        private float WorkPerAA = 25f; // will be set
        private float ForgetBPLuck = 2048f; //will be set

        private int remoteMeat = 0;
        private int remoteBackup = 0;
        private int phenoDrillCount = 0;

        // providing ressources Buildings 
        //List<Building> meatGrinder = new List<Building>();
        //List<Building> phenoDrill = new List<Building>();

        List<LTF_PawnSnapshot> cloneTodo = new List<LTF_PawnSnapshot>();

        static string overlayPath = "Things/Building/CloneVat/Overlay/";

        Mesh WarningMesh = MeshPool.plane08;
        Mesh dotsMesh = MeshPool.plane14;
        Mesh workMesh = MeshPool.plane10;


        private static readonly Material AAFfx = MaterialPool.MatFrom(overlayPath + "AminoAcid", ShaderDatabase.MetaOverlay);
        private static readonly Material DnaGfx = MaterialPool.MatFrom(overlayPath + "Dna", ShaderDatabase.MetaOverlay);
        private static readonly Material EjectGfx = MaterialPool.MatFrom(overlayPath + "VatEject", ShaderDatabase.MetaOverlay);

        const int workBarNum = 27;
        private static readonly Material WSGfx = MaterialPool.MatFrom(overlayPath + "workBarS", ShaderDatabase.Transparent);
        private static readonly Material WMGfx = MaterialPool.MatFrom(overlayPath + "workBarM", ShaderDatabase.Transparent);
        private static readonly Material WLGfx = MaterialPool.MatFrom(overlayPath + "workBarL", ShaderDatabase.Transparent);

        private static readonly Material CloningGfx = MaterialPool.MatFrom(overlayPath + "Cloning", ShaderDatabase.Transparent);

        public CompProperties_LTF_CloneVat Props
        {
            get
            {
                return (CompProperties_LTF_CloneVat)props;
            }
        }

        private void QualityWeightedBenchFactors()
        {
            if (qualityComp == null)
            {
                WorkPerAA = Props.WorkPerAABase;
                ForgetBPLuck = Props.ForgetBPLuckBase;
                Log.Warning("sad bench has no quality");
            }
            // 25 24 .. 19f
            WorkPerAA = Props.WorkPerAABase + (float)qualityComp.Quality * (Props.WorkPerAASpectrum / 8);
            // 2^11 + (-8 / int value)
            // 1/2048 2^11
            // 1/4 2^3
            ForgetBPLuck = (float)Math.Pow(2,Props.ForgetBPLuckBase-(float)qualityComp.Quality);
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Vector3 benchPos = this.parent.DrawPos;
            if (benchPos == null)
            {
                Log.Warning("null pos draw");
                return;
            }
            // higher than ground to be visible
            //benchPos.y += 4;
            benchPos.y += 0.046875f;

            if (!CheckPower())
            {
                //Insert DIE Grace when no power here

                //Log.Warning("nopeverlay");
                return;
            }

            if (IsFull())
            {
                PulseWarning(bench, EjectGfx, benchPos, dotsMesh);
                return;
            }

            if (remoteBackup == 0)
            {
                PulseWarning(bench, DnaGfx, benchPos, dotsMesh);
                return;
            }
            
            if (remoteMeat == 0)
            {
                PulseWarning(bench, AAFfx, benchPos, dotsMesh);
                return;
            }
            

            if (HasSnapshot())
            {
               // Log.Warning("todo:" + cloneTodo.Count);
                if (!HasEnoughAA(GetFirstSnapshot()))
                {
                    PulseWarning(bench, AAFfx, benchPos, dotsMesh);
                }
                else
                {
                    DrawDot(benchPos, CloningGfx, dotsMesh, -1.04f, .07f, bench, true);

                    LTF_PawnSnapshot snap = null;
                    snap = GetFirstSnapshot();
                    if (snap == null)
                    {
                        Log.Warning("null snap");
                        return;
                    }
                        

                    int neededWorkBarN = Mathf.RoundToInt(ProgressToRegister(snap.buildProgress, snap.buildAmount) * workBarNum);
                    //Log.Warning("wBar:"+neededWorkBarN);
                    for (int i = 1; i < neededWorkBarN + 1; i++)
                    {
                        DrawBar(benchPos, workMesh, -1.5f, .12f, i);
                    }
                }
            }
            else
            {
                DrawDot(benchPos, CloningGfx, dotsMesh, -1.04f, .07f, bench, false);
            }



        }

        private void PulseWarning(Thing thing, Material mat, Vector3 drawPos, Mesh mesh)
        {
            float num = (Time.realtimeSinceStartup + 397f * (float)(thing.thingIDNumber % 571)) * 4f;
            float num2 = ((float)Math.Sin((double)num) + 1f) * 0.5f;
            num2 = 0.3f + num2 * 0.7f;
            Material material = FadedMaterialPool.FadedVersionOf(mat, num2);

            Vector3 dotS = new Vector3(.6f, 1f, .6f);
            Matrix4x4 matrix = default(Matrix4x4);

            matrix.SetTRS(drawPos, Quaternion.AngleAxis(0f, Vector3.up), dotS);

            Graphics.DrawMesh(mesh, matrix, material, 0);
        }

        private void DrawDot(Vector3 benchPos, Material dotGfx, Mesh mesh, float x, float z, Thing bench, bool pulse)
        {
            Vector3 dotPos = benchPos;
            dotPos.x += x;
            dotPos.z += z;

            Material material = dotGfx;

            if (pulse)
            {
                float num = (Time.realtimeSinceStartup + 397f * (float)(bench.thingIDNumber % 571)) * 4f;
                float num2 = ((float)Math.Sin((double)num) + 1f) * 0.5f;
                num2 = 0.3f + num2 * 0.7f;
                material = FadedMaterialPool.FadedVersionOf(dotGfx, num2);
            }
            

            Vector3 dotS = new Vector3(.32f, 1f, .29f);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(dotPos, Quaternion.AngleAxis(0f, Vector3.up), dotS);
            //            Graphics.DrawMesh(mesh, benchPos, matrix, dotGfx, 0);
            Graphics.DrawMesh(mesh, matrix, material, 0);
        }

        private void DrawBar(Vector3 benchPos, Mesh mesh, float x, float z, int i)
        {
            float zOffset = 0f;

            Material wBarMat = null;

            if (i < 8)
            {
                wBarMat = WSGfx;
            }
            else if (i < 21)
            {
                wBarMat = WMGfx;
                zOffset += .013f;
            }
            else
            {
                wBarMat = WLGfx;
                zOffset += .02f;
            }

            float myX = -1.145f + (i * .0825f);
            float myY = .562f + zOffset;

            //Log.Warning("wBar" + neededWorkBarN + " x;y: "+ myX+ ";"+ myY );

            FlickerBar(benchPos, wBarMat, mesh, myX, myY);
        }

        private void FlickerBar(Vector3 benchPos, Material dotGfx, Mesh mesh, float x, float z)
        {
            Vector3 dotPos = benchPos;
            dotPos.x += x;
            dotPos.z += z;

            Material fMat = dotGfx;
            if (Rand.Chance(0.85f))
                fMat = FadedMaterialPool.FadedVersionOf(dotGfx, .65f);

            Vector3 barS = new Vector3(.35f, 1f, .275f);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(dotPos, Quaternion.AngleAxis(0f, Vector3.up), barS);

            Graphics.DrawMesh(mesh, matrix, fMat, 0);
        }


         bool ForgetSnapshot(string removeDataId)
        {
            bool didIt = false;
            if (!HasSnapshot())
            {
                //Log.Warning("already empty forget");
                return false;
            }

            var itemToRemove = cloneTodo.SingleOrDefault(r => r.pawnThingId == removeDataId);
            if (itemToRemove != null)
            {
                cloneTodo.Remove(itemToRemove);
                didIt = true;
            }
            return didIt;
        }
        //OverlayDrawer.PowerOffMat = MaterialPool.MatFrom("UI/Overlays/PowerOff", ShaderDatabase.MetaOverlay);
        public bool FindBuildings()
        {
            phenoDrillCount = 0;
            remoteMeat = 0;
            remoteBackup = 0;
            //int i = 0;

            foreach (Building current in bench.Map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("LTF_MeatGrinder")))
            {
                //Log.Warning("building #" + i + " : " + current.def.defName + " meatForNow: " + remoteMeat);
                //+ " DrillforNow: "+ phenoDrillCount);
                //i++;

                if (current == null)
                {
                    //Log.Warning("why the fu");
                    continue;
                }

                float MeatDistance = current.Position.DistanceTo(bench.Position);
                if (MeatDistance > setupRadius)
                {
                    continue;
                }

                Comp_LTF_MeatGrinder meatComp = null;
                meatComp = current.TryGetComp<Comp_LTF_MeatGrinder>();

                if (meatComp == null)
                {
                    Log.Warning("should not possibru");
                }

                if (meatComp.CheckPower())
                {
                    remoteMeat += (int)meatComp.GetAA();
                }
            }

            foreach (Building current in bench.Map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("LTF_Phenodrill")))
            {
                float DrillDistance = current.Position.DistanceTo(bench.Position);
                if (DrillDistance > setupRadius)
                {
                    continue;
                }

                Comp_LTF_Phenodrill phenoComp = null;
                phenoComp = current.TryGetComp<Comp_LTF_Phenodrill>();
                //Log.Warning("building #" + i + " : " + current.def.defName + " DrillforNow: "+ phenoDrillCount + "backedup panws:" + remoteBackup);

                if (phenoComp == null)
                {
                    Log.Warning("should not be possibru");
                    continue;
                }
                if (!phenoComp.CheckPower() )
                {
                    continue;
                }
                //Count drills
                phenoDrillCount++;

                if(!phenoComp.hasRegistered()){
                    continue;
                }

                //Count souls
                int curNum = phenoComp.RegisteredNum();
                remoteBackup += curNum;

                if (curNum > 0)
                {
                    // Is anyone dead in there ?
                    LTF_PawnSnapshot CloneNeeded = null;
                    CloneNeeded = phenoComp.GetFirstDead();

                    if (CloneNeeded != null)
                    {
                        bool wip = false;
                        foreach (var curClone in cloneTodo)
                        {
                            if (curClone.original == CloneNeeded.original)
                            {
                                wip = true;
                            }
                        }

                        if (!wip)
                        {
                            //Log.Warning("+1 clone todo : " + CloneNeeded.original.Label);
                            CloneNeeded.buildProgress = 0;
                            CloneNeeded.buildAmount = CloneNeeded.AAA * WorkPerAA;
                            cloneTodo.Add(CloneNeeded);
                        }
                    }
                }
            }

            if ((remoteMeat > 0) && HasSnapshot())
            {
                return true;
            }
            return false;
        }



        public bool HasSnapshot()
        {
            return (!cloneTodo.NullOrEmpty());
        }

        public LTF_PawnSnapshot GetFirstSnapshot()
        {
            if (!HasSnapshot())
                return null;

            return (cloneTodo.First());
        }

        public float ProgressToRegister(float progress, float goal)
        {
            return (progress / goal);
        }



        public void CancelSnap(LTF_PawnSnapshot forgetmeNow)
        {
            if (forgetmeNow.drillDep == null)
            {
                Log.Error("no drill :(");
                return;
            }

            Comp_LTF_Phenodrill comp = null;
            comp = forgetmeNow.drillDep.TryGetComp<Comp_LTF_Phenodrill>();

            if (comp == null)
            {
                Log.Error("no drill comp:(");
                return;
            }

            string rememberMe = string.Empty;
            rememberMe = String.Copy(forgetmeNow.pawnThingId); 

            /*
            if (!forgetmeNow.okSnap())
            {
                Log.Warning("cancel snap null");
                return;
            }
            */

            //Log.Warning("Forgeting " + rememberMe + " in drill");
            comp.ForgetSnapshot(rememberMe);

            //Log.Warning("Forgeting" + rememberMe + "in vat");

            cloneTodo.Remove(forgetmeNow);
            //ForgetSnapshot(rememberMe);
            //Log.Warning("Forgot");
        }
        public void CancelSnap()
        {
            LTF_PawnSnapshot first = cloneTodo.First();

            Comp_LTF_Phenodrill comp = null;
            comp = first.drillDep.TryGetComp<Comp_LTF_Phenodrill>();

            if (first != null)
            {
                CancelSnap(first);
            }
        }

        public bool CloneSpawn()
        {
            //Log.Warning("cloneSpawn");

            if (!CheckBench())
            {
                return false;
            }

            if (IsFull())
            {
                return false;
            }
            if (!HasSnapshot())
            {
                Log.Error("clonespawn no  snap");
                return false;
            }

            LTF_PawnSnapshot letsDoThis = GetFirstSnapshot();

            if (letsDoThis == null)
            {
                return false;
            }

            if (!HonorMeatDebt(letsDoThis.AAA))
            {
                return false;
            }

            //Putting in casket
            Building_CryptosleepCasket casket = null;
            casket = (Building_CryptosleepCasket)(bench);
            if (casket == null)
            {
                Log.Error("Null cakset");
            }
            casket.TryAcceptThing(letsDoThis.newClone, true);
            letsDoThis.PostSpawnRemoveBionics(ForgetBPLuck);
            CancelSnap(letsDoThis);

            return true;
        }

        // Find first non mepty meat grinder
        private bool HonorMeatDebt(int meatAmount)
        {
            foreach (Building current in bench.Map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("LTF_MeatGrinder")))
            {
                if (current == null)
                {
                    continue;
                }

                float MeatDistance = current.Position.DistanceTo(bench.Position);
                if (MeatDistance > setupRadius)
                {
                    continue;
                }

                Comp_LTF_MeatGrinder meatComp = null;
                meatComp = current.TryGetComp<Comp_LTF_MeatGrinder>();

                if (meatComp == null)
                {
                    Log.Warning("should not possibru");
                }

                if (!meatComp.CheckPower())
                {
                    continue;
                }

                if (meatAmount < 0) Log.Warning("should never ask for negative meat");
                meatAmount -= (int)meatComp.AskForAA(meatAmount);

                if (meatAmount == 0)
                {
                    return true;
                }
            }
            return false;
        }

        private void ProgressHax(LTF_PawnSnapshot haxMe, float perc)
        {
            haxMe.buildProgress += haxMe.buildAmount*perc;
        }

        private void ProgressHax(LTF_PawnSnapshot haxMe)
        {
            haxMe.buildProgress = haxMe.buildAmount;
        }

        private void GraceHax()
        {
            graceProgress = 0;
        }

        private void ResetProgress(LTF_PawnSnapshot resetMe)
        {
            resetMe.buildAmount = resetMe.AAA * 150;
            resetMe.buildProgress = 0;
        }

        string BuildProgressString(LTF_PawnSnapshot wipClone)
        {
            string buffer = string.Empty;
            buffer = "[" + wipClone.buildProgress + " / " + wipClone.buildAmount + "]";
            return (buffer);
        }

        string GraceProgressString()
        {
            string buffer = string.Empty;
            buffer = "[" + graceProgress + " / " + graceMax + "]";
            return (buffer);
        }

        public bool IsFull()
        {
            Building_Casket casket = (Building_Casket)bench;
            if (casket == null)
            {
                Log.Warning("nope casket");
                return false;
            }
            return (casket.HasAnyContents);
        }

        public bool FindRessources()
        {

            // bench pos & map
            if (!CheckBench())
            {
                Log.Warning("null bench");
                return false;
            }

            remoteMeat = 0;

            if (HasSnapshot())
            {
                if (remoteMeat >= GetFirstSnapshot().AAA)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckPower()
        {
            if (this.powerComp == null || !this.powerComp.PowerOn)
                return false;

            return true;
        }

        private bool CheckBench()
        {
            if ((bench == null) || (bench.Position == null) || (bench.Map == null))
                return false;
            return true;
        }

        public void ShowReport()
        {
            StringBuilder stringBuilder = new StringBuilder();
            String buffer = string.Empty;

            stringBuilder.AppendLine("| Clone tube Todo list|");
            stringBuilder.AppendLine("+-------------------+");

            if (phenoDrillCount > 0)
            {
                stringBuilder.AppendLine(phenoDrillCount + " phenodrill" + ((phenoDrillCount > 1) ? ("s") : ("")) + " in radius(" + setupRadius + ")");
                return;
            }

            if (remoteMeat > 0)
            {
                stringBuilder.AppendLine(remoteMeat + " remote meat available");
                return;
            }

            foreach (var records in cloneTodo)
            {
                if (!records.okSnap())
                {
                    Log.Warning("null report");
                    continue;
                }
                stringBuilder.AppendLine("|");
                stringBuilder.AppendLine("+---[" + records.original.Name + "]");
                stringBuilder.AppendLine("|\t+-- " + records.original.gender.GetLabel() +
                                            "(" + records.original.ageTracker.AgeChronologicalYears + ")" + " from " + records.original.Faction.Name + ".");
            }

            Dialog_MessageBox window = new Dialog_MessageBox(stringBuilder.ToString(), null, null, null, null, null, false);
            Find.WindowStack.Add(window);
        }
            

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            bench = (Building)parent;
            powerComp = bench.TryGetComp<CompPowerTrader>();
            qualityComp = bench.TryGetComp<CompQuality>();
            QualityWeightedBenchFactors();
            //DumpProps();
        }

        private void DumpProps()
        {
            Log.Warning("work:" + Props.WorkPerAABase + Props.WorkPerAASpectrum);
            Log.Warning("forget:" + Props.ForgetBPLuckBase);
            //Log.Warning("forget:" + Props.ForgetBPLuckBase + Props.ForgetBPSpectrum);
        }

        public override void CompTick()
        {
            if (!CheckBench())
            {
                Log.Warning("Impossibru");
                return;
            }

            if (!CheckPower())
            {
                //Log.Warning("nope power");
                return;
            }

            // Someone is in the tube
            if (IsFull())
            {
                //Log.Warning("Someone is playing in there");
                return;
            }

            // Passive work when registered Num > 0
            // tries to revive
            if (FindBuildings())
            {
                
                //foreach (var reviveMe in cloneTodo)
                LTF_PawnSnapshot reviveMe = GetFirstSnapshot();

                if (reviveMe != null)
                {
                    if (reviveMe.original.Dead)
                    {
                        //Log.Warning("we shall revive" + reviveMe.name);
                        //maybeDead.cloneNum.

                        if (!HasEnoughAA(reviveMe)) {
                            //Log.Warning("wont work without mat");
                            return;
                        }

                        reviveMe.buildProgress++;

                        if (reviveMe.buildProgress >= reviveMe.buildAmount)
                        {
                            if (CloneSpawn())
                            {
                                //Log.Warning("We revived someone");
                            }
                            else
                            {
                                //Log.Warning("You failed to revive someone");
                            }
                        }
                    }
                    // We should not have a snap without somebody dead
                    else
                    {
                        CancelSnap(reviveMe);
                        //ErrorCallBack
                        
                    }
                }
            }
        }

        public bool HasEnoughAA(LTF_PawnSnapshot reviveMe)
        {
            //Log.Warning(remoteMeat + ">" + reviveMe.AAA);
            return (remoteMeat > reviveMe.AAA);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref cloneTodo, "LTF_clones", LookMode.Reference, new object[0]);
            //Scribe_Collections.Look<LTF_PawnSnapshot>(ref cloneTodo, "LTF_snaps", LookMode.Deep);
            //Scribe_Collections.Look<LTF_PawnSnapshot>(ref cloneTodo, "LTF_snaps");
            //Scribe_References.Look(ref cloneTodo, "LTF_casketTodo");
            //Scribe_Values.Look(ref scanProgress, "LTF_PhenotypeScanProgress");
            //update

        }

        public override string CompInspectStringExtra()
        {
            string text = base.CompInspectStringExtra();
            string result = string.Empty;

            if (this.powerComp == null || !this.powerComp.PowerOn)
            {
                return null;
            }

            //result += cloneTodo.Count + ((cloneTodo.Count > 1) ? ("s") : ("")) + " to do.";
            result += "AminoAcid: " + remoteMeat +".";
            //result += "Drill"+ ((phenoDrillCount > 1) ? ("s") : (""))+ ": " + phenoDrillCount + ".";
            result += "Pawn" + ((phenoDrillCount > 1) ? ("s") : ("")) + ": " + remoteBackup + ".";
            result += "Forget: 1/" + ForgetBPLuck + ".";
            if (FindBuildings())
            {
                if (!HasSnapshot())
                {
                    //Log.Warning("nothing todo");
                    //return null;
                }
                else
                {
                    LTF_PawnSnapshot first = GetFirstSnapshot();

                    if (first != null)
                    {
                        if (!first.okSnap())
                        {
                            //Log.Warning("wtf comp inspect");
                            //return null;
                        }
                        else
                        {
                            result += "\n- " + first.original.Name + " " + first.Progress.ToStringPercent("F0") + " " + BuildProgressString(first);
                        }

                    }
                }
            }

            if (!text.NullOrEmpty())
            {
                result = "\n" + text;
            }
            return result;
        }
        [DebuggerHidden]
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (CheckPower())
            {
                if (HasSnapshot())
                {
                    yield return new Command_Action
                    {
                        action = new Action(this.ShowReport),
                        defaultLabel = "what",
                        defaultDesc = "What",
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport", true)
                    };
                }

                if (FindBuildings())
                {
                    LTF_PawnSnapshot first = GetFirstSnapshot();

                    if (first != null)
                    {
                        yield return new Command_Action
                        {
                            action = new Action(CancelSnap),
                            defaultLabel = "Clone cancel",
                            defaultDesc = "Also removes its phenotype signature",
                            icon = ContentFinder<Texture2D>.Get("UI/Buttons/Delete", true)
                        };

                        if (Prefs.DevMode)
                        {
                            yield return new Command_Action
                            {
                                defaultLabel = "Haaax: +10% ready",
                                action = delegate
                                {
                                    ProgressHax(first,.1f);
                                }
                            };

                            yield return new Command_Action
                            {
                                defaultLabel = "Haaax: insta ready",
                                action = delegate
                                {
                                    ProgressHax(first);
                                }
                            };
                        }
                    }
                }
            }
        }
    }
}