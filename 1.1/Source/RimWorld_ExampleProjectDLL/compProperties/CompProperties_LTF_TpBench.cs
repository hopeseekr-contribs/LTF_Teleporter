/*
 * Created by SharpDevelop.
 * User: Etienne
 * Date: 22/11/2017
 * Time: 16:43
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using Verse;

namespace LTF_Teleport
{
	public class CompProperties_TpBench : CompProperties
	{
        public float FacilityCapacitySpectrum = 16f;
        public float FacilityCapacityBase = 1f;

        public float moreRange = .6f;
        public float moreRangeBase = 1f;

        public CompProperties_TpBench()
		{
			this.compClass = typeof(Comp_TpBench);
		}
	}
}
