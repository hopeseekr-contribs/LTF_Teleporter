using RimWorld;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using UnityEngine;

using Verse;
using Verse.Sound;

namespace LTF_Teleport
{

    // Main
    [StaticConstructorOnStartup]
    public class Comp_LTF_TpSpot : ThingComp
    {
        // Can be a dead animal
        Pawn standingUser = null;
        Building building = null;
        Building facility = null;

        /* Comp */
        /******************/
        private CompPowerTrader powerComp;
        private CompQuality qualityComp;
        // TpBench required
        private CompAffectedByFacilities facilityPassiveComp;
        //        private CompFlickable flickComp;

        /* Caracteristics */
        /******************/
        // Inherits from tpBench
        // 22 - 54 ; 32 / 8 = 4 

        //private float radius = 30f;

        float weightCapacity = 0f;//will be set
        float currentWeight = 0f;//calculated

        float cooldownBase = 60 * 60f;
        float currentCooldown = 0f;

        bool drawVanish = false;

        /* Graphics */
        /******************/
        Mesh mesh1x1 = MeshPool.plane10;
        static string overlayPath = "Things/Building/TpSpot/Overlay/";
        //'nothing':'white','pawn':'yellow','item':'magenta',
        //'nothing':'white','pawn':'yellow','item':'magenta',
        private static readonly Material PoweredGfx = MaterialPool.MatFrom(overlayPath + "PoweredTpSpot", ShaderDatabase.Cutout);
        /*
        private static readonly Material YellowGfx = MaterialPool.MatFrom(overlayPath + "PoweredTpSpot", ShaderDatabase.Cutout, Color.yellow);
        private static readonly Material MagentaGfx = MaterialPool.MatFrom(overlayPath + "PoweredTpSpot", ShaderDatabase.Transparent, Color.magenta);
        */



        /*
                // 01
                private static readonly Material BlueGfx = MaterialPool.MatFrom(overlayPath + "PoweredTpSpot", ShaderDatabase.Transparent, Color.blue);
                // 02
                private static readonly Material CyanGfx = MaterialPool.MatFrom(overlayPath + "PoweredTpSpot", ShaderDatabase.Transparent, Color.cyan);
                // 03
                private static readonly Material GreenGfx = MaterialPool.MatFrom(overlayPath + "PoweredTpSpot", ShaderDatabase.Transparent, Color.green);
                // 04
                private static readonly Material RedGfx = MaterialPool.MatFrom(overlayPath + "PoweredTpSpot", ShaderDatabase.Transparent, Color.red);
                */

        //private static readonly Graphic VanishGfx = GraphicDatabase.Get < Graphic_Slideshow >(overlayPath + "Vanish", ShaderDatabase.TransparentPostLight, Vector2.one, Color.white);
        private static readonly Graphic VanishGfx = GraphicDatabase.Get<Graphic_Slideshow>(overlayPath + "Vanish", ShaderDatabase.MetaOverlay);

        int opacityOrientation = 1;
        float opacityIncrement = .006f;
        float currentOpacity = .5f;


        /* Production */
        /******************/
        List <Thing> tpThingList = new List<Thing>();
        bool gfxDebug=false;
        bool prcDebug = false;

        private void OpacityIterate(float min, float max)
        {
            if (gfxDebug) Log.Warning(currentOpacity + " += " + opacityOrientation + " * " + opacityIncrement);

            currentOpacity += (opacityOrientation * opacityIncrement);

            if ( currentOpacity != (currentOpacity = boundariesRespect(currentOpacity, min, max)))
            {
                OpacityBounce();
                currentOpacity += (opacityOrientation * opacityIncrement);
            }

            if (currentOpacity != (currentOpacity = boundariesRespect(currentOpacity, 0, 1)))
            {
                Log.Warning("should not happen protect opacity crap");
            }
        }

