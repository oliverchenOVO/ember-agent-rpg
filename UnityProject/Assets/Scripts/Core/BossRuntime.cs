using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ember.Core.Phase2
{
    [Serializable] public class StatusState { public int agent; public string kind,element,ability; public float left,power; }
    [Serializable] public class BossRuntime
    {
        [NonSerialized] public Action<Agent,float,string,DamageSource,string> diagnosticDamage;
        [NonSerialized] public Action<Agent,float> diagnosticDrain;
        void Damage(Action<Agent,float,string> fallback,Agent a,float raw,string text,DamageSource source,string id="")
        {if(diagnosticDamage!=null)diagnosticDamage(a,raw,text,source,id);else fallback(a,raw,text);}
        public string definition,ability=""; public BossState visible=new BossState();
        public int phase,casts,interrupts,adds,target=-1; public float elapsed,phaseTime,phaseStartHp,x,z=1,shield,survivalLeft,hazardLeft,hazardX,hazardZ;
        public bool transitioned,deathResolved; public List<StatusState> statuses=new List<StatusState>();
        [NonSerialized] public bool stabilizePhaseWindow,stabilizeManaPressure;public float recoveryWindow;
        public CombatCue cue=new CombatCue();public float hitFlash,weakFlash,interruptFlash,blockedFlash;public int sequence;
        public List<CombatEffect> effects=new List<CombatEffect>();public List<int> revived=new List<int>();public SkillPresentation skillPresentation=new SkillPresentation();
        public List<SkillPresentation> skillEvents=new List<SkillPresentation>();
        public List<BossPresentationEvent> presentationEvents=new List<BossPresentationEvent>();public int presentationSerial;
        void Present(string kind)
        {
            if(presentationEvents==null)presentationEvents=new List<BossPresentationEvent>();
            presentationEvents.Add(new BossPresentationEvent{serial=++presentationSerial,kind=kind,ability=ability,phase=phase,target=target,x=x,z=z,targetX=visible.targetX,targetZ=visible.targetZ,radius=cue.radius,length=cue.length,innerRadius=cue.innerRadius,angle=cue.angle,shape=cue.shape});
            if(presentationEvents.Count>64)presentationEvents.RemoveAt(0);
        }
        public static BossRuntime Create(BossDefinition def)=>new BossRuntime{definition=def.id,visible=new BossState{hp=def.hp,maxHp=def.hp,timer=2},phaseStartHp=def.hp};
        public float Radius(TowerContent data)=>string.IsNullOrEmpty(ability)?3:data.Ability(ability).radius;
        public void Tick(TowerContent data,List<Agent> members,World events,float dt,Action<Agent,float,string> hurt)
        {
            var def=data.Boss(definition);var b=visible;transitioned=false;elapsed+=dt;phaseTime+=dt;
            hitFlash=Mathf.Max(0,hitFlash-dt);weakFlash=Mathf.Max(0,weakFlash-dt);interruptFlash=Mathf.Max(0,interruptFlash-dt);blockedFlash=Mathf.Max(0,blockedFlash-dt);
            bool rooted=effects.Exists(e=>e.kind=="Root"&&e.agent==-1&&(e.radius<=0||Simulation.Distance(e.x,e.z,x,z)<e.radius));
            if(!rooted&&!b.telegraph&&def.movement=="Rail")x=Mathf.Sin(elapsed*.45f)*5;
            if(!rooted&&!b.telegraph&&def.movement=="Pendulum"){x=Mathf.Sin(elapsed*.9f)*3;z=1+Mathf.Cos(elapsed*.9f)*2;}
            if(phase+1<def.phases.Length)
            {
                var next=def.phases[phase+1];
                if(b.hp/b.maxHp<=next.hpBelow||(next.afterSeconds>0&&elapsed>=next.afterSeconds))
                {phase++;phaseTime=0;if(stabilizePhaseWindow){recoveryWindow=5;shield=0;adds=Mathf.Min(adds,2);}phaseStartHp=b.hp;transitioned=true;Present("Phase");b.telegraph=false;b.timer=1;events.Say(-1,Loc.Token("p2.event.phase",Loc.Token(def.nameKey),Loc.Token(next.nameKey)),"boss");}
            }
            recoveryWindow=Mathf.Max(0,recoveryWindow-dt);var p=def.phases[phase];b.enraged=elapsed>=p.enrageAfter;
            if(p.dpsDeadline>0&&phaseTime>=p.dpsDeadline&&phaseStartHp-b.hp<b.maxHp*p.requiredDamage){b.enraged=true;shield=0;}
            if(def.ambientPressure>0)foreach(var a in members)if(a.alive&&!a.escaped)Damage(hurt,a,def.ambientPressure*dt*(recoveryWindow>0?.2f:1)*(1+Mathf.Max(0,elapsed-p.enrageAfter)/20),Loc.Token("p3.cause.pressure"),DamageSource.Pressure);
            // Enrage escalation prevents indefinitely sustainable recovery-only strategies.
            if(elapsed>240)foreach(var a in members)if(a.alive&&!a.escaped)Damage(hurt,a,a.MaxHp*dt*(elapsed-240)*.015f,Loc.Token("p3.cause.enrage"),DamageSource.Enrage);
            for(int i=statuses.Count-1;i>=0;i--)
            {
                var s=statuses[i];s.left-=dt;var a=members.Find(v=>v.id==s.agent);
                if(a!=null&&a.alive&&s.kind=="Burn")Damage(hurt,a,s.power*dt,Loc.Token("p2.cause.status",Loc.Token("p2.status."+s.kind)),DamageSource.Status,s.ability);
                if(s.left<=0){if(a!=null&&a.alive&&s.kind=="Doom")Damage(hurt,a,a.MaxHp*1.2f,Loc.Token("p2.cause.status",Loc.Token("p2.status.Doom")),DamageSource.Status,s.ability);statuses.RemoveAt(i);}
            }
            if(adds>0)
            {
                var victim=members.Find(a=>a.alive&&!a.escaped);if(victim!=null)Damage(hurt,victim,adds*2*dt,Loc.Token("p2.cause.adds"),DamageSource.Summon);
            }
            if(hazardLeft>0){hazardLeft-=dt;foreach(var a in members)if(a.alive&&!a.escaped&&Simulation.Distance(a.x,a.z,hazardX,hazardZ)<3)Damage(hurt,a,7*dt,Loc.Token("p2.cause.hazard"),DamageSource.Hazard);}
            survivalLeft=Mathf.Max(0,survivalLeft-dt);shield=Mathf.Max(0,shield-dt*.1f);
            if(b.hp<=0)return;
            var living=members.FindAll(a=>a.alive&&!a.escaped);if(living.Count==0)return;
            if(b.telegraph)
            {
                b.windup-=dt;cue.left=b.windup;if(b.windup>0)return;
                var abilityDef=data.Ability(ability);b.telegraph=false;b.hits++;casts++;Present("Impact");
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
            cue=new CombatCue{shape=attack.shape,x=b.targetX,z=b.targetZ,angle=(casts%2==0?0:Mathf.PI*.5f),radius=attack.radius,length=attack.length,innerRadius=attack.innerRadius,left=attack.windup,interruptible=attack.interruptible,element=attack.element};
            if(attack.shape==CueShape.Cross||attack.shape==CueShape.Annulus){cue.x=x;cue.z=z;b.targetX=x;b.targetZ=z;}
            sequence++;Present("Windup");
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
                bool hits=cue.Contains(a.x,a.z);
                // Old saves/fixtures did not contain a cue. Their circle remains authoritative.
                if(cue.radius<=0)hits=Simulation.Distance(a.x,a.z,b.targetX,b.targetZ)<attack.radius;
                if(attack.mechanic==Mechanic.Drain)hits=a.id==target;
                if(!hits)continue;
                Damage(hurt,a,attack.damage*multiplier,Loc.Token("p2.cause.ability",Loc.Token(attack.nameKey)),DamageSource.Ability,ability);
                if(attack.push>0){var direction=new Vector2(a.x-x,a.z-z).normalized;if(direction==Vector2.zero)direction=Vector2.right;a.x=Mathf.Clamp(a.x+direction.x*attack.push,-9,9);a.z=Mathf.Clamp(a.z+direction.y*attack.push,-7.5f,7.5f);}
                float mpBefore=a.mp;a.mp=Mathf.Max(0,a.mp-Mathf.Max(0,attack.resourceDrain-(stabilizeManaPressure&&attack.id=="extract"?6:0)));
                if(attack.mechanic==Mechanic.Drain){a.mp=Mathf.Max(0,a.mp-10);b.hp=Mathf.Min(b.maxHp,b.hp+8);}
                diagnosticDrain?.Invoke(a,mpBefore-a.mp);
                if(!string.IsNullOrEmpty(attack.status)&&a.alive)statuses.Add(new StatusState{agent=a.id,kind=attack.status,ability=ability,element=attack.element,left=attack.duration,power=2});
            }
            if(statuses.Count>32)statuses.RemoveRange(0,statuses.Count-32);
        }
        public float Hit(TowerContent data,Agent attacker,float amount,string element,bool canInterrupt)
        {
            var def=data.Boss(definition);var p=def.phases[phase];
            if(canInterrupt&&visible.telegraph)
            {
                var a=data.Ability(ability);
                if(a.interruptible&&visible.windup<=a.interruptWindow){visible.telegraph=false;visible.timer=a.interval;interrupts++;interruptFlash=.65f;Present("Interrupt");}
            }
            // Summons have independent pressure and must be removed before damaging their owner.
            if(adds>0){if(amount>0)adds--;return 0;}
            if(survivalLeft>0){blockedFlash=.4f;return 0;}
            amount=amount<1?Mathf.Max(0,amount)*100/(100+Mathf.Max(0,def.armor)*8)*(1-Mathf.Clamp(shield,0,.8f)):Simulation.Damage(amount,def.armor,shield);
            if(element==def.weaknessElement){amount*=1+p.weakness;weakFlash=.35f;}
            if(element==def.resistElement)amount*=1-p.resistance;
            float dealt=Mathf.Min(visible.hp,amount);visible.hp-=dealt;attacker.damage+=dealt;hitFlash=.2f;return dealt;
        }
        public void ResolveDeath(TowerContent data,List<Agent> members,Action<Agent,float,string> hurt)
        {
            if(deathResolved)return;deathResolved=true;visible.telegraph=false;
            if(data.Boss(definition).deathMechanic=="FinalPulse")foreach(var a in members)if(a.alive&&Simulation.Distance(a.x,a.z,x,z)<2.5f)Damage(hurt,a,18,Loc.Token("p2.cause.final_pulse"),DamageSource.FinalPulse);
        }
        public float MovementMultiplier(int agent)=>statuses.Exists(s=>s.agent==agent&&s.kind=="Stun")?.2f:statuses.Exists(s=>s.agent==agent&&s.kind=="Slow")?.55f:1;
    }
}
