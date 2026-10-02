using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ember.Core.Phase2
{
    [Serializable] public class StatusState { public int agent; public string kind,element; public float left,power; }
    [Serializable] public class BossRuntime
    {
        public string definition,ability=""; public BossState visible=new BossState();
        public int phase,casts,interrupts,adds,target=-1; public float elapsed,phaseTime,phaseStartHp,x,z=1,shield,survivalLeft,hazardLeft,hazardX,hazardZ;
        public bool transitioned,deathResolved; public List<StatusState> statuses=new List<StatusState>();
        public static BossRuntime Create(BossDefinition def)=>new BossRuntime{definition=def.id,visible=new BossState{hp=def.hp,maxHp=def.hp,timer=2},phaseStartHp=def.hp};
        public float Radius(TowerContent data)=>string.IsNullOrEmpty(ability)?3:data.Ability(ability).radius;
        public void Tick(TowerContent data,List<Agent> members,World events,float dt,Action<Agent,float,string> hurt)
        {
            var def=data.Boss(definition);var b=visible;transitioned=false;elapsed+=dt;phaseTime+=dt;
            if(phase+1<def.phases.Length)
            {
                var next=def.phases[phase+1];
                if(b.hp/b.maxHp<=next.hpBelow||(next.afterSeconds>0&&elapsed>=next.afterSeconds))
                {phase++;phaseTime=0;phaseStartHp=b.hp;transitioned=true;b.telegraph=false;b.timer=1;events.Say(-1,Loc.Token("p2.event.phase",Loc.Token(def.nameKey),Loc.Token(next.nameKey)),"boss");}
            }
            var p=def.phases[phase];b.enraged=elapsed>=p.enrageAfter||phase>0;
            if(p.dpsDeadline>0&&phaseTime>=p.dpsDeadline&&phaseStartHp-b.hp<b.maxHp*p.requiredDamage){b.enraged=true;shield=0;}
            for(int i=statuses.Count-1;i>=0;i--)
            {
                var s=statuses[i];s.left-=dt;var a=members.Find(v=>v.id==s.agent);
                if(a!=null&&a.alive&&s.kind=="Burn")hurt(a,s.power*dt,Loc.Token("p2.cause.status",Loc.Token("p2.status."+s.kind)));
                if(s.left<=0){if(a!=null&&a.alive&&s.kind=="Doom")hurt(a,a.MaxHp*1.2f,Loc.Token("p2.cause.status",Loc.Token("p2.status.Doom")));statuses.RemoveAt(i);}
            }
            if(adds>0)
            {
                var victim=members.Find(a=>a.alive&&!a.escaped);if(victim!=null)hurt(victim,adds*2*dt,Loc.Token("p2.cause.adds"));
            }
            if(hazardLeft>0){hazardLeft-=dt;foreach(var a in members)if(a.alive&&!a.escaped&&Simulation.Distance(a.x,a.z,hazardX,hazardZ)<3)hurt(a,7*dt,Loc.Token("p2.cause.hazard"));}
            survivalLeft=Mathf.Max(0,survivalLeft-dt);shield=Mathf.Max(0,shield-dt*.1f);
            if(b.hp<=0)return;
            var living=members.FindAll(a=>a.alive&&!a.escaped);if(living.Count==0)return;
            if(b.telegraph)
            {
                b.windup-=dt;if(b.windup>0)return;
                var abilityDef=data.Ability(ability);b.telegraph=false;b.hits++;casts++;
                Resolve(abilityDef,living,events,hurt);b.timer=abilityDef.interval*(b.enraged?.8f:1);return;
            }
            b.timer-=dt;if(b.timer>0)return;
            ability=p.abilities[casts%p.abilities.Length];var attack=data.Ability(ability);
            var chosen=living[events.Pick(living.Count)];
            foreach(var a in living)
            {
                if(attack.target==TargetRule.LowestHealth&&a.hp/a.MaxHp<chosen.hp/chosen.MaxHp)chosen=a;
                if(attack.target==TargetRule.Farthest&&Simulation.Distance(a.x,a.z,x,z)>Simulation.Distance(chosen.x,chosen.z,x,z))chosen=a;
                if(attack.target==TargetRule.HighestDamage&&a.damage>chosen.damage)chosen=a;
            }
            target=chosen.id;b.targetX=chosen.x;b.targetZ=chosen.z;b.windup=attack.windup;b.telegraph=true;
            events.Say(-1,Loc.Token("p2.event.telegraph",Loc.Token(def.nameKey),Loc.Token(attack.nameKey)),"boss");
        }
        void Resolve(AbilityDefinition attack,List<Agent> living,World events,Action<Agent,float,string> hurt)
        {
            var b=visible;float multiplier=b.enraged?1.2f:1;
            if(attack.mechanic==Mechanic.Summon){adds=Mathf.Min(6,adds+2);return;}
            if(attack.mechanic==Mechanic.Shield){shield=.55f;return;}
            if(attack.mechanic==Mechanic.Survival){survivalLeft=attack.duration;hazardLeft=attack.duration;hazardX=b.targetX;hazardZ=b.targetZ;return;}
            if(attack.mechanic==Mechanic.Charge){x=b.targetX;z=b.targetZ;}
            if(attack.mechanic==Mechanic.Storm){hazardLeft=attack.duration;hazardX=b.targetX;hazardZ=b.targetZ;}
            foreach(var a in living)
            {
                float dist=Simulation.Distance(a.x,a.z,b.targetX,b.targetZ);
                bool hits=dist<attack.radius;
                if(attack.mechanic==Mechanic.Drain)hits=a.id==target;
                if(!hits)continue;
                hurt(a,attack.damage*multiplier,Loc.Token("p2.cause.ability",Loc.Token(attack.nameKey)));
                if(attack.mechanic==Mechanic.Drain){a.mp=Mathf.Max(0,a.mp-10);b.hp=Mathf.Min(b.maxHp,b.hp+8);}
                if(!string.IsNullOrEmpty(attack.status)&&a.alive)statuses.Add(new StatusState{agent=a.id,kind=attack.status,element=attack.element,left=attack.duration,power=2});
            }
            if(statuses.Count>32)statuses.RemoveRange(0,statuses.Count-32);
        }
        public float Hit(TowerContent data,Agent attacker,float amount,string element,bool canInterrupt)
        {
            var def=data.Boss(definition);var p=def.phases[phase];
            if(canInterrupt&&visible.telegraph)
            {
                var a=data.Ability(ability);
                if(a.interruptible&&visible.windup<=a.interruptWindow){visible.telegraph=false;visible.timer=a.interval;interrupts++;}
            }
            // Summons have independent pressure and must be removed before damaging their owner.
            if(adds>0){adds--;return 0;}
            if(survivalLeft>0)return 0;
            amount=Simulation.Damage(amount,def.armor,shield);
            if(element==def.weaknessElement)amount*=1+p.weakness;
            if(element==def.resistElement)amount*=1-p.resistance;
            float dealt=Mathf.Min(visible.hp,amount);visible.hp-=dealt;attacker.damage+=dealt;return dealt;
        }
        public void ResolveDeath(TowerContent data,List<Agent> members,Action<Agent,float,string> hurt)
        {
            if(deathResolved)return;deathResolved=true;visible.telegraph=false;
            if(data.Boss(definition).deathMechanic=="FinalPulse")foreach(var a in members)if(a.alive&&Simulation.Distance(a.x,a.z,x,z)<2.5f)hurt(a,18,Loc.Token("p2.cause.final_pulse"));
        }
        public float MovementMultiplier(int agent)=>statuses.Exists(s=>s.agent==agent&&s.kind=="Stun")?.2f:statuses.Exists(s=>s.agent==agent&&s.kind=="Slow")?.55f:1;
    }
}
