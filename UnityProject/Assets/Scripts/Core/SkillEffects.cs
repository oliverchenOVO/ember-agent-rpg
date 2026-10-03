using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ember.Core.Phase2
{
    [Serializable] public class CombatEffect { public int agent,source;public string kind,element;public float left,power,x,z,radius; }
    [Serializable] public class SkillPresentation { public int agent,target,serial;public string skill,origin,element;public float x,z;public bool infused; }
    public sealed partial class TowerSimulation
    {
        public bool CanUseSkill(Agent a,string id,bool infused)
        {
            if(!infused)return Simulation.CanCast(a,Catalog,id);
            var s=Catalog.Skill(id);return s!=null&&a.alive&&!a.escaped&&a.weapon.infusion==id&&a.mp>=s.mana&&!a.cooldowns.Exists(cd=>cd.id==id&&cd.left>0);
        }
        CombatEffect Effect(BossRuntime b,int agent,string kind)=>b.effects.Find(e=>e.agent==agent&&e.kind==kind);
        void ApplyEffect(BossRuntime b,int agent,int source,string kind,float seconds,float power,string element="")
        {
            var e=Effect(b,agent,kind);if(e==null){e=new CombatEffect{agent=agent,source=source,kind=kind};b.effects.Add(e);}if(power>=e.power)e.source=source;e.left=Mathf.Max(e.left,seconds);e.power=Mathf.Max(e.power,power);e.element=element;if(kind=="Zone"||kind=="Root"){e.x=b.x;e.z=b.z;e.radius=3;}
        }
        void TickEffects(GroupState g,float dt)
        {
            var b=g.boss;
            for(int i=b.effects.Count-1;i>=0;i--)
            {
                var e=b.effects[i];e.left-=dt;
                var source=State.world.agents[e.source];
                if(e.agent==-1&&(e.kind=="Poison"||e.kind=="Burn"||e.kind=="Zone"||e.kind=="Summon")&&b.visible.hp>0&&(e.kind!="Zone"||e.radius<=0||Simulation.Distance(e.x,e.z,b.x,b.z)<e.radius))Deal(source,g,e.power*dt,e.element,false);
                if(e.agent>=0&&e.kind=="Regen"){var a=State.world.agents[e.agent];if(a.alive&&!a.escaped)Heal(source,a,e.power*dt,"Regen");}
                if(e.left<=0)b.effects.RemoveAt(i);
            }
        }
        float Deal(Agent a,GroupState g,float power,string element,bool interrupt)
        {
            if(Effect(g.boss,-1,"Vulnerable")!=null)power*=1.25f;
            float dealt=g.boss.Hit(Data,a,power,element,interrupt);Measure(a,"damage",dealt);if(activeCaster==a.id&&activeCast!=null)activeCast.effectiveDamage+=dealt;return dealt;
        }
        void Heal(Agent source,Agent target,float raw,string recovery="Heal")
        {
            float amount=Mathf.Max(0,Mathf.Min(target.MaxHp-target.hp,raw));RecordRecovery(source,target,recovery,raw,amount);target.hp+=amount;source.healing+=amount;Measure(source,"heal",amount);if(source!=target&&amount>0)Relate(target,source,RelationshipEventKind.Healed,Mathf.Min(1,amount/40));
        }
        bool SpecialSkill(Agent a,GroupState g,SkillDef s,Agent ally)
        {
            var b=g.boss;float p=CodexText.Power(a,s);
            switch(s.id)
            {
                case "revive":
                    if(ally!=null&&!ally.alive&&!State.revivedAgents.Contains(ally.id)){RecordRecovery(a,ally,"Revive",ally.MaxHp*.35f,ally.MaxHp*.35f);ally.alive=true;ally.hp=ally.MaxHp*.35f;ally.escaped=false;State.revivedAgents.Add(ally.id);State.Plan(ally.id).nextDecision=0;Relate(ally,a,RelationshipEventKind.Rescued);}
                    else if(ally!=null)Heal(a,ally,p,s.id);return true;
                case "cleanse":
                    if(ally==null)ally=a;b.statuses.RemoveAll(e=>e.agent==ally.id);Heal(a,ally,p,s.id);return true;
                case "ward":foreach(var id in g.members)if(State.world.agents[id].alive)ApplyEffect(b,id,a.id,"Shield",8,20+a.stats.wis*2);return true;
                case "shield":ApplyEffect(b,a.id,a.id,"Shield",8,35+a.stats.intel*2);return true;
                case "guard":a.guard=.65f;ApplyEffect(b,a.id,a.id,"Shield",5,15+a.stats.vit);return true;
                case "counter":a.guard=.5f;ApplyEffect(b,a.id,a.id,"Counter",5,p);Deal(a,g,p*.4f,"Physical",true);return true;
                case "trap":ApplyEffect(b,-1,a.id,"Root",5,1);ApplyEffect(b,-1,a.id,"Zone",5,5,"Physical");b.x=Mathf.Clamp(b.x,-5,5);b.z=Mathf.Clamp(b.z,-4,4);if(b.visible.telegraph&&Data.Ability(b.ability).interruptible){b.visible.windup=Mathf.Min(.5f,b.visible.windup);b.Hit(Data,a,0,"Physical",true);}return true;
                case "rage":a.enchant=10;ApplyEffect(b,a.id,a.id,"Regen",6,3);return true;
                case "enchant":a.enchant=12;ApplyEffect(b,a.id,a.id,"Enchant",12,12,"Fire");return true;
                case "taunt":b.target=a.id;b.visible.targetX=a.x;b.visible.targetZ=a.z;if(b.cue.shape==CueShape.Circle){b.cue.x=a.x;b.cue.z=a.z;}a.guard=.5f;return true;
                case "poison":Deal(a,g,p*.6f,"Physical",false);ApplyEffect(b,-1,a.id,"Poison",8,6,"Physical");return true;
                case "frost":Deal(a,g,p,"Ice",true);ApplyEffect(b,-1,a.id,"Root",3,1);b.visible.timer+=.8f;return true;
                case "wave":case "pierce":Deal(a,g,p,"Physical",true);ApplyEffect(b,-1,a.id,"Vulnerable",5,.25f);return true;
                case "rain":case "lightning":b.adds=Mathf.Max(0,b.adds-3);Deal(a,g,p,s.id=="lightning"?"Arcane":"Physical",true);return true;
                case "meteor":Deal(a,g,p,"Fire",true);ApplyEffect(b,-1,a.id,"Zone",4,8,"Fire");return true;
                case "fireball":Deal(a,g,p,"Fire",true);ApplyEffect(b,-1,a.id,"Burn",4,4,"Fire");return true;
                case "holy":Deal(a,g,p,"Light",true);Agent lowest=null;foreach(var id in g.members){var other=State.world.agents[id];if(other.alive&&(lowest==null||other.hp/other.MaxHp<lowest.hp/lowest.MaxHp))lowest=other;}if(lowest!=null){float raw=5+a.stats.wis*.3f,amount=Mathf.Min(raw,lowest.MaxHp-lowest.hp);float cost=Rules.holyRecoveryCost&&amount>0?2*amount/raw:0;if(a.mp>=cost){a.mp-=cost;RecordMana(a,cost);Heal(a,lowest,raw,"HolyAttack");}}return true;
                case "heal":if(ally!=null){Heal(a,ally,p);ApplyEffect(b,ally.id,a.id,"Regen",4,3);}return true;
                case "groupheal":foreach(var id in g.members){var other=State.world.agents[id];if(other.alive){Heal(a,other,p*.65f,"GroupHeal");ApplyEffect(b,id,a.id,"Shield",3,12);}}return true;
                case "shot":Deal(a,g,p*(Simulation.Distance(a.x,a.z,b.x,b.z)>5?1.15f:1),"Physical",false);ApplyEffect(b,a.id,a.id,"Haste",2,.3f);return true;
                case "snipe":Deal(a,g,p*1.2f,"Physical",false);a.attackTimer=1.2f;return true;
                case "slash":Deal(a,g,p,"Physical",true);b.x=Mathf.Clamp(b.x+(b.x-a.x)*.08f,-8,8);return true;
                default:return false;
            }
        }
        void SkillFeedback(Agent a,GroupState g,SkillDef s,int target,bool infused)
        {
            string origin=Catalog.Item(a.weapon.id).weapon;
            var b=g.boss;b.skillPresentation=new SkillPresentation{agent=a.id,target=target,serial=b.skillPresentation.serial+1,skill=s.id,infused=infused,origin=origin,element=s.id=="holy"||s.effect=="Heal"?"Light":s.id=="frost"?"Ice":s.id=="fireball"||s.id=="meteor"||s.id=="enchant"||a.enchant>0?"Fire":"Physical",x=a.x,z=a.z};
        }
    }
}
