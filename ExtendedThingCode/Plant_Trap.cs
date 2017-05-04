using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace UnificaMagica
{

    //    [StaticConstructorOnStartup]
    public class ExtThingDef: Verse.ThingDef {
        public List<ExtComp> extcomps = new List<ExtComp>();
    }

    // ExtendedThings are extended with Extended Components (ExtComp and ExtInstComp)
    public interface IExtendedThing {
        List<ExtInstComp> ExtComps {
            get;
            set;
        }
        ExtThingDef def {
            get;
            set;
        }

        // initialize ExtComps when creating and loading
        void PostMake();
        void ExposeData();
    }


    [StaticConstructorOnStartup]
    public class PlantExtended : Plant, IExtendedThing {

        protected List<ExtInstComp> extcomps = null; // set during InitializeExtendedComps
//        ExtThingDef defint;

        public List<ExtInstComp> ExtComps {
            get { return this.extcomps; }
            set { this.extcomps = value; }
        }
        public new ExtThingDef def {
            get { return (ExtThingDef) base.def; }
            set {
                ExtThingDef dd = value as ExtThingDef;
                if ( dd == null ) {
                    Log.Error("Can not set a non-ExtThingDef to an IExtendedThing");
                    Log.Error("Can not set a non-ExtThingDef ("+( value )+") to an IExtendedThing");
                }
                base.def = value;
            }
        }

        public override void PostMake()
        {
            Log.Message("PlantExtended.PostMake 1");
            base.PostMake();
            Log.Message("PlantExtended.PostMake 2");
            ExtendedComponent.InitializeExtComps(this);
            Log.Message("PlantExtended.PostMake 3");
        }

        public override void ExposeData()
        {
            Log.Message("PlantExtended.ExposeData 1");
            base.ExposeData();
            Log.Message("PlantExtended.ExposeData 2");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
            Log.Message("PlantExtended.ExposeData 2.1");
                ExtendedComponent.InitializeExtComps(this);
            Log.Message("PlantExtended.ExposeData 2.2");
            }
            for (int i = 0; i < this.ExtComps.Count; i++)
            {
                this.ExtComps[i].PostExposeData();
            }
            Log.Message("PlantExtended.ExposeData 3");
        }

        public Building_PlantGrower thepot = null;

        public List<ExtInstComp> planttraps = new List<ExtInstComp>();

/*
        public void Initialize()
        {
            // Plant Traps
            for (int i = 0; i < this.def.comps.Count; i++) {
                ThingComp thingComp = (ThingComp)Activator.CreateInstance (this.def.comps [i].compClass);
                thingComp.parent = this;
                this.comps.Add (thingComp);
                thingComp.Initialize (this.def.comps [i]);
            }
        }
        */

        // find the pot this is in
        protected bool initPot() {
            if ( base.Map == null ) return false;
            List<Thing> list = base.Map.thingGrid.ThingsListAt(base.Position);
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing = list[i];
                if (thing is Building_PlantGrower ) {
                    // Log.Warning("PlantTrap found pot");
                    this.thepot = (Building_PlantGrower)thing;
                }
            }
            // Log.Warning("PlantTrap pot : "+this.thepot);
            //            Log.Warning("Creating Plant_Trap with properties:");
            //            Helpers.PrintObject(this.def.planttraps);
            //((DruidPlantProperties)this.def.plant).planttraps);

            return true;
        }



        public IEnumerable<IntVec3> alertzone = null;
        public void initAlertZone() {

            Log.Message("PlantTrap.initAlertZonw 1 ");
            Log.Message("PlantTrap.initAlertZonw 1 : the pot "+this.thepot);
            float r = this.thepot.def.specialDisplayRadius;
            Log.Message("PlantTrap.initAlertZonw 2 ");
            this.alertzone = GenRadial.RadialCellsAround (this.Position, r, true);
            Log.Message("PlantTrap.initAlertZonw 3 ");
        }


        // protected int rearmAt = 0;


        // ---------------------------------------------------------------------
        // From Building_Trap below
        // ---------------------------------------------------------------------
        //        private List<Pawn> touchingPawns = new List<Pawn> ();
        //protected bool Armed = true;


        protected void Spring(Pawn p, ExtInstComp_PlantTrap _ptp)
        {
            //            Log.Message("PlantTrap.Spring! ");
            SoundDef.Named("PlantTrapSpring").PlayOneShot(new TargetInfo(base.Position, base.Map, false));
            if (p.Faction != null)
            {
                p.Faction.TacticalMemory.TrapRevealed(base.Position, base.Map);
            }
            _ptp.SpringPlantTrap(p,this);

        }

        public override void Tick ()
        {
            int curtick = Find.TickManager.TicksGame;

            Log.Message("PlantTrap.Tick 1 "+base.LabelMouseover+ " : " + curtick);
            //this.TickInterval
            if ( curtick % 2000 == 0 )  { this.TickLong(); }

            if ( this.thepot == null ) {
                if ( this.initPot() == false ) {
                    Log.Message("PlantTrap.Tick .. map not set on first pass so return");
                    return;
                }
            } // Map not set on first pass so return

            // only mature trips
            Log.Message("PlantTrap.Tick 2 ");
            if ( base.Map != null && base.LifeStage == PlantLifeStage.Mature ) {

                // check for alert zone initialization
                Log.Message("PlantTrap.Tick 2.1");
                if ( this.alertzone == null ) {
                    Log.Message("PlantTrap.Tick 2.1.1");
                    this.initAlertZone();
                    Log.Message("PlantTrap.Tick 2.1.2");
                }

                // find if there is an active trap; reactivate any needing it
                bool anyarmed = false;
                Log.Message("PlantTrap.Tick 2.2");

                // handle all traps
//                foreach ( ExtInstComp_PlantTrap ptp in this.def.GetComps<ExtInstComp_PlantTrap>() )
                foreach ( ExtInstComp ptp2 in this.ExtComps ) {
                    ExtInstComp_PlantTrap ptp = ptp2 as ExtInstComp_PlantTrap;
                    if ( ptp != null ) {

                        // check for rearm if not armed
                        // Log.Message("PlantTrap.Tick rearm check "+this.Armed+" " + Find.TickManager.TicksGame +" "+ this.rearmAt+ " " + (Find.TickManager.TicksGame > this.rearmAt ));
                        Log.Message("PlantTrap.Tick 2.2.1");
                        if ( ptp.Armed == false && ( ptp.rearmAt != -1 ) && ( curtick > ptp.rearmAt ) ) {
                            Log.Message("PlantTrap.Tick 2.2.1.1");
                            ptp.Armed = true;
                            Log.Message("PlantTrap.Tick 2.2.1.2");
                        }

                        // if armed, check alert zones
                        Log.Message("PlantTrap.Tick 2.2.2");
                        if ( ptp.Armed == true ) {
                            Log.Message("PlantTrap.Tick 2.2.2.1");
                            anyarmed = true;
                            Log.Message("PlantTrap.Tick 2.2.2.2");
                        }

                        Log.Message("PlantTrap.Tick 2.2.3");
                    }
                }

                // find all Pawns nearby
                Log.Message("PlantTrap.Tick 2.3");
                List<Pawn> pawnthings = new List<Pawn>();
                Log.Message("PlantTrap.Tick 2.4");
                if ( anyarmed ) {
                    // find all pawns
                    Log.Message("PlantTrap.Tick 2.4.1");
                    foreach ( IntVec3 pp in this.alertzone) { // for each spot in alert zone
                        Log.Message("PlantTrap.Tick 2.4.1.1");
                        List<Thing> thingList = pp.GetThingList (base.Map);
                        Log.Message("PlantTrap.Tick 2.4.1.2");
                        for (int i = 0; i < thingList.Count; i++) {
                            Log.Message("PlantTrap.Tick 2.4.1.2.1");
                            Pawn pawn = thingList [i] as Pawn;
                            Log.Message("PlantTrap.Tick 2.4.1.2.2");
                            if (pawn != null ) {
                                Log.Message("PlantTrap.Tick 2.4.1.2.2.1");
                                pawnthings.Add(pawn);
                                Log.Message("PlantTrap.Tick 2.4.1.2.2.2");
                            }
                            Log.Message("PlantTrap.Tick 2.4.1.2.3");
                        }
                        Log.Message("PlantTrap.Tick 2.4.1.3");
                    }
                    Log.Message("PlantTrap.Tick 2.4.2");
                }

                // is anyarmed, chekc
                Log.Message("PlantTrap.Tick 2.5");
                if ( anyarmed ) {
                    Log.Message("PlantTrap.Tick 2.5.1");
                    foreach ( ExtInstComp ptp2 in this.ExtComps ) {
                        ExtInstComp_PlantTrap ptp = ptp2 as ExtInstComp_PlantTrap;
                        if ( ptp != null ) {
                            //foreach ( PlantTrapProperties ptp in ((UnificaMagica.DruidPlantDef)this.def).planttraps ) {
                            Log.Message("PlantTrap.Tick 2.5.1.1");
                            if ( ptp.Armed ) {
                                Log.Message("PlantTrap.Tick 2.5.1.1.1");
                                foreach ( Pawn pawn in pawnthings ) {
                                    Log.Message("PlantTrap.Tick 2.5.1.1.1.1");
                                    if ( this.CheckSpring (pawn,ptp)  ) {
                                        Log.Message("PlantTrap.Tick 2.5.1.1.1.1.1");
                                        if ( ptp.Props.isAoE ) break; // apply only once if AoE
                                        //if ( ((ExtComp_PlantTrap)ptp.props).isAoE ) break; // apply only once if AoE
                                    }
                                    Log.Message("PlantTrap.Tick 2.5.1.1.1.2");
                                }
                                Log.Message("PlantTrap.Tick 2.5.1.1.2");
                            }
                            Log.Message("PlantTrap.Tick 2.5.1.2");
                        }
                        Log.Message("PlantTrap.Tick 2.5.2");
                    }
                }

                //delete(pawnthings);
                Log.Message("PlantTrap.Tick 2.6");

            }
            Log.Message("PlantTrap.Tick 3 - End");
        }



        protected virtual bool CheckSpring (Pawn p, ExtInstComp_PlantTrap _ptp)
        {
            bool retval = false;
            if ( ! _ptp.Armed ) return false;

            float sc = this.SpringChance(p);
            float rr = Rand.Value;
            //            Log.Message("PlantTrap.CheckSpring : rand("+rr+") chance("+sc+")");
            if (Rand.Value <  sc ) { //this.SpringChance (p))
                //                Log.Message("PlantTrap.CheckSpring : sprung");
                retval = true;
                this.Spring (p,_ptp);
                if (p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer) {
                    Letter let = new Letter ("LetterFriendlyTrapSprungLabel".Translate (new object[] { p.NameStringShort }), "LetterFriendlyTrapSprung".Translate (new object[] { p.NameStringShort }), LetterType.BadNonUrgent, new TargetInfo (base.Position, base.Map, false));
                    Find.LetterStack.ReceiveLetter (let, null);
                }
            }
            return retval;
        }

        protected virtual float SpringChance (Pawn p)
        {
            float num;
            if (this.KnowsOfTrap (p)) {
                num = 0.004f;
                //                Log.Message("PlantTrap.SpringChance 1 : "+num);
            }
            else {
                num = this.GetStatValue (StatDefOf.TrapSpringChance, true);
                //                Log.Message("PlantTrap.SpringChance 1 : "+num);
            }
            num *= GenMath.LerpDouble (0.4f, 0.8f, 0f, 1f, p.BodySize);
            if (p.RaceProps.Animal) {
                num *= 0.1f;
            }
            //            Log.Message("PlantTrap.SpringChance chance: "+num+"   base is "+StatDefOf.TrapSpringChance);
            return Mathf.Clamp01 (num);
        }

        public bool KnowsOfTrap(Pawn p)
        {
            if ( this.thepot == null ) {
                //                Log.Error("can not find pot of plant "+this+" at "+this.Position);
                return false;
            }
            Faction f = this.thepot.Faction;
            //            Log.Message("PlantTrap.KnowsOfTrap 0 : is faction null "+(f == null));

            bool retval = (p.Faction != null && !p.Faction.HostileTo(f)) || (p.Faction == null && p.RaceProps.Animal && !p.InAggroMentalState) || (p.guest != null && p.guest.released);
            //            Log.Message("PlantTrap.KnowsOfTrap 1 "+retval);
            //            Log.Message("PlantTrap.KnowsOfTrap 2 "+(p.Faction!= null));
            //            Log.Message("PlantTrap.KnowsOfTrap 3 "+f);
            //            Log.Message("PlantTrap.KnowsOfTrap 4 "+p.Faction.HostileTo(f));
            //return (p.Faction != null && !p.Faction.HostileTo(base.Faction)) || (p.Faction == null && p.RaceProps.Animal && !p.InAggroMentalState) || (p.guest != null && p.guest.released);
            return (p.Faction != null && !p.Faction.HostileTo(f)) || (p.Faction == null && p.RaceProps.Animal && !p.InAggroMentalState) || (p.guest != null && p.guest.released);
        }


        public override string GetInspectString()
        {
            string retval = base.GetInspectString();
            StringBuilder stringBuilder = new StringBuilder();

            Log.Message("PlantExtended.GetInspectString()");

            stringBuilder.Append(retval);

            //if ( this.def.ExtComps.Count != 0 )
            if (this.LifeStage == PlantLifeStage.Growing)
            {
                stringBuilder.AppendLine("Druid Plant is not mature");
            } else if (this.LifeStage == PlantLifeStage.Mature) {
                bool fl = false;

                foreach ( ExtInstComp ptp2 in this.ExtComps ) {
                    ExtInstComp_PlantTrap ptp = ptp2 as ExtInstComp_PlantTrap;
                    if ( ptp != null ) {
                        if ( ptp.Armed ) { stringBuilder.AppendLine( ptp.Props.ArmedLabel );  fl = true; }
                        else {
                            stringBuilder.AppendLine( ptp.Props.ArmedLabel + " in " + ptp.rearmAt + " ticks");
                        }
                    }
                }
                if ( fl == false ) { stringBuilder.AppendLine("No armed traps");
            }
        }

        return stringBuilder.ToString();
    }
}
}
