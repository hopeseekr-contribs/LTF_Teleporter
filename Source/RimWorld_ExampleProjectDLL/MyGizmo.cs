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
    public class MyGizmo
    {
        //static string basePath = "Things/Building/TpSpot/";
        static string GizmoPath = "UI/Commands/";

        static string DebugPath = GizmoPath + "Debug/";
        static string HaxPath = GizmoPath + "Hax/";
        static string QualityPath = GizmoPath + "Quality/";

        static string SpotPath = GizmoPath + "TpSpot/";
        static string BenchPath = GizmoPath + "TpBench/";

        //Common
        public static Texture2D DebugOnGz = ContentFinder<Texture2D>.Get(DebugPath + "DebugOn", true);
        public static Texture2D DebugOffGz = ContentFinder<Texture2D>.Get(DebugPath + "DebugOff", true);
        public static Texture2D DebugLogGz = ContentFinder<Texture2D>.Get(DebugPath + "DebugLog", true);

        public static Texture2D HaxAddGz = ContentFinder<Texture2D>.Get(HaxPath + "HaxAdd", true);
        public static Texture2D HaxSubGz = ContentFinder<Texture2D>.Get(HaxPath + "HaxSub", true);
        public static Texture2D HaxFullGz = ContentFinder<Texture2D>.Get(HaxPath + "HaxFull", true);
        public static Texture2D HaxEmptyGz = ContentFinder<Texture2D>.Get(HaxPath + "HaxEmpty", true);
        public static Texture2D HaxWorseGz = ContentFinder<Texture2D>.Get(HaxPath + "HaxWorse", true);
        public static Texture2D HaxBetterGz = ContentFinder<Texture2D>.Get(HaxPath + "HaxBetter", true);

        public static Texture2D QualityBadGz = ContentFinder<Texture2D>.Get(QualityPath + "Bad", true);
        public static Texture2D QualityGoodGz = ContentFinder<Texture2D>.Get(QualityPath + "Good", true);
        public static Texture2D QualityNormalGz = ContentFinder<Texture2D>.Get(QualityPath + "Normal", true);

        // TpSpot
        public static Texture2D AutoOnGz = ContentFinder<Texture2D>.Get(SpotPath + "AutoOn", true);
        public static Texture2D AutoOffGz = ContentFinder<Texture2D>.Get(SpotPath + "AutoOff", true);

        public static Texture2D LinkedGz = ContentFinder<Texture2D>.Get(SpotPath + "LinkOn", true);
        public static Texture2D OrphanGz = ContentFinder<Texture2D>.Get(SpotPath + "LinkOff", true);

        public static Texture2D WayNoGz = ContentFinder<Texture2D>.Get(SpotPath + "WayNo", true);
        public static Texture2D WayInGz = ContentFinder<Texture2D>.Get(SpotPath + "WayIn", true);
        public static Texture2D WayOutGz = ContentFinder<Texture2D>.Get(SpotPath + "WayOut", true);
        public static Texture2D WaySwapGz = ContentFinder<Texture2D>.Get(SpotPath + "WaySwap", true);

        // TpBench
        public static Texture2D TpLogGz = ContentFinder<Texture2D>.Get(BenchPath + "TpLog", true);
        public static Texture2D NextTpGz = ContentFinder<Texture2D>.Get(BenchPath + "NextTp", true);

        public static Texture2D EmptyRegistryGz = ContentFinder<Texture2D>.Get(BenchPath + "EmptyRegistry", true);
        public static Texture2D FullRegistryGz = ContentFinder<Texture2D>.Get(BenchPath + "FullRegistry", true);

        public static Texture2D EmptyStatus2Gizmo(bool empty, bool full)
        {
            Texture2D Answer = MyGizmo.TpLogGz;

            if (empty)
                Answer = MyGizmo.EmptyRegistryGz;
            if (full)
                Answer = MyGizmo.FullRegistryGz;

            return Answer;
        }
    }
}
