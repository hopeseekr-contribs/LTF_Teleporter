using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace LTF_Teleport
{
	public class Graphic_MatrixOverlay : Graphic_Collection
	{
		private const int BaseTicksPerFrameChange = 7;
		private const int ExtraTicksPerFrameChange = 10;

		private const float MaxOffset = 0.05f;

        private bool myDebug = true;

		public override Material MatSingle
		{
			get
			{
                int stairCaseInput = (int)Math.Floor( (double)(Find.TickManager.TicksGame / BaseTicksPerFrameChange) );
                int curFrame = stairCaseInput % subGraphics.Length;
                return subGraphics[curFrame].MatSingle;
			}
		}

		public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
		{
			if (thingDef == null)
			{
				Log.ErrorOnce("Graphic_Animation DrawWorker with null thingDef: " + loc, 3427324, false);
				return;
			}
			if (this.subGraphics == null)
			{
				Log.ErrorOnce("Graphic_Animation has no subgraphics " + thingDef, 358773632, false);
				return;
			}

            CompProperties_MatrixOverlay compProperties_AnimationOverlay = null;

            Vector2 animationSize = new Vector2(1,1);
            float layer = (float)Gfx.Layer.over;
            Vector3 vector = thing.DrawPos;

            compProperties_AnimationOverlay = thingDef.GetCompProperties<CompProperties_MatrixOverlay>();
            if (compProperties_AnimationOverlay != null)
            {
                animationSize = compProperties_AnimationOverlay.animationSize;
                layer = (float)compProperties_AnimationOverlay.layer;
                vector += compProperties_AnimationOverlay.offset;
            }

            Vector3 size = new Vector3(animationSize.x, 1f, animationSize.y);
            Matrix4x4 matrix = default(Matrix4x4);

            vector.y += layer;
            matrix.SetTRS(vector, Quaternion.AngleAxis(0f, Vector3.up), size);

            Graphics.DrawMesh(MeshPool.plane14, matrix, MatSingle, (int)layer);
		}
	}
}
 