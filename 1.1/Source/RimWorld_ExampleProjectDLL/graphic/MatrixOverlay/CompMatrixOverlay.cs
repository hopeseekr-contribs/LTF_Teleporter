using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace LTF_Teleport
{
	[StaticConstructorOnStartup]
	public class CompMatrixOverlay : ThingComp
	{
		protected CompPowerTrader powerComp;
        
        public static readonly Graphic AnimationGraphic = GraphicDatabase.Get<Graphic_MatrixOverlay>("AnimationOverlay/matrix", ShaderDatabase.TransparentPostLight, Vector2.one, Color.white);
        public CompProperties_MatrixOverlay Props
		{
			get
			{
				return (CompProperties_MatrixOverlay)props;
			}
		}

		public override void PostDraw()
		{
			base.PostDraw();
			if (parent == null || !powerComp.PowerOn)
			{
				return;
			}
			Vector3 drawPos = parent.DrawPos;
            AnimationGraphic.Draw(drawPos, Rot4.North, this.parent, 0f);
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
        }
	}
}