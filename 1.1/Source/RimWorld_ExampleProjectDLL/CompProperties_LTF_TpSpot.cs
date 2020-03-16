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
        // Telefrag and tp back also rely on Tpable weight 
        public float weightBase = 35f;
        public float weightQualityFactor = 20f;

        public float rangeBase = 10f;
        public float rangeQualityFactor = 1.2f;

        public int warmUpBase = 5 * 60;
        public int warmUpQualityFactor = (int)(-.55f * 60);

        public int cooldownBase = 20 * 60;
        public int cooldownQualityFactor = -2 * 60;

        public float missRangeBase = 10;
        public float missRangeQualityFactor = -1;
        public float missChanceBase = 2;
        public float missChanceQualityFactor = .5f;

        public float fumbleRangeBase = 1;
        public float fumbleRangeQualityFactor = 1.5f;
        public float fumbleChangeBase = 15;
        public float fumbleChanceQualityFactor = .25f;

        public float benchSynergyBase = .85f;
        public float benchSynergyQualityFactor = .08f;

        // Electricity consumption ?
        public bool PowerRequired = true;
        public bool BenchRequired = true;
        

        public CompProperties_LTF_TpSpot()
		{
			this.compClass = typeof(Comp_LTF_TpSpot);
		}
	}
}
