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
    public class MyGfx
    {
        public static string basePath = "Things/Building/TpSpot/";
        
        public static string underlayPath = basePath + "Underlay/";
        public static string overlayPath = basePath + "Overlay/";
        public static string warningPath = overlayPath + "Warning/";

        // Underlay
        public static readonly Material WayOutM = MaterialPool.MatFrom(underlayPath + "WayOut", ShaderDatabase.MoteGlow);
        public static readonly Material WayInM = MaterialPool.MatFrom(underlayPath + "WayIn", ShaderDatabase.MoteGlow);
        public static readonly Material WaySwapM = MaterialPool.MatFrom(underlayPath + "WaySwap", ShaderDatabase.MoteGlow);
        public static readonly Material InitOrderM = MaterialPool.MatFrom(underlayPath + "InitOrder", ShaderDatabase.MoteGlow);

        public static readonly Material UnderlayM = MaterialPool.MatFrom(underlayPath + "Underlay", ShaderDatabase.MoteGlow);

        // Warning
        public static readonly Material OverweightM = MaterialPool.MatFrom(warningPath + "Overweight", ShaderDatabase.MetaOverlay);
        public static readonly Material CooldownM = MaterialPool.MatFrom(warningPath + "Cooldown", ShaderDatabase.MetaOverlay);
        public static readonly Material FacilityM = MaterialPool.MatFrom(warningPath + "Facility", ShaderDatabase.MetaOverlay);
        public static readonly Material FacilityPowerM = MaterialPool.MatFrom(warningPath + "FacilityPower", ShaderDatabase.MetaOverlay);

        // Overlay
        //        public static readonly Material Empty = MaterialPool.MatFrom(overlayPath + "Overlay", ShaderDatabase.MoteGlow);
        public static readonly Material HumanoidFaction = MaterialPool.MatFrom(overlayPath + "Overlay", ShaderDatabase.MetaOverlay, Color.yellow);
        public static readonly Material Humanoid = MaterialPool.MatFrom(overlayPath + "Overlay", ShaderDatabase.MetaOverlay, Color.red);
        public static readonly Material AnimalFaction = MaterialPool.MatFrom(overlayPath + "Overlay", ShaderDatabase.MetaOverlay, Color.magenta);
        public static readonly Material Animal = MaterialPool.MatFrom(overlayPath + "Overlay", ShaderDatabase.MetaOverlay, Color.blue);
        public static readonly Material ItemFaction = MaterialPool.MatFrom(overlayPath + "Overlay", ShaderDatabase.MetaOverlay, Color.cyan);
        public static readonly Material Item = MaterialPool.MatFrom(overlayPath + "Overlay", ShaderDatabase.MetaOverlay, Color.green);

        // public static readonly Material EmptyTile = MaterialPool.MatFrom(overlayPath + "PoweredTpSpot", ShaderDatabase.Transparent);
        public static readonly Graphic ActiveAnim = GraphicDatabase.Get<GfxSlideShow.Graphic_Slideshow>(overlayPath + "TpAnim", ShaderDatabase.MetaOverlay);
        public static readonly Graphic EndAnim = GraphicDatabase.Get<GfxSlideShow.Graphic_Slideshow>(overlayPath + "Vanish", ShaderDatabase.MetaOverlay);

        public static Material Status2OverlayMaterial(Comp_LTF_TpSpot comp, bool FactionMajority, bool debug = false)
        {
            Material Answer = null;
            string checkIf = string.Empty;

            // Nothing 
            if (comp.HasNothing)
            {
                checkIf = "nothing gfx";
                Tools.Warn(checkIf, debug);
                return null;
            }

            // something 
            if (comp.HasHumanoid)
            {
                checkIf = "humanoid f:" + FactionMajority;
                Answer = ((FactionMajority) ? (HumanoidFaction) : (Humanoid));

            }
            else if (comp.HasAnimal)
            {
                checkIf = "animal f:" + FactionMajority;
                Answer = ((FactionMajority) ? (AnimalFaction) : (Animal));
            }
            else
            {
                checkIf = "Item f:" + FactionMajority;
                Answer = ((FactionMajority) ? (ItemFaction) : (Item));
            }

            Tools.Warn(checkIf, debug);
            return Answer;
        }
        public static Material Status2WarningMaterial(Comp_LTF_TpSpot comp, bool debug = false)
        {
            Material Answer = null;
            string checkIf = string.Empty;

            // no facility
            if (comp.StatusNoFacility)
            {
                checkIf = "no facility gfx";
                Tools.Warn(checkIf, debug);
                return MyGfx.FacilityM;
            }

            // no facility power
            if (!comp.HasPoweredFacility)
            {
                checkIf = "no facility power gfx";
                Tools.Warn(checkIf, debug);
                return MyGfx.FacilityPowerM;
            }
            
            // cooldown
            if (comp.StatusChillin)
            {
                checkIf = "cooldown gfx";
                Tools.Warn(checkIf, debug);
                return MyGfx.CooldownM;
            }
            // overwieght
            if (comp.StatusOverweight)
            {
                checkIf = "overweight gfx";
                Tools.Warn(checkIf, debug);
                return MyGfx.OverweightM;

            }

            checkIf += "everything fine;no warning";
            Tools.Warn(checkIf, debug);
            return Answer;
        }
        /*
        public static Material Status2UnderlayMaterial(Comp_LTF_TpSpot comp, bool debug = false)
        {
            Material Answer = null;
            string what = "Gfx - Under: ";

            if (comp.StatusReady)
            {
                Answer = SomethingM;
                Tools.Warn(what + "something",debug);
                return Answer;
            }

            if ((comp.HasNothing) && (comp.HasPoweredFacility))
            {
                Answer = NothingM;
                Tools.Warn(what + "nothing", debug);
                return Answer;
            }

            if (comp.HasItems)
            {
                Answer = NotReadyM;
                Tools.Warn(what + "notready", debug);
                return Answer;
            }

            Tools.Warn(what+"null", debug);
            return Answer;
        }
        */
        public static Material Status2UnderlayMaterial(Comp_LTF_TpSpot comp, bool debug = false)
        {
            Material Answer = null;
            string what = "Gfx - Under: ";

            switch (comp.MyWay)
            {
                case Comp_LTF_TpSpot.Way.Out:
                    Answer = WayOutM;
                    break;
                case Comp_LTF_TpSpot.Way.In:
                    Answer = WayInM;
                    break;
                case Comp_LTF_TpSpot.Way.Swap:
                    Answer = WaySwapM;
                    break;
                default:
                    return null;
            }

            if (comp.TpSequenceBegin)
                Answer = InitOrderM;

            Tools.Warn(what + "null", debug);
            return Answer;
        }


    }
}
