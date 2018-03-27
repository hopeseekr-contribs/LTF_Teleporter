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
    //Addthought Self hurt
    //Addthought thinking about own death
    //        private CompFlickable flickComp;

    // workAmount Quality + meat amount 
    // Number of scanned people  : Quality


    // Venipuncture
    public class CompUseEffect_LTF_Venipuncture : CompUseEffect
    {
        public override void DoEffect(Pawn usedBy)
        {
            //Log.Warning(">>> DoEffect <<<");
            //base.DoEffect(usedBy);
            base.DoEffect(usedBy);
            Comp_LTF_Phenodrill comp = null;
            comp = parent.TryGetComp<Comp_LTF_Phenodrill>();
            if (comp == null)
            {
                Log.Warning("comp Null in use effect");
                return;
            }

            BodyPartRecord bodyPart = null;
            if (usedBy == null)
            {
                Log.Warning("wtf dude");
                return;
            }
            usedBy.RaceProps.body.GetPartsWithTag("ManipulationLimbCore").TryRandomElement(out bodyPart);
            if (bodyPart == null)
            {
                Log.Warning("no body part Found");
                return;
            }

            //Log.Warning("Found: " + bodyPart.def.label);
            DamageInfo damage = new DamageInfo(DamageDefOf.SurgicalCut, 1, -1f, null, bodyPart, null, DamageInfo.SourceCategory.ThingOrUnknown);

            //HediffDef bloodHediff = HediffDefOf.BloodLoss;
            HediffDef cutHediff = HediffDefOf.SurgicalCut;
            usedBy.health.AddHediff(cutHediff, bodyPart, damage);

            if (comp.IsFull())
            {
                Messages.Message("This "+parent.def.label+" is full.", this.parent, MessageTypeDefOf.TaskCompletion);
                return;
            }

            if (comp.CheckRegisteredSomewhere(usedBy))
            {
                Messages.Message(usedBy.Label + " is already registered somewhere.", this.parent, MessageTypeDefOf.TaskCompletion);
                return;
            }
            //ThoughtMaker.MakeThought()
            comp.SetTarget(usedBy);
            comp.SideEffect(usedBy);
            comp.ResetProgress();
            SoundDefOf.LessonActivated.PlayOneShotOnCamera(usedBy.MapHeld);

        }
    }

    // Main
    [StaticConstructorOnStartup]
    public class Comp_LTF_Phenodrill : ThingComp
    {
        // bench Setup
        Building bench = null;
        private CompPowerTrader powerComp;
        private CompQuality qualityComp;
        private float setupRadius = 10f;

        // pre Scan
        private Pawn bloodGiver = null;
        private float scanProgress = 0;
        
        private float scanWorkAmount = 2000;

        public float XpLossPivot = 8f; // will be set
        public float XpRand = 4f; // will be set

        // Scanned
        List<LTF_PawnSnapshot> RegisteredPawns = new List<LTF_PawnSnapshot>();
        // Should be linked to quality
        int RegisteredPawnMax = 4;

        static string overlayPath = "Things/Building/Phenodrill/overlay/";

        Mesh myMesh = MeshPool.plane10;

        private static readonly Material fullGfx = MaterialPool.MatFrom(overlayPath + "full", ShaderDatabase.MetaOverlay);
        private static readonly Material notFullGfx = MaterialPool.MatFrom(overlayPath + "notFull", ShaderDatabase.MetaOverlay);
        private static readonly Material okGfx = MaterialPool.MatFrom(overlayPath + "ok", ShaderDatabase.MetaOverlay);
        private static readonly Material koGfx = MaterialPool.MatFrom(overlayPath + "ko", ShaderDatabase.MetaOverlay);

        private static readonly Material doorGfx = MaterialPool.MatFrom(overlayPath + "door", ShaderDatabase.Transparent);
        private static readonly Material vatGfx = MaterialPool.MatFrom(overlayPath + "vat", ShaderDatabase.Transparent);
        private static readonly Material clawGfx = MaterialPool.MatFrom(overlayPath + "claw", ShaderDatabase.Transparent);
        //Graphic_Appearances Graphic_Multi
        private static readonly Graphic dnaGfx = GraphicDatabase.Get<Graphic_Flicker>(overlayPath + "dna", ShaderDatabase.TransparentPostLight, Vector2.one*.5f, Color.white);

        public CompProperties_LTF_Phenodrill Props
        {
            get
            {
                return (CompProperties_LTF_Phenodrill)props;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();

            Vector3 benchPos = bench.DrawPos;
            if (benchPos == null)
            {
                Log.Warning("null pos draw");
                return;
            }
            // higher than ground to be visible
            benchPos.y += 0.046875f;
            //benchPos.y += 4;

            if (!CheckBench() || !CheckPower())
            {
                //Insert DIE Grace when no power here
                //Log.Warning("nopeverlay");
                return;
            }

            Material myJob = null;
            bool pulse = false;

            if (CheckUser())
            {
                myJob = koGfx;
                pulse = true;
                DrawClaw(benchPos, clawGfx, myMesh, bench);
            }
            else
            {
                
                if (IsFull())
                {
                    myJob = fullGfx;
                }
                else
                {
                    //if has worker ko
                    if (HasTarget())
                    {
                        myJob = okGfx;
                        pulse = true;
                    }
                    else
                    {
                        myJob = notFullGfx;
                    }
                }
            }

            //DrawDot(benchPos, myJob, myMesh, .06f, .88f, bench, pulse);
            DrawDot(benchPos, myJob, myMesh, .06f, .85f, bench, pulse);

            if (hasRegistered())
            {
                int num = RegisteredNum();
                float myX; float myY;

                for(int i=1; i <= num; i++)
                {
                    myX = .052f; myY = 0f;
                    if (i == 1)
                    {
                        myX += -.905f;
                        myY += .395f;
                    }else if (i == 2)
                    {
                        myX += -.6f;
                        myY += .64f;
                    }
                    else if (i == 3)
                    {
                        myX += .57f;
                        myY += .64f;
                    }
                    else if (i == 4)
                    {
                        myX += .88f;
                        myY += .395f;
                    }
                    else
                    {
                        Log.Warning("Impossibru registered");
                    }

                    DrawDot(benchPos, vatGfx, myMesh, myX, myY, bench, false);
                    Vector3 dnaPos = benchPos;
                    dnaPos.x += myX;
                    dnaPos.z += myY+.2f;
                    dnaGfx.Draw(dnaPos, Rot4.North, this.parent, 0f);
                }
            }
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


            Vector3 dotS = new Vector3(.56f, 1f, .56f);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(dotPos, Quaternion.AngleAxis(0f, Vector3.up), dotS);
            //            Graphics.DrawMesh(mesh, benchPos, matrix, dotGfx, 0);
            Graphics.DrawMesh(mesh, matrix, material, 0);
        }

        private void DrawClaw(Vector3 benchPos, Material dotGfx, Mesh mesh, Thing bench)
        {
            Vector3 dotPos = benchPos;

            Vector3 dotS = new Vector3(bench.def.size.x, 1f, bench.def.size.z);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(dotPos, Quaternion.AngleAxis(0f, Vector3.up), dotS);
            //            Graphics.DrawMesh(mesh, benchPos, matrix, dotGfx, 0);
            Graphics.DrawMesh(mesh, matrix, dotGfx, 0);
        }

        public void SetTarget(Pawn volonteer)
        {
            if(!CheckPawn(volonteer))
            {
                Log.Warning("SetTarget null;");
                return;
            }
            bloodGiver = volonteer;
        }
        void ResetTarget()
        {
            bloodGiver = null;
        }
        public bool HasTarget()
        {
            return (bloodGiver != null);
        }

        public bool CheckRegisteredSomewhere(Pawn candidat)
        {
            int i = 0;
            foreach (Building drill in candidat.Map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("LTF_Phenodrill")))
            {

                //Log.Warning("b" + i + " : " + drill.Label);

                Comp_LTF_Phenodrill comp = null;
                comp = drill.TryGetComp<Comp_LTF_Phenodrill>();

                if (comp == null)
                {
                    Log.Warning("Impossibru");
                }

                //if (comp.CheckPower() && (comp.hasRegistered()))
                if (comp.hasRegistered())
                {
                    //Log.Warning("Found "++" with registered");

                    if (comp.HasSpecificPawn(candidat.ThingID))
                    {
                        //Log.Warning(candidat.NameStringShort + "already registered in " + drill.Label);
                        return true;
                    }
                }
                else
                {
                    //Log.Warning("b" + i + " has noone registered ");
                }

                i++;
            }
            return false;
        }

        public bool ScanCommit()
        {
            if (!CheckBench())
            {
                return false;
            }
             if (IsFull())
            {
                return false;
            }
            if (!CheckPawn(bloodGiver))
            {
                return false;
            }
            
            // If bloodGiver true faction comp_mind

            PawnGenerationRequest request = new PawnGenerationRequest(
              // PawnKindef Faction
              bloodGiver.kindDef, Faction.OfPlayer,
            // Backstories ?
            PawnGenerationContext.PlayerStarter,
            // tile forceGenerateNewPawn
            -1, true,
            //newborn allowDead
            false, true,
            //true, true,
            //downed canGeneratePawnRelations
            true, true,
            //violence colonistRelationChanceFactor
            false, 20f,
            // warm allowGay
            false, true,
            // food  inhabitan
            false, false,
            //crypto forceRedressWorldPawnIfFormerColonist
            true, false,
            // world Predicate<Pawn> validator
            false, null,
            // min fixedBiologicalAge
            null, bloodGiver.ageTracker.AgeBiologicalYearsFloat,
            // ageChrono fixedGender
            bloodGiver.ageTracker.AgeBiologicalYearsFloat, bloodGiver.gender,
            // melanin fixedLastName
            //null, bloodGiver.NameStringShort+(int)Rand.Range(1,100));
            null, null);

            //Log.Warning(bloodGiver.Label + " Init");
            var pawnSnapshot = new LTF_PawnSnapshot();

            //Log.Warning(bloodGiver.Label + " generate");
            // pawn creation query
            pawnSnapshot.newClone = PawnGenerator.GeneratePawn(request);
            //Log.Warning(bloodGiver.Label + " generate");


            pawnSnapshot.original = bloodGiver;
            pawnSnapshot.pawnThingId = pawnSnapshot.original.ThingID;
            pawnSnapshot.newClone.Name = bloodGiver.Name;
            pawnSnapshot.AAA = (int)AANeeded(bloodGiver);
            pawnSnapshot.drillDep = bench;

            //Log.Warning(bloodGiver.Label + " Traits");
            // Traits
            pawnSnapshot.newClone.story.childhood = bloodGiver.story.childhood;
            pawnSnapshot.newClone.story.adulthood = bloodGiver.story.adulthood;

            // APWAL
            pawnSnapshot.newClone.apparel.DestroyAll();
            pawnSnapshot.newClone.story.bodyType = bloodGiver.story.bodyType;
            pawnSnapshot.newClone.story.crownType = bloodGiver.story.crownType;
            pawnSnapshot.newClone.story.melanin = bloodGiver.story.melanin;

            pawnSnapshot.newClone.story.hairDef = bloodGiver.story.hairDef;
            pawnSnapshot.newClone.story.hairColor = bloodGiver.story.hairColor;

            pawnSnapshot.newClone.story.traits.allTraits = bloodGiver.story.traits.allTraits.ListFullCopy();
            
            

            //Log.Warning(bloodGiver.Label + " Hediff");
            // Hediff
            pawnSnapshot.newClone.health.hediffSet.hediffs = bloodGiver.health.hediffSet.hediffs.ListFullCopy();
            //Log.Warning("full copy ok");
            List<Hediff> myHediffs = bloodGiver.health.hediffSet.hediffs;
            pawnSnapshot.rememberToRemove = new List<String>();
            //Log.Warning("Loopin " + myHediffs.Count + " hediffs");
            for (int i = 0; i < myHediffs.Count; i++)
            {
                BodyPartRecord myBP = myHediffs[i].Part;
                
                AddedBodyPartProps addedPartProps = myHediffs[i].def.addedPartProps;
                if (addedPartProps != null && addedPartProps.isBionic)
                {
                    Log.Warning("Adding:"+myBP.def.label);
                    pawnSnapshot.rememberToRemove.Add(myBP.def.label);
                }
            }

            //Passion
            //Log.Warning(bloodGiver.Label + " passion");
            for (int i = 0; i < bloodGiver.skills.skills.Count; i++)
            {
                SkillRecord curBloodSkill = bloodGiver.skills.skills[i];
                SkillRecord curCloneSkill = pawnSnapshot.newClone.skills.skills[i];
                curCloneSkill.passion = curBloodSkill.passion;
            }

            Pawn clone = pawnSnapshot.newClone;

            //Log.Warning(bloodGiver.Label + " SKill");

            //Skills

            ///
            //int nerfLevelNum = 3;


            for(int Disabli = 0; Disabli < 2; Disabli++)
            {
                for (int i = 0; i < bloodGiver.skills.skills.Count; i++)
                {
                    SkillRecord curBloodSkill = bloodGiver.skills.skills[i];
                    SkillRecord curCloneSkill = clone.skills.skills[i];

                    int nerfLevelNum=(int)XpLossPivot;
                    int RNG = Rand.RangeInclusive(-(int)XpRand, (int)XpRand);
                    //int RNG = (int)Math.Round();
                    nerfLevelNum += RNG;

                    //Log.Warning("nerf goal " + curBloodSkill.def.label + ":" + XpLossPivot + "+" + RNG + "=" + nerfLevelNum);
                    if (nerfLevelNum > Props.XpLossMax)
                    {
                        //Log.Warning("wanted"+nerfLevelNum+"limit"+Props.XpLossMax);
                        nerfLevelNum = (int)Props.XpLossMax;
                    }

                    int goalLevel = (curBloodSkill.levelInt - nerfLevelNum);
                    if (goalLevel < 0) goalLevel = 0;
                    if (goalLevel > 20) goalLevel = 20;

                    int levelDiff = goalLevel - curCloneSkill.levelInt;

                   // Log.Warning(i + " :" + curBloodSkill.def.label + ":" + curBloodSkill.levelInt + "/" + curCloneSkill.levelInt + ";need:" + goalLevel);
                    
                    if (curBloodSkill.TotallyDisabled)
                    {
                        goalLevel = 0;
                    }
                    if (curCloneSkill.TotallyDisabled)
                    {
                        //Log.Warning("skipping skill : wont be able to learn");
                        continue;
                    }
                    
                    int loopBreaker = 0;
                    float totalNerf = 0;

                    //Log.Warning(curCloneSkill.def.label +"start");

                    while (curCloneSkill.levelInt != goalLevel)
                    {

                        if ((curCloneSkill.levelInt < 0) || (curCloneSkill.levelInt > 20))
                        {
                            //Log.Warning("nerf overflow");
                            break;
                        }
                        if (loopBreaker++ > 40)
                        {
                            //Log.Warning("Loops" + loopBreaker + ";nerf:"+ totalNerf);
                            break;
                        }
                        levelDiff = curCloneSkill.levelInt - goalLevel;
                        //speedupNerfFactor
                        float sNF = -levelDiff;
                        float nerfAmount;
                        float counterPassion;

                        if (Math.Abs(levelDiff) > 1) {
                            nerfAmount = sNF* Rand.Range(900f, 1000f);
                            counterPassion = 3f;
                        }
                        else
                        {
                            nerfAmount = sNF * Rand.Range(500f, 1000f);
                            counterPassion = 1.5f;
                        }

                         

                        //curCloneSkill.Learn(nerfAmount,);
                        //Log.Warning("trying to learn " + curCloneSkill.def + ":" + nerfAmount);
                        clone.skills.Learn(curCloneSkill.def, nerfAmount* counterPassion, true);
                        totalNerf += nerfAmount;
                        //Log.Warning("Diff=" + levelDiff + ";nerf=" + nerfAmount);
                    }
                    //Log.Warning(i + " done: " + totalNerf + "; original: " + curBloodSkill.levelInt + "; Clone:" + curCloneSkill.levelInt);
                }
               // Log.Warning("EnableInit");
                clone.workSettings.EnableAndInitialize();
            }

        //Parse original and disable clone
            foreach (WorkTypeDef currentDW in bloodGiver.story.DisabledWorkTypes)
            {
                //Log.Warning(currentDW.defName + "needs disable");
                clone.workSettings.Disable(currentDW);
            }
            // set priority what it does
            foreach (WorkTypeDef currentW in from w in DefDatabase<WorkTypeDef>.AllDefs
                                             where w.alwaysStartActive
                                             select w)
            {
                if (!clone.story.WorkTypeIsDisabled(currentW))
                {
                    clone.workSettings.SetPriority(currentW, 3);
                }
            }

            // Cryptosleep sickness
            clone.health.AddHediff(HediffDefOf.CryptosleepSickness, null, null);

            //Log.Warning("EnableInit");
            clone.workSettings.EnableAndInitialize();
            //Log.Warning(bloodGiver.Label + " Add");
            RegisteredPawns.Add(pawnSnapshot);

            ResetProgress();
            ResetTarget();


            //Log.Warning("full commit");
            return true;
        }

        // progress in work
        public float Progress
        {
            get
            {
                return scanProgress / scanWorkAmount;
            }
        }

        private float AANeeded (Pawn pawn)
        {
            float rawValue = 0;
            
            rawValue += pawn.GetStatValue(StatDefOf.MeatAmount, true) *2.5f;

            return rawValue;
        }
            
        public bool HasSpecificPawn(string originalStringId)
        {
            //Log.Warning(bench.Label + " lookin for " + originalStringId + " in " + RegisteredNum() + "pool");
            //int i = 0;
            bool answer = false;
            

            foreach(var registered in RegisteredPawns)
            {
                //Log.Warning("pawn" + i + " : Comparin " + originalStringId + " to " + registered.pawnThingId); i++;
                if (originalStringId == registered.pawnThingId)
                {
                    answer = true;
                }
            }
            return answer;
        }

        public bool hasRegistered()
        {
            return (!RegisteredPawns.NullOrEmpty());
        }

        public void ResetRegistered()
        {
            RegisteredPawns.Clear();
        }

        public int RegisteredNum()
        {
            return (RegisteredPawns.Count);
        }

        public bool ForgetSnapshot(string removeDataId)
        {
            bool didIt = false;

            if (!hasRegistered())
            {
                //Log.Warning("cant forget nothing");
                return false;
            }
            else
            {
                //Log.Warning("I can forget");
            }

            //Log.Warning("Trying to forget" + removeDataId);

            var itemToRemove = RegisteredPawns.SingleOrDefault(r => r.pawnThingId == removeDataId);
            if (itemToRemove != null)
            {
                RegisteredPawns.Remove(itemToRemove);
                didIt = true;
            }
            return didIt;
        }

        public LTF_PawnSnapshot GetFirstDead()
        {
            if (!hasRegistered())
                return null;
            var itemToReturn = RegisteredPawns.SingleOrDefault(r => r.original.Dead == true);
            if (itemToReturn != null)
            {
                return itemToReturn;
            }
            return null;
        }

        private void ProgressHax()
        {
            scanProgress = scanWorkAmount;
        }
        public void ResetProgress()
        {
            scanProgress = 0f;
        }
        string ProgressString()
        {
            string buffer = string.Empty;
            buffer = "[" + scanProgress + " / " + scanWorkAmount + "]";
            return (buffer);
        }
        private bool HasWorkTodo()
        {
            return (HasTarget() && (scanProgress < scanWorkAmount));
        }

        public bool IsFull()
        {
            return (RegisteredPawns.Count >= RegisteredPawnMax);
        }

        public bool TryScanReach()
        {
            // bench pos & map
            if(!CheckBench())
            {
                Log.Warning("null bench");
                return false;
            }

            if (!CheckPawn(bloodGiver))
            {
                //Log.Warning("null bloodGiver");
                ResetProgress();
                return false;
            }
            float benchTargetPawnDistance = 999f;
            benchTargetPawnDistance = bloodGiver.Position.DistanceTo(bench.Position);

            if (benchTargetPawnDistance > Props.ScanRadius)
            {
                //Messages.Message("Target is too far.", this.parent, MessageTypeDefOf.TaskCompletion);
                return false;
            }

            return true;
        }

        public bool BuildingInRadius()
        {
            if ((setupRadius <= 1) || (this.parent.Position == null) || (this.parent.Map == null))
            {
                Log.Warning("null bench");
                return false;
            }

            //Find other buildings
            //setupReachable = true;
            return true;
        }

        private bool CheckPawn(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
                return false;

            return true;
        }

        public bool CheckPower()
        {
            if (this.powerComp == null || !this.powerComp.PowerOn)
                return false;
            return true;
        }

        private bool CheckBench()
        {
            if ((bench == null) || (Props.ScanRadius <= 1) || (bench.Position == null) || (bench.Map == null))
                return false;
            return true;
        }

        private bool CheckUser()
        {
            if (!bench.InteractionCell.IsValid)
            {
                return false;
            }


            List<Pawn> allPawnsSpawned = bench.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            int pawnNum = allPawnsSpawned.Count;

            /*IEnumerable<Pawn> enumerable = from x in PawnsFinder.AllMaps_SpawnedPawnsInFaction(FactionDefOf.PlayerColony)
                                            where x.Position == pawn.def
                                            where bench.InteractionCell.AdjacentTo8WayOrInside(x.Position)
                                           select x;*/
            //Log.Warning(bench.Label + " found " + allPawnsSpawned.Count);
            int num = 0;
            foreach (Pawn current in allPawnsSpawned)
            {
                if (current == null)
                { //Log.Warning("null"); 
                    continue;
                };

                //Log.Warning(num + ":" + current.NameStringShort);
                //Log.Warning("doing:" + current.jobs.curDriver.job.def.defName);
                num++;

                if (current.Faction != Faction.OfPlayer)
                { //Log.Warning("faction");
                    continue;
                };

                if (current.mindState.mentalStateHandler.InMentalState)
                { //Log.Warning("mental");
                    continue;
                };

                float closeEnough = current.Position.DistanceTo(bench.InteractionCell);
                if(closeEnough > 4f)
                {
                    continue;
                }

                
                if (current.jobs.curDriver.job.def.defName != "UseItem")
                { //Log.Warning("mental");
                    continue;
                };
                //Log.Warning("using item");

                if (current.jobs.curDriver.job.targetA.Thing.ThingID != bench.ThingID)
                {
                    continue;
                }

                return true;
            }

        /*
            if ((maybeUser.jobs.curDriver.job == null) || (maybeUser.jobs.curDriver.job.def != JobDefOf.))
            { //Log.Warning("dobill");
                return false;
            }
            */
            return false;

        }


        public void SideEffect(Pawn pawn)
        {

            AddFilth(pawn);
            AddThought(pawn);
        }

        void AddThought(Pawn pawn)
        {
            pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("LTF_CloneRegister"), null);
        }
        
        private bool PawnHasBlood(Pawn pawn)
        {
            return (pawn.RaceProps.BloodDef != null);
        }

        private void AddFilth(Pawn pawn)
        {
            if (PawnHasBlood(pawn))
            {
                FilthMaker.MakeFilth(pawn.Position, pawn.Map, pawn.RaceProps.BloodDef, pawn.LabelIndefinite(), 1);
            }
        }

        public void ShowReport()
        {
            StringBuilder stringBuilder = new StringBuilder();
            String buffer = string.Empty;

            stringBuilder.AppendLine("| Phenodrill pool |");
            stringBuilder.AppendLine("+--------------------+");
            
            if (bloodGiver != null)
            {
                stringBuilder.AppendLine("|");
                stringBuilder.AppendLine("+---[Scan in progress:" + ProgressString() +"]");
                stringBuilder.AppendLine("|\t|");
                stringBuilder.AppendLine("|\t[" + bloodGiver.Label + "]" );
            }
            // count all pawns registered
            stringBuilder.AppendLine("|");
            stringBuilder.AppendLine("+-------[Pawns registered "+ RegisteredPawns.Count + "/" + RegisteredPawnMax + " ]");

            if (RegisteredPawns.Count > 0)
            {
                foreach (var records in RegisteredPawns)
                {
                    int intAge = (int)records.original.ageTracker.AgeBiologicalYears;

                    stringBuilder.AppendLine("|\t|");
                    stringBuilder.AppendLine("|\t+-[ " + records.original.Label + " ][ Meat : " + records.AAA + " ][ " + records.original.gender.GetLabel() + " ][ " + intAge + " ]");
                    //stringBuilder.AppendLine("|\t+-[ " + records.original.NameStringShort + " ][ Meat : " + records.AAA + " ]");
                    //stringBuilder.AppendLine("|\t+-[ " + records.original.gender.GetLabel() + " ][ " + intAge + " ][ Faction: " + records.original.Faction.Name + " ]");
                }
            }

            if (IsFull())
            {
                stringBuilder.AppendLine("|");
                stringBuilder.AppendLine("+---[ Max capacity reached(" + RegisteredPawnMax + ") ]");
            }
            else
            {
                stringBuilder.AppendLine("|");
                stringBuilder.AppendLine("+---[ Right click for free venipuncture. ]");
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
            Log.Warning("xploss:" + Props.XpLossPivotBase + Props.XpLossSpectrum + Props.XpLossMax +
                    "xpRand:" + Props.XpRandBase + Props.XpRandSpectrum);
         }
        

        private void QualityWeightedBenchFactors()
        {
            if (qualityComp == null)
            {
                XpLossPivot = Props.XpLossPivotBase;
                XpRand = Props.XpRandBase;
                Log.Warning("sad bench has no quality");
            }


            // No spectrum, dumb math 8/7/6 5/4/3 2/1/0
            XpLossPivot = Props.XpLossPivotBase - (float)qualityComp.Quality;

            // No spectrum, dumb math 4/3/3 2/2/2 1/1/1

            XpRand = Props.XpRandBase - (float)qualityComp.Quality / 2;

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
                ResetProgress();
                return;
            }

            // Active Work when bloodgiver
            if (IsFull())
            {
                //Log.Warning("cant register anyone anymore")
                return;
            }

            scanProgress++;

            if (scanProgress>= scanWorkAmount)
            {
                ScanCommit();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_References.Look(ref bloodGiver, "LTF_BloodGiver");
            Scribe_Values.Look(ref scanProgress, "LTF_PhenotypeScanProgress");
            
            
            Scribe_Collections.Look<LTF_PawnSnapshot>(ref RegisteredPawns, "LTF_snaps", LookMode.Deep, new object[0]);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.RegisteredPawns == null)
            {
                this.RegisteredPawns = new List<LTF_PawnSnapshot>();
            }
            

            //Scribe_Collections.Look(ref RegisteredPawns, "LTF_snaps", LookMode.Undefined, new object[0]);

            //Scribe_Deep.Look(ref RegisteredPawns, "LTF_snaps");
            //Scribe_Collections.Look(ref RegisteredPawns, "LTF_snaps",LookMode.Reference);
            //Scribe_Collections.Look(ref RegisteredPawns, "LTF_snaps", LookMode.Deep);
            //Scribe_Collections.Look<LTF_PawnSnapshot>(ref RegisteredPawns, "LTF_snaps", LookMode.Deep);
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

            result += RegisteredPawns.Count + " scanned.";

            if (HasTarget())
            {
                result += "\nScan target: " + bloodGiver.Label;
                if (HasWorkTodo())
                {
                    result += "\nProgress: " + Progress.ToStringPercent("F0") + " "+ ProgressString();
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
                yield return new Command_Action
                {
                    action = new Action(this.ShowReport),
                    defaultLabel = "Registered pawns",
                    defaultDesc = "Lists soon to be dead pawns",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport", true)
                };

                if (RegisteredNum() > 0)
                {
                    yield return new Command_Action
                    {
                        action = new Action(this.ResetRegistered),
                        defaultLabel = "Forget pawns",
                        defaultDesc = "No one will remain",
                        icon = ContentFinder<Texture2D>.Get("UI/Buttons/Delete", true)
                    };
                }
                
                if (Prefs.DevMode && HasWorkTodo())
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Haaax: insta ready",
                        action = delegate
                        {
                            ProgressHax();
                        }
                    };
                }
            }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            if (bloodGiver != null && bloodGiver.Map != null)
            {
                GenDraw.DrawLineBetween(this.parent.TrueCenter(), bloodGiver.TrueCenter());
            }
        }
    }
}