using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System;
using Verse;

namespace UnificaMagica {


    public static class ExtendedComponent {

        public static void InitializeExtComps(IExtendedThing _thing) {
            List<ExtInstComp> l = new List<ExtInstComp>();
            _thing.ExtComps = l;
            Log.Message("ExtendedComponent.InitializeExtComps 1");
            for (int i = 0; i < _thing.def.extcomps.Count; i++) {
                Log.Message("ExtendedComponent.InitializeExtComps 1.1");
                ExtInstComp extComp = (ExtInstComp)Activator.CreateInstance (_thing.def.extcomps[i].compClass);
                Log.Message("ExtendedComponent.InitializeExtComps 1.2");
                extComp.parent = _thing;
                Log.Message("ExtendedComponent.InitializeExtComps 1.3");
                l.Add(extComp);// _thing.ExtComps.Add(extComp);
                Log.Message("ExtendedComponent.InitializeExtComps 1.4");
                extComp.Initialize (_thing.def.extcomps[i]);
                Log.Message("ExtendedComponent.InitializeExtComps 1.5");
            }
            Log.Message("ExtendedComponent.InitializeExtComps 2");
        }

    };


    // ExtendedComponent is my redo of Components that don't depdend on parent being a ThingWithComps
    //   ExtComps_X is the load-time defined data of the ExtendedComponent.
    //   ExtInstComps_X   is the instance definition of the ExtendedComponent with local data and functionality.
    //   At Runtime, the ExtComps_X create the ExtInstComps_X.

    public class ExtComp {

        public Type compClass;

        public ExtComp() {
        }
        public ExtComp(Type compClass) {
            this.compClass = compClass;
        }
    }

    // definition of
    public class ExtComp_PlantTrap : ExtComp {
        public int rearmTime = 2000; // num ticks to rearm
        public bool isAoE = true;
        public float TrapSpringChance = 0.8f;
        public string ArmedLabel = "unnamed plant trap";

        public ExtComp_PlantTrap() {
            this.compClass = typeof(ExtInstComp_PlantTrap);
        }
    }


    public class ExtInstComp {
        public IExtendedThing parent;
        public ExtComp  propsint; // static properties of the ExtendedComponent
        //        need to get to write to file and initialize

        public virtual void Initialize(ExtComp props) { this.propsint = props; }
        public virtual void PostExposeData() {
        }

        public ExtComp Props {
            get
            {
                return this.propsint;
            }
        }
    }

    // instance data added to CompProperties_X
    public class ExtInstComp_PlantTrap : ExtInstComp {
        public int rearmAt = -1;
        public bool Armed = true;

        // Returns true if sprung
        public virtual void SpringPlantTrap( Pawn p, PlantExtended pt) {
            this.Armed = false;
            this.rearmAt = Find.TickManager.TicksGame + this.Props.rearmTime;//((ExtComp_PlantTrap)this.props).rearmTime;
        }

        public override void PostExposeData() {
            Log.Message("ExtInstComp_PlantTrap.postExposeData 1");
            base.PostExposeData();
            Log.Message("ExtInstComp_PlantTrap.postExposeData 2");
            Scribe_Values.LookValue<int>(ref this.rearmAt, "rearmAt", -1, false);
            Log.Message("ExtInstComp_PlantTrap.postExposeData 3");
            Scribe_Values.LookValue<bool>(ref this.Armed, "Armed", true, false);
            Log.Message("ExtInstComp_PlantTrap.postExposeData 4");
        }

        public new ExtComp_PlantTrap Props
        {
            get
            {
                return (ExtComp_PlantTrap)this.propsint;
            }
        }
    }



