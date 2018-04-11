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

            Building tpSpot2 = (Building)target;
            Building tpSpot1 = user.CurJob.targetA.Thing as Building;
            if (tpSpot1 == null)
            {
                Tools.Warn("Null worker bench", true);
                return;
            }
            if( (tpSpot1.def.defName != "LTF_TpSpot")|| (tpSpot2.def.defName != "LTF_TpSpot"))
            {
                Tools.Warn("// Not a tp spot", true);
                return;
            }

            Comp_LTF_TpSpot spot1Comp = tpSpot1.TryGetComp<Comp_LTF_TpSpot>();
            Comp_LTF_TpSpot spot2Comp = tpSpot1.TryGetComp<Comp_LTF_TpSpot>();
            if ((spot1Comp == null)|| (spot2Comp == null))
            {
                Tools.Warn("// Not comp", true);
                return;
            }

            Tools.Warn("registering: " + tpSpot2.Label + " in " + tpSpot1.Label, spot1Comp.prcDebug);

            if (!spot1Comp.HasPoweredFacility)
            {
                Tools.Warn("// no powered", true);
                return;
            }

            spot1Comp.Link(tpSpot2, spot2Comp);
            spot2Comp.Link(tpSpot1, spot1Comp);

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
