using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace LTF_Teleport
{
    public static class MyWay
    {
        static string[] WayLabel = 
            {
                "No way",
                "Tp out",
                "Tp in",
                "Swap" };
        static string[] WayArrow = { "(x)", " =>", "<= ", "<=>" };
        static string[] WayActionLabel = { "do nothing", "send away", "bring back", "exchange" };

        public enum Way {
            [Description("No way")]
            No = 0,
            [Description("Out")]
            Out = 1,
            [Description("In")]
            In = 2,
            [Description("Swap")]
            Swap = 3 };
        public static SimpleColor[] WayColor = { SimpleColor.White, SimpleColor.Red, SimpleColor.Cyan, SimpleColor.Magenta };

        public static Way NextWay(this Way MyWay, bool IsOrphan)
        {
            Way Answer = Way.No;

            if (IsOrphan)
                return Answer;

            Answer = ((MyWay == Way.Swap) ? (Way.No) : (MyWay.Next()));

            if ((int)Answer > (int)Way.Swap)
                Answer = Way.No;

            return Answer;
        }

        public static void BrowseWay(this Way MyWay, Comp_LTF_TpSpot compTwin)
        {
            MyWay = MyWay.NextWay(compTwin.IsOrphan);
            switch (MyWay)
            {
                case Way.Out:
                    compTwin.myWay = Way.In;
                    break;
                case Way.In:
                    compTwin.myWay = Way.Out;
                    break;
                case Way.Swap:
                    compTwin.myWay = Way.Swap;
                    break;
                case Way.No:
                    compTwin.myWay = Way.No;
                    break;
            }
        }
        public static bool InvalidWay(this Way MyWay)
        {
            return (!MyWay.ValidWay());
        }
        public static bool ValidWay(this Way MyWay)
        {
            int cast = (int)MyWay;
            int min = (int)Way.No;
            int max = (int)Way.Swap;

            return ((cast >= min) && (cast <= max));
        }

        public static string WayNaming(this Way MyWay)
        {
                if ((int)MyWay > (WayLabel.Length - 1))
                    return ("way outbound");

                return (WayLabel[(int)MyWay]);
        }

        public static string NextWayNaming(this Way MyWay, bool IsOrphan)
        {
            if ((int)MyWay.NextWay(IsOrphan) > WayLabel.Length - 1)
                return ("next way outbound");

            return (WayLabel[(int)MyWay.NextWay(IsOrphan)]);
        }

        public static string WayActionLabeling(this Way MyWay)
        {
            if ((int)MyWay > WayActionLabel.Length - 1)
                return ("way action outbound");

            return (WayActionLabel[(int)MyWay]);
        }

        public static string WayArrowLabeling(this Way MyWay)
        {

            if ((int)MyWay > WayArrow.Length - 1)
                return ("Arrow outbound");

            return (WayArrow[(int)MyWay]);
        }

        public static SimpleColor WayColoring(this Way MyWay)
        {
            if ((int)MyWay > WayColor.Length - 1)
                return (SimpleColor.White);
            //return ("Color outbound");

            return (WayColor[(int)MyWay]);
        }

        public static void ResetWay(this Way MyWay)
        {
            MyWay = Way.No;
        }
        public static void SetOut(this Way MyWay)
        {
            MyWay = Way.Out;
        }
        public static void SetIn(this Way MyWay)
        {
            MyWay = Way.In;
        }
        public static void SetSwap(this Way MyWay)
        {
            MyWay = Way.Swap;
        }

        public static bool IsIn(this Way MyWay)
        {
            return MyWay == Way.In;
        }
        public static bool IsOut(this Way MyWay)
        {
            return MyWay == Way.Out;
        }

        public static bool IsSwap(this Way MyWay)
        {
            return MyWay == Way.Swap;
        }

        public static string WayDescription(this Way MyWay, string AutoLabeling, string myDefName, bool IsLinked)
        {
            string Answer = string.Empty;

            Answer +=
                AutoLabeling +
                "will " + MyWay.WayActionLabeling() + ' ' +
                "what stands on this " + myDefName;

            if (IsLinked)
            {
                Answer += " and its twin.";
            }

            return Answer;

        }
        public static Texture2D WayGizmoing(this Way MyWay)
        {

            if ((int)MyWay > (MyGizmo.WayGizmo.Length - 1))
                return null;

            return (MyGizmo.WayGizmo[(int)MyWay]);
        }
    }
}