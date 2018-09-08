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
    public class ToolsGizmo
    {
        public static float Quality2Size (CompQuality qualityComp)
        {
            float Answer = 0f;
            switch ((int)qualityComp.Quality)
            {
                case (int)QualityCategory.Awful:
                //case (int)QualityCategory.Superior:
                case (int)QualityCategory.Legendary:
                    Answer = 1f;
                    break;
                //case (int)QualityCategory.Shoddy:
                case (int)QualityCategory.Good:
                case (int)QualityCategory.Masterwork:
                    Answer = .9f;
                    break;
                case (int)QualityCategory.Poor:
                case (int)QualityCategory.Normal:
                case (int)QualityCategory.Excellent:
                    Answer = .75f;
                    break;
            }
            return Answer;
        }

        public static Texture2D Quality2Mat(CompQuality qualityComp)
        {
            Texture2D Answer = null;
            switch ((int)qualityComp.Quality)
            {
                case (int)QualityCategory.Awful:
                case (int)QualityCategory.Poor:
                //case (int)QualityCategory.Shoddy:
                    Answer = MyGizmo.QualityBadGz;
                    break;
                case (int)QualityCategory.Normal:
                case (int)QualityCategory.Good:
                //case (int)QualityCategory.Superior:
                    Answer = MyGizmo.QualityNormalGz;
                    break;
                case (int)QualityCategory.Excellent:
                case (int)QualityCategory.Masterwork:
                case (int)QualityCategory.Legendary:

                    Answer = MyGizmo.QualityGoodGz;
                    break;
            }
            return Answer;
        }
    }
}
