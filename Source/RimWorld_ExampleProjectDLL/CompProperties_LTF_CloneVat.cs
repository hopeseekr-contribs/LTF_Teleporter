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
	public class CompProperties_LTF_CloneVat : CompProperties
	{
        public float WorkPerAABase;// = 25f;
        public float WorkPerAASpectrum;// = -16f;
        // 25 24 .. 19f

        public float ForgetBPLuckBase;// = 11f;
        //public float ForgetBPSpectrum;// = -8f;
        // 1/2048 2^11
        // 1/4 2^3


        public CompProperties_LTF_CloneVat()
		{
			this.compClass = typeof(Comp_LTF_CloneVat);
		}
	}
}
