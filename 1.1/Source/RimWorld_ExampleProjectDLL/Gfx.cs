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
    public class Gfx
    {
        /* Graphics */
        /******************/
        public enum OpacityWay
        {
            no,
            forced,
            pulse,
            loop
        };

        public enum AnimStep
        {
            na      = 0,
            begin   = 1,
            active  = 2,
            end     = 3,
        };

        public enum Layer 
        {
            over = 4,
            under = -1,
        };


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
                    newOpacity = LoopFactorOne(thing);
                    break;
            }

            checkBackup = newOpacity;
            if (checkBackup != (newOpacity = Tools.LimitToRange(newOpacity, 0, 1)))
                if(debug)
                    Log.Warning("dumb opacity("+opacityWay+"):" + newOpacity +"(def="+opacity+")");

            return newOpacity;
        }

        public static void DrawTickRotating(Thing thing, Material mat, float x, float z, float size=1f, float angle=0f, float opacity=1, Layer myLayer = Layer.over, bool debug = false)
        {
            Vector3 dotS = new Vector3(size, 1f, size); 

            Matrix4x4 matrix = default(Matrix4x4);
            Vector3 dotPos = thing.TrueCenter();

            dotPos.x += x;
            dotPos.z += z;
            dotPos.y += (float)myLayer;

            Material fadedMat = mat;

            if (opacity!=1)
                fadedMat = FadedMaterialPool.FadedVersionOf(mat, opacity);

            Tools.Warn("Drawing - ang: " + angle + "; opa:"+opacity, debug);
            matrix.SetTRS(dotPos, Quaternion.AngleAxis(angle, Vector3.up), dotS);
            Graphics.DrawMesh(MeshPool.plane10, matrix, fadedMat, 0);

            
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
        public void Draw1x1Overlay(Vector3 buildingPos, Material gfx, Mesh mesh, float drawSize, bool debug)
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

        // Meat grinder
        public static void PulseWarning(Thing thing, Material mat)
        {
            Material material = FadedMaterialPool.FadedVersionOf(mat, VanillaPulse(thing));
            Vector3 dotS = new Vector3(.6f, 1f, .6f);
            Matrix4x4 matrix = default(Matrix4x4);
            Mesh mesh = MeshPool.plane14;
            matrix.SetTRS(thing.DrawPos, Quaternion.AngleAxis(0f, Vector3.up), dotS);
            Graphics.DrawMesh(mesh, matrix, material, 0);
        }

        public static float PulseOpacity(Thing thing, float mask = 1, bool debug=false)
        {
            float num = (Time.realtimeSinceStartup + 397f * (float)(thing.thingIDNumber % 571)) * 4f;
            float num2 = ((float)Math.Sin((double)num) + 1f) * 0.5f;

            Tools.Warn("pulse opacity: !" + num2 + "; mask: "+mask+"; masked: " +num%mask, debug);
            num2 = num2 % mask;
            
            return num2;
        }
        // mask = 1 opacity ; mask 360 rotation
        public static float LoopFactorOne(Thing thing, float mask=1, bool debug = false)
        {
            float num = (Time.realtimeSinceStartup + 397f * (float)(thing.thingIDNumber % 571)) * 4f;
            float num2 = ((float)Math.Tan((double)num) + 1f) * 0.5f;
            //num2 = (0.3f + num2 * 0.7f)%1;
            Tools.Warn("loop factor one" + num2 + "; mask: " + mask + "; masked: " + num % mask,debug);
            num2 = num2 % mask;
            return num2;
        }

        public static float VanillaPulse(Thing thing)
        {
            float num = (Time.realtimeSinceStartup + 397f * (float)(thing.thingIDNumber % 571)) * 4f;
            float num2 = ((float)Math.Sin((double)num) + 1f) * 0.5f;
            num2 = 0.3f + num2 * 0.7f;
            return num2;
        }
        public static float PulseFactorOne(Thing thing, float mask = 1, bool debug = false)
        {
            float timePhaseShiftValue = 397f;
            float thingPhaseShiftValue = 571f;
            float speedUp = 2f;

            float num = (Time.realtimeSinceStartup + timePhaseShiftValue * (float)(thing.thingIDNumber % thingPhaseShiftValue)) * speedUp;
            float num2 = ((float)Math.Sin((double)num) + 1f) * 0.5f;
            Tools.Warn("pulse factor one: " + num2 + "; mask: " + mask + "; masked: " + num % mask,debug);
            num2 = num2 % mask;

            return num2;
        }
        public static float RealLinear(Thing thing, float speedUp, bool debug = false)
        {
            float timePhaseShiftValue = 397f; float thingPhaseShiftValue = 571f;

            float num = (Time.realtimeSinceStartup + timePhaseShiftValue * (float)(thing.thingIDNumber % thingPhaseShiftValue)) * speedUp * 20;
            float num2 = (num % 1000) / 1000;

            Tools.Warn("RealLinear: " + num + "->" + num2, debug);

            return num2;
        }
        public static float RealtimeFactor(Thing thing, float mask = 1f, bool debug=false)
        {
            float timePhaseShiftValue = 397f; float thingPhaseShiftValue = 571f;
            float speedUp = 2f;

            float num = (Time.realtimeSinceStartup + timePhaseShiftValue * (float)(thing.thingIDNumber % thingPhaseShiftValue)) * speedUp;
            float num2 = ((float)Math.Cos((double)-num) + 1f) * 0.5f;

            Tools.Warn("RealtimeFactor:" + num2 + "->" + num2 % mask,debug);

            return (num2 % mask);
        }

    }
}
