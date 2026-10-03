using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ember.Core.Phase2
{
    [Serializable] public class ReviveOpportunityDiagnostic
    {
        public int healer,target=-1,samples; public float seconds;
        public bool deadTargetExists,healerAlive,hasSkill,equipped,cooldownReady,manaSufficient,distanceValid,losValid,dangerAcceptable,targetRevivable;
        public string reasonNotCast;
    }
    [Serializable] public class SkillEconomyDiagnostic
    {
        public string skill; public int casts; public float effectiveDamage,rawHealing,effectiveHealing,manaCost,cooldownSum,castTime,travelSeconds;
    }
    public sealed partial class TowerSimulation
    {
        [NonSerialized] SkillEconomyDiagnostic activeCast;
        [NonSerialized] int activeCaster=-1;
        SkillEconomyDiagnostic Economy(Agent a,string id)
        {var row=Telemetry(State.GroupOf(a.id));if(row==null)return null;var r=row.skillEconomy.Find(v=>v.skill==id);if(r==null){r=new SkillEconomyDiagnostic{skill=id};row.skillEconomy.Add(r);}return r;}
        void BeginCastEconomy(Agent a,SkillDef skill)
        {if(!DiagnosticsEnabled)return;activeCast=Economy(a,skill.id);activeCaster=a.id;if(activeCast!=null){activeCast.casts++;activeCast.cooldownSum+=skill.cooldown;}}
        void EndCastEconomy(){activeCast=null;activeCaster=-1;}
        void BeginBasicEconomy(Agent a,float cooldown)
        {if(!DiagnosticsEnabled)return;activeCast=Economy(a,"BasicAttack");activeCaster=a.id;if(activeCast!=null){activeCast.casts++;activeCast.cooldownSum+=cooldown;}}
        void RecordMaterialsSpent(Agent a,int amount)
        {if(!DiagnosticsEnabled)return;var u=Telemetry(State.GroupOf(a.id))?.team.Find(v=>v.agent==a.id);if(u!=null)u.materialsSpent+=amount;}
        void RecordRestTime(Agent a,GroupState g,float dt)
        {if(!DiagnosticsEnabled)return;var u=Telemetry(g)?.team.Find(v=>v.agent==a.id);if(u!=null)u.restSeconds+=dt;}
        void RecordSkillTravel(Agent a,string id,float dt)
        {if(DiagnosticsEnabled){var r=Economy(a,id);if(r!=null)r.travelSeconds+=dt;}}
        // Instant casts have no cast timer. Cooldown is not time during which basic attacks are blocked.
        // Regen/DoT effects are recorded separately by recovery/source telemetry, not attributed to an unrelated active cast.
        public void ObserveRevive(Agent a,GroupState g,float dt,bool danger)
        {
            if(!DiagnosticsEnabled||a.profession!=Profession.Healer)return;
            var row=Telemetry(g);if(row==null)return;Agent target=null,expired=null;
            foreach(var id in g.members){var v=State.world.agents[id];if(!v.alive){if(State.revivedAgents.Contains(id))expired=v;else if(target==null)target=v;}}
            if(target==null)target=expired;var s=Catalog.Skill("revive");
            var r=new ReviveOpportunityDiagnostic{healer=a.id,target=target?.id??-1,deadTargetExists=target!=null,healerAlive=a.alive&&!a.escaped,hasSkill=a.unlocked.Contains("revive"),equipped=a.equipped.Contains("revive"),cooldownReady=!a.cooldowns.Exists(c=>c.id=="revive"&&c.left>0),manaSufficient=a.mp>=s.mana,distanceValid=target!=null&&(s.range<=0||Simulation.Distance(a.x,a.z,target.x,target.z)<=s.range),losValid=true,dangerAcceptable=!danger,targetRevivable=target!=null&&!State.revivedAgents.Contains(target.id)};
            // The simulation has no LOS obstruction mechanic; LOS is always valid by that model.
            r.reasonNotCast=!r.deadTargetExists?"NO_TARGET":!r.healerAlive?"HEALER_DEAD":!r.hasSkill?"NOT_UNLOCKED":!r.equipped?"NOT_EQUIPPED":!r.targetRevivable?"EXPIRED":!r.cooldownReady?"COOLDOWN":!r.manaSufficient?"NO_MANA":!r.dangerAcceptable?"DANGER":!r.distanceValid?"OUT_OF_RANGE":a.intent.kind==ActionKind.Skill&&a.intent.skill=="revive"&&a.intent.target==target.id?"ATTEMPT":"PRIORITY";
            var prior=row.reviveOpportunities.Find(v=>v.healer==r.healer&&v.target==r.target&&v.reasonNotCast==r.reasonNotCast&&v.deadTargetExists==r.deadTargetExists&&v.healerAlive==r.healerAlive&&v.hasSkill==r.hasSkill&&v.equipped==r.equipped&&v.cooldownReady==r.cooldownReady&&v.manaSufficient==r.manaSufficient&&v.distanceValid==r.distanceValid&&v.dangerAcceptable==r.dangerAcceptable&&v.targetRevivable==r.targetRevivable);
            if(prior==null){prior=r;row.reviveOpportunities.Add(prior);}prior.samples++;prior.seconds+=dt;
        }
    }
}
