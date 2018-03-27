/*
 * Created by SharpDevelop.
 * User: Etienne
 * Date: 22/11/2017
 * Time: 16:43
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using Verse;

namespace LighterThanFast
{
	public class CompProperties_LTF_Phenodrill : CompProperties
	{
        public float XpLossPivotBase;// = 8f;
        public float XpLossSpectrum;// = -8f;
        // No spectrum, dumb math 8/7/6 5/4/3 2/1/0
        public float XpLossMax;// = 5f;

        public float XpRandBase;// = 4f;
        public float XpRandSpectrum;// = -4f;
        // No spectrum, dumb math 4/3/3 2/2/2 1/1/1

        public float ScanRadius;// = 20f;

        public CompProperties_LTF_Phenodrill()
		{
			this.compClass = typeof(Comp_LTF_Phenodrill);
		}
	}
}
