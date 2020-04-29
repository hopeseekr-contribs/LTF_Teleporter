using System;
using UnityEngine;
using Verse;

namespace LTF_Teleport
{
	public class CompProperties_MatrixOverlay : CompProperties
	{
		public Vector2 animationSize = new Vector2(1,1);
		public Vector3 offset = new Vector3(0,0,0);

        public Gfx.Layer layer = Gfx.Layer.over;

		public CompProperties_MatrixOverlay()
		{
			compClass = typeof(CompMatrixOverlay);
		}
	}
}