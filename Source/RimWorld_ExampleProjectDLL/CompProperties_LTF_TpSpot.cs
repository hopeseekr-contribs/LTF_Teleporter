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
	public class CompProperties_LTF_TpSpot : CompProperties
	{
        // Capacity = weight that can be Tped // TpCapa
        // Telefrag and tp back also rely on Tpable weight 
        //public float WeightedCapacityBase;// = 50f;
        //public float WeightSpectrum;// = 160f; 20 / level
        // max = 210

        public float weightBase = 50f;
        public float weightSpectrum = 160f;

        // 60 sec base cooldown
        // Best = 20 min
        // 8*5 ; -5 sec / quality
        public float cooldownBase = 60*60f;
        public float cooldownSpectrum = -40*60f;

        // Electricity consuption ?

        public CompProperties_LTF_TpSpot()
		{
			this.compClass = typeof(Comp_LTF_TpSpot);
		}
	}
}
