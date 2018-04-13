using RimWorld;

using Verse;
using Verse.Sound;

namespace LTF_Teleport
{
    public class CompTargetEffect_TpSpotLink : CompTargetEffect
    {
        public override void DoEffectOn(Pawn user, Thing target)
        {
            //Tools.Warn(">>> DoEffectOn  <<<", true);

            Building tpSpot1 = (Building)target;
            Building tpSpot2 = user.CurJob.targetA.Thing as Building;
            if (tpSpot1 == null)
            {
                Tools.Warn("Null worker bench", true);
                return;
            }

            string Spot1Valid = Comp_LTF_TpSpot.ValidTpSpot(tpSpot1);
            if (!Spot1Valid.NullOrEmpty())
            {
                Messages.Message(Spot1Valid, this.parent, MessageTypeDefOf.TaskCompletion);
                return;
            }
            string Spot2Valid = Comp_LTF_TpSpot.ValidTpSpot(tpSpot2);
            if (!Spot2Valid.NullOrEmpty())
            {
                Messages.Message(Spot2Valid, this.parent, MessageTypeDefOf.TaskCompletion);
                return;
            }

            if (tpSpot1.def.defName == "LTF_TpCatcher" && tpSpot2.def.defName == "LTF_TpCatcher")
            {
                Messages.Message("At least one of the two spots must be powered.", this.parent, MessageTypeDefOf.TaskCompletion);
                return;
            }

            if (tpSpot1 == tpSpot2)
            {
                Messages.Message(tpSpot1.Label + " can not target itself", this.parent, MessageTypeDefOf.TaskCompletion);
                return;
            }

            Comp_LTF_TpSpot spot1Comp = tpSpot1.TryGetComp<Comp_LTF_TpSpot>();
            Comp_LTF_TpSpot spot2Comp = tpSpot2.TryGetComp<Comp_LTF_TpSpot>();
            if ((spot1Comp == null)|| (spot2Comp == null))
            {
                Tools.Warn("// Not comp", true);
                return;
            }

            Tools.Warn("registering: " + tpSpot2.Label + " in " + tpSpot1.Label, spot1Comp.prcDebug);

            if ((!spot1Comp.IsCatcher) &&(!spot1Comp.HasPoweredFacility))
            {
                Messages.Message(tpSpot1.Label + " requires a powered facility to be linked", this.parent, MessageTypeDefOf.TaskCompletion);
                return;
            }

            bool didSomething = spot1Comp.CreateLink(tpSpot2, spot2Comp) && spot2Comp.CreateLink(tpSpot1, spot1Comp); ;

            Messages.Message(
                Tools.OkStr(didSomething)+' '+
                tpSpot1.Label + spot1Comp.MyCoordinates + 
                " was "+((didSomething)?("") :("not "))+"linked to " +
                tpSpot2.Label + spot2Comp.MyCoordinates
                , this.parent, MessageTypeDefOf.TaskCompletion);

            Tools.Warn("registered: " + tpSpot2.Label + " in "+ tpSpot1.Label, spot1Comp.prcDebug);
        }
    }

    /*
    public class CompUseEffect_TpSpotRegister : CompUseEffect
    {
        public override void DoEffect(Pawn usedBy)
        {
            Tools.Warn(">>> DoEffect <<<", true);
            base.DoEffect(usedBy);
            SoundDefOf.LessonActivated.PlayOneShotOnCamera(usedBy.MapHeld);
            //usedBy.records.Increment(RecordDefOf.ArtifactsActivated);
        }
    }
    */
}
