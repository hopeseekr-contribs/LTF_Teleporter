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
        static string IssuePath = GizmoPath + "TpBench/Issue";

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
        public static Texture2D TpLogGz = ContentFinder<Texture2D>.Get(BenchPath + "RegistryLog", true);
        public static Texture2D EmptyRegistryGz = ContentFinder<Texture2D>.Get(BenchPath + "RegistryEmpty", true);
        public static Texture2D FullRegistryGz = ContentFinder<Texture2D>.Get(BenchPath + "RegistryFull", true);

        public static Texture2D NextTpGz = ContentFinder<Texture2D>.Get(BenchPath + "NextTp", true);

        public static Texture2D IssuePowerGz = ContentFinder<Texture2D>.Get(IssuePath + "NoPower", true);
        public static Texture2D IssueNoPoweredFacilityGz = ContentFinder<Texture2D>.Get(IssuePath + "NoPoweredFacility", true);

        public static Texture2D IssueNoFacilityGz = ContentFinder<Texture2D>.Get(IssuePath + "NoFacility", true);
        public static Texture2D IssueOrphanGz = ContentFinder<Texture2D>.Get(IssuePath + "Orphan", true);

        public static Texture2D IssueCooldownGz = ContentFinder<Texture2D>.Get(IssuePath + "Cooldown", true);
        public static Texture2D IssueOverweightGz = ContentFinder<Texture2D>.Get(IssuePath + "Overweight", true);

        public static Texture2D[] WayGizmo = { WayNoGz, WayOutGz, WayInGz, WaySwapGz };

        public static Texture2D EmptyStatus2Gizmo(bool empty, bool full)
        {
            Texture2D Answer = TpLogGz;

            if (empty)
                Answer = EmptyRegistryGz;
            if (full)
                Answer = FullRegistryGz;

            return Answer;
        }
    }
}
