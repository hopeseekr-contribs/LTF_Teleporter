using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace LTF_Teleport
{
	public class Graphic_Slideshow : Graphic_Collection
	{
		private const int BaseTicksPerFrameChange = 20;
		private const int ExtraTicksPerFrameChange = 10;

		private const float MaxOffset = 0.05f;

        
        //int frameRef = 0;
        int frameMax = 23;
        int frameI = 0;

        bool isInitialized = false;

        public override Material MatSingle
		{
			get
			{
                //return this.subGraphics[Rand.Range(0, this.subGraphics.Length)].MatSingle;
                return this.subGraphics[0].MatSingle;
            }
		}

        public void Init()
        {
            //frameRef = Find.TickManager.TicksGame;
            frameI = 0;
            isInitialized = false;
            if (subGraphics != null)
            {
                frameMax = this.subGraphics.Length;
                //Log.Warning("frameM:" + frameMax + " check:" + this.subGraphics.Length);
                isInitialized = true;
            }

            //% this.subGraphics.Length; ;
            //Log.Warning(">>Init "+isInitialized);
        }

		public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
		{

            if (!isInitialized) Init();

            if (thingDef == null)
			{
				Log.ErrorOnce("slide DrawWorker with null thingDef: " + loc, 3427324);
				return;
			}
			if (this.subGraphics == null)
			{
				Log.ErrorOnce("Graphic_slide has no subgraphics " + thingDef, 358773632);
				return;
			}

            //int frameNum = Find.TickManager.TicksGame % this.subGraphics.Length;

            /*
			if ( frameI >= this.subGraphics.Length)
			{
				Log.ErrorOnce("slide drawing out of range: " + frameI, 7453435);
                frameI = 0;
			}
            */

            //if ((frameI >= frameMax) || if (frameI >= this.subGraphics.Length))
            if (frameI >= frameMax)
            {
                Comp_LTF_TpSpot tpSpot = null;
                tpSpot = getTpSpot(thing);
                if (tpSpot == null)
                {
                    Log.Warning("can tpspot");
                    return;
                }

                Unset();
                tpSpot.StopVanish();
                //Log.Warning("<<Stoppin " + frameI + "/" + frameMax + "/" + this.subGraphics.Length);
                return;
            }

            Graphic graphic = this.subGraphics[frameI];
            Vector3 dotS = new Vector3(1f, 1f, 1f);

            Vector3 drawPos = thing.DrawPos;
            if (drawPos == null)
            {
                Log.Warning("null pos draw");
                return;
            }

            // higher than ground to be visible
            drawPos.y += 4;
            //drawPos.y += 0.046875f;

            //Vector3 dotPos = thing.Position.ToVector3Shifted();

            Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(drawPos, Quaternion.identity, dotS);
			Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
            //Log.Warning("Drew " + frameI + "/" + frameMax);

            frameI += 1;

            
        }

        private Comp_LTF_TpSpot getTpSpot(Thing thing)
        {
            Building building = thing as Building;
            if (building == null)
            {
                return null;
            }

            Comp_LTF_TpSpot tpSpot = null;
            tpSpot = building.TryGetComp<Comp_LTF_TpSpot>();

            return tpSpot;
        }

        private void Unset()
        {
            //willDrawNextTick = false;
            isInitialized = false;
            frameI = 0;
            Log.Warning("Unset");
        }

        
        /*
        private void SetFrameRef()
        {
            frameRef = Find.TickManager.TicksGame;
        }
        */

        public override string ToString()
		{
			return string.Concat(new object[]
			{
				"Flicker(subGraphic[0]=",
				this.subGraphics[0].ToString(),
				", count=",
				this.subGraphics.Length,
				")"
			});
		}
	}
}