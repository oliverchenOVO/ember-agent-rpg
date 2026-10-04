using UnityEngine;

namespace Ember.Core.Phase2
{
    public sealed partial class TowerSimulation
    {
        float HealPower(Agent a,SkillDef s)=>TeamBalance.Power(a,s);
        float HealTravel(Agent a,Agent target,SkillDef s)=>Mathf.Max(0,Simulation.Distance(a.x,a.z,target.x,target.z)-s.range)/(Simulation.MoveSpeed*Mathf.Max(.1f,State.GroupOf(a.id).boss.MovementMultiplier(a.id)));
        float IncomingDamage(GroupState g,Agent target,float horizon)
        {
            if(horizon<=0)return 0;var b=g.boss;var phase=Data.Boss(b.definition).phases[b.phase];
            float incoming=PressureField.Forecast(Data,b,target,horizon),burst=0;
            if(b.visible.telegraph&&b.visible.windup<=horizon){var attack=Data.Ability(b.ability);bool hit=attack.mechanic==Mechanic.Drain?b.target==target.id:b.cue.radius>0?b.cue.Contains(target.x,target.z):Simulation.Distance(target.x,target.z,b.visible.targetX,b.visible.targetZ)<attack.radius;if(hit)burst+=attack.damage*(b.visible.enraged?1.2f:1);}
            foreach(var status in b.statuses)if(status.agent==target.id){if(status.kind=="Burn")incoming+=status.power*Mathf.Min(horizon,status.left);if(status.kind=="Doom"&&status.left<=horizon)burst+=target.MaxHp*1.2f;}
            if(b.hazardLeft>0&&Simulation.Distance(target.x,target.z,b.hazardX,b.hazardZ)<3)incoming+=7*Mathf.Min(horizon,b.hazardLeft);
            foreach(var id in g.members){var v=State.world.agents[id];if(v.alive&&!v.escaped){if(id==target.id)incoming+=b.adds*2*horizon;break;}}
            incoming=incoming*100/(100+Mathf.Max(0,target.armor)*8)*(1-Mathf.Clamp(target.guard,0,.8f));if(burst>0)incoming+=Simulation.Damage(burst,target.armor,target.guard);
            var shield=Effect(b,target.id,"Shield");return Mathf.Max(0,incoming-(shield?.power??0));
        }
        float PredictedMissing(Agent a,GroupState g,Agent target,SkillDef s)
        {float travel=Mathf.Clamp(HealTravel(a,target,s),0,1.5f),regen=0;foreach(var effect in g.boss.effects)if(effect.agent==target.id&&effect.kind=="Regen")regen+=effect.power*Mathf.Min(travel,effect.left);return Mathf.Clamp(target.MaxHp-target.hp+IncomingDamage(g,target,travel)-regen-CommittedHealing(a,g,target),0,target.MaxHp);}
        bool HealingUrgent(GroupState g,Agent target)=>target.hp<target.MaxHp&&(target.hp-IncomingDamage(g,target,.5f))<target.MaxHp*.35f;
        Agent ChooseHealingTarget(Agent a,GroupState g,SkillDef s)
        {
            Agent best=null;float bestUtility=float.NegativeInfinity,power=Mathf.Max(1,HealPower(a,s));
            foreach(var id in g.members)
            {
                var v=State.world.agents[id];if(!v.alive||v.escaped)continue;
                float missing=PredictedMissing(a,g,v,s),effective=Mathf.Min(power,missing),overheal=(power-effective)/power;
                float urgency=HealingUrgent(g,v)?2:0,utility=effective/power+missing/v.MaxHp+urgency-overheal-HealTravel(a,v,s)*.15f;
                if(s.id=="cleanse"&&g.boss.statuses.Exists(e=>e.agent==id))utility+=3;
                if(utility>bestUtility){bestUtility=utility;best=v;}
            }
            return best;
        }
        bool PredictedHealUseful(Agent a,GroupState g,SkillDef s,Agent target)
        {
            if(target==null)return false;
            if(s.id=="revive"&&!target.alive)return !State.revivedAgents.Contains(target.id);
            if(s.id=="cleanse"&&g.boss.statuses.Exists(e=>e.agent==target.id))return true;
            float power=HealPower(a,s);
            if(s.id=="groupheal")
            {
                float missing=0,raw=0;int needy=0;bool urgent=false;foreach(var id in g.members){var v=State.world.agents[id];if(!v.alive||v.escaped)continue;raw+=power*.65f;float deficit=PredictedMissing(a,g,v,s);missing+=Mathf.Min(power*.65f,deficit);if(deficit>=power*.65f*.25f)needy++;urgent|=HealingUrgent(g,v);}
                return needy>=2&&(urgent||missing>=raw*.55f);
            }
            return HealingUrgent(g,target)||PredictedMissing(a,g,target,s)>=power*.55f;
        }
    }
}
