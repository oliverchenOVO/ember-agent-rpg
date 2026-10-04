using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Ember.Core.Phase2
{
    [Serializable] public sealed class PressureCore { public float x,z,hp,maxHp,left; public int stage=2; [NonSerialized] public float activeThisTick; }
    public static class PressureField
    {
        public const float Radius=3, Warning=3, Active=5;
        public static List<PressureCore> Create(BossDefinition d)
        {
            var list=new List<PressureCore>();if(d.ambientPressure<=0)return list;float hp=Mathf.Clamp(60+d.hp*.004f,60,140);
            foreach(var p in new[]{new Vector2(-6,3),new Vector2(6,3),new Vector2(0,-4)})list.Add(new PressureCore{x=p.x,z=p.y,hp=hp,maxHp=hp,left=3+list.Count*6});return list;
        }
        public static float Cycle(BossRuntime b)=>b.elapsed>=240?14:20;
        // Each source owns its warning timer. Overtime changes the NEXT cooldown,
        // never jumps a source into an active pulse without a full warning.
        public static float Advance(PressureCore n,float dt,bool overtime)
        {
            float active=0;if(n.hp<=0)return active;
            while(dt>0){float step=Mathf.Min(dt,Mathf.Max(0,n.left));if(n.stage==1)active+=step;n.left-=step;dt-=step;
                if(n.left<=0){n.stage=(n.stage+1)%3;n.left=n.stage==0?Warning:n.stage==1?Active:overtime?6:12;}}
            return active;
        }
        public static void Tick(BossRuntime b,float dt)
        {foreach(var n in b.pressureCores)n.activeThisTick=b.visible.hp>0?Advance(n,dt,b.elapsed>=240):0;}
        public static float DamageDuringTick(TowerContent data,BossRuntime b,Agent a)
        {float damage=0;foreach(var n in b.pressureCores)if(Simulation.Distance(a.x,a.z,n.x,n.z)<Radius)damage+=n.activeThisTick;return damage*Multiplier(data,b);}
        static float Multiplier(TowerContent data,BossRuntime b)=>data.Boss(b.definition).ambientPressure*(b.recoveryWindow>0?.2f:1)*(b.visible.enraged?1.5f:1);
        public static bool Live(BossRuntime b,int i)=>b.pressureCores!=null&&i>=0&&i<b.pressureCores.Count&&b.pressureCores[i].hp>0&&b.visible.hp>0;
        public static bool Pulsing(BossRuntime b,int i)=>Live(b,i)&&b.pressureCores[i].stage==1;
        public static bool Threat(BossRuntime b,float x,float z,float margin=0)
        {if(b.pressureCores==null)return false;for(int i=0;i<b.pressureCores.Count;i++)if(Live(b,i)&&b.pressureCores[i].stage!=2&&Simulation.Distance(x,z,b.pressureCores[i].x,b.pressureCores[i].z)<Radius+margin)return true;return false;}
        public static float Rate(TowerContent data,BossRuntime b,float x,float z)
        {
            if(b.pressureCores==null)return 0;float rate=0;var d=data.Boss(b.definition);
            for(int i=0;i<b.pressureCores.Count;i++)if(Pulsing(b,i)&&Simulation.Distance(x,z,b.pressureCores[i].x,b.pressureCores[i].z)<Radius)rate+=Multiplier(data,b);
            return rate;
        }
        public static float Forecast(TowerContent data,BossRuntime b,Agent a,float horizon)
        {
            if(b.pressureCores==null||b.visible.hp<=0)return 0;float sum=0;
            foreach(var n in b.pressureCores){if(n.hp<=0||Simulation.Distance(a.x,a.z,n.x,n.z)>=Radius)continue;
                var copy=new PressureCore{hp=n.hp,stage=n.stage,left=n.left};
                for(float t=0;t<horizon;t+=.1f)sum+=Advance(copy,Mathf.Min(.1f,horizon-t),b.elapsed+t>=240)*data.Boss(b.definition).ambientPressure*(b.recoveryWindow>t?.2f:1)*(b.visible.enraged?1.5f:1);}
            return sum;
        }

    }
    public sealed partial class TowerSimulation
    {
        bool PressureDodge(Agent a,GroupState g,float dt,float speed)
        {
            var b=g.boss;if(!PressureField.Threat(b,a.x,a.z,.6f))return false;Vector2 best=new Vector2(a.x,a.z);float score=float.NegativeInfinity;
            for(int x=-9;x<=9;x++)for(int z=-7;z<=7;z++)
            {
                if(PressureField.Threat(b,x,z,.7f)||b.visible.telegraph&&b.cue.Contains(x,z,.8f)||b.hazardLeft>0&&Simulation.Distance(x,z,b.hazardX,b.hazardZ)<3.6f)continue;
                float value=-Simulation.Distance(a.x,a.z,x,z)-Simulation.Distance(x,z,b.x,b.z)*.12f;if(value>score){score=value;best=new Vector2(x,z);}
            }
            a.intent=new Intent{kind=ActionKind.Protect,reason=Loc.Token("pressure.dodge")};Move(a,best.x,best.y,dt,speed);return true;
        }
        bool AttackPressureCore(Agent a,GroupState g,float dt,float speed)
        {
            var b=g.boss;if(b.pressureCores==null)return false;int index=-1;float distance=float.PositiveInfinity;
            for(int i=0;i<b.pressureCores.Count;i++){var n=b.pressureCores[i];float d=Simulation.Distance(a.x,a.z,n.x,n.z);if(n.hp>0&&d<distance){index=i;distance=d;}}
            if(index<0)return false;
            // Critical healing takes precedence over disabling a source.
            if(a.equipped.Any(id=>Catalog.Skill(id).effect=="Heal")&&State.Members(g).Any(v=>v.alive&&v.hp<v.MaxHp*.35f))return false;
            var core=b.pressureCores[index];var weapon=Catalog.Item(a.weapon.id);float range=weapon.weapon=="Bow"||weapon.weapon=="Staff"?9:2.8f;
            // Approach a point outside the pulse, never the center of its danger area.
            var away=new Vector2(a.x-core.x,a.z-core.z).normalized;if(away==Vector2.zero)away=Vector2.down;var stand=new Vector2(core.x,core.z)+away*Mathf.Min(range-.2f,3.8f);
            a.intent=new Intent{kind=ActionKind.Attack,reason=Loc.Token("pressure.disable",index+1)};
            if(distance>range){Move(a,stand.x,stand.y,dt,speed);return true;}
            if(a.attackTimer>0)return true;float stat=weapon.weapon=="Bow"?a.stats.dex:weapon.weapon=="Staff"?(a.profession==Profession.Healer?a.stats.wis:a.stats.intel):a.stats.str;
            float hit=(weapon.power*a.weapon.quality+a.weapon.upgrade*3+stat*.9f)*Simulation.Proficiency(a,Catalog);core.hp=Mathf.Max(0,core.hp-hit);a.attackTimer=Mathf.Max(.65f,1.7f-a.stats.dex*.025f);
            RecordSkillVisual(a,g,"basic",-1,false,"Physical");b.skillPresentation.pressureAttack=true;b.skillPresentation.pressureTarget=index;
            if(core.hp==0){State.world.Say(a.id,Loc.Token("pressure.destroyed",index+1),"boss");State.Memory(a.id).Add(new Knowledge{key="pressure:"+b.definition+":"+index,text=Loc.Token("pressure.memory",index+1),scope=MemoryScope.Run,source=KnowledgeSource.OwnExperience,run=State.world.run,confidence=1,importance=.8f});}
            return true;
        }
    }
}
