﻿/*
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
    public class Gfx
    {
        /* Graphics */
        /******************/
        //public Mesh mesh1x1 = MeshPool.plane10;

        public static string overlayPath = "Things/Building/TpSpot/Overlay/";

        public enum OpacityWay
        {
            no,
            forced,
            pulse,
            loop
        };

        public enum Layer 
        {
            over = 4,
            under = -1,
        };

        //'nothing':'white','pawn':'yellow','item':'magenta',
        //'nothing':'white','pawn':'yellow','item':'magenta',
        //public static readonly Material EmptyTile = MaterialPool.MatFrom(overlayPath + "PoweredTpSpot", ShaderDatabase.MoteGlow);

        public static readonly Material Empty = MaterialPool.MatFrom(overlayPath + "Swirl", ShaderDatabase.MoteGlow);

        public static readonly Material HumanoidFaction = MaterialPool.MatFrom(overlayPath + "Swirl", ShaderDatabase.MoteGlow, Color.yellow);
        public static readonly Material Humanoid = MaterialPool.MatFrom(overlayPath + "Swirl", ShaderDatabase.MoteGlow, Color.red);

        public static readonly Material AnimalFaction = MaterialPool.MatFrom(overlayPath + "Swirl", ShaderDatabase.MoteGlow, Color.magenta);
        public static readonly Material Animal = MaterialPool.MatFrom(overlayPath + "Swirl", ShaderDatabase.MoteGlow, Color.blue);

        public static readonly Material ItemFaction = MaterialPool.MatFrom(overlayPath + "Swirl", ShaderDatabase.MoteGlow, Color.cyan);
        public static readonly Material Item = MaterialPool.MatFrom(overlayPath + "Swirl", ShaderDatabase.MoteGlow, Color.green);

        // public static readonly Material EmptyTile = MaterialPool.MatFrom(overlayPath + "PoweredTpSpot", ShaderDatabase.Transparent);
        public static readonly Graphic Vanish = GraphicDatabase.Get<Graphic_Slideshow>(overlayPath + "Vanish", ShaderDatabase.MetaOverlay);

        //Gizmo
        private static string GizmoPath = "UI/Commands/";
        private static string DebugPath = GizmoPath + "Debug/";
        private static string HaxPath = GizmoPath + "Hax/";
        private static string QualityPath = GizmoPath + "Quality/";

        public static Texture2D DebugOnGz = ContentFinder<Texture2D>.Get(DebugPath + "DebugOn", true);
        public static Texture2D DebugOffGz = ContentFinder<Texture2D>.Get(DebugPath + "DebugOff", true);
        public static Texture2D DebugLogGz = ContentFinder<Texture2D>.Get(DebugPath + "DebugLog", true);

        public static Texture2D HaxAddGz = ContentFinder<Texture2D>.Get(HaxPath + "HaxAdd", true);
        public static Texture2D HaxSubGz = ContentFinder<Texture2D>.Get(HaxPath + "HaxSub", true);
        public static Texture2D HaxFullGz = ContentFinder<Texture2D>.Get(HaxPath + "HaxFull", true);
        public static Texture2D HaxEmptyGz = ContentFinder<Texture2D>.Get(HaxPath + "HaxEmpty", true);
        public static Texture2D HaxWorseGz = ContentFinder<Texture2D>.Get(HaxPath + "HaxWorse", true);
        public static Texture2D HaxBetterGz = ContentFinder<Texture2D>.Get(HaxPath + "HaxBetter", true);

        private static float UpdateOpacity(Thing thing, OpacityWay opacityWay = OpacityWay.no, float opacity=1, bool debug=false)
        {
            float newOpacity = -1;
            float checkBackup = -1;

            switch (opacityWay)
            {
                case OpacityWay.no:
                    newOpacity = 1;
                    break;
                case OpacityWay.forced:
                    newOpacity = opacity;
                    // default
                    break;
                case OpacityWay.pulse:
                    newOpacity = PulseOpacity(thing);
                    break;
                case OpacityWay.loop:
                    newOpacity = LoopOpacity(thing);
                    break;
            }

            checkBackup = newOpacity;
            if (checkBackup != (newOpacity = Tools.LimitToRange(newOpacity, 0, 1)))
                if(debug)
                    Log.Warning("dumb opacity("+opacityWay+"):" + newOpacity +"(def="+opacity+")");

            return newOpacity;
        }


        public static void DrawTickRotating(Thing thing, Material dotM, float x, float z, Layer myLayer = Layer.over, bool debug=false)
        {
            Vector3 dotS = new Vector3(1f, 1f, 1f);
            Matrix4x4 matrix = default(Matrix4x4);
            Vector3 dotPos = thing.TrueCenter();

            dotPos.x += x;
            dotPos.z += z;
            dotPos.y += (float)myLayer;
            float angle = (float)360 * LoopOpacity(thing);

            Tools.Warn("tickRot angle: " + angle, debug);
            matrix.SetTRS(dotPos, Quaternion.AngleAxis(angle, Vector3.up), dotS);
            Graphics.DrawMesh(MeshPool.plane10, matrix, dotM, 0);
        }

        public static void DrawRandRotating(Thing thing, Material dotM, float x, float z, Layer myLayer = Layer.over, bool debug=false)
        {
            Vector3 dotPos = thing.DrawPos;

            dotPos.x += x;
            dotPos.z += z;
            dotPos.y += (float)myLayer;

            float angle = (float)Rand.Range(0, 360);

            Tools.Warn("randRot angle: " + angle, debug);
            Vector3 dotS = new Vector3(1f, 1f, 1f);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(dotPos, Quaternion.AngleAxis(angle, Vector3.up), dotS);

            Graphics.DrawMesh(MeshPool.plane10, matrix, dotM, 0);
        }

        public static void DrawPulse(
            Thing thing, Material mat, Mesh mesh,
            Layer myLayer = Layer.over, 
            OpacityWay opacityWay = OpacityWay.no,
            bool debug = false )
        {
            float myOpacity = UpdateOpacity(thing, opacityWay, 1, debug);

            Material material = FadedMaterialPool.FadedVersionOf(mat, myOpacity);
            Vector3 gfxPos = thing.DrawPos;

            gfxPos.y += (float)myLayer;
              
            Vector3 dotS = new Vector3(1f, 1f, 1f);
            Matrix4x4 matrix = default(Matrix4x4);

            matrix.SetTRS(gfxPos, Quaternion.AngleAxis(0f, Vector3.up), dotS);

            Graphics.DrawMesh(mesh, matrix, material, 0);

            if (debug)
                Log.Warning(
                    thing.LabelShort +
                    "; opa: " + myOpacity +
                    "; pos: " + gfxPos +
                    "; col: " + mat.color
                    );
        }
        public void DrawColorPulse(Thing thing, Material mat, Vector3 drawPos, Mesh mesh, Color color)
        {
            float myOpacity = PulseOpacity(thing);
            Material material = FadedMaterialPool.FadedVersionOf(mat, myOpacity);
            ChangeColor(mat, color, myOpacity);

            Vector3 dotS = new Vector3(1f, 1f, 1f);
            Matrix4x4 matrix = default(Matrix4x4);

            matrix.SetTRS(drawPos, Quaternion.AngleAxis(0f, Vector3.up), dotS);

            Graphics.DrawMesh(mesh, matrix, material, 0);
        }
        private void Draw1x1Overlay(Vector3 buildingPos, Material gfx, Mesh mesh, float drawSize, bool debug)
        {
            Vector3 dotS = new Vector3(drawSize, 1f, drawSize);
            Matrix4x4 matrix = default(Matrix4x4);

            Graphics.DrawMesh(mesh, matrix, gfx, 0);
            if (debug)
                Log.Warning("Drew:" + gfx.color);
        }
        public void Draw1x1OverlayBS(Vector3 buildingPos, Material gfx, Mesh mesh, float x, float z,
                                Color overlayColor,
                                bool randRotation = false,
                                bool randFlicker = false,
                                bool oscillatingOpacity = false,
                                float currentOpacity =.5f,
                                float noFlickChance = .985f, float minOpacity = .65f, float maxOpacity = 1f,
                                bool debug = false
                                )

        {
            Vector3 dotPos = buildingPos;
            dotPos.x += x;
            dotPos.z += z;

            Vector3 dotS = new Vector3(1f, 1f, 1f);
            Matrix4x4 matrix = default(Matrix4x4);

            float flickChance = 1 - noFlickChance;

            float angle = 0f;
            if (randRotation == true)
                angle = (float)Rand.Range(0, 360);

            float calculatedOpacity = maxOpacity;

            Material fMat = null;
            if (randFlicker)
            {
                if (Rand.Chance(flickChance))
                {
                    calculatedOpacity -= (currentOpacity - minOpacity) / 4;
                }
            }

            if (calculatedOpacity < minOpacity)
            {
                calculatedOpacity = minOpacity;
            }
            else if (calculatedOpacity > maxOpacity)
            {
                calculatedOpacity = maxOpacity;
            }

            fMat = FadedMaterialPool.FadedVersionOf(gfx, calculatedOpacity);

            matrix.SetTRS(dotPos, Quaternion.AngleAxis(angle, Vector3.up), dotS);
            if (mesh == null)
            {
                Log.Warning("mesh null");
                return;
            }

            Color newColor = overlayColor;
            newColor.a = calculatedOpacity;
            fMat.color = newColor;

            Graphics.DrawMesh(mesh, matrix, fMat, 0);
            if (debug)
                Log.Warning("Drew:" + newColor);
        }

        public static void ChangeColor(Material mat, Color color, float opacity=-1, bool debug=false)
        {
            Color newColor = color;

            if (debug)
                Log.Warning("In Color: " + mat.color);

            if (opacity == -1)
            {

                newColor.a = Tools.LimitToRange(mat.color.a, 0, 1);
            }
            newColor.a = opacity;
            mat.color = newColor;

            if (debug)
                Log.Warning("Out Color: " + mat.color);
        }

        public static float PulseOpacity(Thing thing)
        {
            float num = (Time.realtimeSinceStartup + 397f * (float)(thing.thingIDNumber % 571)) * 4f;
            float num2 = ((float)Math.Sin((double)num) + 1f) * 0.5f;
            num2 = 0.3f + num2 * 0.7f;

            return num2;
        }
        public static float LoopFactorOne(Thing thing)
        {
            float num = (Time.realtimeSinceStartup + 397f * (float)(thing.thingIDNumber % 571)) * 4f;
            float num2 = ((float)Math.Tan((double)num) + 1f) * 0.5f;
            num2 = (0.3f + num2 * 0.7f)%1;

            return num2;
        }

        public static float PulseFactorOne(Thing thing)
        {
            float timePhaseShiftValue = 397f;
            float thingPhaseShiftValue = 571f;
            float speedUp = 2f;

            float num = (Time.realtimeSinceStartup + timePhaseShiftValue * (float)(thing.thingIDNumber % thingPhaseShiftValue)) * speedUp;
            float num2 = ((float)Math.Sin((double)num) + 1f) * 0.5f;

            num2 = num2 % 1;

            return num2;
        }


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