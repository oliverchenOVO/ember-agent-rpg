using System.Linq;
using UnityEngine;

namespace Ember.Core.Phase2
{
    public sealed partial class TowerSimulation
    {
        public void NextFloor(GroupState g)
        {
            if(g.terminal||g.phase!=Phase.Rest||g.boss.visible.hp>0||State.Members(g).Any(a=>a.alive&&!a.escaped))return;
            if(g.floor==25)
            {
                g.completed=true;g.terminal=true;g.phase=Phase.Ended;
                foreach(var a in State.Members(g).Where(a=>a.alive))
                {
                    State.completedAgents.Add(a.id);
                    State.Memory(a.id).Add(new Knowledge{key="tower_clear",text=Loc.Token("p2.memory.clear"),scope=MemoryScope.LongTerm,source=KnowledgeSource.OwnExperience,run=State.world.run,confidence=1,importance=1,emotionalWeight=1});
                }
                State.world.Say(-1,Loc.Token("p2.event.group_clear",g.id),"party");return;
            }
            g.floor++;g.phase=Phase.Battle;g.phaseClock=0;g.travelLeft=0;g.boss=BossRuntime.Create(Data.Boss(Data.Floor(g.floor).bossId));g.claimedLoot.Clear();
            foreach(var a in State.Members(g).Where(a=>a.alive))
            {a.escaped=false;a.x=-6+a.id*3.5f;a.z=-5;a.taskTimer=0;a.task="";a.intent=new Intent();var p=State.Plan(a.id);p.nextDecision=0;p.workLeft=0;p.visited.Clear();}
            State.world.Say(-1,Loc.Token("p2.event.floor",g.id,g.floor,Loc.Token(Data.Floor(g.floor).nameKey)),"floor");
        }
        void ExecuteCombat(Agent a,GroupState g,float dt)
        {
            var b=g.boss;var plan=State.Plan(a.id);float radius=b.Radius(Data);
            bool danger=b.visible.telegraph&&Simulation.Distance(a.x,a.z,b.visible.targetX,b.visible.targetZ)<radius+.6f;
            bool hazard=b.hazardLeft>0&&Simulation.Distance(a.x,a.z,b.hazardX,b.hazardZ)<3.4f;
            float speed=b.MovementMultiplier(a.id);
            if(danger||hazard)
            {
                float tx=danger?b.visible.targetX:b.hazardX,tz=danger?b.visible.targetZ:b.hazardZ;
                // Search reachable safe directions; clamping an outward vector at the arena wall can trap an Agent forever.
                Vector2 best=new Vector2(a.x,a.z);float bestScore=float.NegativeInfinity;
                for(int i=0;i<16;i++)
                {
                    float angle=i*Mathf.PI/8;float x=Mathf.Clamp(tx+Mathf.Cos(angle)*(radius+2),-9,9),z=Mathf.Clamp(tz+Mathf.Sin(angle)*(radius+2),-7.5f,7.5f);
                    float clearance=Simulation.Distance(x,z,tx,tz);float score=(clearance>radius+.7f?100:clearance*5)-Simulation.Distance(a.x,a.z,x,z)-Simulation.Distance(x,z,b.x,b.z)*3;
                    if(b.hazardLeft>0&&Simulation.Distance(x,z,b.hazardX,b.hazardZ)<3.5f)score-=100;
                    if(score>bestScore){bestScore=score;best=new Vector2(x,z);}
                }
                Move(a,best.x,best.y,dt,speed);a.intent.kind=ActionKind.Protect;return;
            }
            // Tactical evaluation is local only; LLM never runs here or receives a Transform.
            a.decisionTimer-=dt;
            if(a.decisionTimer<=0)
            {
                var tacticalWorld=new World{agents=State.Members(g).ToList(),phase=Phase.Battle,boss=b.visible};
                a.intent=tactical.Decide(tacticalWorld,a,Catalog);a.decisionTimer=.6f;
                if(plan.decision.intent=="Support"||plan.decision.intent=="Rescue")
                {
                    var ally=State.Members(g).Where(v=>v.alive&&!v.escaped).OrderBy(v=>v.hp/v.MaxHp).FirstOrDefault();
                    if(ally!=null&&ally.hp<ally.MaxHp*.75f&&Simulation.CanCast(a,Catalog,"heal"))a.intent=new Intent{kind=ActionKind.Skill,skill="heal",target=ally.id,reason=Loc.Token("reason.heal",Loc.Ref("skill","heal"))};
                }
            }
            if(a.intent.kind==ActionKind.GiveUp){a.intent.kind=ActionKind.Attack;}
            if(a.intent.kind==ActionKind.Protect&&plan.decision.intent=="Fight")a.intent.kind=ActionKind.Attack;
            if(a.intent.kind==ActionKind.Protect)
            {
                var ally=State.Members(g).FirstOrDefault(v=>v.id==a.intent.target&&v.alive);
                if(ally!=null){Move(a,ally.x,ally.z-1,dt,speed);a.guard=.3f;if(a.attackTimer==0){ally.guard=.25f;Relate(ally,a,RelationshipEventKind.Rescued,.3f);a.attackTimer=2;}}return;
            }
            if(a.intent.kind==ActionKind.Skill)
            {
                var s=Catalog.Skill(a.intent.skill);var ally=s.effect=="Heal"?State.Members(g).FirstOrDefault(v=>v.id==a.intent.target&&v.alive):null;
                float tx=ally?.x??b.x,tz=ally?.z??b.z;
                if(s.range>0&&Simulation.Distance(a.x,a.z,tx,tz)>s.range)Move(a,tx,tz,dt,speed);
                else Cast(a,g,s.id,a.intent.target,a.weapon.infusion==s.id);
            }
            else
            {
                var weapon=Catalog.Item(a.weapon.id);float range=weapon.weapon=="Bow"||weapon.weapon=="Staff"?9:2.8f;
                if(Simulation.Distance(a.x,a.z,b.x,b.z)>range)Move(a,b.x,b.z,dt,speed);
                else if(a.attackTimer==0)
                {
                    float stat=weapon.weapon=="Bow"?a.stats.dex:weapon.weapon=="Staff"?(a.profession==Profession.Healer?a.stats.wis:a.stats.intel):a.stats.str;
                    b.Hit(Data,a,(weapon.power*a.weapon.quality+a.weapon.upgrade*3+stat*.9f+(a.enchant>0?12:0))*Simulation.Proficiency(a,Catalog),a.enchant>0?"Fire":"Physical",false);
                    a.attackTimer=Mathf.Max(.65f,1.7f-a.stats.dex*.025f);
                }
            }
            string potion=a.hp<a.MaxHp*.35f?"hp":a.mp<a.MaxMp*.2f?"mp":"";var item=a.inventory.Find(i=>i.id==potion);
            if(item!=null){if(potion=="hp")a.hp=Mathf.Min(a.MaxHp,a.hp+Catalog.Item(potion).power);else a.mp=Mathf.Min(a.MaxMp,a.mp+Catalog.Item(potion).power);a.inventory.Remove(item);}
        }
        public bool Cast(Agent a,GroupState g,string id,int target=-1,bool weapon=false)
        {
            if(State.GroupOf(a.id)!=g||g.phase!=Phase.Battle||!Simulation.CanCast(a,Catalog,id,weapon))return false;
            var s=Catalog.Skill(id);var ally=State.Members(g).FirstOrDefault(v=>v.id==target&&v.alive&&!v.escaped);
            if(s.effect=="Heal"&&ally==null)return false;
            float tx=s.effect=="Heal"?ally.x:g.boss.x,tz=s.effect=="Heal"?ally.z:g.boss.z;
            if(s.range>0&&Simulation.Distance(a.x,a.z,tx,tz)>s.range)return false;
            a.mp-=s.mana;var cooldown=a.cooldowns.Find(cd=>cd.id==id);if(cooldown==null){cooldown=new Cooldown{id=id};a.cooldowns.Add(cooldown);}cooldown.left=s.cooldown;
            if(id=="cleanse"){g.boss.statuses.RemoveAll(st=>st.agent==a.id);}
            else if(s.effect=="Heal")
            {
                var targets=id=="groupheal"?State.Members(g).Where(v=>v.alive&&!v.escaped):new[]{ally};
                foreach(var receiver in targets){float amount=Mathf.Min(receiver.MaxHp-receiver.hp,s.power+a.stats.wis*1.4f);receiver.hp+=amount;a.healing+=amount;if(receiver!=a&&amount>0)Relate(receiver,a,RelationshipEventKind.Healed,Mathf.Min(1,amount/40));}
                State.world.Say(a.id,Loc.Token("event.heal",Loc.Ref("skill",id),ally.name,(int)s.power),"heal");
            }
            else if(s.effect=="Guard")a.guard=Mathf.Max(a.guard,s.power);
            else if(s.effect=="Enchant")a.enchant=12;
            else if(s.effect=="Taunt"){g.boss.target=a.id;g.boss.visible.targetX=a.x;g.boss.visible.targetZ=a.z;a.guard=.45f;}
            else
            {
                string element=id=="fireball"||id=="meteor"?"Fire":id=="frost"?"Ice":id=="holy"?"Light":"Physical";
                g.boss.Hit(Data,a,(s.power+(a.profession==Profession.Healer?a.stats.wis:a.stats.intel)*.7f+a.stats.str*.3f)*Simulation.Proficiency(a,Catalog),element,true);
                State.world.Say(a.id,Loc.Token("event.skill",Loc.Ref("skill",id)),"skill");
            }
            return true;
        }
    }
}
