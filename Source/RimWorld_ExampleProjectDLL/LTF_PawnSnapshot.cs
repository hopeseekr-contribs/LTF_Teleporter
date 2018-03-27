/*
 * Created by SharpDevelop.
 * User: Etienne
 * Date: 22/11/2017
 * Time: 16:43
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
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
	public class LTF_PawnSnapshot : IExposable
	{
        // should send a signal to bench when dies instead of benchscan

        public Pawn original;
        public Pawn newClone;
        public string pawnThingId;

        public List<String> rememberToRemove;
        //public BodyPartRecord[] rememberToRemove;

        public int cloneNum;
        public int AAA;

        public Building drillDep;

        public float buildProgress;
        public float buildAmount;

        public void ExposeData()
        {
            Scribe_Deep.Look<Pawn>(ref newClone, "LTF_newClone");
            Scribe_References.Look<Pawn>(ref original, "LTF_snapOriginal");

            Scribe_Values.Look(ref pawnThingId, "LTF_cloneOriginalId");

            Scribe_Collections.Look(ref rememberToRemove, "LTF_BP");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.rememberToRemove == null)
            {
                this.rememberToRemove = new List<String>();
            }

            Scribe_Values.Look<int>(ref cloneNum, "LTFcloneNum");
            Scribe_Values.Look<int>(ref AAA, "LTFcloneAAA");

            Scribe_References.Look<Building>(ref drillDep, "LTF_drillDep");

            Scribe_Values.Look<float>(ref buildProgress, "LTF_cloneWProgress");
            Scribe_Values.Look<float>(ref buildAmount, "LTF_cloneWAmount");
        }

        public bool okSnap()
        {
            int bad = 0;

            if (newClone == null)
            {
                Log.Warning("new null");
                bad++;
            }

            if (original == null)
            {
                Log.Warning("original null");
                bad++;
            }
            if(bad>0)
            return false;
            //return ((original != null) && (newClone != null));
            return true;
        }

        // progress in work
        public float Progress
        {
            get
            {
                return buildProgress / buildAmount;
            }
        }

        public bool PostSpawnRemoveBionics(float ForgetLuck) {
            
            bool didSomething = false;

            List<Hediff> hediffs = newClone.health.hediffSet.hediffs;

            List<Hediff> cutMe = new List<Hediff>();

            foreach (String currentBP in rememberToRemove)
            {
                for (int i = hediffs.Count - 1; i >= 0; i--)
                {
                Hediff hediff = hediffs[i];
                //foreach (BodyPartRecord currentBP in rememberToRemove)
               
                    BodyPartRecord bodyPartRecord = null;
                    bodyPartRecord = hediff.Part;
                    //Log.Warning("Comparing " + currentBP + " to " + bodyPartRecord.def.label);
                    if (bodyPartRecord.def.label == currentBP)
                    {
                        didSomething = true;
                        //cutMe.Add(hediff);
                        //hediff.PostRemoved();
                        hediffs.RemoveAt(i);
                        if (currentBP != null)
                        {
                            string maybeLuck = String.Empty;

                            if (ForgetLuck < 1) ForgetLuck = 1;
                            //Log.Warning("1/"+ForgetLuck+" to forget "+currentBP);
                            if (Rand.Chance(1 / ForgetLuck))
                            {
                                maybeLuck = drillDep.Label + " forgot " + bodyPartRecord.def.label;
                                if (IsVital(bodyPartRecord))
                                {
                                    maybeLuck += "(vital)";
                                }
                                maybeLuck += ". It had a 1/" + ForgetLuck + " chance to happen.";
                                Messages.Message(maybeLuck, drillDep, MessageTypeDefOf.TaskCompletion);
                                BPRepair(newClone, bodyPartRecord);
                                break;
                            }


                            newClone.health.AddHediff(HediffDefOf.MissingBodyPart, bodyPartRecord, null);
                            break;
                        }

                    }
                }
            }
            return didSomething;
        }

        void BPRepair(Pawn pawn, BodyPartRecord BP)
        {
            if ((BP == null) || (pawn == null))
            {
                Log.Warning("Cant do that");
                return;
            }

            pawn.health.RestorePart(BP, null, true);
            IEnumerable<BodyPartRecord> myChildren = BP.GetDirectChildParts();
            //myChildren = from target in myChildren
            //             select target;
            foreach (BodyPartRecord mybp in myChildren)
            {
                BPRepair(pawn, mybp);
            }
        }

        public static bool IsVital(BodyPartRecord myBP)
        {
            string[] VitalSource = {
                "BloodPumpingSource",
//                "BloodFiltrationSource",
                "MetabolismSource",
                "BreathingSource",
                "ConsciousnessSource"
                };
            foreach (string maybeSource in VitalSource)
            {
                if (myBP.def.tags.Contains(maybeSource))
                    return true;
            }
            return false;
        }

    }
}