    /*
    // definition of
    public class CompProperties_PlantTrap_Explode : CompProperties_PlantTrap {
    public int damageAmountBase = 1;
    public SoundDef soundExplode = null;

    public CompProperties_PlantTrap_Explode() {
    this.compClass = typeof(CompPlantTrap_Explode);
}
};

// instance of
public class CompPlantTrap_Explode : CompPlantTrap {
//public float explosionRadius =this.thepot.def.specialDisplayRadius;

public override void SpringPlantTrap( Pawn p, Plant_Trap pt ){
// public static void DoExplosion (IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator, SoundDef explosionSound = null, ThingDef projectile = null, ThingDef source = null, ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f, int postExplosionSpawnThingCount = 1, bool applyDamageToExplosionCellsNeighbors = false, ThingDef preExplosionSpawnThingDef = null, float preExplosionSpawnChance = 0f, int preExplosionSpawnThingCount = 1)
GenExplosion.DoExplosion (pt.Position, pt.Map, pt.thepot.def.specialDisplayRadius, DamageDefOf.Bomb, pt, this.soundExplode);
}
}
*/


public class ExtComp_PlantTrap_Generator : ExtComp_PlantTrap {
    public int numGen;
    public ThingDef genthing;

    public ExtComp_PlantTrap_Generator() {
        this.compClass = typeof(ExtInstComp_PlantTrap_Generator);
    }
}



public class ExtInstComp_PlantTrap_Generator : ExtInstComp_PlantTrap {

    public override void SpringPlantTrap( Pawn p, PlantExtended pt)
    {
        base.SpringPlantTrap(p,pt);
        for(int i = 0; i< this.Props.numGen  ; i++ ) {
            Thing thing = ThingMaker.MakeThing(this.Props.genthing, null);
            GenPlace.TryPlaceThing(thing, pt.Position, pt.Map , ThingPlaceMode.Near, null);
        }
    }

    public override void PostExposeData() {
        Log.Message("ExtInstComp_PlantTrap_Generator.postExposeData 1");
        base.PostExposeData();
        Log.Message("ExtInstComp_PlantTrap_Generator.postExposeData 2");
//        Scribe_Values.LookValue<int>(ref this.numGen, "numGen", null, false); // default value is in ThingDef
//        Log.Message("ExtInstComp_PlantTrap_Generator.postExposeData 3");
//        Scribe_Values.LookValue<bool>(ref this.genthing, "genthing", null , false); // default value is in ThingDef
//        Log.Message("ExtInstComp_PlantTrap_Generator.postExposeData 4");
    }

    public new ExtComp_PlantTrap_Generator Props
    {
        get
        {
            return (ExtComp_PlantTrap_Generator)this.propsint;
        }
    }
}




public class ExtComp_PlantTrap_Explosive : ExtComp_PlantTrap {
    public int damageAmountBase = 1;
    public SoundDef soundExplode = null;

    public ExtComp_PlantTrap_Explosive() {
        this.compClass = typeof(ExtInstComp_PlantTrap_Explosive);
    }
}

public class ExtInstComp_PlantTrap_Explosive : ExtInstComp_PlantTrap {

    public override void SpringPlantTrap( Pawn p, PlantExtended pt)
    {
        base.SpringPlantTrap(p,pt);
        // public static void DoExplosion (IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator, SoundDef explosionSound = null, ThingDef projectile = null, ThingDef source = null, ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f, int postExplosionSpawnThingCount = 1, bool applyDamageToExplosionCellsNeighbors = false, ThingDef preExplosionSpawnThingDef = null, float preExplosionSpawnChance = 0f, int preExplosionSpawnThingCount = 1)
        GenExplosion.DoExplosion (pt.Position, pt.Map, pt.thepot.def.specialDisplayRadius, DamageDefOf.Bomb, pt, this.Props.soundExplode);
    }

    public override void PostExposeData() {
        base.PostExposeData();
//        Scribe_Values.LookValue<int>(ref this.numGen, "numGen", null, false); // default value is in ThingDef
//        Log.Message("ExtInstComp_PlantTrap_Generator.postExposeData 3");
//        Scribe_Values.LookValue<bool>(ref this.genthing, "genthing", null , false); // default value is in ThingDef
//        Log.Message("ExtInstComp_PlantTrap_Generator.postExposeData 4");
    }

    public new ExtComp_PlantTrap_Explosive Props
    {
        get
        {
            return (ExtComp_PlantTrap_Explosive)this.propsint;
        }
    }
}
}
