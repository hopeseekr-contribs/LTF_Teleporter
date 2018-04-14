/*
 * Created by SharpDevelop.
 * User: Etienne
 * Date: 22/11/2017
 * Time: 16:43
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
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
    [StaticConstructorOnStartup]
    public class GfxSlideShow
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

                if (thing.def.defName != "LTF_TpSpot")
                {
                    Tools.Warn("slide show for tp spot only", true);
                    return;
                }

                Comp_LTF_TpSpot comp = null;
                comp = getTpSpot(thing);
                if (comp == null)
                {
                    Tools.Warn("Cant find tpspot comp", true);
                    return;
                }

                if (frameI >= frameMax)
                {
                    Unset();
                    if ((comp.TpOutActive) || (comp.TpOutEnd))
                        //if ((tpSpot.TpOutBegin) || (tpSpot.TpOutActive))
                        comp.NextAnim();

                    if (comp.TpOutNa)
                        comp.SlideShowOn = false;
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
                //matrix.SetTRS(drawPos, (extraRotation != 0)?(Quaternion.AngleAxis(extraRotation, Vector3.up)):Quaternion.identity, dotS);
                matrix.SetTRS(loc, (extraRotation != 0) ? (Quaternion.AngleAxis(extraRotation, Vector3.up)) : Quaternion.identity, dotS);

                Material material = FadedMaterialPool.FadedVersionOf(graphic.MatSingle, comp.AnimOpacity);

                Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
                //Log.Warning("Drew " + frameI + "/" + frameMax);
                //Tools.Warn("2tick1true:"+ Tools.TwoTicksOneTrue(5), true);
                if (comp.IncFrameSlower() == 0) {
                    frameI += 1;
                    comp.SetFrameSlower();
                }

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
                //Log.Warning("Unset");
            }
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
}
