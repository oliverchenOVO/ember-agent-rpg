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
        public bool TransferItem(Agent owner,Agent recipient,Item item)
        {
            var g=State.GroupOf(owner.id);
            if(owner==recipient||!owner.alive||!recipient.alive||owner.escaped||recipient.escaped||State.GroupOf(recipient.id)!=g||g.phase!=Phase.Rest||!owner.inventory.Contains(item))return false;
            owner.inventory.Remove(item);item.uid=++recipient.uidCounter;Adapter.AddItem(recipient,item);Adapter.ResolveInventory(recipient);
            Relate(recipient,owner,RelationshipEventKind.Conversation,1.2f);State.world.Say(owner.id,Loc.Token("event.gift",recipient.name,Loc.Ref("item",item.id),string.IsNullOrEmpty(item.infusion)?Loc.Token("ui.none"):Loc.Ref("skill",item.infusion)),"social");return true;
        }
        public void EnterRest(GroupState g)
        {
            CloseTelemetry(g,true);
            g.phase=Phase.Rest;g.phaseClock=0;g.boss.visible.telegraph=false;State.floorsCleared++;
            var f=Data.Floor(g.floor);g.restId=f.restPool[State.world.Pick(f.restPool.Length)];g.refugeVersion=1;g.refugeVariant=State.world.Pick(6);
            var layout=Data.Rest(g.restId);g.restSitesRolled=true;g.availableRestSites.Clear();
            if(State.world.Roll()>=layout.emptyChance)
                foreach(var site in layout.sites)if(State.world.Roll()<layout.facilityChance)g.availableRestSites.Add(site.id);
            if(!string.IsNullOrEmpty(f.deathKey))State.world.Say(-1,Loc.Token(f.deathKey),"boss");
            State.world.Say(-1,Loc.Token("p2.event.victory",Loc.Token(Data.Boss(g.boss.definition).nameKey),g.id),"run");
            foreach(var a in State.Members(g).Where(a=>a.alive))
            {
                a.kills++;a.level++;a.skillPoints++;
                for(int j=0;j<3;j++)a.stats.Add(RuleBasedReasoner.SelectAttribute(a,State.Profile(a.id)),1);
                string skill=RuleBasedReasoner.SelectSkill(a,Catalog,State.Profile(a.id));if(skill!=null)Adapter.Unlock(a,skill);
                // Keep only four active skills, choosing a strategy rather than growing slots.
                if(a.unlocked.Count>4){var choice=a.unlocked.Select(id=>Catalog.Skill(id)).OrderByDescending(s=>s.effect=="Heal"?a.personality.empathy*80:s.power+(s.effect=="Guard"?State.Profile(a.id).caution*40:0)).Take(4).Select(s=>s.id).ToList();var attack=a.unlocked.FirstOrDefault(id=>Catalog.Skill(id).effect=="Damage");if(attack!=null&&!choice.Any(id=>Catalog.Skill(id).effect=="Damage")){choice[3]=attack;}if(a.unlocked.Contains("revive")&&a.personality.empathy>.35f&&State.Members(g).Any(v=>!v.alive)&&!choice.Contains("revive")){int replace=choice.FindIndex(id=>id!="holy"&&id!="heal");if(replace>=0)choice[replace]="revive";}a.equipped=choice;}
                foreach(string tableId in f.lootTables)
                {
                    var table=Array.Find(Data.loot,l=>l.id==tableId);a.materials+=table.materials;
                    for(int i=0;i<table.rolls;i++){var item=a.Make(!string.IsNullOrEmpty(table.affix)&&i==0?Catalog.Class(a.profession).weapon:table.items[State.world.Pick(table.items.Length)],!string.IsNullOrEmpty(table.affix)?1.15f:1);item.affix=table.affix??"";if(Catalog.Item(item.id).kind=="Weapon")item.infusion=table.infusion??"";Adapter.AddItem(a,item);g.claimedLoot.Add(a.id+":"+item.uid);Measure(a,"loot",1,item.id+":"+item.affix);}
                }
                while(g.claimedLoot.Count>32)g.claimedLoot.RemoveAt(0);Adapter.ResolveInventory(a);ResolveLoadout(a);
                a.x=RefugeMap.Entry.x;a.z=-3+a.id*2;a.escaped=false;a.readBook=false;a.intent=new Intent();a.taskTimer=0;a.task="";
                var plan=State.Plan(a.id);plan.workLeft=0;plan.discussionLeft=0;plan.knownRooms.Clear();plan.route=new RefugeRoute();plan.site="";plan.visited.Clear();plan.nextDecision=0;
                State.Memory(a.id).Add(new Knowledge{key="victory:"+g.floor,text=Loc.Token("p2.memory.victory",g.floor),scope=MemoryScope.Run,source=KnowledgeSource.OwnExperience,run=State.world.run,confidence=1,importance=.8f});
                foreach(var other in State.Members(g).Where(o=>o.alive&&o!=a))Relate(a,other,RelationshipEventKind.TacticSuccess,.5f);
            }
            if(Rules.combatRecovery&&Rules.roleLoadout)foreach(var member in State.Members(g).Where(v=>v.alive))EquipRoleSkills(member,g);
        }
        void ExecuteRest(Agent a,GroupState g,float dt)
        {
            var rest=Data.Rest(g.restId);float collapse=RefugeMap.Front(rest,g);
            if(g.phaseClock>=RefugeMap.Limit(rest,g)&&a.x<collapse){RecordDeath(a,DamageSource.Collapse,"",a.hp,a.hp);var collapseRow=Telemetry(g);if(collapseRow!=null)collapseRow.collapses++;Die(a,Loc.Token("cause.collapse"));return;}
            var p=State.Plan(a.id);
            if(p.discussionLeft>0){p.discussionLeft=Mathf.Max(0,p.discussionLeft-dt);a.taskTimer=p.discussionLeft;a.task="discussion";a.intent.kind=ActionKind.Rest;if(p.discussionLeft==0){ShareRooms(a,g);a.task="";}return;}
            if(p.workLeft>0)
            {
                p.workLeft=Mathf.Max(0,p.workLeft-dt);a.taskTimer=p.workLeft;
                if(p.workLeft==0){if(p.site.StartsWith("search:"))RevealRoom(a,g,int.Parse(p.site.Substring(7)));else CompleteSite(a,g,Array.Find(SpatialSites(g),s=>s.id==p.site));a.task="";p.visited.Add(p.site);p.nextDecision=0;}
                return;
            }
            if(p.decision.intent=="Exit"||p.decision.intent=="LeaveParty"||p.decision.intent=="Rejoin")
            {a.intent.kind=ActionKind.Exit;Move(a,g.refugeVersion==1?RefugeMap.Exit.x:10,g.refugeVersion==1?RefugeMap.Exit.y:a.z,dt);if(g.refugeVersion==1?Simulation.Distance(a.x,a.z,RefugeMap.Exit.x,RefugeMap.Exit.y)<.7f:a.x>=9.5f){a.escaped=true;State.world.Say(a.id,Loc.Token("event.escape"),"exit");}return;}
            if(p.decision.intent=="Rescue")
            {
                var ally=State.Members(g).Where(o=>o!=a&&o.alive&&!o.escaped).OrderBy(o=>o.hp/o.MaxHp).FirstOrDefault();
                if(ally!=null&&ally.hp<ally.MaxHp*.6f){Move(a,ally.x,ally.z,dt);if(Simulation.Distance(a.x,a.z,ally.x,ally.z)<1&&a.mp>=8&&a.attackTimer==0){RecordMana(a,8);RecordRecovery(a,ally,"RestRescue",12,Mathf.Min(ally.MaxHp-ally.hp,12));a.mp-=8;ally.hp=Mathf.Min(ally.MaxHp,ally.hp+12);a.healing+=12;a.attackTimer=2;Relate(ally,a,g.phaseClock>20?RelationshipEventKind.RiskyRescue:RelationshipEventKind.Rescued);}}
                else {p.decision.intent="Exit";p.nextDecision=State.world.clock+1.5f;}return;
            }
            string site=p.decision.proposedAction;
            if(p.decision.intent=="Recover"&&KnownSite(a,g,"clinic")&&g.boss.statuses.Any(s=>s.agent==a.id))site="clinic";
            if(p.decision.intent=="Support")site="church";
            if(KnownSite(a,g,"clinic")&&Rules.clinicSupplies&&g.floor<=10&&p.decision.intent=="Recover"&&a.materials>=1+Mathf.Clamp(Rules.clinicKitCost,1,3)&&!p.visited.Contains("clinic")&&(a.inventory.FindAll(i=>i.id=="hp").Count<Mathf.Clamp(Rules.clinicStockLimit,1,2)||a.inventory.FindAll(i=>i.id=="mp").Count<Mathf.Clamp(Rules.clinicStockLimit,1,2)))site="clinic";
            var station=Array.Find(RestOptions(a,g),s=>s.id==site);
            if(station==null||(site=="bed"?p.visited.Count(v=>v==site)>=2:p.visited.Contains(site))||a.materials<station.materialCost){p.decision.intent="Exit";return;}
            a.intent.kind=station.effect=="Craft"?ActionKind.Craft:station.effect=="Read"||station.effect=="Intel"?ActionKind.Read:station.effect=="Heal"||station.effect=="Cleanse"?ActionKind.Rest:ActionKind.Explore;
            Move(a,station.x,station.z,dt);
            if(Simulation.Distance(a.x,a.z,station.x,station.z)<1)
            {
                // A task is never free: reserve cost and duration, including its travel and escape margin in the brain context.
                a.materials-=station.materialCost;RecordMaterialsSpent(a,station.materialCost);p.site=site;p.workLeft=station.seconds;a.taskTimer=p.workLeft;a.task=site;
                State.world.Say(a.id,Loc.Token("p2.event.work",Loc.Token(station.nameKey)),"rest");
            }
        }
        public void CompleteSite(Agent a,GroupState g,RestSite site)
        {
            if(site==null)return;Measure(a,"rest",1,site.id);if(site.effect=="Craft")Measure(a,"craft");
            switch(site.effect)
            {
                case "Heal":RecordRecovery(a,a,"Rest:"+site.id,site.reward+a.MaxHp*.35f,Mathf.Min(a.MaxHp-a.hp,site.reward+a.MaxHp*.35f));a.hp=Mathf.Min(a.MaxHp,a.hp+site.reward+a.MaxHp*.35f);a.mp=Mathf.Min(a.MaxMp,a.mp+35);break;
                case "Cleanse":RecordRecovery(a,a,"Rest:"+site.id,site.reward+a.MaxHp*.3f,Mathf.Min(a.MaxHp-a.hp,site.reward+a.MaxHp*.3f));g.boss.statuses.RemoveAll(s=>s.agent==a.id);a.hp=Mathf.Min(a.MaxHp,a.hp+site.reward+a.MaxHp*.3f);break;
                case "Bless":a.guard=Mathf.Max(a.guard,site.reward);break;
                case "Craft":
                    var recipe=Catalog.Recipe("forge"+(int)a.profession);var item=a.Make(recipe.item,recipe.quality+.25f+a.stats.wis*.01f,a.equipped.FirstOrDefault()??"");item.affix="Custom";
                    Adapter.AddItem(a,a.weapon);a.weapon=item;State.world.Say(a.id,Loc.Token("event.craft_done",Loc.Ref("item",item.id),Loc.Ref("skill",item.infusion),item.quality.ToString("F2")),"craft");break;
                case "Intel":
                    int next=Math.Min(25,g.floor+1);State.Memory(a.id).Add(new Knowledge{key="intel:"+next,text=Loc.Token("p2.memory.intel",next),scope=MemoryScope.Run,source=KnowledgeSource.OwnExperience,run=State.world.run,confidence=site.reward,importance=.8f});
                    foreach(var ally in State.Members(g).Where(v=>v!=a&&v.alive))State.Memory(ally.id).Add(new Knowledge{key="intel:"+next,text=Loc.Token("p2.memory.told",a.name,next),scope=MemoryScope.Run,source=KnowledgeSource.ToldByAgent,informant=a.id,run=State.world.run,confidence=site.reward*.75f,importance=.65f});break;
                case "Read":
                    ReadLegacy(a,g);break;
                case "Loot":Adapter.AddItem(a,a.Make(Catalog.items[State.world.Pick(Catalog.items.Length)].id));Adapter.ResolveInventory(a);break;
                case "Materials":a.materials+=(int)site.reward;break;
            }
            if(site.risk>0&&State.world.Roll()<site.risk)HurtDiagnostic(a,14+g.floor*.3f,Loc.Token("p2.cause.resource"),DamageSource.RestRisk);
            StockClinic(a,g,site);ResolveLoadout(a);
            State.world.Say(a.id,Loc.Token("p2.event.work_done",Loc.Token(site.nameKey)),"rest");
        }
        void UpdateDepartures(GroupState g)
        {
            var alive=State.Members(g).Where(a=>a.alive).ToList();if(alive.Count==0){g.terminal=true;g.phase=Phase.Ended;return;}
            if(alive.All(a=>a.escaped)){g.travelLeft=1.5f;return;}
            // Those who chose to leave can advance while other Agents continue spending the refuge budget.
            var departed=alive.Where(a=>a.escaped).Select(a=>a.id).ToList();
            if(departed.Count>0&&departed.Count<g.members.Count&&g.phaseClock>=RefugeMap.Limit(Data.Rest(g.restId),g)-3)
            {var branch=Split(g.id,departed);if(branch!=null)branch.travelLeft=1.5f;}
        }
        void Move(Agent a,float x,float z,float dt,float multiplier=1)
        {
            var group=State.GroupOf(a.id);if(group!=null&&group.phase==Phase.Rest&&group.refugeVersion==1){MoveInRefuge(a,group,new Vector2(x,z),dt*multiplier);return;}
            var p=Vector2.MoveTowards(new Vector2(a.x,a.z),new Vector2(x,z),Simulation.MoveSpeed*dt*multiplier);a.x=Mathf.Clamp(p.x,-9.5f,10);a.z=Mathf.Clamp(p.y,-8,8);
        }
    }
}
