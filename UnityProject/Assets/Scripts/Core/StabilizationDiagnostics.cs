using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ember.Core.Phase2
{
    public enum DamageSource { Other, Pressure, Enrage, Status, Summon, Hazard, Ability, FinalPulse, Collapse, RestRisk }
    public enum DeathCause { BURST, ATTRITION, MANA_COLLAPSE, HAZARD, FAILED_DODGE, FAILED_INTERRUPT, NO_RECOVERY, AI_PRIORITY, COLLAPSE, OTHER }
    [Serializable] public class SourceAmount { public string id; public float raw,effective,overheal,mana; public int count; }
    [Serializable] public class PhaseDiagnostic
    {
        public int phase,deaths,potions; public float duration,damageTaken,mpSum,agentSeconds,healReadySeconds,defenseReadySeconds,potionReadySeconds,skillReadySeconds;
        public List<SourceAmount> damageSources=new List<SourceAmount>();
    }
    [Serializable] public class DeathDiagnostic
    {
        public int agent,phase,groupSize; public float clock,hpBefore,mp,mpFraction,rawHit,distanceToDanger; public DamageSource source; public DeathCause cause;
        public string ability,hazard,intent; public bool healingAvailable,potionAvailable,defenseAvailable,interruptAvailable,rescueAttempted;
    }
    public sealed partial class TowerSimulation
    {
        // Diagnostics are opt-in and never consume random numbers or alter decisions.
        public bool DiagnosticsEnabled;
        public const string SimulationVersion="phase3.1-diagnostics-1", BalanceVersion="phase3.1-candidate-1";
        static SourceAmount Source(List<SourceAmount> rows,string id)
        {var r=rows.Find(v=>v.id==id);if(r==null){r=new SourceAmount{id=id};rows.Add(r);}return r;}
        PhaseDiagnostic DiagnosticPhase(GroupState g)
        {
            var row=Telemetry(g);if(row==null)return null;if(row.phases==null)row.phases=new List<PhaseDiagnostic>();
            var p=row.phases.Find(v=>v.phase==g.boss.phase);if(p==null){p=new PhaseDiagnostic{phase=g.boss.phase};row.phases.Add(p);}return p;
        }
        bool Ready(Agent a,string effect)
        {foreach(var id in a.equipped){var s=Catalog.Skill(id);if(s!=null&&(s.effect==effect||effect=="Defense"&&(id=="shield"||id=="ward"||id=="guard"||id=="counter"))&&CanUseSkill(a,id,false))return true;}return false;}
        bool AnySkillReady(Agent a){foreach(var id in a.equipped)if(CanUseSkill(a,id,false))return true;return false;}
        bool HealingReady(GroupState g,Agent target)
        {foreach(var id in g.members){var a=State.world.agents[id];if(a.alive&&!a.escaped&&Ready(a,"Heal"))return true;}return false;}
        void SampleDiagnostics(GroupState g,float dt)
        {
            if(!DiagnosticsEnabled)return;var p=DiagnosticPhase(g);if(p==null)return;p.duration+=dt;
            foreach(var id in g.members){var a=State.world.agents[id];if(!a.alive||a.escaped)continue;p.agentSeconds+=dt;p.mpSum+=a.mp*dt;
                if(HealingReady(g,a))p.healReadySeconds+=dt;if(Ready(a,"Defense"))p.defenseReadySeconds+=dt;
                if(a.inventory.Exists(i=>i.id=="hp"))p.potionReadySeconds+=dt;if(AnySkillReady(a))p.skillReadySeconds+=dt;
                var u=Telemetry(g).team.Find(v=>v.agent==id);if(u!=null){u.mpMinimum=Mathf.Min(u.mpMinimum,a.mp);RecordOpportunity(a,u,"taunt",dt);RecordOpportunity(a,u,"revive",dt);}
            }
        }
        void RecordOpportunity(Agent a,UnitTelemetry u,string skill,float dt)
        {if(u.opportunities==null)u.opportunities=new List<SourceAmount>();var r=Source(u.opportunities,skill);if(a.unlocked.Contains(skill))r.raw+=dt;if(a.equipped.Contains(skill))r.effective+=dt;if(CanUseSkill(a,skill,false))r.mana+=dt;}
        void RecordRecovery(Agent actor,Agent target,string source,float raw,float effective,float mana=0)
        {
            if(!DiagnosticsEnabled)return;var row=Telemetry(State.GroupOf(target.id));if(row==null)return;
            if(row.recovery==null)row.recovery=new List<SourceAmount>();var r=Source(row.recovery,source);r.raw+=raw;r.effective+=effective;r.overheal+=Mathf.Max(0,raw-effective);r.mana+=mana;r.count++;
            var u=row.team.Find(v=>v.agent==actor.id);if(u!=null&&source!="Potion:mp"){u.rawHealing+=raw;u.overheal+=Mathf.Max(0,raw-effective);}
        }
        void RecordMana(Agent a,float amount,bool drained=false)
        {if(!DiagnosticsEnabled)return;var row=Telemetry(State.GroupOf(a.id));var u=row?.team.Find(v=>v.agent==a.id);if(u!=null){if(drained)u.mpDrained+=amount;else u.mpSpent+=amount;}}
        void DiagnosticBossDamage(Agent a,float raw,string text,DamageSource source,string ability)=>HurtDiagnostic(a,raw,text,source,ability);
        void HurtDiagnostic(Agent a,float raw,string text,DamageSource source,string ability="")
        {
            if(!a.alive||a.escaped)return;float hp=a.hp;HurtInternal(a,raw,text,source,ability,hp);
        }
        void RecordDamage(Agent a,float raw,float effective,DamageSource source,string ability,float hpBefore)
        {
            if(!DiagnosticsEnabled)return;var g=State.GroupOf(a.id);var p=DiagnosticPhase(g);if(p==null)return;p.damageTaken+=effective;
            var r=Source(p.damageSources,source+":"+ability);r.raw+=raw;r.effective+=effective;r.count++;
            if(effective>=hpBefore)RecordDeath(a,source,ability,hpBefore,raw);
        }
        void RecordDeath(Agent a,DamageSource source,string ability,float hpBefore,float raw)
        {
            if(!DiagnosticsEnabled)return;var g=State.GroupOf(a.id);var row=Telemetry(g);if(row==null)return;var p=DiagnosticPhase(g);p.deaths++;
            bool heal=HealingReady(g,a),potion=a.inventory.Exists(i=>i.id=="hp");
            // The primary label describes the lethal source; availability is separate evidence, not a causal claim.
            var cause=source==DamageSource.Collapse?DeathCause.COLLAPSE:source==DamageSource.Hazard?DeathCause.HAZARD:source==DamageSource.Ability||source==DamageSource.FinalPulse?DeathCause.BURST:source==DamageSource.Pressure||source==DamageSource.Status||source==DamageSource.Summon||source==DamageSource.Enrage?DeathCause.ATTRITION:DeathCause.OTHER;
            bool rescue=false;int size=0;foreach(var id in g.members){var other=State.world.agents[id];if(other.alive&&!other.escaped)size++;if(id!=a.id&&State.Plan(id).decision.intent=="Rescue")rescue=true;}
            if(row.deathEvents==null)row.deathEvents=new List<DeathDiagnostic>();row.deathEvents.Add(new DeathDiagnostic{agent=a.id,phase=g.boss.phase,groupSize=size,clock=State.world.clock,hpBefore=hpBefore,mp=a.mp,mpFraction=a.mp/a.MaxMp,rawHit=raw,source=source,cause=cause,ability=ability,hazard=g.boss.hazardLeft>0?"ground":"",intent=a.intent.kind.ToString(),healingAvailable=heal,potionAvailable=potion,defenseAvailable=Ready(a,"Defense"),interruptAvailable=g.boss.visible.telegraph&&g.boss.cue.interruptible&&AnySkillReady(a),rescueAttempted=rescue,distanceToDanger=g.boss.cue.SignedDistance(a.x,a.z)});
        }
    }
}
