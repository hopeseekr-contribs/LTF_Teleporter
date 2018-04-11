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
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Verse;

    public class CompTargetable_Building : CompTargetable
    {
        protected override bool PlayerChoosesTarget
        {
            get
            {
                return true;
            }
        }

        protected override TargetingParameters GetTargetingParameters()
        {
            return new TargetingParameters
            {
                canTargetPawns = false,
                canTargetBuildings = true,
                validator = ((TargetInfo x) => base.BaseTargetValidator(x.Thing))
            };
        }

        [DebuggerHidden]
        public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
        {
            yield return targetChosenByPlayer;
        }
    }
 
}