        private float boundariesRespect(float val, float min, float max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }
        private void OpacityBounce()
        {
            if (gfxDebug) Log.Warning("Bouncing" + building.Label);
            opacityOrientation *= -1;
        }

        public CompProperties_LTF_TpSpot Props
        {
            get
            {
                return (CompProperties_LTF_TpSpot)props;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();

            //Log.Warning("start drawing");
            if (!CheckPower())
            {
                return;
            }

            Vector3 buildingPos = building.DrawPos;
            if (buildingPos == null)
            {
                Log.Warning("null pos draw");
                return;
            }

            if (building.Rotation != Rot4.North)
            {
                Log.Warning("Rotation");
                return;
            }

            // higher than ground to be visible
            //buildingPos.y += 4;
            buildingPos.y += 0.046875f;

            // nothing there standing
            if (!HasFacility()) return;

            Material overlay = PoweredGfx;
            Color overlayColor = Color.white;

            // Nothing on tile
            if (!HasRegisteredItems)
            {
//                overlay = PoweredGfx;
                if (gfxDebug) Log.Warning("white");
            }
            // something over building
            else
            {
                if (HasRegisteredPawn)
                {
                    overlayColor = Color.yellow;
                    //overlay = YellowGfx;
                    if (gfxDebug) Log.Warning("yellow");
                }
                else
                {
                    overlayColor = Color.magenta;
                    if (gfxDebug) Log.Warning("magenta");
                }
            }

            //ChangeColor(overlay, myColor);
            DrawPulse(parent, overlay, buildingPos, mesh1x1);

            //Draw1x1Overlay( buildingPos, overlay, mesh1x1, 0, 0, myColor, randRot, flick, oscil, noFlickChance, minOpacity, maxOpacity);

            if (drawVanish)
            {
                Vector3 drawPos = this.parent.DrawPos;
                //drawPos.y += 0.046875f;
                drawPos.y += 4;
                VanishGfx.Draw(drawPos, Rot4.North, this.parent, 0f);
            }
            //Log.Warning("end drawing");
        }

        private void Draw1x1Overlay(Vector3 buildingPos, Material gfx, Mesh mesh, float x, float z,
                                Color overlayColor,
                                bool randRotation = false,
                                bool randFlicker = false,
                                bool oscillatingOpacity = false,
                                float noFlickChance = .985f, float minOpacity = .65f, float maxOpacity = 1f
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

            float calculatedOpacity= maxOpacity;
            if (oscillatingOpacity)
            {
                OpacityIterate(minOpacity,maxOpacity);
                calculatedOpacity = currentOpacity;
            }

            Material fMat = null;
            if (randFlicker)
            {
                if (Rand.Chance(flickChance))
                {
                    calculatedOpacity -= (currentWeight - minOpacity)/4;
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
            if (gfxDebug)
                Log.Warning("Drew:" + newColor);
        }

        private void ChangeColor(Material mat, Color color)
        {
            float baseOpacity = mat.color.a;
            Color newColor = color;
            newColor.a = baseOpacity;
            mat.color = newColor;
        }

        private float PulseOpacity(Thing thing)
        {
            float num = (Time.realtimeSinceStartup + 397f * (float)(thing.thingIDNumber % 571)) * 4f;
            float num2 = ((float)Math.Sin((double)num) + 1f) * 0.5f;
            num2 = 0.3f + num2 * 0.7f;

            return num2;
        }
        

        private void DrawPulse(Thing thing, Material mat, Vector3 drawPos, Mesh mesh)
        {
            Material material = FadedMaterialPool.FadedVersionOf(mat, PulseOpacity(thing));

            Vector3 dotS = new Vector3(1f, 1f, 1f);
            Matrix4x4 matrix = default(Matrix4x4);

            matrix.SetTRS(drawPos, Quaternion.AngleAxis(0f, Vector3.up), dotS);

            Graphics.DrawMesh(mesh, matrix, material, 0);
        }

        private void MoreWeight(Thing thing)
        {
            ChangeWeight(thing);
        }
        private void LessWeight(Thing thing)
        {
            ChangeWeight(thing, false);
        }

        private void ChangeWeight(Thing thing, bool addWeight = true)
        {
            float newWeight = thing.GetStatValue(StatDefOf.Mass, true);
            int plusOrMinus = ((addWeight) ? (1) : (-1));

            currentWeight += plusOrMinus * newWeight;
            currentWeight = boundariesRespect(currentWeight, 0, 3000);
            currentWeight = (float)Math.Round((Decimal)currentWeight, 2, MidpointRounding.AwayFromZero);

            if(prcDebug)
                Log.Warning(thing.LabelShort + " adds(" + plusOrMinus + ")" + newWeight + " -> " + currentWeight);
        }

        private void ResetWeight()
        {
            currentWeight = 0;
        }

        // Check distant tile

        string CapacityString(float capacity, float capacityMax)
        {
            string buffer = string.Empty;
            buffer = capacity + " / " + capacityMax;
            return (buffer);
        }

        public bool notCapaRequiring(float capacity)
        {
            return (currentWeight == 0f);
        }

        public bool IsOverCapa(float capacity, float capacityMax)
        {
            return (capacity > capacityMax);
        }

        public void FullHax(float capacity, float capacityMax)
        {
            capacity = capacityMax;
        }

        public void StopVanish()
        {
            drawVanish = false;
        }

        public void StartVanish()
        {
            drawVanish = true;
        }

        public void ToggleForceDrawVanish()
        {
            drawVanish = !drawVanish;
        }
        public void TogglePrcDebug()
        {
            prcDebug = !prcDebug;
        }
        public void ToggleGfxDebug()
        {
            gfxDebug = !gfxDebug;
        }

        public bool CheckPower()
        {
            if (powerComp == null || !powerComp.PowerOn)
                return false;
            return true;
        }
        private bool CheckBuilding()
        {
            if (parent == null || parent.Map == null)
                return false;
            return true;
        }
        /*
        private bool CheckUser()
        {
            standingUser = null;

            Pawn maybeUser = null;
            maybeUser = building.Position.GetFirstPawn(parent.Map);

            if (maybeUser == null)
                { //Log.Warning("null"); 
                return false; };

            if( 
                (maybeUser.Faction != Faction.OfPlayer) ||
                (maybeUser.mindState.mentalStateHandler.InMentalState)
            )
                return false;            

            standingUser = maybeUser;
            return true;

        }
        */
        public int RegisteredCount
        {
            get
            {
                return tpThingList.Count;
            }
        }

        protected bool HasRegisteredItems
        {
            get
            {
                return !tpThingList.NullOrEmpty();
            }
        }
/*
        public bool hasItems()
        {
            return (!tpThingList.NullOrEmpty());
        }
        */
        private void ResetItems()
        {
            tpThingList.Clear();
            ResetWeight();
        }

        private bool RemoveItemsIfAbsent()
        {

            if (!HasRegisteredItems)
                return false;


            //Log.Warning(building.Label + " checks history");
            //for (int i = 0; i < tpThingList.Count; i++)
            for (int i = tpThingList.Count - 1; i >= 0; i--)
            {
                Thing thing = null;
                thing = tpThingList[i];
                if (thing == null)
                {
                    Log.Warning("lol what");
                    continue;
                }

                Pawn pawn = thing as Pawn;
                if ((pawn != null) && (standingUser != null)) {
                    //Log.Warning(building.Label + " concerned about pawns");
                    if ((pawn != standingUser) || (pawn.Position != building.Position))
                    {
                        //Log.Warning(" reseting bc he left  or someone" + standingUser.LabelShort);
                        ResetPawn();
                    }
                }

                if (thing.Position != building.Position)
                {
                    RemoveItem(thing);
                    tpThingList.Remove(thing);
                }
            }

            return (HasRegisteredItems);
        }

        private void AddItem(Thing thing)
        {
            if(prcDebug)
                Log.Warning("Adding " + thing.Label + " to " + building.Label);

            tpThingList.Add(thing);
        }

        private void RemoveItem(Thing thing)
        {
            if (prcDebug)
                Log.Warning("Removing " + thing.Label + " from " + building.Label);

            tpThingList.Remove(thing);
        }

        private bool CheckNewItems()
        {
            return (AddSpotItems(building.Position.GetThingList(building.Map)));
        }

        private bool CheckItems()
        {
            bool foundItem = false;

            foundItem |= RemoveItemsIfAbsent();
            foundItem |= CheckNewItems();

            return (foundItem);
        }

        private void CalculateWeight (bool foundItem)
        {
            if (foundItem)
            {
                SumWeight();
                if (prcDebug)
                    Log.Warning("weight:" + currentWeight);
            }
            else
            {
                if (prcDebug)
                    Log.Warning("noting to weight");

                if (currentWeight != 0)
                    currentWeight = 0;
            }
        }

        private bool CheckThingPresent(List<Thing> all)
        {
            return (! CheckNothing(all) );
        }
        private bool CheckNothing(List<Thing> all)
        {
            return (all.NullOrEmpty());
        }

        private bool AddSpotItems(List<Thing> allThings, bool clearIfEmpty=true)
        {
            //Log.Warning(building.Label+" checking items");
            //if (CheckNothing(building.Position.GetThingList(building.Map)))
            if (CheckNothing(allThings))
            {
                if (clearIfEmpty)
                    ResetItems();

                return false;
            }
            
            Thing thing = null;
            bool found = false;
            int pawnN = 0;

            Pawn passenger = null;

            if(prcDebug)
                Log.Warning(building.Label+":"+allThings.Count);

            for (int i = 0; i < allThings.Count; i++)
            {

                thing = allThings[i];
                if (thing != null)
                {
                    //Projectile projectile = thing as Projectile;
                    Pawn pawn = thing as Pawn;
                    // Can have 2 buildings, there if myself != null, myself=parent => idgaf
                    Building myself = thing as Building;

                    //if ((thing.def.mote != null || thing.def.IsFilth || projectile != null || myself != null))
                    if ((thing.def.mote != null || thing.def.IsFilth || myself != null))
                        continue;

                    if (pawn != null)
                    {
                        passenger = pawn;
                        pawnN += 1;
                    }

                    if (!tpThingList.Contains(thing))
                    {
                        AddItem(thing);
                        if (prcDebug)
                            Log.Warning(thing.Label + " added");
                    }

                    found = true;
                }
            }

            if (pawnN == 0)
            {
                ResetPawn();
            }
            else if (pawnN > 1)
            {
                ResetPawn();
                ResetItems();
                //tpThingList.Clear();
                //Log.Warning("More than 1 pawn. Cant.");
            }
            else
            {
                SetPawn(passenger);
                if (prcDebug)
                    Log.Warning(passenger.LabelShort + " added");
            }

            if (!found)
            {
                ResetItems();
            }

            return found;
        }

        private void SetPawn(Pawn pawn=null)
        {
            standingUser = pawn;
        }

        private void ResetPawn()
        {
            SetPawn();
        }

        private bool HasRegisteredPawn
        {
            get
            {
                return (standingUser != null);
            }
        }

        private bool HasFacility()
        {
            return (facility != null);
        }

        private void ResetFacility()
        {
            facility = null;
        }

        private bool SetFacility(CompAffectedByFacilities facilityPassiveComp)
        {
            //Log.Warning("Trying to set facility");
            if (facilityPassiveComp == null)
            {
                Log.Warning("facility comp err");
                ResetFacility();
                return false;
            }

            Thing thing = null;
            if (facilityPassiveComp.LinkedFacilitiesListForReading.NullOrEmpty())
            {
                ResetFacility();
                return false;
            }

            thing = facilityPassiveComp.LinkedFacilitiesListForReading.RandomElement();
            if (thing == null)
            {
                // will happen when loading
                //Log.Warning("facility thing err");
                ResetFacility();
                return false;
            }

            facility = thing as Building;
            if (facility == null)
            {
                Log.Warning("facility building err");
                ResetFacility();
                return false;
            }


            return true;
        }
        private bool IsChilling()
        {
            return (!IsReady());
        }
        private bool IsReady()
        {
            return (currentCooldown == 0);
        }

        private float WeightedCapacity(float capacityBase, float capacitySpectrum)
        {
            if (qualityComp == null)
            {
                return (capacityBase);
            }
            // 0..8
            return (capacityBase + (float)qualityComp.Quality * (capacitySpectrum / 8));
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            building = (Building)parent;


            powerComp = building.TryGetComp<CompPowerTrader>();
            qualityComp = building.TryGetComp<CompQuality>();
            facilityPassiveComp = building.TryGetComp<CompAffectedByFacilities>();

            SetFacility(facilityPassiveComp);
            SetWeightCapacity();
            SetCooldownBase();

            //DumpProps();
        }
        private void SetWeightCapacity() {
            weightCapacity = WeightedCapacity(Props.weightBase, Props.weightSpectrum);
        }
        private void SetCooldownBase()
        {
            cooldownBase = WeightedCapacity(Props.cooldownBase, Props.cooldownSpectrum);
        }
        private void DumpProps(float val1, float val2, string myString = "bla: ")
        {
            Log.Warning( myString + val1+ " / "+val2 );
        }

        /*
        public override void CompTickRare()
        {
            base.CompTickRare();
            */
        
        public bool TeleportCapable()
        {
            if (HasRegisteredItems)
                return false;

            if (IsOverCapa(currentWeight, weightCapacity))
                return false;

            if (IsChilling())
                return false;

            return true;
        }

        public override void CompTick()
        {
            base.CompTick();
        
            if (!CheckBuilding())
            {
                Log.Warning("Impossibru");
                return;
            }

            if (CheckPower())
            {
                if (IsChilling())
                {
                    currentCooldown -= 1;
                    currentCooldown = ((currentCooldown < 0) ? (0) : (currentCooldown));
                }

                if (HasFacility())
                {
                    //CalculateWeight
                    //Log.Warning(building.Label + "has facility");
                    if (CheckItems())
                    {

                        if (prcDebug)
                            Log.Warning("N:" + tpThingList.Count +":" + DumpList());
                    }
                }
                else
                {
                    SetFacility(facilityPassiveComp);
                }
                
            }
        }

        private void SumWeight()
        {
            currentWeight = 0;
            
            foreach(Thing item in tpThingList)
            {
                MoreWeight(item);
            }
            return;
        }

        private string DumpList()
        {
            string bla = String.Empty;
            foreach(Thing item in tpThingList)
            {
                bla += item.Label + ";";
            }
            return bla;
        }

        void SideEffect(Pawn pawn)
        {
            //random tp back ?
//            AddThought(pawn);
        }
        /*
        private void AddThought(Pawn pawn)
        {
            if (CorpseGrinding())
            {
                if (HumanCorpseGrinding())
                {
                    standingUser.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.ButcheredHumanlikeCorpse, null);
                    foreach (Pawn current in standingUser.Map.mapPawns.SpawnedPawnsInFaction(standingUser.Faction))
                    {
                        if (current != standingUser && current.needs != null && current.needs.mood != null && current.needs.mood.thoughts != null)
                        {
                            current.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.KnowButcheredHumanlikeCorpse, null);
                        }
                    }
                    TaleRecorder.RecordTale(TaleDefOf.ButcheredHumanlikeCorpse, new object[]
                    {
                    standingUser
                    });
                }
                else
                {
                    standingUser.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("LTF_AnimalGrind"), null);
                }
            }
        }
        */
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentCooldown, "cooldown");
            Scribe_Values.Look(ref currentWeight, "weight");

            Scribe_References.Look(ref standingUser, "user");
            Scribe_Collections.Look(ref tpThingList, "things", LookMode.Reference, new object[0]);

            //Scribe_References.Look(ref standingUser, "LTF_standingUser");

        }

        public override string CompInspectStringExtra()
        {
            string text = base.CompInspectStringExtra();
            string result = string.Empty;

            if (this.powerComp == null || !this.powerComp.PowerOn || !HasFacility())
            {
                return null;
            }

            if (!HasRegisteredItems)
            {
                result += "Nothing on tile.";
                return result;
            }

            bool mayI = TeleportCapable();
            result += ((mayI) ? ("[Ok]") : ("[Ko]")) + " ";

            if (mayI)
            {
                int itemCount = RegisteredCount;
                result += itemCount + " item" + ((itemCount > 1) ? ("s") : ("")) + ". " + currentWeight + " kg. Max: " + weightCapacity + " kg.";
            }
            else
            {
                if (currentWeight > weightCapacity)
                {
                    result += "" + currentWeight + "kg. >" + weightCapacity + " kg.";
                }
                //string percent
                else if (IsChilling() && cooldownBase != 0)
                {
                    float coolPerc = currentCooldown / cooldownBase;
                    result += "Cooldown: " + coolPerc.ToStringPercent("F0");
                }
            }
            
            if (!text.NullOrEmpty())
            {
                result = "\n" + text;
            }

            return result;
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (CheckPower() && HasFacility())
            {
                if (HasRegisteredItems)
                {
                    /*
                    yield return new Command_Action
                    {
                        action = new Action(this.Evacuation),
                        defaultLabel = "Emergency evacuation",
                        defaultDesc = "Forces amino acids evacuation",
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport", true)
                    };
                    */

                }
                if (Prefs.DevMode)
                {

                    yield return new Command_Action
                    {
                        defaultLabel = "bad capa" + weightCapacity * .5f,
                        defaultDesc = "wc:"+weightCapacity,
                        action = delegate
                        {
                            FullHax(weightCapacity, weightCapacity * .5f);
                        }
                    };

                    if(weightCapacity<40)
                    yield return new Command_Action
                    {
                        defaultLabel = "reset weight",
                        action = delegate
                        {
                            SetWeightCapacity();
                        }
                    };

                    yield return new Command_Action
                    {
                        defaultLabel = "bad cooldown" + cooldownBase * 2f,
                        defaultDesc = "cb:" + cooldownBase,
                        action = delegate
                        {
                            FullHax(cooldownBase, cooldownBase * 2f);
                        }
                    };

                    if(cooldownBase>10000)
                    yield return new Command_Action
                    {
                        defaultLabel = "reset cool base",
                        action = delegate
                        {
                            SetCooldownBase();
                        }
                    };


                    if (IsReady())
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = "force cooldown",
                            defaultDesc = "c:" + currentCooldown,
                            action = delegate
                            {
                                FullHax(currentCooldown, cooldownBase);
                            }
                        };
                    }
                    else
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = "reset cooldown",
                            defaultDesc = "c:" + currentCooldown,
                            action = delegate
                            {
                                FullHax(currentCooldown, 0);
                            }
                        };
                    }

                    if(gfxDebug)
                    yield return new Command_Action
                        {
                            defaultLabel = "gfx vanish",
                        action = delegate
                            {
                                ToggleForceDrawVanish();
                            }
                        };

                    yield return new Command_Action
                    {
                        defaultLabel = "gfx Debug",
                        defaultDesc = "gd:" + gfxDebug,
                        action = delegate
                        {
                            ToggleGfxDebug();
                        }
                    };
                    yield return new Command_Action
                    {
                        defaultLabel = "prc Debug",
                        defaultDesc = "pd:" + prcDebug,
                        action = delegate
                        {
                            TogglePrcDebug();
                        }
                    };

                }
            }
        }
    }
}
