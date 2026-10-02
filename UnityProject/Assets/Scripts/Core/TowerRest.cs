using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ember.Core.Phase2
{
    public sealed partial class TowerSimulation
    {
        Simulation adapter;
        Simulation Adapter {get{if(adapter==null)adapter=new Simulation(Catalog,1);adapter.Restore(State.world);return adapter;}}
        public void EnterRest(GroupState g)
        {
            g.phase=Phase.Rest;g.phaseClock=0;g.boss.visible.telegraph=false;State.floorsCleared++;
            var f=Data.Floor(g.floor);g.restId=f.restPool[State.world.Pick(f.restPool.Length)];
            State.world.Say(-1,Loc.Token("p2.event.victory",Loc.Token(Data.Boss(g.boss.definition).nameKey),g.id),"run");
            foreach(var a in State.Members(g).Where(a=>a.alive))
            {
                a.kills++;a.level++;a.skillPoints++;
                for(int j=0;j<3;j++)a.stats.Add(RuleBasedReasoner.SelectAttribute(a,State.Profile(a.id)),1);
                string skill=RuleBasedReasoner.SelectSkill(a,Catalog,State.Profile(a.id));if(skill!=null)Adapter.Unlock(a,skill);
                // Keep only four active skills, choosing a strategy rather than growing slots.
                if(a.unlocked.Count>4){var choice=a.unlocked.Select(id=>Catalog.Skill(id)).OrderByDescending(s=>s.effect=="Heal"?a.personality.empathy*80:s.power+(s.effect=="Guard"?State.Profile(a.id).caution*40:0)).Take(4).Select(s=>s.id);a.equipped=choice.ToList();}
                foreach(string tableId in f.lootTables)
                {
                    var table=Array.Find(Data.loot,l=>l.id==tableId);a.materials+=table.materials;
                    for(int i=0;i<table.rolls;i++){var item=a.Make(table.items[State.world.Pick(table.items.Length)]);Adapter.AddItem(a,item);g.claimedLoot.Add(a.id+":"+item.uid);}
                }
                while(g.claimedLoot.Count>32)g.claimedLoot.RemoveAt(0);Adapter.ResolveInventory(a);
                a.x=-8;a.z=-3+a.id*2;a.escaped=false;a.readBook=false;a.intent=new Intent();a.taskTimer=0;a.task="";
                var plan=State.Plan(a.id);plan.workLeft=0;plan.site="";plan.visited.Clear();plan.nextDecision=0;
                State.Memory(a.id).Add(new Knowledge{key="victory:"+g.floor,text=Loc.Token("p2.memory.victory",g.floor),scope=MemoryScope.Run,source=KnowledgeSource.OwnExperience,run=State.world.run,confidence=1,importance=.8f});
                foreach(var other in State.Members(g).Where(o=>o.alive&&o!=a))Relate(a,other,RelationshipEventKind.TacticSuccess,.5f);
            }
        }
        void ExecuteRest(Agent a,GroupState g,float dt)
        {
            var rest=Data.Rest(g.restId);float collapse=-10+Mathf.Max(0,g.phaseClock-rest.collapseAfter)*rest.collapseSpeed;
            if(g.phaseClock>=rest.collapseAfter&&a.x<collapse){Die(a,Loc.Token("cause.collapse"));return;}
            var p=State.Plan(a.id);
            if(p.workLeft>0)
            {
                p.workLeft=Mathf.Max(0,p.workLeft-dt);a.taskTimer=p.workLeft;
                if(p.workLeft==0){CompleteSite(a,g,Array.Find(rest.sites,s=>s.id==p.site));a.task="";p.visited.Add(p.site);p.nextDecision=0;}
                return;
            }
            if(p.decision.intent=="Exit"||p.decision.intent=="LeaveParty"||p.decision.intent=="Rejoin")
            {a.intent.kind=ActionKind.Exit;Move(a,10,a.z,dt);if(a.x>=9.5f){a.escaped=true;State.world.Say(a.id,Loc.Token("event.escape"),"exit");}return;}
            if(p.decision.intent=="Rescue")
            {
                var ally=State.Members(g).Where(o=>o!=a&&o.alive&&!o.escaped).OrderBy(o=>o.hp/o.MaxHp).FirstOrDefault();
                if(ally!=null&&ally.hp<ally.MaxHp*.6f){Move(a,ally.x,ally.z,dt);if(Simulation.Distance(a.x,a.z,ally.x,ally.z)<1&&a.mp>=8&&a.attackTimer==0){a.mp-=8;ally.hp=Mathf.Min(ally.MaxHp,ally.hp+12);a.healing+=12;a.attackTimer=2;Relate(ally,a,g.phaseClock>20?RelationshipEventKind.RiskyRescue:RelationshipEventKind.Rescued);}}
                else {p.decision.intent="Exit";p.nextDecision=State.world.clock+1.5f;}return;
            }
            string site=p.decision.proposedAction;
            if(p.decision.intent=="Recover"&&g.boss.statuses.Any(s=>s.agent==a.id))site="clinic";
            if(p.decision.intent=="Support")site="church";
            var station=Array.Find(rest.sites,s=>s.id==site);
            if(station==null||p.visited.Contains(site)||a.materials<station.materialCost){p.decision.intent="Exit";return;}
            a.intent.kind=station.effect=="Craft"?ActionKind.Craft:station.effect=="Read"||station.effect=="Intel"?ActionKind.Read:station.effect=="Heal"||station.effect=="Cleanse"?ActionKind.Rest:ActionKind.Explore;
            Move(a,station.x+(a.id%2==0?-.35f:.35f),station.z,dt);
            if(Simulation.Distance(a.x,a.z,station.x,station.z)<1)
            {
                // A task is never free: reserve cost and duration, including its travel and escape margin in the brain context.
                a.materials-=station.materialCost;p.site=site;p.workLeft=station.seconds;a.taskTimer=p.workLeft;a.task=site;
                State.world.Say(a.id,Loc.Token("p2.event.work",Loc.Token(station.nameKey)),"rest");
            }
        }
        public void CompleteSite(Agent a,GroupState g,RestSite site)
        {
            if(site==null)return;
            switch(site.effect)
            {
                case "Heal":a.hp=Mathf.Min(a.MaxHp,a.hp+site.reward);a.mp=Mathf.Min(a.MaxMp,a.mp+35);break;
                case "Cleanse":g.boss.statuses.RemoveAll(s=>s.agent==a.id);a.hp=Mathf.Min(a.MaxHp,a.hp+site.reward);break;
                case "Bless":a.guard=Mathf.Max(a.guard,site.reward);break;
                case "Craft":
                    var recipe=Catalog.Recipe("forge"+(int)a.profession);var item=a.Make(recipe.item,recipe.quality+.25f+a.stats.wis*.01f,a.equipped.FirstOrDefault()??"");
                    Adapter.AddItem(a,a.weapon);a.weapon=item;State.world.Say(a.id,Loc.Token("event.craft_done",Loc.Ref("item",item.id),Loc.Ref("skill",item.infusion),item.quality.ToString("F2")),"craft");break;
                case "Intel":
                    int next=Math.Min(25,g.floor+1);State.Memory(a.id).Add(new Knowledge{key="intel:"+next,text=Loc.Token("p2.memory.intel",next),scope=MemoryScope.Run,source=KnowledgeSource.OwnExperience,run=State.world.run,confidence=site.reward,importance=.8f});
                    foreach(var ally in State.Members(g).Where(v=>v!=a&&v.alive))State.Memory(ally.id).Add(new Knowledge{key="intel:"+next,text=Loc.Token("p2.memory.told",a.name,next),scope=MemoryScope.Run,source=KnowledgeSource.ToldByAgent,informant=a.id,run=State.world.run,confidence=site.reward*.75f,importance=.65f});break;
                case "Read":
                    a.readBook=true;var entry=State.world.book.LastOrDefault(e=>e.author==a.id);
                    State.Memory(a.id).Add(new Knowledge{key="book:"+g.floor,text=entry?.text??Loc.Token("event.blank"),scope=MemoryScope.Run,source=KnowledgeSource.BookOfDead,run=State.world.run,confidence=.6f,importance=.7f});break;
                case "Loot":Adapter.AddItem(a,a.Make(Catalog.items[State.world.Pick(Catalog.items.Length)].id));Adapter.ResolveInventory(a);break;
                case "Materials":a.materials+=(int)site.reward;break;
            }
            if(site.risk>0&&State.world.Roll()<site.risk)Hurt(a,14+g.floor*.3f,Loc.Token("p2.cause.resource"));
            State.world.Say(a.id,Loc.Token("p2.event.work_done",Loc.Token(site.nameKey)),"rest");
        }
        void UpdateDepartures(GroupState g)
        {
            var alive=State.Members(g).Where(a=>a.alive).ToList();if(alive.Count==0){g.terminal=true;g.phase=Phase.Ended;return;}
            if(alive.All(a=>a.escaped)){g.travelLeft=1.5f;return;}
            // Those who chose to leave can advance while other Agents continue spending the refuge budget.
            var departed=alive.Where(a=>a.escaped).Select(a=>a.id).ToList();
            if(departed.Count>0&&departed.Count<g.members.Count)
            {var branch=Split(g.id,departed);if(branch!=null)branch.travelLeft=1.5f;}
        }
        void Move(Agent a,float x,float z,float dt,float multiplier=1)
        {
            var p=Vector2.MoveTowards(new Vector2(a.x,a.z),new Vector2(x,z),Simulation.MoveSpeed*dt*multiplier);a.x=Mathf.Clamp(p.x,-9.5f,10);a.z=Mathf.Clamp(p.y,-8,8);
        }
    }
}
